using System.Collections.Generic;
using Frontline.Missions;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// UI panel for interacting with mission terminals.
    /// Displays available missions in a scrollable list with details and accept functionality.
    /// Inspired by Star Wars Galaxies mission terminal interface.
    /// </summary>
    public sealed class MissionTerminalPanel : MonoBehaviour
    {
        public static MissionTerminalPanel Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private KeyCode _closeKey = KeyCode.Escape;

        /// <summary>Whether the terminal panel is currently open.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>The current terminal being viewed.</summary>
        public MissionTerminal CurrentTerminal { get; private set; }

        private MissionTerminalEntry[] _missions;
        private int _selectedIndex = -1;
        private Vector2 _listScrollPos;
        private Vector2 _detailScrollPos;

        // UI state
        private Rect _windowRect;
        private bool _initialized;
        private string _filterCategory = "";

        // Styles
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _selectedButtonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _descriptionStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Center window
            var width = 800f;
            var height = 550f;
            _windowRect = new Rect(
                (Screen.width - width) / 2f,
                (Screen.height - height) / 2f,
                width,
                height
            );
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            // Close on escape
            if (Input.GetKeyDown(_closeKey))
            {
                Close();
            }
        }

        /// <summary>
        /// Opens the terminal panel for a specific terminal.
        /// </summary>
        public void OpenForTerminal(MissionTerminal terminal)
        {
            if (terminal == null)
                return;

            CurrentTerminal = terminal;
            RefreshMissions();
            _selectedIndex = _missions.Length > 0 ? 0 : -1;
            _listScrollPos = Vector2.zero;
            _detailScrollPos = Vector2.zero;
            _filterCategory = "";
            IsOpen = true;

            Debug.Log($"MissionTerminalPanel: Opened for '{terminal.DisplayName}'");
        }

        /// <summary>
        /// Closes the terminal panel.
        /// </summary>
        public void Close()
        {
            IsOpen = false;
            CurrentTerminal = null;
            Debug.Log("MissionTerminalPanel: Closed");
        }

        /// <summary>
        /// Refreshes the mission list from the current terminal.
        /// </summary>
        public void RefreshMissions()
        {
            if (CurrentTerminal == null)
            {
                _missions = new MissionTerminalEntry[0];
                return;
            }

            CurrentTerminal.RefreshMissionCache();
            _missions = CurrentTerminal.GetMissionEntries();
        }

        private void InitStyles()
        {
            if (_initialized)
                return;

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 14
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 6, 6)
            };

            _selectedButtonStyle = new GUIStyle(_buttonStyle)
            {
                normal = { textColor = Color.yellow },
                fontStyle = FontStyle.Bold
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };

            _descriptionStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 8, 8)
            };

            _initialized = true;
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;

            InitStyles();

            // Draw semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Draw main window
            _windowRect = GUI.Window(9001, _windowRect, DrawWindow, "", _windowStyle);
        }

        private void DrawWindow(int windowId)
        {
            var terminalName = CurrentTerminal != null ? CurrentTerminal.DisplayName : "Mission Terminal";

            // Header
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(terminalName, _headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                Close();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Category filter
            DrawCategoryFilter();

            GUILayout.Space(5);

            // Main content area
            GUILayout.BeginHorizontal();

            // Left panel: Mission list
            DrawMissionList();

            GUILayout.Space(10);

            // Right panel: Mission details
            DrawMissionDetails();

            GUILayout.EndHorizontal();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 30));
        }

        private void DrawCategoryFilter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(45));

            var categories = GetAvailableCategories();
            foreach (var cat in categories)
            {
                var isSelected = _filterCategory == cat;
                var displayName = string.IsNullOrEmpty(cat) ? "All" : CapitalizeFirst(cat);

                if (GUILayout.Toggle(isSelected, displayName, GUI.skin.button, GUILayout.Width(80)))
                {
                    if (!isSelected)
                    {
                        _filterCategory = cat;
                        _selectedIndex = 0;
                    }
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{GetFilteredMissions().Count} missions available");
            GUILayout.EndHorizontal();
        }

        private List<string> GetAvailableCategories()
        {
            var categories = new List<string> { "" }; // "All" option

            if (_missions == null)
                return categories;

            foreach (var m in _missions)
            {
                if (!string.IsNullOrEmpty(m.category) && !categories.Contains(m.category))
                {
                    categories.Add(m.category);
                }
            }

            return categories;
        }

        private List<MissionTerminalEntry> GetFilteredMissions()
        {
            if (_missions == null)
                return new List<MissionTerminalEntry>();

            var filtered = new List<MissionTerminalEntry>();
            foreach (var m in _missions)
            {
                if (string.IsNullOrEmpty(_filterCategory) ||
                    string.Equals(m.category, _filterCategory, System.StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(m);
                }
            }

            return filtered;
        }

        private void DrawMissionList()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300), GUILayout.ExpandHeight(true));
            GUILayout.Label("Available Missions", _labelStyle);
            GUILayout.Space(5);

            _listScrollPos = GUILayout.BeginScrollView(_listScrollPos);

            var filtered = GetFilteredMissions();
            for (int i = 0; i < filtered.Count; i++)
            {
                var mission = filtered[i];
                var isSelected = _selectedIndex == i;
                var style = isSelected ? _selectedButtonStyle : _buttonStyle;

                // Mission button with difficulty indicator
                var buttonText = $"{mission.DifficultyStars} {mission.displayName}";
                if (mission.isRepeatable && mission.completionCount > 0)
                {
                    buttonText += $" ({mission.completionCount}x)";
                }

                if (GUILayout.Button(buttonText, style))
                {
                    _selectedIndex = i;
                    _detailScrollPos = Vector2.zero;
                }
            }

            if (filtered.Count == 0)
            {
                GUILayout.Label("No missions available", _labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawMissionDetails()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Label("Mission Details", _labelStyle);
            GUILayout.Space(5);

            var filtered = GetFilteredMissions();

            if (_selectedIndex >= 0 && _selectedIndex < filtered.Count)
            {
                var mission = filtered[_selectedIndex];

                _detailScrollPos = GUILayout.BeginScrollView(_detailScrollPos);

                // Mission title
                var titleStyle = new GUIStyle(_headerStyle)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft
                };
                GUILayout.Label(mission.displayName, titleStyle);
                GUILayout.Space(5);

                // Category and difficulty
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Category: {CapitalizeFirst(mission.category)}", _labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Difficulty: {mission.DifficultyStars}", _labelStyle);
                GUILayout.EndHorizontal();

                // Time limit
                GUILayout.Label($"Time Limit: {mission.FormattedTimeLimit}", _labelStyle);

                if (mission.isRepeatable)
                {
                    GUILayout.Label($"Repeatable (Completed: {mission.completionCount}x)", _labelStyle);
                }

                GUILayout.Space(10);

                // Description
                GUILayout.Label("Description:", _labelStyle);
                GUILayout.Label(mission.description, _descriptionStyle, GUILayout.MinHeight(80));

                GUILayout.Space(10);

                // Rewards
                GUILayout.Label("Rewards:", _labelStyle);
                DrawRewardsBox(mission);

                GUILayout.Space(10);

                // Objectives preview
                DrawObjectivesPreview(mission.missionId);

                GUILayout.EndScrollView();

                GUILayout.FlexibleSpace();

                // Accept button
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.enabled = true;
                var acceptStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };

                if (GUILayout.Button("Accept Mission", acceptStyle, GUILayout.Width(150), GUILayout.Height(35)))
                {
                    AcceptSelectedMission();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Select a mission to view details", _labelStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawRewardsBox(MissionTerminalEntry mission)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            var def = MissionService.Instance?.GetMissionDefinition(mission.missionId);
            if (def?.rewards != null)
            {
                var rewards = def.rewards;

                if (rewards.trustPoints > 0)
                    GUILayout.Label($"  • +{rewards.trustPoints} Trust Points", _labelStyle);

                if (rewards.credits > 0)
                    GUILayout.Label($"  • {rewards.credits} Credits", _labelStyle);

                if (rewards.experience > 0)
                    GUILayout.Label($"  • {rewards.experience} Experience", _labelStyle);

                if (!string.IsNullOrWhiteSpace(rewards.grantCertification))
                    GUILayout.Label($"  • Certification: {rewards.grantCertification}", _labelStyle);

                if (rewards.items != null && rewards.items.Count > 0)
                {
                    foreach (var item in rewards.items)
                    {
                        GUILayout.Label($"  • {item.quantity}x {item.itemId}", _labelStyle);
                    }
                }
            }
            else
            {
                GUILayout.Label($"  {mission.rewards}", _labelStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawObjectivesPreview(string missionId)
        {
            var def = MissionService.Instance?.GetMissionDefinition(missionId);
            if (def?.objectives == null || def.objectives.Count == 0)
                return;

            GUILayout.Label("Objectives:", _labelStyle);
            GUILayout.BeginVertical(GUI.skin.box);

            foreach (var obj in def.objectives)
            {
                var optional = obj.optional ? " (optional)" : "";
                GUILayout.Label($"  • {obj.description}{optional}", _labelStyle);

                // Show location if available
                if (obj.location != null && obj.location.IsValid)
                {
                    var loc = obj.location;
                    GUILayout.Label($"      Location: ({loc.x:F0}, {loc.y:F0}, {loc.z:F0})", _labelStyle);
                }
            }

            GUILayout.EndVertical();
        }

        private void AcceptSelectedMission()
        {
            var filtered = GetFilteredMissions();
            if (_selectedIndex < 0 || _selectedIndex >= filtered.Count)
                return;

            var mission = filtered[_selectedIndex];

            if (CurrentTerminal != null)
            {
                var success = CurrentTerminal.AcceptMission(mission.missionId);
                if (success)
                {
                    Debug.Log($"MissionTerminalPanel: Accepted mission '{mission.displayName}'");
                    RefreshMissions();

                    // Select next available mission or close if none
                    if (_missions.Length == 0)
                    {
                        Close();
                    }
                    else
                    {
                        _selectedIndex = Mathf.Min(_selectedIndex, _missions.Length - 1);
                    }
                }
            }
        }

        private static string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
