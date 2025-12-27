using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Definitions;
using Frontline.PlayerCard;
using Frontline.Trust;
using UnityEngine;

namespace Frontline.Missions
{
    /// <summary>
    /// Central service for managing player missions.
    /// Handles mission assignment, progress tracking, time limits, completion, and rewards.
    /// 
    /// Inspired by Star Wars Galaxies mission terminal system:
    /// - Missions can be offered from terminals
    /// - Time limits for mission completion
    /// - Difficulty scaling with rewards
    /// - Location-based objectives for waypoint display
    /// </summary>
    public sealed class MissionService : MonoBehaviour
    {
        public static MissionService Instance { get; private set; }

        private const string SaveFileName = "player_missions_v1";
        private const float TimeCheckInterval = 5f; // Check time limits every 5 seconds

        /// <summary>All active and completed missions for the current player.</summary>
        public Dictionary<string, PlayerMissionState> MissionStates { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>Event fired when a mission is accepted.</summary>
        public event Action<string> OnMissionAccepted;

        /// <summary>Event fired when mission progress is updated.</summary>
        public event Action<string, string, int, int> OnObjectiveProgress; // missionId, objectiveId, current, required

        /// <summary>Event fired when a mission is completed.</summary>
        public event Action<string> OnMissionCompleted;

        /// <summary>Event fired when a mission fails (time limit exceeded, etc.).</summary>
        public event Action<string, string> OnMissionFailed; // missionId, reason

        /// <summary>Event fired when time remaining on a mission updates.</summary>
        public event Action<string, float> OnMissionTimeUpdate; // missionId, secondsRemaining

        private readonly Dictionary<string, MissionDefinition> _missionDefsById = new(StringComparer.Ordinal);
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        private float _nextTimeCheck;

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
            AssignAutoMissions();
        }

        private void Update()
        {
            // Periodically check time limits on active missions
            if (Time.time >= _nextTimeCheck)
            {
                _nextTimeCheck = Time.time + TimeCheckInterval;
                CheckTimeLimits();
            }
        }

        private void OnApplicationQuit()
        {
            SaveToDisk();
        }

        private void LoadDefinitions()
        {
            var reg = DefinitionRegistry.Instance;
            var defs = reg != null ? reg.Definitions : new GameDefinitions();

            _missionDefsById.Clear();

            if (defs.missions != null && defs.missions.missions != null)
            {
                foreach (var m in defs.missions.missions)
                {
                    if (m == null || string.IsNullOrWhiteSpace(m.missionId))
                        continue;

                    _missionDefsById[m.missionId] = m;
                }
            }

            Debug.Log($"MissionService: Loaded {_missionDefsById.Count} mission definitions.");
        }

        /// <summary>
        /// Gets the definition for a mission by ID.
        /// </summary>
        public MissionDefinition GetMissionDefinition(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
                return null;

            return _missionDefsById.TryGetValue(missionId, out var def) ? def : null;
        }

        /// <summary>
        /// Gets all mission definitions.
        /// </summary>
        public IEnumerable<MissionDefinition> GetAllMissionDefinitions() => _missionDefsById.Values;

        /// <summary>
        /// Gets all available missions the player can accept.
        /// </summary>
        public List<MissionDefinition> GetAvailableMissions()
        {
            return GetAvailableMissions(null, null);
        }

        /// <summary>
        /// Gets available missions filtered by category and/or terminal type.
        /// </summary>
        /// <param name="category">Optional category filter (e.g., "training", "combat").</param>
        /// <param name="terminalType">Optional terminal type filter (e.g., "training", "logistics").</param>
        public List<MissionDefinition> GetAvailableMissions(string category, string terminalType)
        {
            var available = new List<MissionDefinition>();
            var trustService = TrustService.Instance;
            var trustState = trustService?.State;

            foreach (var def in _missionDefsById.Values)
            {
                // Filter by category if specified
                if (!string.IsNullOrWhiteSpace(category) &&
                    !string.Equals(def.category, category, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Filter by terminal type if specified
                if (!string.IsNullOrWhiteSpace(terminalType))
                {
                    if (!def.terminalAvailable)
                        continue;

                    if (def.terminalTypes.Count > 0 &&
                        !def.terminalTypes.Any(t => string.Equals(t, terminalType, StringComparison.OrdinalIgnoreCase)))
                        continue;
                }

                // Skip if already active
                if (MissionStates.TryGetValue(def.missionId, out var state))
                {
                    if (state.status == MissionStatus.Active)
                        continue;

                    if (state.status == MissionStatus.Completed && !def.repeatable)
                        continue;

                    // Check cooldown for repeatable missions
                    if (def.repeatable && state.status == MissionStatus.Completed && def.repeatCooldown > 0)
                    {
                        var nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var cooldownEnd = state.completedUtc + (long)def.repeatCooldown;
                        if (nowUtc < cooldownEnd)
                            continue;
                    }
                }

                // Check certification requirements
                if (def.requiredCertifications.Count > 0 && trustState != null)
                {
                    var hasAllCerts = true;
                    foreach (var certId in def.requiredCertifications)
                    {
                        if (!trustState.certsById.TryGetValue(certId, out var cert) ||
                            cert == null || !cert.isActive)
                        {
                            hasAllCerts = false;
                            break;
                        }
                    }
                    if (!hasAllCerts)
                        continue;
                }

                // Check permission requirements
                if (def.requiredPermissions.Count > 0)
                {
                    var hasAllPerms = true;
                    foreach (var perm in def.requiredPermissions)
                    {
                        if (!PermissionGate.Can(trustState, perm, out _))
                        {
                            hasAllPerms = false;
                            break;
                        }
                    }
                    if (!hasAllPerms)
                        continue;
                }

                // Check minimum trust score
                if (def.minTrustScore > 0 && trustState != null)
                {
                    if (trustState.trustScore < def.minTrustScore)
                        continue;
                }

                // Check minimum rank
                if (!string.IsNullOrWhiteSpace(def.minRankId) && trustService != null)
                {
                    var playerRankIndex = GetRankIndex(trustState?.rankId ?? "e0");
                    var requiredRankIndex = GetRankIndex(def.minRankId);
                    if (playerRankIndex < requiredRankIndex)
                        continue;
                }

                // Check faction requirement
                // TODO: Implement faction checking when faction system is added

                available.Add(def);
            }

            // Sort by difficulty, then by display name
            available.Sort((a, b) =>
            {
                var diffCompare = a.difficulty.CompareTo(b.difficulty);
                return diffCompare != 0 ? diffCompare : string.Compare(a.displayName, b.displayName, StringComparison.Ordinal);
            });

            return available;
        }

        /// <summary>
        /// Gets the index of a rank for comparison purposes.
        /// </summary>
        private int GetRankIndex(string rankId)
        {
            if (string.IsNullOrWhiteSpace(rankId))
                return 0;

            var trustService = TrustService.Instance;
            if (trustService == null)
                return 0;

            var ranks = trustService.RankDefinitions;
            for (int i = 0; i < ranks.Count; i++)
            {
                if (string.Equals(ranks[i].rankId, rankId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }

        /// <summary>
        /// Gets all missions available at a specific terminal type.
        /// </summary>
        public List<MissionDefinition> GetTerminalMissions(string terminalType)
        {
            return GetAvailableMissions(null, terminalType);
        }

        /// <summary>
        /// Gets all currently active missions.
        /// </summary>
        public List<PlayerMissionState> GetActiveMissions()
        {
            return MissionStates.Values
                .Where(s => s.status == MissionStatus.Active)
                .ToList();
        }

        /// <summary>
        /// Gets time remaining for an active mission with a time limit.
        /// Returns -1 if mission has no time limit or is not active.
        /// </summary>
        public float GetTimeRemaining(string missionId)
        {
            if (!MissionStates.TryGetValue(missionId, out var state))
                return -1f;

            if (state.status != MissionStatus.Active)
                return -1f;

            if (!_missionDefsById.TryGetValue(missionId, out var def))
                return -1f;

            if (def.timeLimit <= 0)
                return -1f;

            var nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var elapsed = nowUtc - state.acceptedUtc;
            var remaining = def.timeLimit - elapsed;

            return Mathf.Max(0f, remaining);
        }

        /// <summary>
        /// Accepts a mission by ID. Returns true if successful.
        /// </summary>
        public bool AcceptMission(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
                return false;

            if (!_missionDefsById.TryGetValue(missionId, out var def))
            {
                Debug.LogWarning($"MissionService: Unknown mission '{missionId}'");
                return false;
            }

            // Check if already active
            if (MissionStates.TryGetValue(missionId, out var existingState))
            {
                if (existingState.status == MissionStatus.Active)
                {
                    Debug.LogWarning($"MissionService: Mission '{missionId}' already active");
                    return false;
                }

                if (existingState.status == MissionStatus.Completed && !def.repeatable)
                {
                    Debug.LogWarning($"MissionService: Mission '{missionId}' already completed and not repeatable");
                    return false;
                }

                // Check cooldown for repeatable missions
                if (def.repeatable && existingState.status == MissionStatus.Completed && def.repeatCooldown > 0)
                {
                    var nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var cooldownEnd = existingState.completedUtc + (long)def.repeatCooldown;
                    if (nowUtc < cooldownEnd)
                    {
                        var remaining = cooldownEnd - nowUtc;
                        Debug.LogWarning($"MissionService: Mission '{missionId}' on cooldown ({remaining}s remaining)");
                        return false;
                    }
                }
            }

            // Create new state
            var state = new PlayerMissionState
            {
                missionId = missionId,
                status = MissionStatus.Active,
                acceptedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                objectiveProgress = new List<ObjectiveProgress>()
            };

            // Initialize objective progress
            foreach (var obj in def.objectives)
            {
                state.objectiveProgress.Add(new ObjectiveProgress
                {
                    objectiveId = obj.objectiveId,
                    currentCount = 0,
                    isComplete = false
                });
            }

            MissionStates[missionId] = state;
            SaveToDisk();

            var timeInfo = def.timeLimit > 0 ? $" (time limit: {FormatTime((int)def.timeLimit)})" : "";
            Debug.Log($"MissionService: Accepted mission '{def.displayName}'{timeInfo}");
            OnMissionAccepted?.Invoke(missionId);

            return true;
        }

        /// <summary>
        /// Checks time limits on all active missions and fails those that have expired.
        /// </summary>
        private void CheckTimeLimits()
        {
            var nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiredMissions = new List<(PlayerMissionState state, MissionDefinition def)>();

            foreach (var state in MissionStates.Values)
            {
                if (state.status != MissionStatus.Active)
                    continue;

                if (!_missionDefsById.TryGetValue(state.missionId, out var def))
                    continue;

                if (def.timeLimit <= 0)
                    continue;

                var elapsed = nowUtc - state.acceptedUtc;
                var remaining = def.timeLimit - elapsed;

                // Fire time update event
                OnMissionTimeUpdate?.Invoke(state.missionId, Mathf.Max(0f, remaining));

                if (remaining <= 0)
                {
                    expiredMissions.Add((state, def));
                }
            }

            // Fail expired missions
            foreach (var (state, def) in expiredMissions)
            {
                FailMission(state, def, "Time limit exceeded");
            }
        }

        /// <summary>
        /// Reports progress on an objective. Call this when the player performs relevant actions.
        /// </summary>
        /// <param name="objectiveType">The type of objective (e.g., "hit_target", "kill_npc").</param>
        /// <param name="targetId">Optional target ID for specific objectives.</param>
        /// <param name="amount">Amount of progress to add (default 1).</param>
        public void ReportProgress(string objectiveType, string targetId = null, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(objectiveType))
                return;

            foreach (var state in MissionStates.Values)
            {
                if (state.status != MissionStatus.Active)
                    continue;

                if (!_missionDefsById.TryGetValue(state.missionId, out var def))
                    continue;

                foreach (var objDef in def.objectives)
                {
                    // Check if objective type matches
                    if (!string.Equals(objDef.type, objectiveType, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Check if target ID matches (if specified in definition)
                    if (!string.IsNullOrWhiteSpace(objDef.targetId) &&
                        !string.Equals(objDef.targetId, targetId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Find or create progress entry
                    var progress = state.objectiveProgress.FirstOrDefault(p => p.objectiveId == objDef.objectiveId);
                    if (progress == null)
                    {
                        progress = new ObjectiveProgress { objectiveId = objDef.objectiveId };
                        state.objectiveProgress.Add(progress);
                    }

                    // Skip if already complete
                    if (progress.isComplete)
                        continue;

                    // Update progress
                    progress.currentCount += amount;

                    // Check if objective is now complete
                    if (progress.currentCount >= objDef.requiredCount)
                    {
                        progress.currentCount = objDef.requiredCount;
                        progress.isComplete = true;
                        Debug.Log($"MissionService: Objective '{objDef.description}' completed!");
                    }

                    OnObjectiveProgress?.Invoke(state.missionId, objDef.objectiveId, progress.currentCount, objDef.requiredCount);

                    // Check if all required objectives are complete
                    CheckMissionCompletion(state, def);
                }
            }

            SaveToDisk();
        }

        /// <summary>
        /// Reports progress at a specific location. Used for reach_location objectives.
        /// </summary>
        public void ReportLocationReached(Vector3 playerPosition, string locationId = null)
        {
            foreach (var state in MissionStates.Values)
            {
                if (state.status != MissionStatus.Active)
                    continue;

                if (!_missionDefsById.TryGetValue(state.missionId, out var def))
                    continue;

                foreach (var objDef in def.objectives)
                {
                    if (objDef.GetObjectiveType() != MissionObjectiveType.ReachLocation)
                        continue;

                    // If targetId specified, check it matches
                    if (!string.IsNullOrWhiteSpace(objDef.targetId) &&
                        !string.Equals(objDef.targetId, locationId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Check if player is within radius of objective location
                    if (!objDef.location.IsValid)
                        continue;

                    var distance = objDef.location.DistanceTo(playerPosition);
                    if (distance > objDef.locationRadius)
                        continue;

                    // Find progress entry
                    var progress = state.objectiveProgress.FirstOrDefault(p => p.objectiveId == objDef.objectiveId);
                    if (progress == null || progress.isComplete)
                        continue;

                    // Complete the objective
                    progress.currentCount = objDef.requiredCount;
                    progress.isComplete = true;
                    Debug.Log($"MissionService: Reached location for objective '{objDef.description}'");

                    OnObjectiveProgress?.Invoke(state.missionId, objDef.objectiveId, progress.currentCount, objDef.requiredCount);
                    CheckMissionCompletion(state, def);
                }
            }

            SaveToDisk();
        }

        /// <summary>
        /// Checks if a mission is complete and grants rewards if so.
        /// </summary>
        private void CheckMissionCompletion(PlayerMissionState state, MissionDefinition def)
        {
            // Check all non-optional objectives are complete
            foreach (var objDef in def.objectives)
            {
                if (objDef.optional)
                    continue;

                var progress = state.objectiveProgress.FirstOrDefault(p => p.objectiveId == objDef.objectiveId);
                if (progress == null || !progress.isComplete)
                    return; // Not all required objectives complete
            }

            // Mission complete!
            CompleteMission(state, def);
        }

        /// <summary>
        /// Completes a mission and grants rewards.
        /// </summary>
        private void CompleteMission(PlayerMissionState state, MissionDefinition def)
        {
            if (state.status == MissionStatus.Completed)
                return;

            state.status = MissionStatus.Completed;
            state.completedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            state.completionCount++;

            Debug.Log($"MissionService: Mission '{def.displayName}' completed!");

            // Grant rewards
            GrantRewards(def.rewards);

            // Track mission completion in player stats
            var statsService = PlayerStatsService.Instance;
            if (statsService != null)
            {
                var isTraining = def.category == "training";
                statsService.RecordMissionCompleted(isTraining);
            }

            SaveToDisk();
            OnMissionCompleted?.Invoke(state.missionId);
        }

        /// <summary>
        /// Fails a mission (e.g., time limit exceeded).
        /// </summary>
        private void FailMission(PlayerMissionState state, MissionDefinition def, string reason)
        {
            if (state.status != MissionStatus.Active)
                return;

            state.status = MissionStatus.Failed;
            state.completedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            Debug.Log($"MissionService: Mission '{def.displayName}' failed: {reason}");

            // Track mission failure in player stats
            var statsService = PlayerStatsService.Instance;
            if (statsService != null)
            {
                statsService.RecordMissionFailed();
            }

            SaveToDisk();
            OnMissionFailed?.Invoke(state.missionId, reason);
        }

        /// <summary>
        /// Grants mission rewards to the player.
        /// </summary>
        private void GrantRewards(MissionRewardsDefinition rewards)
        {
            if (rewards == null)
                return;

            var trustService = TrustService.Instance;

            // Grant trust points
            if (rewards.trustPoints > 0 && trustService != null)
            {
                trustService.State.trustScore += rewards.trustPoints;
                trustService.State.rankId = trustService.EvaluateRankId(trustService.State.trustScore);
                Debug.Log($"MissionService: Granted {rewards.trustPoints} trust points. New score: {trustService.State.trustScore}");
            }

            // Grant certification
            if (!string.IsNullOrWhiteSpace(rewards.grantCertification) && trustService != null)
            {
                if (trustService.GrantCertification(rewards.grantCertification))
                {
                    Debug.Log($"MissionService: Granted certification '{rewards.grantCertification}'");
                }
            }

            // Grant credits
            if (rewards.credits > 0)
            {
                // TODO: Integrate with economy/wallet system when implemented
                Debug.Log($"MissionService: Granted {rewards.credits} credits (economy integration pending)");
            }

            // Grant experience
            if (rewards.experience > 0)
            {
                // TODO: Integrate with experience system when implemented
                Debug.Log($"MissionService: Granted {rewards.experience} XP (XP integration pending)");
            }

            // Grant item rewards
            if (rewards.items != null && rewards.items.Count > 0)
            {
                // TODO: Integrate with inventory system when implemented
                foreach (var item in rewards.items)
                {
                    Debug.Log($"MissionService: Granted {item.quantity}x {item.itemId} (inventory integration pending)");
                }
            }
        }

        /// <summary>
        /// Abandons an active mission.
        /// </summary>
        public bool AbandonMission(string missionId)
        {
            if (!MissionStates.TryGetValue(missionId, out var state))
                return false;

            if (state.status != MissionStatus.Active)
                return false;

            state.status = MissionStatus.Abandoned;

            // Track mission abandon in player stats
            var statsService = PlayerStatsService.Instance;
            if (statsService != null)
            {
                statsService.RecordMissionAbandoned();
            }

            SaveToDisk();

            Debug.Log($"MissionService: Abandoned mission '{missionId}'");
            return true;
        }

        /// <summary>
        /// Force-completes a mission (for debugging).
        /// </summary>
        public bool DevCompleteMission(string missionId)
        {
            if (!MissionStates.TryGetValue(missionId, out var state))
            {
                // Try to accept it first
                if (!AcceptMission(missionId))
                    return false;
                state = MissionStates[missionId];
            }

            if (!_missionDefsById.TryGetValue(missionId, out var def))
                return false;

            // Mark all objectives complete
            foreach (var objDef in def.objectives)
            {
                var progress = state.objectiveProgress.FirstOrDefault(p => p.objectiveId == objDef.objectiveId);
                if (progress != null)
                {
                    progress.currentCount = objDef.requiredCount;
                    progress.isComplete = true;
                }
            }

            CompleteMission(state, def);
            return true;
        }

        /// <summary>
        /// Assigns auto-assign missions to the player if not already assigned.
        /// </summary>
        private void AssignAutoMissions()
        {
            foreach (var def in _missionDefsById.Values)
            {
                if (!def.autoAssign)
                    continue;

                // Skip if already has state (started, completed, etc.)
                if (MissionStates.ContainsKey(def.missionId))
                    continue;

                AcceptMission(def.missionId);
            }
        }

        /// <summary>
        /// Checks if a specific mission has been completed.
        /// </summary>
        public bool IsMissionCompleted(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
                return false;

            return MissionStates.TryGetValue(missionId, out var state) &&
                   state.status == MissionStatus.Completed;
        }

        /// <summary>
        /// Gets the completion count for a repeatable mission.
        /// </summary>
        public int GetCompletionCount(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
                return 0;

            return MissionStates.TryGetValue(missionId, out var state) ? state.completionCount : 0;
        }

        /// <summary>
        /// Gets objective location for waypoint display.
        /// </summary>
        public MissionLocation GetObjectiveLocation(string missionId, string objectiveId)
        {
            if (!_missionDefsById.TryGetValue(missionId, out var def))
                return null;

            var objDef = def.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
            return objDef?.location;
        }

        /// <summary>
        /// Formats time in seconds to a readable string.
        /// </summary>
        public static string FormatTime(int totalSeconds)
        {
            if (totalSeconds < 60)
                return $"{totalSeconds}s";

            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;

            if (minutes < 60)
                return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";

            var hours = minutes / 60;
            minutes %= 60;
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        #region Persistence

        private void LoadFromDiskOrCreateNew()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    MissionStates = new Dictionary<string, PlayerMissionState>(StringComparer.Ordinal);
                    return;
                }

                var json = File.ReadAllText(SavePath);
                var snapshot = JsonUtility.FromJson<MissionSaveSnapshot>(json);

                if (snapshot == null || snapshot.schemaVersion <= 0)
                {
                    MissionStates = new Dictionary<string, PlayerMissionState>(StringComparer.Ordinal);
                    return;
                }

                MissionStates = new Dictionary<string, PlayerMissionState>(StringComparer.Ordinal);
                if (snapshot.missions != null)
                {
                    foreach (var state in snapshot.missions)
                    {
                        if (state != null && !string.IsNullOrWhiteSpace(state.missionId))
                            MissionStates[state.missionId] = state;
                    }
                }

                Debug.Log($"MissionService: Loaded {MissionStates.Count} mission states from disk.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"MissionService: Failed to load '{SavePath}': {ex.Message}");
                MissionStates = new Dictionary<string, PlayerMissionState>(StringComparer.Ordinal);
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var snapshot = new MissionSaveSnapshot
                {
                    schemaVersion = 1,
                    missions = MissionStates.Values.ToList()
                };

                var json = JsonUtility.ToJson(snapshot, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"MissionService: Failed to save '{SavePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all mission progress (for debugging).
        /// </summary>
        public void DevResetAllMissions()
        {
            MissionStates.Clear();
            SaveToDisk();
            AssignAutoMissions();
            Debug.Log("MissionService: Reset all mission progress.");
        }

        [Serializable]
        private sealed class MissionSaveSnapshot
        {
            public int schemaVersion = 1;
            public List<PlayerMissionState> missions = new();
        }

        #endregion
    }
}
