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

            // Patch 6: collision-aware NPC movement via CharacterController.
            // Disable the primitive collider to avoid double-colliders (CharacterController provides its own).
            var col = go.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1.0f, 0f);
            cc.height = 2.0f;
            cc.radius = 0.45f;
            cc.slopeLimit = 60f;
            cc.stepOffset = 0.35f;
        }
    }
}

