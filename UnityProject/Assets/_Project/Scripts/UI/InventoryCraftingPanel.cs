using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Gameplay;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Minimal IMGUI inventory + crafting UI for Milestone 3.
    /// Avoids creating editor UI assets.
    /// </summary>
    public sealed class InventoryCraftingPanel : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.I;

        private Vector2 _scroll;

        private static readonly string[] ResourceOrder =
        {
            ToolRecipes.Wood,
            ToolRecipes.Stone,
            ToolRecipes.Iron,
            ToolRecipes.Coal,
            ToolRecipes.Diesel
        };

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            const int pad = 10;
            var rect = new Rect(Screen.width - 420 - pad, pad, 420, Screen.height - pad * 2);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("Inventory + Crafting (I to toggle)");

            if (PlayerInventoryService.Instance == null)
            {
                GUILayout.Label("PlayerInventoryService: MISSING");
                GUILayout.EndArea();
                return;
            }

            DrawEquipped();
            GUILayout.Space(6);
            DrawResources();
            GUILayout.Space(6);
            DrawTools();
            GUILayout.Space(6);
            DrawCrafting();

            GUILayout.EndArea();
        }

        private void DrawEquipped()
        {
            var inv = PlayerInventoryService.Instance;
            var eq = inv.EquippedTool;
            if (eq == null)
            {
                GUILayout.Label("Equipped: (none)");
                GUILayout.Label("Hotkeys: 1 Axe, 2 Shovel, 3 Wrench, 4 Hammer, 5 Gas Can");
                return;
            }

            GUILayout.Label($"Equipped: {eq.itemId} ({eq.toolType})");
            DrawDurabilityBar(eq.currentDurability, eq.maxDurability);
        }

        private void DrawDurabilityBar(int current, int max)
        {
            max = Mathf.Max(1, max);
            current = Mathf.Clamp(current, 0, max);

            var t = current / (float)max;
            var r = GUILayoutUtility.GetRect(1, 18, GUILayout.ExpandWidth(true));
            GUI.Box(r, "");

            var fill = new Rect(r.x + 2, r.y + 2, (r.width - 4) * t, r.height - 4);
            var c = GUI.color;
            GUI.color = Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 0.2f), t);
            GUI.Box(fill, "");
            GUI.color = c;

            GUI.Label(r, $"Durability: {current}/{max}");
        }

        private void DrawResources()
        {
            GUILayout.Label("Resources:");

            foreach (var id in ResourceOrder)
            {
                var amt = PlayerInventoryService.Instance.GetResource(id);
                GUILayout.Label($"- {id}: {amt}");
            }
        }

        private void DrawTools()
        {
            var inv = PlayerInventoryService.Instance;
            GUILayout.Label("Tools:");

            if (inv.Tools.Count == 0)
            {
                GUILayout.Label("(none)");
                return;
            }

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(120));
            for (var i = 0; i < inv.Tools.Count; i++)
            {
                var t = inv.Tools[i];
                if (t == null)
                    continue;

                GUILayout.BeginHorizontal();
                var isEq = i == inv.EquippedToolIndex;
                GUILayout.Label($"{(isEq ? ">" : " ")} [{i}] {t.itemId}", GUILayout.Width(210));
                GUILayout.Label($"{t.currentDurability}/{t.maxDurability}", GUILayout.Width(90));
                if (GUILayout.Button("Equip", GUILayout.Width(80)))
                    inv.EquipIndex(i);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawCrafting()
        {
            GUILayout.Label("Crafting:");

            var inv = PlayerInventoryService.Instance;
            foreach (var recipe in ToolRecipes.All)
            {
                if (recipe == null)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{recipe.displayName}", GUILayout.Width(170));
                GUILayout.Label(CostString(recipe.costs), GUILayout.Width(170));

                var canCraft = inv.CanAfford(recipe.costs);
                var prev = GUI.enabled;
                GUI.enabled = canCraft;
                if (GUILayout.Button("Craft", GUILayout.Width(60)))
                    CraftingService.TryCraft(recipe);
                GUI.enabled = prev;

                GUILayout.EndHorizontal();
            }
        }

        private static string CostString(List<ToolRecipe.Cost> costs)
        {
            if (costs == null || costs.Count == 0)
                return "";
            return string.Join(", ", costs.Select(c => $"{c.amount} {c.resourceId}"));
        }
    }
}

