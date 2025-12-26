using System;
using System.Collections.Generic;
using System.Linq;

namespace Frontline.Crafting
{
    public static class ToolRecipes
    {
        public const string Wood = "mat_wood";
        public const string Stone = "mat_stone";
        public const string Iron = "mat_iron";
        public const string Coal = "mat_coal";
        public const string Diesel = "mat_diesel";

        private static readonly List<ToolRecipe> _all = BuildAll();
        private static readonly Dictionary<string, ToolRecipe> _byId = _all
            .Where(r => !string.IsNullOrWhiteSpace(r.itemId))
            .ToDictionary(r => r.itemId, r => r, StringComparer.Ordinal);

        public static IReadOnlyList<ToolRecipe> All => _all;

        public static ToolRecipe Get(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;
            return _byId.TryGetValue(itemId, out var r) ? r : null;
        }

        public static ToolRecipe BestOwnedForType(ToolType type, IEnumerable<string> ownedItemIds)
        {
            if (type == ToolType.None || ownedItemIds == null)
                return null;

            var owned = new HashSet<string>(ownedItemIds.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.Ordinal);
            ToolRecipe best = null;
            foreach (var r in _all)
            {
                if (r.toolType != type)
                    continue;
                if (!owned.Contains(r.itemId))
                    continue;
                if (best == null || r.tier > best.tier)
                    best = r;
            }
            return best;
        }

        private static List<ToolRecipe> BuildAll()
        {
            // Durability tuned to last longer (Milestone 4.1: ~8x).
            const int woodDur = 12 * 8;
            const int stoneDur = 24 * 8;
            const int ironDur = 48 * 8;

            const int hitWood = 10;
            const int hitStone = 12;
            const int hitIron = 15;

            return new List<ToolRecipe>
            {
                // Axe
                Make(CraftingStationType.Inventory, "tool_axe_wood", "Wood Axe", ToolType.Axe, ToolTier.Wood, woodDur, hitWood, (Wood, 10)),
                Make(CraftingStationType.Workbench, "tool_axe_stone", "Stone Axe", ToolType.Axe, ToolTier.Stone, stoneDur, hitStone, (Stone, 8), (Wood, 4)),
                Make(CraftingStationType.Foundry, "tool_axe_iron", "Iron Axe", ToolType.Axe, ToolTier.Iron, ironDur, hitIron, (Iron, 6), (Stone, 4)),

                // Shovel (per spec: harvests rocks)
                Make(CraftingStationType.Inventory, "tool_shovel_wood", "Wood Shovel", ToolType.Shovel, ToolTier.Wood, woodDur, hitWood, (Wood, 10)),
                Make(CraftingStationType.Workbench, "tool_shovel_stone", "Stone Shovel", ToolType.Shovel, ToolTier.Stone, stoneDur, hitStone, (Stone, 8), (Wood, 4)),
                Make(CraftingStationType.Foundry, "tool_shovel_iron", "Iron Shovel", ToolType.Shovel, ToolTier.Iron, ironDur, hitIron, (Iron, 6), (Stone, 4)),

                // Wrench
                Make(CraftingStationType.Inventory, "tool_wrench_wood", "Wood Wrench", ToolType.Wrench, ToolTier.Wood, woodDur, hitWood, (Wood, 10)),
                Make(CraftingStationType.Workbench, "tool_wrench_stone", "Stone Wrench", ToolType.Wrench, ToolTier.Stone, stoneDur, hitStone, (Stone, 8), (Wood, 4)),
                Make(CraftingStationType.Foundry, "tool_wrench_iron", "Iron Wrench", ToolType.Wrench, ToolTier.Iron, ironDur, hitIron, (Iron, 6), (Stone, 4)),

                // Hammer
                Make(CraftingStationType.Inventory, "tool_hammer_wood", "Wood Hammer", ToolType.Hammer, ToolTier.Wood, woodDur, hitWood, (Wood, 10)),
                Make(CraftingStationType.Workbench, "tool_hammer_stone", "Stone Hammer", ToolType.Hammer, ToolTier.Stone, stoneDur, hitStone, (Stone, 8), (Wood, 4)),
                Make(CraftingStationType.Foundry, "tool_hammer_iron", "Iron Hammer", ToolType.Hammer, ToolTier.Iron, ironDur, hitIron, (Iron, 6), (Stone, 4)),

                // Gas can (single tier for now; crafted from non-diesel so it can bootstrap)
                // Milestone 4 station choice: Workbench
                Make(CraftingStationType.Workbench, "tool_gas_can", "Gas Can", ToolType.GasCan, ToolTier.None, 32 * 8, 10, (Iron, 8), (Wood, 2)),

                // Milestone 6.1: Melee weapons (Workbench).
                // Durability: keep modest; upgrade restores to full.
                Make(CraftingStationType.Workbench, "weapon_knife", "Knife", ToolType.MeleeWeapon, ToolTier.None, 36 * 8, 6, (Wood, 6), (Iron, 2)),
                Make(CraftingStationType.Workbench, "weapon_sword", "Sword", ToolType.MeleeWeapon, ToolTier.None, 48 * 8, 2, (Wood, 10), (Iron, 4)),
                Make(CraftingStationType.Workbench, "weapon_pole", "Pole", ToolType.MeleeWeapon, ToolTier.None, 60 * 8, 3, (Wood, 14), (Iron, 6)),
            };
        }

        private static ToolRecipe Make(
            CraftingStationType stationType,
            string itemId,
            string name,
            ToolType type,
            ToolTier tier,
            int durability,
            int hitDamage,
            params (string resourceId, int amount)[] costs)
        {
            var r = new ToolRecipe
            {
                itemId = itemId,
                displayName = name,
                toolType = type,
                tier = tier,
                stationType = stationType,
                maxDurability = Math.Max(1, durability),
                hitDamage = Math.Max(1, hitDamage),
                costs = new List<ToolRecipe.Cost>()
            };

            foreach (var c in costs)
                r.costs.Add(new ToolRecipe.Cost { resourceId = c.resourceId, amount = Math.Max(0, c.amount) });

            return r;
        }
    }
}

