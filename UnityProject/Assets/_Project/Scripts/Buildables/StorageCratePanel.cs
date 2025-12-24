using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Gameplay;
using UnityEngine;

namespace Frontline.Buildables
{
    /// <summary>
    /// Minimal IMGUI world container UI for StorageCrate (Milestone 5).
    /// - Resource-only storage (mat_*) for v1.
    /// - Transfer All both directions.
    /// </summary>
    public sealed class StorageCratePanel : MonoBehaviour
    {
        public static StorageCratePanel Instance { get; private set; }

        [SerializeField] private bool visible;

        private StorageCrate _active;
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

        public void Open(StorageCrate crate)
        {
            _active = crate;
            visible = crate != null;
        }

        public void Close()
        {
            visible = false;
            _active = null;
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            if (Input.GetKeyDown(KeyCode.Escape))
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
            GUILayout.Label("Storage Crate");

            GUILayout.Space(6);
            GUILayout.Label($"Capacity: slots {_active.SlotsUsed}/{_active.MaxSlots} | count {_active.TotalCount}/{_active.MaxTotalCount}");

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Transfer All: Player → Crate", GUILayout.Width(220)))
                TransferAllPlayerToCrate();
            if (GUILayout.Button("Transfer All: Crate → Player", GUILayout.Width(220)))
                TransferAllCrateToPlayer();
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
    }
}

