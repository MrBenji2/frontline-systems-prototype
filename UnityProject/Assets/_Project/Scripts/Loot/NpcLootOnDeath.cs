using Frontline.Combat;
using Frontline.World;
using UnityEngine;

namespace Frontline.Loot
{
    [RequireComponent(typeof(Health))]
    public sealed class NpcLootOnDeath : MonoBehaviour
    {
        private Health _health;
        private NpcController _npc;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _npc = GetComponent<NpcController>();
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void OnDied(Health h)
        {
            var type = _npc != null ? _npc.NpcType : "NPC";
            var pos = transform.position;
            pos.y = 0f;
            DestroyedPoolLootRoller.TryRollAndSpawn(type, pos);
        }
    }
}

