using Frontline.Crafting;
using Frontline.Tactical;
using Frontline.UI;
using Frontline.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frontline.Core
{
    /// <summary>
    /// Additive Milestone 4 bootstrap (does not modify earlier systems).
    /// - Ensures CraftingStationPanel singleton exists
    /// - Spawns Workbench + Foundry test stations into TacticalTest
    /// </summary>
    public static class Milestone4Bootstrap
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
            EnsureSingletonGO<CraftingStationPanel>("_CraftingStationPanel");
            EnsureSingletonGO<LootWindowPanel>("_LootWindowPanel");

            // Only auto-spawn in the milestone test scene.
            if (!scene.IsValid() || scene.name != "TacticalTest")
                return;

            if (Object.FindFirstObjectByType<CraftingStation>() != null)
                return; // already present (either authored or previously spawned)

            var player = Object.FindFirstObjectByType<TacticalPlayerController>();
            var center = player != null ? player.transform.position : Vector3.zero;
            center.y = 0f;

            SpawnStation(
                "Workbench",
                CraftingStationType.Workbench,
                new Color(0.55f, 0.35f, 0.18f),
                center + new Vector3(2.0f, 0f, 2.0f));

            SpawnStation(
                "Foundry",
                CraftingStationType.Foundry,
                new Color(0.35f, 0.35f, 0.35f),
                center + new Vector3(-2.5f, 0f, 2.0f));
        }

        private static void SpawnStation(string name, CraftingStationType type, Color tint, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos + Vector3.up * 0.5f;
            go.transform.localScale = new Vector3(1.8f, 1.0f, 1.2f);

            var col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true; // don't block movement in test scene

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = tint;
            }

            var station = go.AddComponent<CraftingStation>();
            station.Configure(type, name);
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

