using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.UI;
using UnityEngine;

namespace Frontline.Buildables
{
    /// <summary>
    /// Minimal IMGUI world container UI for StorageCrate (Milestone 5/7.3).
    /// - Resource-only storage (mat_*) for v1.
    /// - Transfer All both directions.
    /// - Milestone 7.3: Added label editing, weight display, upgrade, destroy.
    /// </summary>
    public sealed class StorageCratePanel : MonoBehaviour
    {
        private const string ModalId = "storage_crate";

        public static StorageCratePanel Instance { get; private set; }

        [SerializeField] private bool visible;

        private StorageCrate _active;
        private Vector2 _scroll;

        // Milestone 7.3: Label editing state.
        private bool _editingLabel;
        private string _labelEditBuffer = "";

        private static readonly string[] ResourceOrder =
        {
            ToolRecipes.Wood,
            ToolRecipes.Stone,
            ToolRecipes.Iron,
            ToolRecipes.Coal,
            ToolRecipes.Diesel
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsOpen => visible && _active != null;

        public void Open(StorageCrate crate)
        {
            _active = crate;
            visible = crate != null;

            // Milestone 7.3: Reset label editing state.
            _editingLabel = false;
            _labelEditBuffer = crate != null ? crate.Label : "";

            if (visible && UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterOpen(ModalId, Close, openedByInteract: true);
        }

        public void Close()
        {
            // Milestone 7.3: Save label if editing.
            if (_editingLabel && _active != null)
            {
                _active.Label = _labelEditBuffer;
                if (BuildablesService.Instance != null)
                    BuildablesService.Instance.MarkDirty();
            }

            visible = false;
            _active = null;
            _editingLabel = false;
            _labelEditBuffer = "";

            if (UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterClosed(ModalId);
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            // Universal close rules are handled by UiModalManager (Esc always closes, E toggles interact-opened modals).
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;
            if (PlayerInventoryService.Instance == null)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(600, Screen.width - 20);
            var panelHeight = Mathf.Min(580, Screen.height - 20);
            var rect = new Rect((Screen.width - panelWidth) * 0.5f, pad, panelWidth, panelHeight);

            GUILayout.BeginArea(rect, GUI.skin.window);

            // Milestone 7.3: Label editing.
            DrawLabelSection();

            GUILayout.Space(6);
            // Milestone 7.3: Weight-based capacity display.
            var weightStr = $"{_active.CurrentWeight:F1}/{_active.MaxWeight:F0} kg";
            var slotsStr = $"slots {_active.SlotsUsed}/{_active.MaxSlots}";
            var countStr = $"count {_active.TotalCount}/{_active.MaxTotalCount}";
            GUILayout.Label($"Capacity: {weightStr} | {slotsStr} | {countStr}");

            if (_active.IsOverWeight)
            {
                var prevColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label("⚠ Over weight capacity!");
                GUI.color = prevColor;
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Transfer All: Player → Crate", GUILayout.Width(200)))
                TransferAllPlayerToCrate();
            if (GUILayout.Button("Transfer All: Crate → Player", GUILayout.Width(200)))
                TransferAllCrateToPlayer();
            if (GUILayout.Button("Close", GUILayout.Width(70)))
                Close();
            GUILayout.EndHorizontal();

            // Milestone 7.3: Upgrade and Destroy buttons.
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Upgrade (Lv.{_active.UpgradeLevel})", GUILayout.Width(120)))
            {
                _active.TryUpgrade();
                if (BuildablesService.Instance != null)
                    BuildablesService.Instance.MarkDirty();
            }
            GUILayout.FlexibleSpace();
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Destroy Crate", GUILayout.Width(120)))
            {
                _active.DestroyCrate();
                Close();
            }
            GUI.backgroundColor = prevBg;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Player Resources (mat_*):");
            foreach (var id in ResourceOrder)
            {
                var amt = PlayerInventoryService.Instance.GetResource(id);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{id}: {amt}", GUILayout.Width(220));
                var prev = GUI.enabled;
                GUI.enabled = amt > 0;
                if (GUILayout.Button("+1", GUILayout.Width(50)))
                    MovePlayerToCrate(id, 1);
                if (GUILayout.Button("+5", GUILayout.Width(50)))
                    MovePlayerToCrate(id, 5);
                if (GUILayout.Button("All", GUILayout.Width(60)))
                    MovePlayerToCrate(id, amt);
                GUI.enabled = prev;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("Crate Contents:");

            var items = _active.Items.OrderBy(kv => kv.Key).ToList();
            if (items.Count == 0)
                GUILayout.Label("(empty)");
            foreach (var kv in items)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{kv.Key}: {kv.Value}", GUILayout.Width(220));
                if (GUILayout.Button("-1", GUILayout.Width(50)))
                    MoveCrateToPlayer(kv.Key, 1);
                if (GUILayout.Button("-5", GUILayout.Width(50)))
                    MoveCrateToPlayer(kv.Key, 5);
                if (GUILayout.Button("All", GUILayout.Width(60)))
                    MoveCrateToPlayer(kv.Key, kv.Value);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void TransferAllPlayerToCrate()
        {
            if (_active == null || PlayerInventoryService.Instance == null)
                return;

            foreach (var id in ResourceOrder)
            {
                var amt = PlayerInventoryService.Instance.GetResource(id);
                if (amt > 0)
                    MovePlayerToCrate(id, amt);
            }
        }

        private void TransferAllCrateToPlayer()
        {
            if (_active == null || PlayerInventoryService.Instance == null)
                return;

            // Copy to avoid mutating while iterating.
            var keys = _active.Items.Keys.ToList();
            foreach (var id in keys)
            {
                var amt = _active.GetCount(id);
                if (amt > 0)
                    MoveCrateToPlayer(id, amt);
            }
        }

        private void MovePlayerToCrate(string resourceId, int amount)
        {
            if (_active == null || PlayerInventoryService.Instance == null)
                return;
            if (amount <= 0)
                return;

            var canMove = Mathf.Min(amount, PlayerInventoryService.Instance.GetResource(resourceId));
            if (canMove <= 0)
                return;

            // Respect capacity (if we can't add all, add as much as possible).
            var moved = 0;
            while (moved < canMove && _active.CanAdd(resourceId, 1))
            {
                if (!PlayerInventoryService.Instance.TryRemoveResource(resourceId, 1))
                    break;
                if (!_active.TryAdd(resourceId, 1))
                {
                    PlayerInventoryService.Instance.AddResource(resourceId, 1);
                    break;
                }
                moved++;
            }

            if (moved > 0 && BuildablesService.Instance != null)
                BuildablesService.Instance.MarkDirty();
        }

        private void MoveCrateToPlayer(string resourceId, int amount)
        {
            if (_active == null || PlayerInventoryService.Instance == null)
                return;
            if (amount <= 0)
                return;

            var canMove = Mathf.Min(amount, _active.GetCount(resourceId));
            if (canMove <= 0)
                return;

            if (!_active.TryRemove(resourceId, canMove))
                return;
            PlayerInventoryService.Instance.AddResource(resourceId, canMove);

            if (BuildablesService.Instance != null)
                BuildablesService.Instance.MarkDirty();
        }

        /// <summary>
        /// Milestone 7.3: Draw the label editing section.
        /// </summary>
        private void DrawLabelSection()
        {
            GUILayout.BeginHorizontal();

            if (_editingLabel)
            {
                _labelEditBuffer = GUILayout.TextField(_labelEditBuffer, 32, GUILayout.Width(280));
                if (GUILayout.Button("Save", GUILayout.Width(60)))
                {
                    _active.Label = _labelEditBuffer;
                    _editingLabel = false;
                    if (BuildablesService.Instance != null)
                        BuildablesService.Instance.MarkDirty();
                }
                if (GUILayout.Button("Cancel", GUILayout.Width(60)))
                {
                    _labelEditBuffer = _active.Label;
                    _editingLabel = false;
                }
            }
            else
            {
                GUILayout.Label(_active.Label, GUILayout.Width(280));
                if (GUILayout.Button("Rename", GUILayout.Width(70)))
                {
                    _labelEditBuffer = _active.Label;
                    _editingLabel = true;
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}

