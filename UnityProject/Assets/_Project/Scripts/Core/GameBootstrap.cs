using Frontline.Definitions;
using Frontline.Economy;
using Frontline.Gameplay;
using Frontline.UI;
using UnityEngine;

namespace Frontline.Core
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureSingletonGO<DefinitionRegistry>("_DefinitionRegistry");
            EnsureSingletonGO<DestroyedPoolService>("_DestroyedPool");
            EnsureSingletonGO<CreatedPoolService>("_CreatedPool");
            EnsureSingletonGO<SalvagePoolService>("_SalvagePool");
            EnsureSingletonGO<PlayerInventoryService>("_PlayerInventory");
            EnsureSingletonGO<PlayerSkillsService>("_PlayerSkills");
            EnsureSingletonGO<DestroyedPoolDebugPanel>("_DestroyedPoolDebugPanel");
            EnsureSingletonGO<InventoryCraftingPanel>("_InventoryCraftingPanel");
            EnsureSingletonGO<UiModalManager>("_UiModalManager");
            EnsureSingletonGO<BuildCatalogPanel>("_BuildCatalogPanel");
        }

        private static void EnsureSingletonGO<T>(string name) where T : Component
        {
            if (Object.FindFirstObjectByType<T>() != null)
                return;

            var go = new GameObject(name);
            Object.DontDestroyOnLoad(go);
            go.AddComponent<T>();
        }
    }
}

