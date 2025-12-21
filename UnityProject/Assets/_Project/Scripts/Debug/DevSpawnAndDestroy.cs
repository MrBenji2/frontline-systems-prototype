using Frontline.Economy;
using Frontline.World;
using UnityEngine;

namespace Frontline.DebugTools
{
    public static class DevSpawnAndDestroy
    {
        public static void SpawnAndDestroy(string definitionId, int count, bool markCraftedFirst = true)
        {
            if (count <= 0)
                return;

            if (markCraftedFirst && DestroyedPoolService.Instance != null)
                DestroyedPoolService.Instance.MarkCrafted(definitionId);

            for (var i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"DEV_{definitionId}_{i}";

                // keep them out of the way; no gameplay visuals intended at this milestone
                go.transform.position = new Vector3(1000 + i * 2, 0.5f, 1000);

                var health = go.AddComponent<Health>();
                var destructible = go.AddComponent<Destructible>();
                destructible.SetDefinitionId(definitionId);

                // Kill immediately to exercise the actual destruction pipeline.
                health.Kill();
            }
        }
    }
}

