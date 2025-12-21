using Frontline.Combat;
using Frontline.Tactical;
using Frontline.Loot;
using Frontline.World;
using UnityEngine;

namespace Frontline.DebugTools
{
    public static class DevSpawnNpcs
    {
        private const int IgnoreRaycastLayer = 2;

        public static void Spawn(NpcDifficulty difficulty, NpcAttackType attackType, int count = 1)
        {
            count = Mathf.Clamp(count, 1, 10);

            var player = Object.FindFirstObjectByType<TacticalPlayerController>();
            var center = player != null ? player.transform.position : Vector3.zero;
            center.y = 0f;

            for (var i = 0; i < count; i++)
            {
                var pos = center + new Vector3(6 + i * 1.5f, 0f, 6);
                SpawnOne(difficulty, attackType, pos);
            }
        }

        private static void SpawnOne(NpcDifficulty difficulty, NpcAttackType attackType, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"NPC_{NpcCombatConfig.NpcTypeId(difficulty, attackType)}";
            go.layer = IgnoreRaycastLayer; // do not occlude fog of war (which raycasts default layer)
            go.transform.position = pos + Vector3.up * 1.0f;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = NpcCombatConfig.GetTint(difficulty, attackType);
            }

            // Health + AI
            var health = go.AddComponent<Health>();
            var npc = go.AddComponent<NpcController>();
            npc.Configure(difficulty, attackType);
            go.AddComponent<NpcLootOnDeath>();

            // Prevent the capsule collider from blocking fog / station interactions, but keep it hittable by combat raycasts (~0 mask).
            var col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;
        }
    }
}

