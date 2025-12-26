using System;
using System.Collections.Generic;
using System.Linq;
using Frontline.Buildables;
using Frontline.Definitions;
using Frontline.Economy;
using Frontline.DebugTools;
using Frontline.Harvesting;
using Frontline.Combat;
using UnityEngine;
using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.Vehicles;
using Frontline.World;

namespace Frontline.UI
{
    /// <summary>
    /// Lightweight live panel to inspect the DestroyedPool and run dev seeding actions.
    /// (IMGUI to avoid any editor-created UI assets.)
    /// </summary>
    public sealed class DestroyedPoolDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private Vector2 _panelScroll;
        private Vector2 _scroll;
        private Vector2 _scrollCreated;
        private Vector2 _scrollSalvage;
        private string _spawnDestroyId = "wpn_rifle";
        private int _spawnDestroyCount = 1;

        // Patch 3: dev spawn per item (resources/tools) into player inventory.
        private int _devSpawnSelectedIdx;
        private string _devSpawnQtyStr = "1";
        private Vector2 _devSpawnScroll;
        private readonly List<string> _devSpawnIds = new();

        private readonly List<string> _cachedIds = new();
        private float _nextIdRefreshTime;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;

            if (Time.unscaledTime >= _nextIdRefreshTime)
            {
                RefreshIds();
                _nextIdRefreshTime = Time.unscaledTime + 1.0f;
            }
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(520, Screen.width - 20);
            var panelHeight = Mathf.Min(720, Screen.height - 20);
            var rect = new Rect(pad, pad, panelWidth, panelHeight);
            GUILayout.BeginArea(rect, GUI.skin.window);
            _panelScroll = GUILayout.BeginScrollView(_panelScroll);
            GUILayout.Label("DestroyedPool Debug (F1 to toggle)");

            if (DestroyedPoolService.Instance == null)
            {
                GUILayout.Label("DestroyedPoolService: MISSING");
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            if (DefinitionRegistry.Instance == null)
                GUILayout.Label("DefinitionRegistry: MISSING (IDs not validated)");

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Pool", GUILayout.Width(120)))
                DestroyedPoolService.Instance.ResetAll();
            if (GUILayout.Button("Seed: Craft+Destroy ALL (x1)", GUILayout.Width(220)))
                DestroyedPoolService.Instance.SeedAllKnownDefinitionsAsCraftedThenDestroyed(1);
            if (GUILayout.Button("Seed: Craft+Destroy ALL (x5)", GUILayout.Width(220)))
                DestroyedPoolService.Instance.SeedAllKnownDefinitionsAsCraftedThenDestroyed(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Spawn+Destroy (wires Destructible → DestroyedPool)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("ID", GUILayout.Width(22));
            _spawnDestroyId = GUILayout.TextField(_spawnDestroyId, GUILayout.Width(200));
            GUILayout.Label("Count", GUILayout.Width(42));
            var countStr = GUILayout.TextField(_spawnDestroyCount.ToString(), GUILayout.Width(40));
            if (int.TryParse(countStr, out var parsed))
                _spawnDestroyCount = Mathf.Clamp(parsed, 1, 50);
            if (GUILayout.Button("Spawn+Destroy", GUILayout.Width(140)))
            {
                DevSpawnAndDestroy.SpawnAndDestroy(_spawnDestroyId, _spawnDestroyCount);
            }
            if (GUILayout.Button("Mark Crafted", GUILayout.Width(120)))
            {
                DestroyedPoolService.Instance.MarkCrafted(_spawnDestroyId);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Milestone 3 (Harvesting)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Harvest Nodes", GUILayout.Width(180)))
                DevSpawnHarvestNodes.SpawnNodeSetNearPlayer();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            DrawDevSpawnInventory();

            GUILayout.Space(6);
            GUILayout.Label("NPC Spawns:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Easy Ranged"))
                DevSpawnNpcs.Spawn(NpcDifficulty.Easy, NpcAttackType.Ranged, 1);
            if (GUILayout.Button("Easy Melee"))
                DevSpawnNpcs.Spawn(NpcDifficulty.Easy, NpcAttackType.Melee, 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Medium Ranged"))
                DevSpawnNpcs.Spawn(NpcDifficulty.Medium, NpcAttackType.Ranged, 1);
            if (GUILayout.Button("Medium Melee"))
                DevSpawnNpcs.Spawn(NpcDifficulty.Medium, NpcAttackType.Melee, 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hard Ranged"))
                DevSpawnNpcs.Spawn(NpcDifficulty.Hard, NpcAttackType.Ranged, 1);
            if (GUILayout.Button("Hard Melee"))
                DevSpawnNpcs.Spawn(NpcDifficulty.Hard, NpcAttackType.Melee, 1);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Milestone 5 (Buildables):");
            if (BuildablesService.Instance == null)
            {
                GUILayout.Label("BuildablesService: MISSING");
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"Toggle Build Mode ({(BuildablesService.Instance.IsBuildModeActive ? "ON" : "OFF")})", GUILayout.Width(220)))
                    BuildablesService.Instance.ToggleBuildMode();
                if (GUILayout.Button("Save World", GUILayout.Width(120)))
                {
                    BuildablesService.Instance.SaveWorld();
                    if (TransportTruckService.Instance != null)
                        TransportTruckService.Instance.SaveWorld();
                    if (PlayerInventoryService.Instance != null)
                        PlayerInventoryService.Instance.SaveToolsToDisk();
                }
                if (GUILayout.Button("Load World", GUILayout.Width(120)))
                {
                    BuildablesService.Instance.LoadWorld();
                    if (TransportTruckService.Instance != null)
                        TransportTruckService.Instance.LoadWorld();
                    if (PlayerInventoryService.Instance != null)
                        PlayerInventoryService.Instance.LoadToolsFromDisk();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear All Buildables (DEV)", GUILayout.Width(220)))
                    BuildablesService.Instance.ClearAllBuildablesDev();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("Milestone 6 (Transport Truck v1):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Truck", GUILayout.Width(180)))
            {
                if (TransportTruckService.Instance != null)
                    TransportTruckService.Instance.SpawnNearPlayer();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Milestone 6.3 (Safety + Test Tools):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Target Dummy", GUILayout.Width(180)))
                SpawnTargetDummyNearPlayer();
            if (GUILayout.Button("Reset All Dummies", GUILayout.Width(180)))
                ResetAllDummies();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage Truck -25", GUILayout.Width(180)))
                DamageTruckNearestOrOccupied(25);
            if (GUILayout.Button("Repair Truck +25", GUILayout.Width(180)))
                RepairTruckNearestOrOccupied(25);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Trucks", GUILayout.Width(180)))
            {
                if (TransportTruckService.Instance != null)
                    TransportTruckService.Instance.ClearAllTrucks();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Milestone 5.3 (Skills - DEV):");
            if (PlayerSkillsService.Instance == null)
            {
                GUILayout.Label("PlayerSkillsService: MISSING");
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Grant skill_construction_1", GUILayout.Width(220)))
                    PlayerSkillsService.Instance.GrantSkill(SkillIds.Construction1);
                if (GUILayout.Button("Grant skill_construction_2", GUILayout.Width(220)))
                    PlayerSkillsService.Instance.GrantSkill(SkillIds.Construction2);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            DrawPoolTables();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawDevSpawnInventory()
        {
            GUILayout.Label("Patch 5.1 Dev Spawn (per-item → Player Inventory):");

            if (PlayerInventoryService.Instance == null)
            {
                GUILayout.Label("PlayerInventoryService: MISSING");
                return;
            }

            EnsureDevSpawnIds();
            if (_devSpawnIds.Count == 0)
            {
                GUILayout.Label("(no spawnable IDs)");
                return;
            }

            // Quantity
            GUILayout.BeginHorizontal();
            GUILayout.Label("Qty", GUILayout.Width(30));
            _devSpawnQtyStr = GUILayout.TextField(_devSpawnQtyStr, GUILayout.Width(60));
            if (!int.TryParse(_devSpawnQtyStr, out var qty))
                qty = 1;
            qty = Mathf.Clamp(qty, 1, 999);

            var selectedId = _devSpawnIds[Mathf.Clamp(_devSpawnSelectedIdx, 0, _devSpawnIds.Count - 1)];
            if (GUILayout.Button($"Spawn {selectedId} x{qty}", GUILayout.Width(220)))
            {
                SpawnToInventory(selectedId, qty);
                SelectionUIState.SetSelected($"Selected: {selectedId} x{qty}");
            }
            GUILayout.EndHorizontal();

            // List
            _devSpawnScroll = GUILayout.BeginScrollView(_devSpawnScroll, GUILayout.Height(120));
            var newIdx = GUILayout.SelectionGrid(_devSpawnSelectedIdx, _devSpawnIds.ToArray(), 1);
            if (newIdx != _devSpawnSelectedIdx)
            {
                _devSpawnSelectedIdx = newIdx;
                var id = _devSpawnIds[Mathf.Clamp(_devSpawnSelectedIdx, 0, _devSpawnIds.Count - 1)];
                SelectionUIState.SetSelected($"Selected: {id}");
            }
            GUILayout.EndScrollView();
        }

        private static void SpawnTargetDummyNearPlayer()
        {
            var player = Object.FindFirstObjectByType<Frontline.Tactical.TacticalPlayerController>();
            if (player == null)
                return;

            var basePos = player.transform.position + new Vector3(2f, 0f, -2f);
            basePos = SnapToGround(basePos);

            var prefab = Resources.Load<GameObject>("TargetDummy");
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, basePos, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "TargetDummy";
                go.transform.position = basePos;
                go.AddComponent<Health>();
                go.AddComponent<TargetDummy>();
            }
        }

        private static void ResetAllDummies()
        {
            foreach (var d in Object.FindObjectsByType<TargetDummy>(FindObjectsSortMode.None))
            {
                if (d != null)
                    d.ResetToFull();
            }
        }

        private static void DamageTruckNearestOrOccupied(int amount)
        {
            var truck = FindTruckNearestOrOccupied();
            if (truck == null)
                return;
            truck.DebugDamage(amount);
        }

        private static void RepairTruckNearestOrOccupied(int amount)
        {
            var truck = FindTruckNearestOrOccupied();
            if (truck == null)
                return;
            truck.DebugRepair(amount);
        }

        private static TransportTruckController FindTruckNearestOrOccupied()
        {
            // Prefer occupied (so testing while driving is easy).
            foreach (var t in Object.FindObjectsByType<TransportTruckController>(FindObjectsSortMode.None))
            {
                if (t != null && t.IsOccupied)
                    return t;
            }

            var player = Object.FindFirstObjectByType<Frontline.Tactical.TacticalPlayerController>();
            var playerPos = player != null ? player.transform.position : Vector3.zero;
            playerPos.y = 0f;

            TransportTruckController best = null;
            var bestDist = float.MaxValue;
            foreach (var t in Object.FindObjectsByType<TransportTruckController>(FindObjectsSortMode.None))
            {
                if (t == null)
                    continue;
                var p = t.transform.position;
                p.y = 0f;
                var d = Vector3.Distance(playerPos, p);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }
            return best;
        }

        private static Vector3 SnapToGround(Vector3 pos)
        {
            var origin = pos + Vector3.up * 50f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
            {
                pos.y = hit.point.y + 1.0f;
                return pos;
            }
            pos.y = Mathf.Max(pos.y, 1.0f);
            return pos;
        }

        private void EnsureDevSpawnIds()
        {
            if (_devSpawnIds.Count > 0)
                return;

            // Resources first (common debug case), then tools.
            _devSpawnIds.AddRange(new[]
            {
                ToolRecipes.Wood,
                ToolRecipes.Stone,
                ToolRecipes.Iron,
                ToolRecipes.Coal,
                ToolRecipes.Diesel
            });

            foreach (var r in ToolRecipes.All)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.itemId))
                    continue;
                _devSpawnIds.Add(r.itemId);
            }
        }

        private static void SpawnToInventory(string itemId, int qty)
        {
            if (PlayerInventoryService.Instance == null)
                return;
            qty = Mathf.Clamp(qty, 1, 999);

            var recipe = ToolRecipes.Get(itemId);
            if (recipe != null)
            {
                for (var i = 0; i < qty; i++)
                    PlayerInventoryService.Instance.AddTool(recipe.itemId, recipe.maxDurability, recipe.toolType, recipe.tier, recipe.hitDamage);
                return;
            }

            if (itemId != null && itemId.StartsWith("mat_"))
            {
                PlayerInventoryService.Instance.AddResource(itemId, qty);
                return;
            }

            Debug.LogWarning($"DevSpawn: '{itemId}' not supported (ignored).");
        }

        private void DrawPoolTables()
        {
            var destroyed = DestroyedPoolService.Instance.GetAllDestroyedCounts();
            var destroyedButUncrafted = DestroyedPoolService.Instance.GetDestroyedButUncraftedCounts();

            GUILayout.Label("Eligible DestroyedPool (craftedEver && destroyed):");
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(220));

            var ids = _cachedIds.Count > 0
                ? _cachedIds
                : destroyed.Keys.Concat(destroyedButUncrafted.Keys).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();

            foreach (var id in ids)
            {
                var eligibleDestroyed = destroyed.TryGetValue(id, out var d) ? d : 0;
                var uncraftedDestroyed = destroyedButUncrafted.TryGetValue(id, out var u) ? u : 0;
                var craftedEver = DestroyedPoolService.Instance.HasCraftedEver(id);
                var type = "?";
                if (DefinitionRegistry.Instance != null)
                {
                    var resolved = DefinitionRegistry.Instance.GetTypeForId(id);
                    type = string.IsNullOrEmpty(resolved) ? "?" : resolved;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{id} ({type})", GUILayout.Width(260));
                GUILayout.Label($"crafted: {(craftedEver ? "Y" : "N")}", GUILayout.Width(90));
                GUILayout.Label($"pool: {eligibleDestroyed}", GUILayout.Width(80));
                if (uncraftedDestroyed > 0)
                    GUILayout.Label($"uncraftedDestroyed: {uncraftedDestroyed}", GUILayout.Width(160));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(8);
            DrawCreatedPool();

            GUILayout.Space(8);
            DrawSalvagePool();
        }

        private void RefreshIds()
        {
            _cachedIds.Clear();
            if (DefinitionRegistry.Instance == null)
                return;
            _cachedIds.AddRange(DefinitionRegistry.Instance.AllDefinitionIds());
        }

        private void DrawCreatedPool()
        {
            if (CreatedPoolService.Instance == null)
            {
                GUILayout.Label("CreatedPool: MISSING");
                return;
            }

            GUILayout.Label("CreatedPool (crafted tools):");
            _scrollCreated = GUILayout.BeginScrollView(_scrollCreated, GUILayout.Height(140));

            var created = CreatedPoolService.Instance.GetAllCreatedCounts();
            foreach (var kv in created.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(kv.Key, GUILayout.Width(260));
                GUILayout.Label($"created: {kv.Value}", GUILayout.Width(120));
                GUILayout.EndHorizontal();
            }

            if (created.Count == 0)
                GUILayout.Label("(empty)");

            GUILayout.EndScrollView();
        }

        private void DrawSalvagePool()
        {
            if (SalvagePoolService.Instance == null)
            {
                GUILayout.Label("SalvagePool: MISSING");
                return;
            }

            GUILayout.Label("SalvagePool (resource credits from broken tools):");
            _scrollSalvage = GUILayout.BeginScrollView(_scrollSalvage, GUILayout.Height(140));

            var salvage = SalvagePoolService.Instance.GetAllCredits();
            foreach (var kv in salvage.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(kv.Key, GUILayout.Width(260));
                GUILayout.Label($"credits: {kv.Value}", GUILayout.Width(120));
                GUILayout.EndHorizontal();
            }

            if (salvage.Count == 0)
                GUILayout.Label("(empty)");

            GUILayout.EndScrollView();
        }
    }
}

