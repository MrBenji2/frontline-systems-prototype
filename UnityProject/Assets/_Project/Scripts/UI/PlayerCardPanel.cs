using System.Collections.Generic;
using System.Linq;
using Frontline.PlayerCard;
using Frontline.Trust;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// UI panel displaying the Player Card with comprehensive stats,
    /// rank, certifications, and name history.
    /// Toggle with P key.
    /// </summary>
    public sealed class PlayerCardPanel : MonoBehaviour
    {
        public static PlayerCardPanel Instance { get; private set; }

        [SerializeField] private KeyCode toggleKey = KeyCode.P;

        private bool _visible;
        private Vector2 _scroll;
        private int _selectedTab;
        private string _newNameInput = "";
        private string _nameChangeMessage = "";
        private float _messageTimer;

        private readonly string[] _tabNames = { "Overview", "Combat", "Logistics", "Leadership", "History" };

        public bool IsVisible => _visible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                _visible = !_visible;

            if (_messageTimer > 0)
                _messageTimer -= Time.deltaTime;
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
            {
                DrawCenteredMessage("PlayerStatsService not available");
                return;
            }

            var card = statsService.BuildPlayerCard();

            var panelWidth = Mathf.Min(550, Screen.width - 40);
            var panelHeight = Mathf.Min(600, Screen.height - 40);
            var x = (Screen.width - panelWidth) / 2f;
            var y = (Screen.height - panelHeight) / 2f;

            var rect = new Rect(x, y, panelWidth, panelHeight);
            GUI.Box(rect, "");

            GUILayout.BeginArea(rect);

            // Header
            DrawHeader(card, statsService);

            // Tabs
            GUILayout.Space(5);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            GUILayout.Space(10);

            // Content
            _scroll = GUILayout.BeginScrollView(_scroll);

            switch (_selectedTab)
            {
                case 0:
                    DrawOverviewTab(card, statsService);
                    break;
                case 1:
                    DrawCombatTab(card.stats);
                    break;
                case 2:
                    DrawLogisticsTab(card.stats);
                    break;
                case 3:
                    DrawLeadershipTab(card.stats);
                    break;
                case 4:
                    DrawHistoryTab(card, statsService);
                    break;
            }

            GUILayout.EndScrollView();

            // Close hint
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=10><color=#888888>[P] Close</color></size>");

            GUILayout.EndArea();
        }

        private void DrawHeader(PlayerCardData card, PlayerStatsService statsService)
        {
            GUILayout.BeginHorizontal();

            // Player avatar placeholder
            GUILayout.Box("", GUILayout.Width(60), GUILayout.Height(60));

            GUILayout.BeginVertical();

            // Name and rank
            GUILayout.Label($"<size=20><b>{card.stats.displayName}</b></size>");
            GUILayout.Label($"<color=#FFD700>{card.rankDisplayName}</color> ({card.rankId})");

            // Trust and faction
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Trust: <b>{card.trustScore}</b>");
            GUILayout.Label($"Faction: <b>{card.faction}</b>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // Imprisonment warning
            if (card.isImprisoned)
            {
                GUILayout.Space(5);
                GUILayout.Label("<color=#FF4444><b>⚠ IMPRISONED</b></color>");
            }
        }

        private void DrawOverviewTab(PlayerCardData card, PlayerStatsService statsService)
        {
            var stats = card.stats;

            // Quick stats
            GUILayout.Label("<b>Quick Stats</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Time Served", statsService.GetFormattedTimeServed());
            DrawStatRow("K/D Ratio", $"{statsService.GetKDRatio():F2}");
            DrawStatRow("Kills", stats.kills.ToString());
            DrawStatRow("Deaths", stats.deaths.ToString());
            DrawStatRow("Missions Completed", stats.missionsCompleted.ToString());
            DrawStatRow("Structures Built", stats.structuresBuilt.ToString());
            DrawStatRow("Resources Delivered", FormatNumber(stats.resourcesDelivered));

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Active Certifications
            GUILayout.Label("<b>Active Certifications</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            if (card.activeCertifications.Count == 0)
            {
                GUILayout.Label("<i>No active certifications</i>");
            }
            else
            {
                foreach (var certId in card.activeCertifications.Take(10))
                {
                    GUILayout.Label($"• {certId}");
                }

                if (card.activeCertifications.Count > 10)
                {
                    GUILayout.Label($"<i>... and {card.activeCertifications.Count - 10} more</i>");
                }
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Medals
            if (stats.medals.Count > 0)
            {
                GUILayout.Label("<b>Medals</b>");
                GUILayout.BeginVertical(GUI.skin.box);

                foreach (var medal in stats.medals)
                {
                    GUILayout.Label($"🏅 {medal}");
                }

                GUILayout.EndVertical();
            }
        }

        private void DrawCombatTab(PlayerStats stats)
        {
            GUILayout.Label("<b>Combat Statistics</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Total Kills", stats.kills.ToString());
            DrawStatRow("  Player Kills", stats.playerKills.ToString());
            DrawStatRow("  NPC Kills", stats.npcKills.ToString());
            DrawStatRow("Deaths", stats.deaths.ToString());
            DrawStatRow("Damage Dealt", FormatNumber(stats.damageDealt));
            DrawStatRow("Damage Taken", FormatNumber(stats.damageTaken));

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>Medical</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Revives", stats.revives.ToString());
            DrawStatRow("Healing Done", FormatNumber(stats.healingDone));

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>Discipline</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            var ffColor = stats.friendlyFireIncidents > 5 ? "#FF6666" : "#FFFFFF";
            DrawStatRow("Friendly Fire Incidents", $"<color={ffColor}>{stats.friendlyFireIncidents}</color>");

            var griefColor = stats.griefingReportsUpheld > 0 ? "#FF6666" : "#FFFFFF";
            DrawStatRow("Griefing Reports", $"<color={griefColor}>{stats.griefingReportsReceived} ({stats.griefingReportsUpheld} upheld)</color>");

            GUILayout.EndVertical();
        }

        private void DrawLogisticsTab(PlayerStats stats)
        {
            GUILayout.Label("<b>Gathering & Delivery</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Resources Gathered", FormatNumber(stats.resourcesGathered));
            DrawStatRow("Resources Delivered", FormatNumber(stats.resourcesDelivered));
            DrawStatRow("Supply Runs Completed", stats.supplyRunsCompleted.ToString());
            DrawStatRow("Cargo Distance", $"{stats.cargoDistanceTraveled / 1000f:F1} km");

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>Engineering</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Structures Built", stats.structuresBuilt.ToString());
            DrawStatRow("Structures Repaired", stats.structuresRepaired.ToString());
            DrawStatRow("Structures Demolished", stats.structuresDemolished.ToString());
            DrawStatRow("Fortifications Upgraded", stats.fortificationsUpgraded.ToString());

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>Vehicles</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Distance Driven", $"{stats.distanceDriven / 1000f:F1} km");
            DrawStatRow("Vehicles Operated", stats.vehiclesOperated.ToString());
            DrawStatRow("Vehicle Collisions", stats.vehicleCollisions.ToString());

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>Missions</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Missions Completed", stats.missionsCompleted.ToString());
            DrawStatRow("Training Completed", stats.trainingMissionsCompleted.ToString());
            DrawStatRow("Missions Abandoned", stats.missionsAbandoned.ToString());
            DrawStatRow("Missions Failed", stats.missionsFailed.ToString());

            GUILayout.EndVertical();
        }

        private void DrawLeadershipTab(PlayerStats stats)
        {
            GUILayout.Label("<b>Command</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Orders Issued", stats.ordersIssued.ToString());
            DrawStatRow("Squad Members Commanded", stats.squadMembersCommanded.ToString());
            DrawStatRow("Largest Division Size", stats.largestDivisionSize.ToString());

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>Reputation</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Commendations Received", stats.commendationsReceived.ToString());
            DrawStatRow("Commendations Given", stats.commendationsGiven.ToString());

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>War Record</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Wars Participated", stats.warsParticipated.ToString());
            DrawStatRow("Wars Won", stats.warsWon.ToString());
            var winRate = stats.warsParticipated > 0
                ? (float)stats.warsWon / stats.warsParticipated * 100f
                : 0f;
            DrawStatRow("Win Rate", $"{winRate:F0}%");

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>Discipline Record</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            var prisonColor = stats.timesImprisoned > 0 ? "#FF6666" : "#88FF88";
            DrawStatRow("Times Imprisoned", $"<color={prisonColor}>{stats.timesImprisoned}</color>");
            DrawStatRow("Prison Time Served", $"{stats.prisonTimeServedHours:F1} hours");
            DrawStatRow("Certifications Revoked", stats.certificationsRevoked.ToString());

            GUILayout.EndVertical();
        }

        private void DrawHistoryTab(PlayerCardData card, PlayerStatsService statsService)
        {
            var stats = card.stats;

            // Account info
            GUILayout.Label("<b>Account Information</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            DrawStatRow("Player ID", stats.playerId.Substring(0, Mathf.Min(8, stats.playerId.Length)) + "...");
            DrawStatRow("Account Created", FormatTimestamp(stats.createdUtc));
            DrawStatRow("Current Name", stats.displayName);

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Name change
            GUILayout.Label("<b>Change Name</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("New name:", GUILayout.Width(80));
            _newNameInput = GUILayout.TextField(_newNameInput, 24);

            if (GUILayout.Button("Change", GUILayout.Width(80)))
            {
                if (statsService.ChangeName(_newNameInput))
                {
                    _nameChangeMessage = "Name changed successfully!";
                    _messageTimer = 3f;
                    _newNameInput = "";
                }
                else
                {
                    _nameChangeMessage = "Invalid name (2-24 characters required)";
                    _messageTimer = 3f;
                }
            }

            GUILayout.EndHorizontal();

            if (_messageTimer > 0 && !string.IsNullOrEmpty(_nameChangeMessage))
            {
                var color = _nameChangeMessage.Contains("success") ? "#88FF88" : "#FF8888";
                GUILayout.Label($"<color={color}>{_nameChangeMessage}</color>");
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Name history
            GUILayout.Label("<b>Name History</b>");
            GUILayout.BeginVertical(GUI.skin.box);

            var history = statsService.GetNameHistory();
            if (history.Count == 0)
            {
                GUILayout.Label("<i>No name changes recorded</i>");
            }
            else
            {
                foreach (var record in history.OrderByDescending(r => r.changedUtc).Take(10))
                {
                    var date = FormatTimestamp(record.changedUtc);
                    var adminTag = record.adminForced ? " <color=#FF6666>[ADMIN]</color>" : "";
                    GUILayout.Label($"<b>{record.previousName}</b> → <b>{record.newName}</b>{adminTag}");
                    GUILayout.Label($"  <size=10><color=#888888>{date}</color></size>");
                    if (!string.IsNullOrEmpty(record.reason))
                    {
                        GUILayout.Label($"  <size=10><color=#888888>Reason: {record.reason}</color></size>");
                    }

                    GUILayout.Space(3);
                }

                if (history.Count > 10)
                {
                    GUILayout.Label($"<i>... and {history.Count - 10} more changes</i>");
                }
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Achievements
            if (stats.achievements.Count > 0)
            {
                GUILayout.Label("<b>Achievements</b>");
                GUILayout.BeginVertical(GUI.skin.box);

                foreach (var achievement in stats.achievements)
                {
                    GUILayout.Label($"🏆 {achievement}");
                }

                GUILayout.EndVertical();
            }
        }

        private void DrawStatRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(180));
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }

        private void DrawCenteredMessage(string message)
        {
            var panelWidth = 300f;
            var panelHeight = 100f;
            var x = (Screen.width - panelWidth) / 2f;
            var y = (Screen.height - panelHeight) / 2f;

            GUI.Box(new Rect(x, y, panelWidth, panelHeight), "");
            GUI.Label(new Rect(x + 20, y + 40, panelWidth - 40, 30), message);
        }

        private static string FormatNumber(long value)
        {
            if (value >= 1000000)
                return $"{value / 1000000f:F1}M";
            if (value >= 1000)
                return $"{value / 1000f:F1}K";
            return value.ToString();
        }

        private static string FormatTimestamp(long utcSeconds)
        {
            if (utcSeconds <= 0)
                return "Unknown";

            var dt = System.DateTimeOffset.FromUnixTimeSeconds(utcSeconds);
            return dt.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
