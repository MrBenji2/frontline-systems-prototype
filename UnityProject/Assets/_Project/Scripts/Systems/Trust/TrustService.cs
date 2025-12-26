using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Definitions;
using UnityEngine;

namespace Frontline.Trust
{
    public sealed class TrustService : MonoBehaviour
    {
        public static TrustService Instance { get; private set; }

        private const string SaveFileName = "player_trust_state_v1";

        public PlayerTrustState State { get; private set; } = new();
        public TrustAuditLog AuditLog { get; } = new(TrustAuditLog.DefaultCapacity);

        private readonly Dictionary<string, CertificationTierDefinition> _certDefsById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _ladderIdByCertId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _certIdsByPermission = new(StringComparer.Ordinal);
        private List<RankDefinition> _ranks = new();

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadDefinitions();
            LoadFromDiskOrCreateNew();
        }

        private void OnApplicationQuit()
        {
            SaveToDisk();
        }

        private void LoadDefinitions()
        {
            var reg = DefinitionRegistry.Instance;
            var defs = reg != null ? reg.Definitions : new GameDefinitions();

            _certDefsById.Clear();
            _ladderIdByCertId.Clear();
            _certIdsByPermission.Clear();

            if (defs.certifications != null && defs.certifications.ladders != null)
            {
                foreach (var ladder in defs.certifications.ladders)
                {
                    if (ladder == null || ladder.tiers == null)
                        continue;

                    foreach (var t in ladder.tiers)
                    {
                        if (t == null || string.IsNullOrWhiteSpace(t.certId))
                            continue;

                        _certDefsById[t.certId] = t;
                        _ladderIdByCertId[t.certId] = ladder.ladderId ?? "";

                        if (t.permissions == null)
                            continue;

                        foreach (var p in t.permissions)
                        {
                            if (string.IsNullOrWhiteSpace(p))
                                continue;

                            if (!_certIdsByPermission.TryGetValue(p, out var list))
                            {
                                list = new List<string>();
                                _certIdsByPermission[p] = list;
                            }

                            if (!list.Contains(t.certId))
                                list.Add(t.certId);
                        }
                    }
                }
            }

            _ranks = (defs.ranks != null && defs.ranks.factionRanks != null)
                ? defs.ranks.factionRanks.Where(r => r != null).OrderBy(r => r.minTrust).ToList()
                : new List<RankDefinition>();

            if (_ranks.Count == 0)
            {
                _ranks.Add(new RankDefinition { rankId = "r0", displayName = "Recruit", minTrust = 0 });
            }
        }

        public List<string> GetCertsGrantingPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return new List<string>();
            return _certIdsByPermission.TryGetValue(permission, out var list) ? list : new List<string>();
        }

        public string EvaluateRankId(int trustScore)
        {
            var best = _ranks[0].rankId;
            for (var i = 0; i < _ranks.Count; i++)
            {
                var r = _ranks[i];
                if (trustScore >= r.minTrust)
                    best = r.rankId;
                else
                    break;
            }
            return string.IsNullOrWhiteSpace(best) ? "r0" : best;
        }

        public bool GrantCertification(string certId)
        {
            if (string.IsNullOrWhiteSpace(certId))
                return false;
            if (!_certDefsById.TryGetValue(certId, out var def) || def == null)
                return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!State.certsById.TryGetValue(certId, out var cs) || cs == null)
            {
                cs = new PlayerCertificationState { certId = certId };
                State.certsById[certId] = cs;
            }

            cs.tier = def.tier;
            cs.versionEarned = def.version;
            cs.isExpired = false;
            cs.isActive = true;
            cs.earnedUtc = cs.earnedUtc > 0 ? cs.earnedUtc : now;
            cs.lastUsedUtc = now;
            cs.ladderId = _ladderIdByCertId.TryGetValue(certId, out var ladderId) ? ladderId : cs.ladderId;

            State.rankId = EvaluateRankId(State.trustScore);

            AuditLog.Add(State.playerId, State.faction, "CERT_GRANTED",
                $"{{\"certId\":\"{Escape(certId)}\",\"tier\":{cs.tier},\"version\":{cs.versionEarned}}}");

            SaveToDisk();
            return true;
        }

        public bool RevokeCertification(string certId)
        {
            if (string.IsNullOrWhiteSpace(certId))
                return false;
            if (!State.certsById.TryGetValue(certId, out var cs) || cs == null)
                return false;

            cs.isActive = false;

            AuditLog.Add(State.playerId, State.faction, "CERT_REVOKED", $"{{\"certId\":\"{Escape(certId)}\"}}");
            SaveToDisk();
            return true;
        }

        public bool SetCertificationActive(string certId, bool active)
        {
            if (string.IsNullOrWhiteSpace(certId))
                return false;
            if (!State.certsById.TryGetValue(certId, out var cs) || cs == null)
                return false;

            if (active && cs.isExpired)
                return false;

            cs.isActive = active;
            SaveToDisk();
            return true;
        }

        public void MarkCertificationUsed(string certId)
        {
            if (string.IsNullOrWhiteSpace(certId))
                return;
            if (!State.certsById.TryGetValue(certId, out var cs) || cs == null)
                return;

            cs.lastUsedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void SetFaction(FactionId faction)
        {
            if (State.faction == faction)
                return;

            var prev = State.faction;
            State.faction = faction;

            // Loyalty rule: switching factions resets rank/trust and inactivates certifications (not deleted).
            State.trustScore = 0;
            State.rankId = EvaluateRankId(State.trustScore);

            foreach (var kv in State.certsById)
            {
                if (kv.Value == null)
                    continue;
                kv.Value.isActive = false;
            }

            AuditLog.Add(State.playerId, State.faction, "FACTION_CHANGED",
                $"{{\"from\":\"{prev}\",\"to\":\"{State.faction}\"}}");

            SaveToDisk();
        }

        public void RecomputeExpiration()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var kv in State.certsById)
            {
                var cs = kv.Value;
                if (cs == null || string.IsNullOrWhiteSpace(cs.certId))
                    continue;

                var wasExpired = cs.isExpired;
                var expired = false;

                if (!_certDefsById.TryGetValue(cs.certId, out var def) || def == null)
                {
                    expired = true;
                }
                else
                {
                    if (def.version > cs.versionEarned)
                        expired = true;

                    var daysStr = def.expires != null ? def.expires.days : null;
                    if (!expired && !string.IsNullOrWhiteSpace(daysStr) && int.TryParse(daysStr, out var days) && days > 0)
                    {
                        var ageSec = now - cs.earnedUtc;
                        if (cs.earnedUtc > 0 && ageSec >= days * 86400L)
                            expired = true;
                    }
                }

                cs.isExpired = expired;
                if (expired)
                    cs.isActive = false;

                if (!wasExpired && expired)
                {
                    AuditLog.Add(State.playerId, State.faction, "CERT_EXPIRED",
                        $"{{\"certId\":\"{Escape(cs.certId)}\"}}");
                }
            }

            SaveToDisk();
        }

        public bool DevIncrementDefinitionVersion(string certId)
        {
            if (string.IsNullOrWhiteSpace(certId))
                return false;
            if (!_certDefsById.TryGetValue(certId, out var def) || def == null)
                return false;

            def.version += 1;
            return true;
        }

        private void LoadFromDiskOrCreateNew()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    CreateNewProfile();
                    SaveToDisk();
                    return;
                }

                var json = File.ReadAllText(SavePath);
                var snap = JsonUtility.FromJson<PlayerTrustStateSnapshot>(json);
                if (snap == null || snap.schemaVersion <= 0)
                {
                    CreateNewProfile();
                    SaveToDisk();
                    return;
                }

                State = new PlayerTrustState
                {
                    playerId = string.IsNullOrWhiteSpace(snap.playerId) ? "local" : snap.playerId,
                    faction = ParseFaction(snap.faction),
                    trustScore = snap.trustScore,
                    rankId = string.IsNullOrWhiteSpace(snap.rankId) ? "r0" : snap.rankId,
                };

                State.RebuildIndexFromList(snap.certs);
                State.rankId = EvaluateRankId(State.trustScore);

                RecomputeExpiration();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TrustService: failed to load '{SavePath}': {ex.Message}");
                CreateNewProfile();
                SaveToDisk();
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var snap = new PlayerTrustStateSnapshot
                {
                    schemaVersion = 1,
                    playerId = State.playerId,
                    faction = State.faction.ToString(),
                    trustScore = State.trustScore,
                    rankId = State.rankId,
                    certs = State.certsById.Values.Where(x => x != null && !string.IsNullOrWhiteSpace(x.certId)).ToList(),
                };

                var json = JsonUtility.ToJson(snap, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TrustService: failed to save '{SavePath}': {ex.Message}");
            }
        }

        private void CreateNewProfile()
        {
            State = new PlayerTrustState
            {
                playerId = "local",
                faction = FactionId.Neutral,
                trustScore = 0,
            };

            State.certsById = new Dictionary<string, PlayerCertificationState>(StringComparer.Ordinal);
            State.rankId = EvaluateRankId(State.trustScore);

            // Required default cert on new profile: Infantry I.
            GrantCertification("infantry_1_rifleman");
        }

        private static FactionId ParseFaction(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return FactionId.Neutral;
            if (Enum.TryParse<FactionId>(s, true, out var f))
                return f;
            return FactionId.Neutral;
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        [Serializable]
        private sealed class PlayerTrustStateSnapshot
        {
            public int schemaVersion = 1;
            public string playerId = "local";
            public string faction = "Neutral";
            public int trustScore;
            public string rankId = "r0";
            public List<PlayerCertificationState> certs = new();
        }
    }
}

