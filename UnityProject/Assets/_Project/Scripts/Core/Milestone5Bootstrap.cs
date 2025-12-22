using Frontline.Buildables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frontline.Core
{
    /// <summary>
    /// Additive Milestone 5 bootstrap (does not modify earlier systems).
    /// - Ensures BuildablesService + StorageCratePanel singletons exist
    /// </summary>
    public static class Milestone5Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSingletonGO<BuildablesService>("_BuildablesService");
            EnsureSingletonGO<StorageCratePanel>("_StorageCratePanel");
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

