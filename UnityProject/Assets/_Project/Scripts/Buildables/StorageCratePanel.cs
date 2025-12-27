using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.UI;
using UnityEngine;

namespace Frontline.Buildables
{
    /// <summary>
    /// IMGUI world container UI for StorageCrate.
    /// Milestone 7.5: Added click-to-transfer (Shift+Click moves entire stack).
    /// </summary>
    public sealed class StorageCratePanel : MonoBehaviour
    {
        private const string ModalId = "storage_crate";

        public static StorageCratePanel Instance { get; private set; }

        [SerializeField] private bool visible;

        private StorageCrate _active;
        private Vector2 _scrollLeft;
        private Vector2 _scrollRight;

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
            var panelWidth = Mathf.Min(720, Screen.width - 20);
            var panelHeight = Mathf.Min(580, Screen.height - 20);
            var rect = new Rect((Screen.width - panelWidth) * 0.5f, pad, panelWidth, panelHeight);

            GUILayout.BeginArea(rect, GUI.skin.window);

            // Milestone 7.3: Label editing.
            DrawLabelSection();

            GUILayout.Space(6);
            // Milestone 7.5: Weight-based capacity display with correct slot/count semantics.
            var weightStr = $"{_active.CurrentWeight:F1}/{_active.MaxWeight:F0} kg";
            var slotsStr = $"types {_active.SlotsUsed}/{_active.MaxSlots}";
            var qtyStr = $"items {_active.TotalQuantity}";
            GUILayout.Label($"Crate Capacity: {weightStr} | {slotsStr} | {qtyStr}");

            if (_active.IsOverWeight)
            {
                var prevColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label("⚠ Over weight capacity!");
                GUI.color = prevColor;
            }

            GUILayout.Space(4);
            GUILayout.Label("Shift+Click item to quick-transfer | Click buttons to transfer amounts");

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Transfer All →", GUILayout.Width(120)))
                TransferAllPlayerToCrate();
            if (GUILayout.Button("← Transfer All", GUILayout.Width(120)))
                TransferAllCrateToPlayer();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button($"Upgrade (Lv.{_active.UpgradeLevel})", GUILayout.Width(120)))
            {
                _active.TryUpgrade();
                if (BuildablesService.Instance != null)
                    BuildablesService.Instance.MarkDirty();
            }
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Destroy", GUILayout.Width(70)))
            {
                _active.DestroyCrate();
                Close();
            }
            GUI.backgroundColor = prevBg;
            if (GUILayout.Button("Close", GUILayout.Width(60)))
                Close();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Milestone 7.5: Two-panel layout (Player | Crate).
            GUILayout.BeginHorizontal();

            // Left panel: Player inventory.
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(340));
            GUILayout.Label("Player Inventory");
            _scrollLeft = GUILayout.BeginScrollView(_scrollLeft, GUILayout.Height(380));

            foreach (var id in ResourceOrder)
            {
                var amt = PlayerInventoryService.Instance.GetResource(id);
                if (amt <= 0) continue;

                GUILayout.BeginHorizontal();
                // Shift+Click to transfer all.
                if (GUILayout.Button($"{id}: {amt}", GUILayout.Width(180)))
                {
                    if (Event.current.shift)
                        MovePlayerToCrate(id, amt);
                    else
                        MovePlayerToCrate(id, 1);
                }
                if (GUILayout.Button("+5", GUILayout.Width(40)))
                    MovePlayerToCrate(id, 5);
                if (GUILayout.Button("All", GUILayout.Width(40)))
                    MovePlayerToCrate(id, amt);
                GUILayout.EndHorizontal();
            }

            // Show empty slots.
            var playerEmpty = true;
            foreach (var id in ResourceOrder)
            {
                if (PlayerInventoryService.Instance.GetResource(id) > 0)
                {
                    playerEmpty = false;
                    break;
                }
            }
            if (playerEmpty)
                GUILayout.Label("(no resources)");

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Right panel: Crate inventory.
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(340));
            GUILayout.Label("Crate Contents");
            _scrollRight = GUILayout.BeginScrollView(_scrollRight, GUILayout.Height(380));

            var items = _active.Items.OrderBy(kv => kv.Key).ToList();
            if (items.Count == 0)
                GUILayout.Label("(empty)");

            foreach (var kv in items)
            {
                GUILayout.BeginHorizontal();
                // Shift+Click to transfer all.
                if (GUILayout.Button($"{kv.Key}: {kv.Value}", GUILayout.Width(180)))
                {
                    if (Event.current.shift)
                        MoveCrateToPlayer(kv.Key, kv.Value);
                    else
                        MoveCrateToPlayer(kv.Key, 1);
                }
                if (GUILayout.Button("-5", GUILayout.Width(40)))
                    MoveCrateToPlayer(kv.Key, 5);
                if (GUILayout.Button("All", GUILayout.Width(40)))
                    MoveCrateToPlayer(kv.Key, kv.Value);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Show transfer errors.
            InventoryTransferService.DrawErrorIfAny();

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

