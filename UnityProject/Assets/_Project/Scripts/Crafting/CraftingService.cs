using Frontline.Economy;
using Frontline.Gameplay;
using Frontline.UI;

namespace Frontline.Crafting
{
    public static class CraftingService
    {
        public static bool TryCraft(ToolRecipe recipe)
        {
            if (recipe == null)
                return false;
            if (PlayerInventoryService.Instance == null)
                return false;

            if (!PlayerInventoryService.Instance.CanAfford(recipe.costs))
            {
                // Patch 5.4B: insufficient materials popup for crafting.
                if (HudMessagePopup.Instance != null)
                    HudMessagePopup.Instance.Show("Insufficient materials");
                return false;
            }

            PlayerInventoryService.Instance.Spend(recipe.costs);
            PlayerInventoryService.Instance.AddTool(recipe.itemId, recipe.maxDurability, recipe.toolType, recipe.tier, recipe.hitDamage);

            // Integration with existing pool system:
            // - "Crafted/Created pool" for eligibility tracking
            if (CreatedPoolService.Instance != null)
                CreatedPoolService.Instance.RegisterCreated(recipe.itemId, 1);

            // - Mark crafted in the closed-economy gate (DestroyedPoolService.craftedEver)
            if (DestroyedPoolService.Instance != null)
                DestroyedPoolService.Instance.MarkCrafted(recipe.itemId);

            return true;
        }
    }
}

