using UnityEngine;

namespace Frontline.Combat
{
    /// <summary>
    /// Minimal player HP for Milestone 4 combat without affecting harvesting systems.
    /// </summary>
    public sealed class PlayerCombatVitals : MonoBehaviour
    {
        [SerializeField] private int maxHp = 100;

        public int MaxHp => maxHp;
        public int CurrentHp { get; private set; }
        public bool IsDead { get; private set; }

        private void Awake()
        {
            CurrentHp = Mathf.Max(1, maxHp);
        }

        public void ApplyDamage(int amount)
        {
            if (IsDead)
                return;
            if (amount <= 0)
                return;

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            if (CurrentHp == 0)
                IsDead = true;
        }
    }
}

