using System;
using System.Collections.Generic;
using System.Linq;

namespace Frontline.Crafting
{
    [Serializable]
    public sealed class ToolRecipe
    {
        [Serializable]
        public struct Cost
        {
            public string resourceId;
            public int amount;
        }

        public string itemId;
        public string displayName;
        public ToolType toolType;
        public ToolTier tier;
        public CraftingStationType stationType = CraftingStationType.Inventory;
        public int maxDurability;
        public int hitDamage;
        public List<Cost> costs = new();

        public IEnumerable<Cost> SalvageCostsHalf()
        {
            foreach (var c in costs)
            {
                var salvage = c.amount / 2;
                if (salvage > 0 && !string.IsNullOrWhiteSpace(c.resourceId))
                    yield return new Cost { resourceId = c.resourceId, amount = salvage };
            }
        }

        public override string ToString()
        {
            var costStr = costs == null || costs.Count == 0
                ? ""
                : string.Join(", ", costs.Select(c => $"{c.amount}x {c.resourceId}"));
            return $"{displayName} ({itemId}) [{tier}] costs: {costStr}";
        }
    }
}

