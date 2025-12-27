using Frontline.Missions;
using Frontline.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frontline.Core
{
    /// <summary>
    /// Milestone 8 bootstrap:
    /// - Ensures MissionService singleton exists.
    /// - Ensures MissionHudPanel singleton exists.
    /// - Ensures TrainingRangeSpawner exists for the tactical test scene.
    /// </summary>
    public static class Milestone8Bootstrap
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
            // Ensure MissionService exists
            EnsureSingletonGO<MissionService>("_MissionService");

            // Ensure Mission HUD exists
            EnsureSingletonGO<MissionHudPanel>("_MissionHudPanel");

            // Ensure Mission Debug Hotkeys exist (dev builds only)
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            EnsureSingletonGO<MissionDebugHotkeys>("_MissionDebugHotkeys");
            #endif

            // Spawn training range in tactical test scenes
            if (scene.name.Contains("Tactical") || scene.name.Contains("Test"))
            {
                EnsureSingletonGO<TrainingRangeSpawner>("_TrainingRangeSpawner");
            }
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
