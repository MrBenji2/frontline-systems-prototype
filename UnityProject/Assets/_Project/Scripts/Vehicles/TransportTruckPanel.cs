using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.UI;
using UnityEngine;

namespace Frontline.Vehicles
{
    /// <summary>
    /// Minimal IMGUI world container UI for Transport Truck storage (Milestone 6).
    /// Close rules:
    /// - Esc closes via UiModalManager (openedByInteract: false).
    /// - F toggles (closes) when opened by F.
    /// </summary>
    public sealed class TransportTruckPanel : MonoBehaviour
    {
        private const string ModalId = "truck_storage";

        public static TransportTruckPanel Instance { get; private set; }

        [SerializeField] private bool visible;

        private TransportTruckController _active;
        private Vector2 _scroll;

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

            // Esc closes via UiModalManager.
            if (Input.GetKeyDown(KeyCode.F))
                Close();
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;
            if (PlayerInventoryService.Instance == null)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(560, Screen.width - 20);
            var panelHeight = Mathf.Min(520, Screen.height - 20);
            var rect = new Rect((Screen.width - panelWidth) * 0.5f, pad, panelWidth, panelHeight);

            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("Truck Storage");

            GUILayout.Space(6);
            GUILayout.Label($"Capacity: slots {_active.SlotsUsed}/{_active.MaxSlots} | count {_active.TotalCount}/{_active.MaxTotalCount}");

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Transfer All: Player → Truck", GUILayout.Width(220)))
                TransferAllPlayerToTruck();
            if (GUILayout.Button("Transfer All: Truck → Player", GUILayout.Width(220)))
                TransferAllTruckToPlayer();
            if (GUILayout.Button("Close", GUILayout.Width(80)))
                Close();
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
                    MovePlayerToTruck(id, 1);
                if (GUILayout.Button("+5", GUILayout.Width(50)))
                    MovePlayerToTruck(id, 5);
                if (GUILayout.Button("All", GUILayout.Width(60)))
                    MovePlayerToTruck(id, amt);
                GUI.enabled = prev;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("Truck Contents:");

            var items = _active.Items.OrderBy(kv => kv.Key).ToList();
            if (items.Count == 0)
                GUILayout.Label("(empty)");
            foreach (var kv in items)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{kv.Key}: {kv.Value}", GUILayout.Width(220));
                if (GUILayout.Button("-1", GUILayout.Width(50)))
                    MoveTruckToPlayer(kv.Key, 1);
                if (GUILayout.Button("-5", GUILayout.Width(50)))
                    MoveTruckToPlayer(kv.Key, 5);
                if (GUILayout.Button("All", GUILayout.Width(60)))
                    MoveTruckToPlayer(kv.Key, kv.Value);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
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

