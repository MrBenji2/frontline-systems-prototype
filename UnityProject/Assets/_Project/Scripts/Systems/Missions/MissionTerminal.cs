using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontline.Missions
{
    /// <summary>
    /// Interactive mission terminal that players can use to browse and accept missions.
    /// Inspired by Star Wars Galaxies mission terminals.
    /// 
    /// Place this component on an interactable object in the world. Players interact
    /// with the terminal to open the mission browser UI filtered by this terminal's type.
    /// </summary>
    public sealed class MissionTerminal : MonoBehaviour
    {
        [Header("Terminal Configuration")]
        [Tooltip("Type of terminal determines which missions are available (e.g., 'training', 'logistics', 'combat', 'general')")]
        [SerializeField] private string _terminalType = "general";

        [Tooltip("Display name shown in the UI when interacting with this terminal")]
        [SerializeField] private string _displayName = "Mission Terminal";

        [Tooltip("Faction this terminal belongs to (empty = neutral/any faction)")]
        [SerializeField] private string _faction = "";

        [Tooltip("Minimum rank required to use this terminal (empty = no requirement)")]
        [SerializeField] private string _minRankId = "";

        [Tooltip("Maximum interaction distance in meters")]
        [SerializeField] private float _interactionRange = 3f;

        [Header("Visual")]
        [Tooltip("Optional prompt text shown when player is in range")]
        [SerializeField] private string _interactPrompt = "Press [E] to use Mission Terminal";

        /// <summary>The type of this terminal (for filtering available missions).</summary>
        public string TerminalType => _terminalType;

        /// <summary>Display name for UI.</summary>
        public string DisplayName => _displayName;

        /// <summary>Faction this terminal belongs to.</summary>
        public string Faction => _faction;

        /// <summary>Whether a player is currently in range of this terminal.</summary>
        public bool IsPlayerInRange { get; private set; }

        /// <summary>Event fired when player enters interaction range.</summary>
        public event Action<MissionTerminal> OnPlayerEntered;

        /// <summary>Event fired when player leaves interaction range.</summary>
        public event Action<MissionTerminal> OnPlayerExited;

        /// <summary>Event fired when player interacts with the terminal.</summary>
        public event Action<MissionTerminal> OnInteracted;

        /// <summary>Cached available missions for this terminal.</summary>
        private List<MissionDefinition> _cachedMissions;
        private float _cacheTime;
        private const float CacheInvalidateTime = 5f;

        private Transform _playerTransform;
        private bool _wasInRange;

        private void Start()
        {
            // Try to find the player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
        }

        private void Update()
        {
            UpdatePlayerProximity();
            HandleInput();
        }

        private void UpdatePlayerProximity()
        {
            if (_playerTransform == null)
            {
                // Try to find player again
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _playerTransform = player.transform;
                else
                    return;
            }

            var distance = Vector3.Distance(transform.position, _playerTransform.position);
            IsPlayerInRange = distance <= _interactionRange;

            // Fire events on state change
            if (IsPlayerInRange && !_wasInRange)
            {
                OnPlayerEntered?.Invoke(this);
            }
            else if (!IsPlayerInRange && _wasInRange)
            {
                OnPlayerExited?.Invoke(this);
            }

            _wasInRange = IsPlayerInRange;
        }

        private void HandleInput()
        {
            // Only process input if player is in range
            if (!IsPlayerInRange)
                return;

            // Check for interaction input (E key by default)
            if (Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
        }

        /// <summary>
        /// Interacts with the terminal, opening the mission browser.
        /// </summary>
        public void Interact()
        {
            if (!IsPlayerInRange)
            {
                Debug.LogWarning($"MissionTerminal: Player not in range of '{_displayName}'");
                return;
            }

            // Check faction requirement
            // TODO: Implement faction checking when faction system is added

            // Check rank requirement
            if (!string.IsNullOrWhiteSpace(_minRankId))
            {
                // TODO: Check player rank
            }

            Debug.Log($"MissionTerminal: Interacting with '{_displayName}' (type: {_terminalType})");
            OnInteracted?.Invoke(this);

            // Open the terminal UI
            var terminalUI = FindAnyObjectByType<MissionTerminalPanel>();
            if (terminalUI != null)
            {
                terminalUI.OpenForTerminal(this);
            }
            else
            {
                Debug.LogWarning("MissionTerminal: No MissionTerminalPanel found in scene");
            }
        }

        /// <summary>
        /// Gets missions available at this terminal.
        /// Results are cached for performance.
        /// </summary>
        public List<MissionDefinition> GetAvailableMissions()
        {
            var now = Time.time;
            if (_cachedMissions == null || now - _cacheTime > CacheInvalidateTime)
            {
                RefreshMissionCache();
            }

            return _cachedMissions ?? new List<MissionDefinition>();
        }

        /// <summary>
        /// Forces a refresh of the available missions cache.
        /// </summary>
        public void RefreshMissionCache()
        {
            var service = MissionService.Instance;
            if (service == null)
            {
                _cachedMissions = new List<MissionDefinition>();
                return;
            }

            _cachedMissions = service.GetTerminalMissions(_terminalType);
            _cacheTime = Time.time;
        }

        /// <summary>
        /// Gets mission details formatted for display.
        /// </summary>
        public MissionTerminalEntry[] GetMissionEntries()
        {
            var missions = GetAvailableMissions();
            var entries = new MissionTerminalEntry[missions.Count];

            for (int i = 0; i < missions.Count; i++)
            {
                var def = missions[i];
                entries[i] = new MissionTerminalEntry
                {
                    missionId = def.missionId,
                    displayName = def.displayName,
                    description = def.description,
                    category = def.category,
                    difficulty = def.difficulty,
                    timeLimit = def.timeLimit,
                    rewards = FormatRewards(def.rewards),
                    isRepeatable = def.repeatable,
                    completionCount = MissionService.Instance?.GetCompletionCount(def.missionId) ?? 0
                };
            }

            return entries;
        }

        private string FormatRewards(MissionRewardsDefinition rewards)
        {
            if (rewards == null)
                return "None";

            var parts = new List<string>();

            if (rewards.trustPoints > 0)
                parts.Add($"+{rewards.trustPoints} Trust");

            if (rewards.credits > 0)
                parts.Add($"{rewards.credits} Credits");

            if (rewards.experience > 0)
                parts.Add($"{rewards.experience} XP");

            if (!string.IsNullOrWhiteSpace(rewards.grantCertification))
                parts.Add($"Cert: {rewards.grantCertification}");

            if (rewards.items != null && rewards.items.Count > 0)
                parts.Add($"+{rewards.items.Count} item(s)");

            return parts.Count > 0 ? string.Join(", ", parts) : "None";
        }

        /// <summary>
        /// Accepts a mission from this terminal.
        /// </summary>
        public bool AcceptMission(string missionId)
        {
            var service = MissionService.Instance;
            if (service == null)
            {
                Debug.LogWarning("MissionTerminal: MissionService not available");
                return false;
            }

            var success = service.AcceptMission(missionId);
            if (success)
            {
                // Invalidate cache so the list updates
                _cachedMissions = null;
            }

            return success;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }

        private void OnGUI()
        {
            // Show interaction prompt when in range
            if (!IsPlayerInRange || string.IsNullOrWhiteSpace(_interactPrompt))
                return;

            // Only show if terminal UI is not already open
            var terminalUI = FindAnyObjectByType<MissionTerminalPanel>();
            if (terminalUI != null && terminalUI.IsOpen)
                return;

            // Simple centered prompt
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            var content = new GUIContent(_interactPrompt);
            var size = style.CalcSize(content);
            var rect = new Rect(
                (Screen.width - size.x - 20) / 2f,
                Screen.height * 0.7f,
                size.x + 20,
                size.y + 10
            );

            GUI.Box(rect, _interactPrompt, style);
        }
    }

    /// <summary>
    /// Data structure for displaying a mission in the terminal UI.
    /// </summary>
    [Serializable]
    public struct MissionTerminalEntry
    {
        public string missionId;
        public string displayName;
        public string description;
        public string category;
        public int difficulty;
        public float timeLimit;
        public string rewards;
        public bool isRepeatable;
        public int completionCount;

        /// <summary>Formats time limit for display.</summary>
        public string FormattedTimeLimit =>
            timeLimit > 0 ? MissionService.FormatTime((int)timeLimit) : "No limit";

        /// <summary>Difficulty as star string (e.g., "★★★☆☆").</summary>
        public string DifficultyStars
        {
            get
            {
                var stars = Mathf.Clamp(difficulty, 1, 5);
                return new string('★', stars) + new string('☆', 5 - stars);
            }
        }
    }
}
