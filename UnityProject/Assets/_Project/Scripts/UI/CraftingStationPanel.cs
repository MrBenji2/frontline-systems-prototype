using System.Collections.Generic;
using System.Linq;
using Frontline.Combat;
using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.Economy;
using Frontline.World;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Minimal IMGUI window for world crafting stations (Milestone 4).
    /// Reuses ToolRecipes + CraftingService; only filters by station type.
    /// </summary>
    public sealed class CraftingStationPanel : MonoBehaviour
    {
        private const string ModalId = "crafting_station";

        public static CraftingStationPanel Instance { get; private set; }

        [SerializeField] private bool visible;

        private CraftingStation _activeStation;
        private Vector2 _scroll;

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

        public bool IsOpen => visible && _activeStation != null;

        public void Open(CraftingStation station)
        {
            _activeStation = station;
            visible = station != null;
            _scroll = Vector2.zero;

            if (visible && UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterOpen(ModalId, Close, openedByInteract: true);
        }

        public void Close()
        {
            visible = false;
            _activeStation = null;

            if (UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterClosed(ModalId);
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            // Universal close rules are handled by UiModalManager.
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;
            if (PlayerInventoryService.Instance == null)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(520, Screen.width - 20);
            var panelHeight = Mathf.Min(720, Screen.height - 20);
            var rect = new Rect((Screen.width - panelWidth) * 0.5f, pad, panelWidth, panelHeight);

            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label($"{_activeStation.DisplayName} (E toggles, Esc closes)");

            GUILayout.Space(6);
            DrawResources();

            GUILayout.Space(6);
            GUILayout.Label("Crafting:");
            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawRecipesForStation();
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Close"))
                Close();

            GUILayout.EndArea();
        }

        private void DrawResources()
        {
            GUILayout.Label("Resources:");
            var inv = PlayerInventoryService.Instance;
            foreach (var id in new[] { ToolRecipes.Wood, ToolRecipes.Stone, ToolRecipes.Iron, ToolRecipes.Coal, ToolRecipes.Diesel })
                GUILayout.Label($"- {id}: {inv.GetResource(id)}");
        }

        private void DrawRecipesForStation()
        {
            var inv = PlayerInventoryService.Instance;
            var stationType = _activeStation.StationType;

            foreach (var recipe in ToolRecipes.All.Where(r => r != null && r.stationType == stationType))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(recipe.displayName, GUILayout.Width(150));
                GUILayout.Label(CostString(recipe.costs), GUILayout.Width(180));

                // Patch 6.1: optional melee stat line.
                if (MeleeWeaponStats.TryGet(recipe.itemId, out var s))
                    GUILayout.Label($"DMG {s.damage} | RNG {s.rangeMeters:0.#}m | SPD {s.speed:0.#}", GUILayout.Width(150));
                else
                    GUILayout.Label("", GUILayout.Width(150));

                if (GUILayout.Button("Craft", GUILayout.Width(70)))
                    CraftingService.TryCraft(recipe);

                GUILayout.EndHorizontal();
            }

            // Patch 6.1: melee weapon upgrade (Workbench only).
            if (stationType == CraftingStationType.Workbench)
                DrawWeaponUpgrade();
        }

        private void DrawWeaponUpgrade()
        {
            var inv = PlayerInventoryService.Instance;
            if (inv == null)
                return;

            // Find a candidate melee weapon: prefer equipped, else first in inventory.
            var idx = inv.EquippedToolIndex;
            var t = inv.EquippedTool;
            if (t == null || !MeleeWeaponStats.TryGetUpgradeTarget(t.itemId, out _))
            {
                idx = -1;
                t = null;
                for (var i = 0; i < inv.Tools.Count; i++)
                {
                    var tool = inv.Tools[i];
                    if (tool == null)
                        continue;
                    if (MeleeWeaponStats.TryGetUpgradeTarget(tool.itemId, out _))
                    {
                        idx = i;
                        t = tool;
                        break;
                    }
                }
            }

            if (t == null || idx < 0)
                return;

            if (!MeleeWeaponStats.TryGetUpgradeTarget(t.itemId, out var upgradeToId))
                return;

            var toRecipe = ToolRecipes.Get(upgradeToId);
            if (toRecipe == null)
                return;

            // Simple fixed upgrade cost (non-zero wood+iron).
            var upgradeCosts = new List<ToolRecipe.Cost>
            {
                new ToolRecipe.Cost { resourceId = ToolRecipes.Wood, amount = 4 },
                new ToolRecipe.Cost { resourceId = ToolRecipes.Iron, amount = 2 },
            };

            GUILayout.Space(8);
            GUILayout.Label("Upgrade (restores to full durability):");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{t.itemId} → {upgradeToId}", GUILayout.Width(180));
            GUILayout.Label(CostString(upgradeCosts), GUILayout.Width(180));

            if (GUILayout.Button($"Upgrade", GUILayout.Width(70)))
            {
                if (!inv.CanAfford(upgradeCosts))
                {
                    SelectionUIState.SetSelected("Insufficient materials");
                }
                else
                {
                    inv.Spend(upgradeCosts);

                    // Replace in-place to preserve inventory size/indexing.
                    t.itemId = toRecipe.itemId;
                    t.toolType = toRecipe.toolType;
                    t.tier = toRecipe.tier;
                    t.maxDurability = Mathf.Max(1, toRecipe.maxDurability);
                    t.currentDurability = t.maxDurability; // full repair
                    t.hitDamage = Mathf.Max(1, toRecipe.hitDamage);

                    inv.NotifyChanged();

                    // Pool integration for the new crafted item (upgrade counts as crafting output).
                    if (CreatedPoolService.Instance != null)
                        CreatedPoolService.Instance.RegisterCreated(toRecipe.itemId, 1);
                    if (DestroyedPoolService.Instance != null)
                        DestroyedPoolService.Instance.MarkCrafted(toRecipe.itemId);

                    SelectionUIState.SetSelected($"Upgraded to {toRecipe.itemId} (full durability)");
                }
            }
            GUILayout.EndHorizontal();
        }

        private static string CostString(List<ToolRecipe.Cost> costs)
        {
            if (costs == null || costs.Count == 0)
                return "";
            return string.Join(", ", costs.Select(c => $"{c.amount} {c.resourceId}"));
        }
    }
}

