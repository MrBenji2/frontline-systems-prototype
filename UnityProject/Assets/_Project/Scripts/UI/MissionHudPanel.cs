using System.Collections.Generic;
using System.Linq;
using Frontline.Missions;
using Frontline.Trust;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// HUD panel showing active missions and their progress.
    /// Also shows mission completion notifications.
    /// Toggle with M key.
    /// </summary>
    public sealed class MissionHudPanel : MonoBehaviour
    {
        public static MissionHudPanel Instance { get; private set; }

        [SerializeField] private KeyCode toggleKey = KeyCode.M;
        [SerializeField] private bool showMiniTracker = true;
        [SerializeField] private bool showFullPanel;

        private Vector2 _panelScroll;
        private readonly List<CompletionNotification> _notifications = new();
        private const float NotificationDuration = 5f;

        public bool IsVisible => showFullPanel;

        private struct CompletionNotification
        {
            public string Message;
            public float ExpireTime;
            public Color Color;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // Subscribe to mission events
            var missionService = MissionService.Instance;
            if (missionService != null)
            {
                missionService.OnMissionCompleted += HandleMissionCompleted;
                missionService.OnObjectiveProgress += HandleObjectiveProgress;
                missionService.OnMissionFailed += HandleMissionFailed;
            }
        }

        private void OnDestroy()
        {
            var missionService = MissionService.Instance;
            if (missionService != null)
            {
                missionService.OnMissionCompleted -= HandleMissionCompleted;
                missionService.OnObjectiveProgress -= HandleObjectiveProgress;
                missionService.OnMissionFailed -= HandleMissionFailed;
            }
        }

        private void HandleMissionFailed(string missionId, string reason)
        {
            var missionService = MissionService.Instance;
            var def = missionService?.GetMissionDefinition(missionId);
            var displayName = def?.displayName ?? missionId;

            _notifications.Add(new CompletionNotification
            {
                Message = $"Mission Failed: {displayName}\n{reason}",
                ExpireTime = Time.time + NotificationDuration,
                Color = new Color(0.9f, 0.3f, 0.3f)
            });
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                showFullPanel = !showFullPanel;

            // Clean up expired notifications
            _notifications.RemoveAll(n => Time.time >= n.ExpireTime);
        }

        private void HandleMissionCompleted(string missionId)
        {
            var missionService = MissionService.Instance;
            var def = missionService?.GetMissionDefinition(missionId);
            var displayName = def?.displayName ?? missionId;

            // Check what rewards were granted
            var rewardText = "";
            if (def?.rewards != null)
            {
                if (!string.IsNullOrWhiteSpace(def.rewards.grantCertification))
                {
                    rewardText = $"\nCertification unlocked: {def.rewards.grantCertification}";
                }

                if (def.rewards.trustPoints > 0)
                {
                    rewardText += $"\n+{def.rewards.trustPoints} Trust";
                }
            }

            _notifications.Add(new CompletionNotification
            {
                Message = $"Mission Complete: {displayName}{rewardText}",
                ExpireTime = Time.time + NotificationDuration,
                Color = new Color(0.2f, 0.8f, 0.2f)
            });
        }

        private void HandleObjectiveProgress(string missionId, string objectiveId, int current, int required)
        {
            // Could show a brief progress notification here
            if (current == required)
            {
                var missionService = MissionService.Instance;
                var def = missionService?.GetMissionDefinition(missionId);
                var objDef = def?.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);

                if (objDef != null)
                {
                    _notifications.Add(new CompletionNotification
                    {
                        Message = $"✓ {objDef.description}",
                        ExpireTime = Time.time + 3f,
                        Color = new Color(0.6f, 0.9f, 0.6f)
                    });
                }
            }
        }

        private void OnGUI()
        {
            // Always show notifications
            DrawNotifications();

            // Mini tracker (top right)
            if (showMiniTracker && !showFullPanel)
                DrawMiniTracker();

            // Full panel
            if (showFullPanel)
                DrawFullPanel();
        }

        private void DrawNotifications()
        {
            if (_notifications.Count == 0)
                return;

            var y = 80f;
            var width = 350f;
            var x = Screen.width - width - 20f;

            foreach (var notif in _notifications)
            {
                var style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };

                // Fade out effect
                var timeLeft = notif.ExpireTime - Time.time;
                var alpha = Mathf.Clamp01(timeLeft / 1f);
                var color = notif.Color;
                color.a = alpha;

                GUI.backgroundColor = new Color(0, 0, 0, 0.7f * alpha);
                GUI.contentColor = color;

                var content = new GUIContent(notif.Message);
                var height = style.CalcHeight(content, width - 20) + 10;
                var rect = new Rect(x, y, width, height);

                GUI.Box(rect, notif.Message, style);
                y += height + 5;
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
        }

        private void DrawMiniTracker()
        {
            var missionService = MissionService.Instance;
            if (missionService == null)
                return;

            var activeMissions = missionService.GetActiveMissions();
            if (activeMissions.Count == 0)
                return;

            var width = 280f;
            var x = Screen.width - width - 10f;
            var y = 10f;

            GUI.backgroundColor = new Color(0, 0, 0, 0.6f);

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 5, 5)
            };

            var content = BuildMiniTrackerContent(activeMissions);
            var height = style.CalcHeight(new GUIContent(content), width - 20) + 10;
            height = Mathf.Min(height, 200f);

            var rect = new Rect(x, y, width, height);
            GUI.Box(rect, "", style);

            GUI.contentColor = Color.white;
            GUI.Label(new Rect(x + 8, y + 5, width - 16, height - 10), content, style);

            // Press M hint
            var hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.LowerRight
            };
            GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(x, y + height - 20, width - 10, 15), "[M] Full view", hintStyle);

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
        }

        private string BuildMiniTrackerContent(List<PlayerMissionState> activeMissions)
        {
            var missionService = MissionService.Instance;
            var lines = new List<string> { "<b>Active Missions</b>" };

            foreach (var state in activeMissions.Take(3)) // Show max 3
            {
                var def = missionService.GetMissionDefinition(state.missionId);
                if (def == null)
                    continue;

                // Show time remaining if timed mission
                var timeRemaining = missionService.GetTimeRemaining(state.missionId);
                var timeText = "";
                if (timeRemaining > 0)
                {
                    var timeColor = timeRemaining < 60 ? "#FF4444" : (timeRemaining < 180 ? "#FFAA44" : "#AAFFAA");
                    timeText = $" <color={timeColor}>[{MissionService.FormatTime((int)timeRemaining)}]</color>";
                }

                lines.Add($"\n<color=#FFD700>{def.displayName}</color>{timeText}");

                foreach (var objDef in def.objectives.Where(o => !o.optional).OrderBy(o => o.order))
                {
                    var progress = state.objectiveProgress.FirstOrDefault(p => p.objectiveId == objDef.objectiveId);
                    var current = progress?.currentCount ?? 0;
                    var isComplete = progress?.isComplete ?? false;

                    var checkmark = isComplete ? "✓" : "○";
                    var color = isComplete ? "#88FF88" : "#FFFFFF";
                    lines.Add($"  <color={color}>{checkmark} {objDef.description} ({current}/{objDef.requiredCount})</color>");
                }
            }

            if (activeMissions.Count > 3)
                lines.Add($"\n... and {activeMissions.Count - 3} more");

            return string.Join("\n", lines);
        }

        private void DrawFullPanel()
        {
            var missionService = MissionService.Instance;
            if (missionService == null)
                return;

            var panelWidth = Mathf.Min(450, Screen.width - 40);
            var panelHeight = Mathf.Min(500, Screen.height - 40);
            var x = (Screen.width - panelWidth) / 2f;
            var y = (Screen.height - panelHeight) / 2f;

            var rect = new Rect(x, y, panelWidth, panelHeight);
            GUI.Box(rect, "");

            GUILayout.BeginArea(rect);
            _panelScroll = GUILayout.BeginScrollView(_panelScroll);

            GUILayout.Label("<size=18><b>Missions</b></size> (M to close)");
            GUILayout.Space(10);

            // Show player status
            DrawPlayerStatus();
            GUILayout.Space(10);

            // Active Missions
            var activeMissions = missionService.GetActiveMissions();
            GUILayout.Label($"<b>Active Missions ({activeMissions.Count})</b>");
            GUILayout.Space(5);

            if (activeMissions.Count == 0)
            {
                GUILayout.Label("  <i>No active missions</i>");
            }
            else
            {
                foreach (var state in activeMissions)
                {
                    DrawMissionEntry(state, missionService);
                }
            }

            GUILayout.Space(15);

            // Available Missions
            var availableMissions = missionService.GetAvailableMissions();
            GUILayout.Label($"<b>Available Missions ({availableMissions.Count})</b>");
            GUILayout.Space(5);

            if (availableMissions.Count == 0)
            {
                GUILayout.Label("  <i>No available missions</i>");
            }
            else
            {
                foreach (var def in availableMissions)
                {
                    DrawAvailableMissionEntry(def, missionService);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPlayerStatus()
        {
            var trustService = TrustService.Instance;
            if (trustService == null)
                return;

            var state = trustService.State;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Rank:</b> {state.rankId}");
            GUILayout.Label($"<b>Trust:</b> {state.trustScore}");
            GUILayout.EndHorizontal();

            // Show key certifications
            var certs = state.certsById.Values
                .Where(c => c != null && c.isActive)
                .Select(c => c.certId)
                .ToList();

            if (certs.Count > 0)
            {
                var certList = string.Join(", ", certs.Take(5));
                if (certs.Count > 5)
                    certList += $" +{certs.Count - 5} more";
                GUILayout.Label($"<b>Certs:</b> {certList}");
            }
        }

        private void DrawMissionEntry(PlayerMissionState state, MissionService missionService)
        {
            var def = missionService.GetMissionDefinition(state.missionId);
            if (def == null)
                return;

            GUILayout.BeginVertical(GUI.skin.box);

            // Title with time remaining
            var timeRemaining = missionService.GetTimeRemaining(state.missionId);
            if (timeRemaining > 0)
            {
                var timeColor = timeRemaining < 60 ? "#FF4444" : (timeRemaining < 180 ? "#FFAA44" : "#AAFFAA");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color=#FFD700><b>{def.displayName}</b></color>");
                GUILayout.FlexibleSpace();
                GUILayout.Label($"<color={timeColor}><b>{MissionService.FormatTime((int)timeRemaining)}</b></color>");
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label($"<color=#FFD700><b>{def.displayName}</b></color>");
            }

            GUILayout.Label($"<size=11>{def.description}</size>");
            GUILayout.Space(5);

            // Objectives
            foreach (var objDef in def.objectives.OrderBy(o => o.order))
            {
                var progress = state.objectiveProgress.FirstOrDefault(p => p.objectiveId == objDef.objectiveId);
                var current = progress?.currentCount ?? 0;
                var isComplete = progress?.isComplete ?? false;

                var checkmark = isComplete ? "✓" : "○";
                var color = isComplete ? "#88FF88" : (objDef.optional ? "#AAAAAA" : "#FFFFFF");
                var optionalTag = objDef.optional ? " (optional)" : "";

                GUILayout.Label($"  <color={color}>{checkmark} {objDef.description} ({current}/{objDef.requiredCount}){optionalTag}</color>");
            }

            // Rewards preview
            if (def.rewards != null)
            {
                var rewards = new List<string>();
                if (def.rewards.trustPoints > 0)
                    rewards.Add($"+{def.rewards.trustPoints} Trust");
                if (!string.IsNullOrWhiteSpace(def.rewards.grantCertification))
                    rewards.Add($"Cert: {def.rewards.grantCertification}");

                if (rewards.Count > 0)
                {
                    GUILayout.Space(3);
                    GUILayout.Label($"<size=10><color=#AAFFAA>Rewards: {string.Join(", ", rewards)}</color></size>");
                }
            }

            // Abandon button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Abandon", GUILayout.Width(80)))
            {
                missionService.AbandonMission(state.missionId);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawAvailableMissionEntry(MissionDefinition def, MissionService missionService)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Title with difficulty
            var difficultyStars = new string('★', Mathf.Clamp(def.difficulty, 1, 5));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{def.displayName}</b>");
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<color=#FFDD44>{difficultyStars}</color>");
            GUILayout.EndHorizontal();

            GUILayout.Label($"<size=11>{def.description}</size>");

            // Time limit and category
            GUILayout.BeginHorizontal();
            if (def.timeLimit > 0)
            {
                GUILayout.Label($"<size=10>Time: {MissionService.FormatTime((int)def.timeLimit)}</size>");
            }
            if (!string.IsNullOrWhiteSpace(def.category))
            {
                GUILayout.Label($"<size=10>Category: {def.category}</size>");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Requirements
            if (def.requiredCertifications.Count > 0)
            {
                GUILayout.Label($"<size=10><color=#FFAAAA>Requires: {string.Join(", ", def.requiredCertifications)}</color></size>");
            }

            // Rewards preview
            var rewards = new List<string>();
            if (def.rewards.trustPoints > 0)
                rewards.Add($"+{def.rewards.trustPoints} Trust");
            if (def.rewards.credits > 0)
                rewards.Add($"{def.rewards.credits} Credits");
            if (!string.IsNullOrWhiteSpace(def.rewards.grantCertification))
                rewards.Add($"Cert: {def.rewards.grantCertification}");

            if (rewards.Count > 0)
            {
                GUILayout.Label($"<size=10><color=#AAFFAA>Rewards: {string.Join(", ", rewards)}</color></size>");
            }

            // Accept button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Accept", GUILayout.Width(80)))
            {
                missionService.AcceptMission(def.missionId);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }
}
