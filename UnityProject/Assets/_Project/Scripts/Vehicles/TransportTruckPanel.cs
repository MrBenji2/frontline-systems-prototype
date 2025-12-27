using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.UI;
using UnityEngine;

namespace Frontline.Vehicles
{
    /// <summary>
    /// IMGUI world container UI for Transport Truck storage.
    /// Milestone 7.5: Two-panel layout with shift+click to transfer.
    /// </summary>
    public sealed class TransportTruckPanel : MonoBehaviour
    {
        private const string ModalId = "truck_storage";

        public static TransportTruckPanel Instance { get; private set; }

        [SerializeField] private bool visible;

        private TransportTruckController _active;
        private Vector2 _scrollLeft;
        private Vector2 _scrollRight;

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

        public void Open(TransportTruckController truck)
        {
            _active = truck;
            visible = truck != null;

            if (visible && UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterOpen(ModalId, Close, openedByInteract: false);
        }

        public void Close()
        {
            visible = false;
            _active = null;

            if (UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterClosed(ModalId);
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            // Milestone 7.3: E toggles inventory (Esc also closes via UiModalManager).
            if (Input.GetKeyDown(KeyCode.E))
                Close();
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;
            if (PlayerInventoryService.Instance == null)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(720, Screen.width - 20);
            var panelHeight = Mathf.Min(520, Screen.height - 20);
            var rect = new Rect((Screen.width - panelWidth) * 0.5f, pad, panelWidth, panelHeight);

            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("Truck Storage");

            GUILayout.Space(6);
            // Milestone 7.5: Show weight-based capacity and distinct item types.
            var weightStr = $"{_active.CurrentCargoWeight:F1}/{_active.MaxCargoWeight:F0} kg";
            var slotsStr = $"types {_active.SlotsUsed}/{_active.MaxSlots}";
            var qtyStr = $"items {_active.TotalQuantity}";
            GUILayout.Label($"Capacity: {weightStr} | {slotsStr} | {qtyStr}");

            if (_active.IsOverloaded)
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
                TransferAllPlayerToTruck();
            if (GUILayout.Button("← Transfer All", GUILayout.Width(120)))
                TransferAllTruckToPlayer();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80)))
                Close();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Milestone 7.5: Two-panel layout (Player | Truck).
            GUILayout.BeginHorizontal();

            // Left panel: Player inventory.
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(340));
            GUILayout.Label("Player Inventory");
            _scrollLeft = GUILayout.BeginScrollView(_scrollLeft, GUILayout.Height(340));

            foreach (var id in ResourceOrder)
            {
                var amt = PlayerInventoryService.Instance.GetResource(id);
                if (amt <= 0) continue;

                GUILayout.BeginHorizontal();
                // Shift+Click to transfer all.
                if (GUILayout.Button($"{id}: {amt}", GUILayout.Width(180)))
                {
                    if (Event.current.shift)
                        MovePlayerToTruck(id, amt);
                    else
                        MovePlayerToTruck(id, 1);
                }
                if (GUILayout.Button("+5", GUILayout.Width(40)))
                    MovePlayerToTruck(id, 5);
                if (GUILayout.Button("All", GUILayout.Width(40)))
                    MovePlayerToTruck(id, amt);
                GUILayout.EndHorizontal();
            }

            // Show empty message if no resources.
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

            // Right panel: Truck inventory.
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(340));
            GUILayout.Label("Truck Contents");
            _scrollRight = GUILayout.BeginScrollView(_scrollRight, GUILayout.Height(340));

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
                        MoveTruckToPlayer(kv.Key, kv.Value);
                    else
                        MoveTruckToPlayer(kv.Key, 1);
                }
                if (GUILayout.Button("-5", GUILayout.Width(40)))
                    MoveTruckToPlayer(kv.Key, 5);
                if (GUILayout.Button("All", GUILayout.Width(40)))
                    MoveTruckToPlayer(kv.Key, kv.Value);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Show transfer errors.
            InventoryTransferService.DrawErrorIfAny();

            GUILayout.EndArea();
        }

        private void TransferAllPlayerToTruck()
        {
            if (_active == null || PlayerInventoryService.Instance == null)
                return;

            foreach (var id in ResourceOrder)
            {
                var amt = PlayerInventoryService.Instance.GetResource(id);
                if (amt > 0)
                    MovePlayerToTruck(id, amt);
            }
        }

        private void TransferAllTruckToPlayer()
        {
            if (_active == null || PlayerInventoryService.Instance == null)
                return;

            // Copy to avoid mutating while iterating.
            var keys = _active.Items.Keys.ToList();
            foreach (var id in keys)
            {
                var amt = _active.GetCount(id);
                if (amt > 0)
                    MoveTruckToPlayer(id, amt);
            }
        }

        private void MovePlayerToTruck(string resourceId, int amount)
        {
            if (_active == null || PlayerInventoryService.Instance == null)
                return;
            if (amount <= 0)
                return;

            var canMove = Mathf.Min(amount, PlayerInventoryService.Instance.GetResource(resourceId));
            if (canMove <= 0)
                return;

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
        }

        private void MoveTruckToPlayer(string resourceId, int amount)
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
        }
    }
}

