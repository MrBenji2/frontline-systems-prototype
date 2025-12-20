using System;
using System.Collections.Generic;
using System.Linq;
using Frontline.Definitions;
using Frontline.Economy;
using Frontline.DebugTools;
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

        private Vector2 _scroll;
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
            var rect = new Rect(pad, pad, 520, Screen.height - pad * 2);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("DestroyedPool Debug (F1 to toggle)");

            if (DestroyedPoolService.Instance == null)
            {
                GUILayout.Label("DestroyedPoolService: MISSING");
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
            GUILayout.Label("NPC Spawns (locked until NPC system milestone)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Easy NPC"))
                Debug.Log("NPC system not implemented yet (blocked until DestroyedPool milestone is committed).");
            if (GUILayout.Button("Spawn Medium NPC"))
                Debug.Log("NPC system not implemented yet (blocked until DestroyedPool milestone is committed).");
            if (GUILayout.Button("Spawn Hard NPC"))
                Debug.Log("NPC system not implemented yet (blocked until DestroyedPool milestone is committed).");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            DrawPoolTables();

            GUILayout.EndArea();
        }

        private void DrawPoolTables()
        {
            var destroyed = DestroyedPoolService.Instance.GetAllDestroyedCounts();
            var destroyedButUncrafted = DestroyedPoolService.Instance.GetDestroyedButUncraftedCounts();

            GUILayout.Label("Eligible DestroyedPool (craftedEver && destroyed):");
            _scroll = GUILayout.BeginScrollView(_scroll);

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
        }

        private void RefreshIds()
        {
            _cachedIds.Clear();
            if (DefinitionRegistry.Instance == null)
                return;
            _cachedIds.AddRange(DefinitionRegistry.Instance.AllDefinitionIds());
        }
    }
}

