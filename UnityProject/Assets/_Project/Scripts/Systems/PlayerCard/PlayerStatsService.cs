using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Trust;
using UnityEngine;

namespace Frontline.PlayerCard
{
    /// <summary>
    /// Central service for tracking and persisting player statistics.
    /// Integrates with TrustService for rank/certification data.
    /// </summary>
    public sealed class PlayerStatsService : MonoBehaviour
    {
        public static PlayerStatsService Instance { get; private set; }

        private const string SaveFileName = "player_stats_v1";

        /// <summary>The current player's stats.</summary>
        public PlayerStats Stats { get; private set; } = new();

        /// <summary>Event fired when any stat changes.</summary>
        public event Action<string> OnStatChanged;

        /// <summary>Event fired when a name change occurs.</summary>
        public event Action<string, string> OnNameChanged;

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        private float _saveTimer;
        private const float AutoSaveInterval = 30f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadFromDiskOrCreateNew();
        }

        private void Update()
        {
            // Track session time
            UpdateSessionTime();

            // Auto-save periodically
            _saveTimer += Time.deltaTime;
            if (_saveTimer >= AutoSaveInterval)
            {
                _saveTimer = 0;
                SaveToDisk();
            }
        }

        private void OnApplicationQuit()
        {
            // Final time update before saving
            UpdateSessionTime();
            SaveToDisk();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                UpdateSessionTime();
                SaveToDisk();
            }
            else
            {
                Stats.sessionStartUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        private void UpdateSessionTime()
        {
            if (Stats.sessionStartUtc <= 0)
                return;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sessionSeconds = now - Stats.sessionStartUtc;
            Stats.timeServedSeconds += sessionSeconds;
            Stats.sessionStartUtc = now;
        }

        #region Stat Recording Methods

        /// <summary>Records a kill.</summary>
        public void RecordKill(bool isPlayerKill = false)
        {
            Stats.kills++;
            if (isPlayerKill)
                Stats.playerKills++;
            else
                Stats.npcKills++;

            OnStatChanged?.Invoke("kills");
        }

        /// <summary>Records a death.</summary>
        public void RecordDeath()
        {
            Stats.deaths++;
            OnStatChanged?.Invoke("deaths");
        }

        /// <summary>Records a revive.</summary>
        public void RecordRevive()
        {
            Stats.revives++;
            OnStatChanged?.Invoke("revives");
        }

        /// <summary>Records healing done.</summary>
        public void RecordHealing(int amount)
        {
            Stats.healingDone += amount;
            OnStatChanged?.Invoke("healingDone");
        }

        /// <summary>Records damage dealt.</summary>
        public void RecordDamageDealt(int amount)
        {
            Stats.damageDealt += amount;
            OnStatChanged?.Invoke("damageDealt");
        }

        /// <summary>Records damage taken.</summary>
        public void RecordDamageTaken(int amount)
        {
            Stats.damageTaken += amount;
            OnStatChanged?.Invoke("damageTaken");
        }

        /// <summary>Records a friendly fire incident.</summary>
        public void RecordFriendlyFire()
        {
            Stats.friendlyFireIncidents++;
            OnStatChanged?.Invoke("friendlyFireIncidents");

            // This may trigger trust loss
            var trustService = TrustService.Instance;
            if (trustService != null)
            {
                trustService.State.trustScore = Mathf.Max(0, trustService.State.trustScore - 2);
                trustService.AuditLog.Add(trustService.State.playerId, trustService.State.faction,
                    "FRIENDLY_FIRE", $"{{\"total\":{Stats.friendlyFireIncidents}}}");
            }
        }

        /// <summary>Records resources gathered.</summary>
        public void RecordResourcesGathered(int amount)
        {
            Stats.resourcesGathered += amount;
            OnStatChanged?.Invoke("resourcesGathered");
        }

        /// <summary>Records resources delivered.</summary>
        public void RecordResourcesDelivered(int amount)
        {
            Stats.resourcesDelivered += amount;
            OnStatChanged?.Invoke("resourcesDelivered");
        }

        /// <summary>Records a supply run completion.</summary>
        public void RecordSupplyRunCompleted()
        {
            Stats.supplyRunsCompleted++;
            OnStatChanged?.Invoke("supplyRunsCompleted");
        }

        /// <summary>Records cargo distance traveled.</summary>
        public void RecordCargoDistance(float meters)
        {
            Stats.cargoDistanceTraveled += meters;
            OnStatChanged?.Invoke("cargoDistanceTraveled");
        }

        /// <summary>Records a structure built.</summary>
        public void RecordStructureBuilt()
        {
            Stats.structuresBuilt++;
            OnStatChanged?.Invoke("structuresBuilt");
        }

        /// <summary>Records a structure repaired.</summary>
        public void RecordStructureRepaired()
        {
            Stats.structuresRepaired++;
            OnStatChanged?.Invoke("structuresRepaired");
        }

        /// <summary>Records a structure demolished.</summary>
        public void RecordStructureDemolished()
        {
            Stats.structuresDemolished++;
            OnStatChanged?.Invoke("structuresDemolished");
        }

        /// <summary>Records a fortification upgraded.</summary>
        public void RecordFortificationUpgraded()
        {
            Stats.fortificationsUpgraded++;
            OnStatChanged?.Invoke("fortificationsUpgraded");
        }

        /// <summary>Records distance driven.</summary>
        public void RecordDistanceDriven(float meters)
        {
            Stats.distanceDriven += meters;
            OnStatChanged?.Invoke("distanceDriven");
        }

        /// <summary>Records a vehicle operated.</summary>
        public void RecordVehicleOperated()
        {
            Stats.vehiclesOperated++;
            OnStatChanged?.Invoke("vehiclesOperated");
        }

        /// <summary>Records a vehicle collision.</summary>
        public void RecordVehicleCollision()
        {
            Stats.vehicleCollisions++;
            OnStatChanged?.Invoke("vehicleCollisions");
        }

        /// <summary>Records a mission completed.</summary>
        public void RecordMissionCompleted(bool isTraining = false)
        {
            Stats.missionsCompleted++;
            if (isTraining)
                Stats.trainingMissionsCompleted++;
            OnStatChanged?.Invoke("missionsCompleted");
        }

        /// <summary>Records a mission abandoned.</summary>
        public void RecordMissionAbandoned()
        {
            Stats.missionsAbandoned++;
            OnStatChanged?.Invoke("missionsAbandoned");
        }

        /// <summary>Records a mission failed.</summary>
        public void RecordMissionFailed()
        {
            Stats.missionsFailed++;
            OnStatChanged?.Invoke("missionsFailed");
        }

        /// <summary>Records war participation.</summary>
        public void RecordWarParticipation(bool won)
        {
            Stats.warsParticipated++;
            if (won)
                Stats.warsWon++;
            OnStatChanged?.Invoke("warsParticipated");
        }

        /// <summary>Records an order issued.</summary>
        public void RecordOrderIssued()
        {
            Stats.ordersIssued++;
            OnStatChanged?.Invoke("ordersIssued");
        }

        /// <summary>Records squad members commanded.</summary>
        public void RecordSquadMembersCommanded(int count)
        {
            Stats.squadMembersCommanded += count;
            if (count > Stats.largestDivisionSize)
                Stats.largestDivisionSize = count;
            OnStatChanged?.Invoke("squadMembersCommanded");
        }

        /// <summary>Records a commendation received.</summary>
        public void RecordCommendationReceived()
        {
            Stats.commendationsReceived++;
            OnStatChanged?.Invoke("commendationsReceived");

            // Commendations increase trust
            var trustService = TrustService.Instance;
            if (trustService != null)
            {
                trustService.State.trustScore += 1;
                trustService.State.rankId = trustService.EvaluateRankId(trustService.State.trustScore);
            }
        }

        /// <summary>Records a commendation given.</summary>
        public void RecordCommendationGiven()
        {
            Stats.commendationsGiven++;
            OnStatChanged?.Invoke("commendationsGiven");
        }

        /// <summary>Records imprisonment.</summary>
        public void RecordImprisonment()
        {
            Stats.timesImprisoned++;
            OnStatChanged?.Invoke("timesImprisoned");
        }

        /// <summary>Records prison time served.</summary>
        public void RecordPrisonTimeServed(float hours)
        {
            Stats.prisonTimeServedHours += hours;
            OnStatChanged?.Invoke("prisonTimeServedHours");
        }

        /// <summary>Records a certification revocation.</summary>
        public void RecordCertificationRevoked()
        {
            Stats.certificationsRevoked++;
            OnStatChanged?.Invoke("certificationsRevoked");
        }

        /// <summary>Records a griefing report.</summary>
        public void RecordGriefingReport(bool upheld)
        {
            Stats.griefingReportsReceived++;
            if (upheld)
                Stats.griefingReportsUpheld++;
            OnStatChanged?.Invoke("griefingReportsReceived");
        }

        /// <summary>Awards a medal.</summary>
        public void AwardMedal(string medalId)
        {
            if (string.IsNullOrWhiteSpace(medalId))
                return;

            if (!Stats.medals.Contains(medalId))
            {
                Stats.medals.Add(medalId);
                OnStatChanged?.Invoke("medals");
                Debug.Log($"PlayerStatsService: Awarded medal '{medalId}'");
            }
        }

        /// <summary>Unlocks an achievement.</summary>
        public void UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
                return;

            if (!Stats.achievements.Contains(achievementId))
            {
                Stats.achievements.Add(achievementId);
                OnStatChanged?.Invoke("achievements");
                Debug.Log($"PlayerStatsService: Unlocked achievement '{achievementId}'");
            }
        }

        #endregion

        #region Name Management

        /// <summary>Changes the player's display name.</summary>
        public bool ChangeName(string newName, string reason = "", bool adminForced = false)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return false;

            newName = newName.Trim();
            if (newName.Length < 2 || newName.Length > 24)
                return false;

            var oldName = Stats.displayName;
            if (oldName == newName)
                return false;

            // Record in history
            Stats.nameHistory.Add(new NameChangeRecord
            {
                previousName = oldName,
                newName = newName,
                changedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                reason = reason,
                adminForced = adminForced
            });

            Stats.displayName = newName;
            SaveToDisk();

            OnNameChanged?.Invoke(oldName, newName);
            Debug.Log($"PlayerStatsService: Name changed from '{oldName}' to '{newName}'");

            return true;
        }

        /// <summary>Gets the full name history.</summary>
        public List<NameChangeRecord> GetNameHistory()
        {
            return new List<NameChangeRecord>(Stats.nameHistory);
        }

        #endregion

        #region Player Card Data

        /// <summary>Builds the full PlayerCardData for display.</summary>
        public PlayerCardData BuildPlayerCard()
        {
            var trustService = TrustService.Instance;
            var trustState = trustService?.State;

            var card = new PlayerCardData
            {
                stats = Stats,
                rankId = trustState?.rankId ?? "e0",
                trustScore = trustState?.trustScore ?? 0,
                faction = trustState?.faction.ToString() ?? "Neutral"
            };

            // Get rank display name from definitions
            var reg = Definitions.DefinitionRegistry.Instance;
            if (reg?.Definitions?.ranks?.factionRanks != null)
            {
                var rankDef = reg.Definitions.ranks.factionRanks
                    .FirstOrDefault(r => r.rankId == card.rankId);
                card.rankDisplayName = rankDef?.displayName ?? card.rankId;
            }

            // Get active certifications
            if (trustState?.certsById != null)
            {
                card.activeCertifications = trustState.certsById.Values
                    .Where(c => c != null && c.isActive && !c.isExpired)
                    .Select(c => c.certId)
                    .ToList();
            }

            return card;
        }

        /// <summary>Gets formatted time served string.</summary>
        public string GetFormattedTimeServed()
        {
            var totalSeconds = Stats.timeServedSeconds;

            // Add current session time
            if (Stats.sessionStartUtc > 0)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                totalSeconds += (now - Stats.sessionStartUtc);
            }

            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;

            if (hours >= 24)
            {
                var days = hours / 24;
                hours = hours % 24;
                return $"{days}d {hours}h {minutes}m";
            }

            return $"{hours}h {minutes}m";
        }

        /// <summary>Gets K/D ratio.</summary>
        public float GetKDRatio()
        {
            if (Stats.deaths == 0)
                return Stats.kills;
            return (float)Stats.kills / Stats.deaths;
        }

        #endregion

        #region Persistence

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
                var snapshot = JsonUtility.FromJson<PlayerStatsSaveSnapshot>(json);

                if (snapshot == null || snapshot.schemaVersion <= 0)
                {
                    CreateNewProfile();
                    SaveToDisk();
                    return;
                }

                Stats = snapshot.stats ?? new PlayerStats();

                // Ensure lists are initialized
                if (Stats.nameHistory == null)
                    Stats.nameHistory = new List<NameChangeRecord>();
                if (Stats.medals == null)
                    Stats.medals = new List<string>();
                if (Stats.achievements == null)
                    Stats.achievements = new List<string>();

                // Start new session
                Stats.sessionStartUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Debug.Log($"PlayerStatsService: Loaded stats for '{Stats.displayName}' (Time served: {GetFormattedTimeServed()})");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PlayerStatsService: Failed to load '{SavePath}': {ex.Message}");
                CreateNewProfile();
                SaveToDisk();
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var snapshot = new PlayerStatsSaveSnapshot
                {
                    schemaVersion = 1,
                    stats = Stats
                };

                var json = JsonUtility.ToJson(snapshot, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PlayerStatsService: Failed to save '{SavePath}': {ex.Message}");
            }
        }

        private void CreateNewProfile()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            Stats = new PlayerStats
            {
                playerId = Guid.NewGuid().ToString(),
                displayName = "Soldier",
                createdUtc = now,
                sessionStartUtc = now,
                nameHistory = new List<NameChangeRecord>(),
                medals = new List<string>(),
                achievements = new List<string>()
            };

            Debug.Log("PlayerStatsService: Created new player profile");
        }

        /// <summary>Resets all stats (for debugging).</summary>
        public void DevResetStats()
        {
            CreateNewProfile();
            SaveToDisk();
            Debug.Log("PlayerStatsService: Reset all stats");
        }

        [Serializable]
        private sealed class PlayerStatsSaveSnapshot
        {
            public int schemaVersion = 1;
            public PlayerStats stats;
        }

        #endregion
    }
}
