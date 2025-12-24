using System;
using UnityEngine;
using Frontline.UI;

namespace Frontline.World
{
    public sealed class Health : MonoBehaviour
    {
        public event Action<Health> Died;
        public event Action<Health> Changed;
        public event Action<Health, int> Damaged;

        [SerializeField] private int maxHp = 100;
        [SerializeField] private bool destroyGameObjectOnDeath = true;

        public int MaxHp => maxHp;
        public int CurrentHp { get; private set; }
        public bool IsDead { get; private set; }

        private void Awake()
        {
            CurrentHp = maxHp;
        }

        public void Configure(int hp, bool destroyOnDeath = true)
        {
            maxHp = Mathf.Max(1, hp);
            destroyGameObjectOnDeath = destroyOnDeath;
            CurrentHp = maxHp;
            IsDead = false;
            Changed?.Invoke(this);
        }

        public void ApplyDamage(int amount, GameObject instigator = null)
        {
            if (IsDead)
                return;
            if (amount <= 0)
                return;

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            Damaged?.Invoke(this, amount);
            Changed?.Invoke(this);

            // Patch 5.2: damage feedback numbers.
            // Spawn at object center + small offset (hit-point not available at this layer yet).
            var pos = transform.position + Vector3.up * 1.2f;
            DamageNumber.Spawn(pos, amount);

            if (CurrentHp == 0)
                Die(instigator);
        }

        public void Restore(int amount)
        {
            if (IsDead)
                return;
            if (amount <= 0)
                return;

            CurrentHp = Mathf.Clamp(CurrentHp + amount, 0, maxHp);
            Changed?.Invoke(this);
        }

        /// <summary>
        /// Used by local persistence to restore pre-existing damage state.
        /// </summary>
        public void SetCurrentHpForLoad(int currentHp)
        {
            if (IsDead)
                return;
            CurrentHp = Mathf.Clamp(currentHp, 0, maxHp);
            Changed?.Invoke(this);
        }

        public void Kill(GameObject instigator = null)
        {
            if (IsDead)
                return;
            Die(instigator);
        }

        private void Die(GameObject instigator)
        {
            if (IsDead)
                return;

            IsDead = true;
            Died?.Invoke(this);

            if (destroyGameObjectOnDeath)
                Destroy(gameObject);
        }
    }
}

