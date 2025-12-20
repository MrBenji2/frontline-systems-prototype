using Frontline.Definitions;
using Frontline.Economy;
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
            EnsureSingletonGO<DestroyedPoolDebugPanel>("_DestroyedPoolDebugPanel");
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

