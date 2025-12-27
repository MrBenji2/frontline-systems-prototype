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
    /// Handles mission assignment, progress tracking, completion, and rewards.
    /// </summary>
    public sealed class MissionService : MonoBehaviour
    {
        public static MissionService Instance { get; private set; }

        private const string SaveFileName = "player_missions_v1";

        /// <summary>All active and completed missions for the current player.</summary>
        public Dictionary<string, PlayerMissionState> MissionStates { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>Event fired when a mission is accepted.</summary>
        public event Action<string> OnMissionAccepted;

        /// <summary>Event fired when mission progress is updated.</summary>
        public event Action<string, string, int, int> OnObjectiveProgress; // missionId, objectiveId, current, required

        /// <summary>Event fired when a mission is completed.</summary>
        public event Action<string> OnMissionCompleted;

        private readonly Dictionary<string, MissionDefinition> _missionDefsById = new(StringComparer.Ordinal);
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
            AssignAutoMissions();
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
        /// Gets all available missions the player can accept.
        /// </summary>
        public List<MissionDefinition> GetAvailableMissions()
        {
            var available = new List<MissionDefinition>();
            var trustState = TrustService.Instance?.State;

            foreach (var def in _missionDefsById.Values)
            {
                // Skip if already active or completed (unless repeatable)
                if (MissionStates.TryGetValue(def.missionId, out var state))
                {
                    if (state.status == MissionStatus.Active)
                        continue;

                    if (state.status == MissionStatus.Completed && !def.repeatable)
                        continue;
                }

                // Check certification requirement
                if (!string.IsNullOrWhiteSpace(def.requiredCertId) && trustState != null)
                {
                    if (!trustState.certsById.TryGetValue(def.requiredCertId, out var cert) ||
                        cert == null || !cert.isActive)
                        continue;
                }

                // Check permission requirement
                if (!string.IsNullOrWhiteSpace(def.requiredPermission))
                {
                    if (!PermissionGate.Can(trustState, def.requiredPermission, out _))
                        continue;
                }

                available.Add(def);
            }

            return available;
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

            Debug.Log($"MissionService: Accepted mission '{def.displayName}'");
            OnMissionAccepted?.Invoke(missionId);

            return true;
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

            // TODO: Grant item rewards (requires inventory integration)
            // foreach (var itemReward in rewards.items) { ... }
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
