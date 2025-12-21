using System;
using UnityEngine;

namespace Frontline.World
{
    public sealed class Health : MonoBehaviour
    {
        public event Action<Health> Died;

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
        }

        public void ApplyDamage(int amount, GameObject instigator = null)
        {
            if (IsDead)
                return;
            if (amount <= 0)
                return;

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            if (CurrentHp == 0)
                Die(instigator);
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

