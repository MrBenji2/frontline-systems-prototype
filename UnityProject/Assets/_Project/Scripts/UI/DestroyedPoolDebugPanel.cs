using System;
using System.Collections.Generic;
using System.Linq;
using Frontline.Definitions;
using Frontline.Economy;
using Frontline.DebugTools;
using Frontline.Harvesting;
using Frontline.Combat;
using UnityEngine;

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

            GUILayout.Space(10);
            DrawPoolTables();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
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

