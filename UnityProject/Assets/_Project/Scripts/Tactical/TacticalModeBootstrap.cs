using Frontline.Tactical;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frontline.Core
{
    /// <summary>
    /// Boots Tactical Mode essentials for the MVP milestone:
    /// - player movement (WASD)
    /// - top-down camera follow
    /// - fog of war overlay (LOS + memory fog)
    /// - test map spawner (ground + obstacles)
    /// </summary>
    public static class TacticalModeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            // Ensure we run on initial scene and subsequent loads.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Object.FindFirstObjectByType<TacticalPlayerController>() == null)
                TacticalFactory.CreatePlayer();

            if (Object.FindFirstObjectByType<TopDownCameraController>() == null)
                TacticalFactory.CreateTopDownCamera();

            if (Object.FindFirstObjectByType<FogOfWarSystem>() == null)
                TacticalFactory.CreateFogOfWar();

            if (Object.FindFirstObjectByType<TacticalTestMapSpawner>() == null)
                TacticalFactory.CreateTestMapSpawner();
        }
    }
}

