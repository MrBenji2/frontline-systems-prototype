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
        private const string ModalId = "inventory_panel";

        [SerializeField] private bool visible = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.I;

        private Vector2 _panelScroll;
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
            // If this panel starts visible, treat it as a gameplay modal for input lock rules.
            if (visible)
                Open();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                if (!visible)
                {
                    // Don't open if another gameplay modal is open.
                    if (UiModalManager.Instance != null && UiModalManager.Instance.HasOpenModal)
                        return;
                    Open();
                }
                else
                {
                    Close();
                }
            }

            // If closed externally (Esc via UiModalManager), stop drawing.
            if (visible && UiModalManager.Instance != null && UiModalManager.Instance.HasOpenModal == false)
            {
                // Inventory is not guaranteed to be the current modal, so don't auto-close here.
            }
        }

        private void Open()
        {
            visible = true;
            if (UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterOpen(ModalId, Close, openedByInteract: false);
        }

        private void Close()
        {
            visible = false;
            if (UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterClosed(ModalId);
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(520, Screen.width - 20);
            var panelHeight = Mathf.Min(720, Screen.height - 20);
            var rect = new Rect(Screen.width - panelWidth - pad, pad, panelWidth, panelHeight);
            GUILayout.BeginArea(rect, GUI.skin.window);
            _panelScroll = GUILayout.BeginScrollView(_panelScroll);
            GUILayout.Label("Inventory + Crafting (I to toggle)");

            if (PlayerInventoryService.Instance == null)
            {
                GUILayout.Label("PlayerInventoryService: MISSING");
                GUILayout.EndScrollView();
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

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawEquipped()
        {
            var inv = PlayerInventoryService.Instance;

            // Patch 7.1D: Display 5-slot equipment system.
            GUILayout.Label($"Active Slot: {inv.ActiveSlot} ({GetSlotName(inv.ActiveSlot)})");
            GUILayout.Label("Slots: 1=Primary, 2=Secondary, 3=Throwable, 4=Deployable, 5=Medical");

            GUILayout.Space(4);

            // Show all slots with their contents.
            for (var slot = 1; slot <= PlayerInventoryService.SLOT_COUNT; slot++)
            {
                var tool = inv.GetToolInSlot(slot);
                var isActive = slot == inv.ActiveSlot;
                var marker = isActive ? ">" : " ";
                var slotName = GetSlotName(slot);

                if (tool == null)
                {
                    GUILayout.Label($"{marker} [{slot}] {slotName}: (empty)");
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{marker} [{slot}] {slotName}: {tool.itemId}", GUILayout.Width(280));
                    GUILayout.Label($"{tool.currentDurability}/{tool.maxDurability}", GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(4);

            // Show currently equipped item details.
            var eq = inv.EquippedTool;
            if (eq != null)
            {
                GUILayout.Label($"Equipped: {eq.itemId} ({eq.toolType})");
                DrawDurabilityBar(eq.currentDurability, eq.maxDurability);
            }
        }

        private static string GetSlotName(int slot)
        {
            return slot switch
            {
                1 => "Primary",
                2 => "Secondary",
                3 => "Throwable",
                4 => "Deployable",
                5 => "Medical",
                _ => "Unknown"
            };
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
            GUILayout.Label("All Tools:");

            if (inv.Tools.Count == 0)
            {
                GUILayout.Label("(none)");
                return;
            }

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(150));
            for (var i = 0; i < inv.Tools.Count; i++)
            {
                var t = inv.Tools[i];
                if (t == null)
                    continue;

                GUILayout.BeginHorizontal();
                var isEq = i == inv.EquippedToolIndex;
                // Patch 7.1D: Show slot assignment.
                var slotInfo = t.slotNumber > 0 ? $"[S{t.slotNumber}]" : "[--]";
                GUILayout.Label($"{(isEq ? ">" : " ")} {slotInfo} {t.itemId}", GUILayout.Width(220));
                GUILayout.Label($"{t.currentDurability}/{t.maxDurability}", GUILayout.Width(70));
                if (GUILayout.Button("Equip", GUILayout.Width(60)))
                    inv.EquipIndex(i);

                // Patch 7B: repair tool/weapon durability.
                var repairInfo = GetRepairInfo(t);
                var prev = GUI.enabled;
                GUI.enabled = repairInfo.canRepair && repairInfo.canAfford;
                if (GUILayout.Button("Repair", GUILayout.Width(55)))
                    inv.TryRepairTool(i);
                GUI.enabled = prev;
                GUILayout.EndHorizontal();

                if (repairInfo.canRepair)
                    GUILayout.Label($"   Repair cost: {repairInfo.costString}");
            }
            GUILayout.EndScrollView();
        }

        private static (bool canRepair, bool canAfford, string costString) GetRepairInfo(PlayerInventoryService.ToolInstance t)
        {
            if (PlayerInventoryService.Instance == null || t == null)
                return (false, false, "");

            var max = Mathf.Max(1, t.maxDurability);
            var cur = Mathf.Clamp(t.currentDurability, 0, max);
            if (cur >= max)
                return (false, true, ""); // already full

            var recipe = ToolRecipes.Get(t.itemId);
            if (recipe == null || recipe.costs == null || recipe.costs.Count == 0)
                return (false, false, "(no recipe)");

            var inv = PlayerInventoryService.Instance;
            var missingPercent = (max - cur) / (float)max;

            var costs = new List<ToolRecipe.Cost>();
            foreach (var c in recipe.costs)
            {
                if (string.IsNullOrWhiteSpace(c.resourceId) || c.amount <= 0)
                    continue;
                var amt = Mathf.CeilToInt(c.amount * PlayerInventoryService.REPAIR_FRACTION * missingPercent);
                if (amt > 0)
                    costs.Add(new ToolRecipe.Cost { resourceId = c.resourceId, amount = amt });
            }

            var costStr = costs.Count == 0 ? "(free)" : string.Join(", ", costs.Select(c => $"{c.amount} {c.resourceId}"));
            var canAfford = inv.CanAfford(costs);
            return (true, canAfford, costStr);
        }

        private void DrawCrafting()
        {
            GUILayout.Label("Crafting:");

            var inv = PlayerInventoryService.Instance;
            foreach (var recipe in ToolRecipes.All)
            {
                if (recipe == null)
                    continue;
                if (recipe.stationType != CraftingStationType.Inventory)
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

