using Frontline.Crafting;
using UnityEngine;

namespace Frontline.Harvesting
{
    /// <summary>
    /// Simple harvestable resource node:
    /// - has HP
    /// - only takes damage from the correct ToolType
    /// - on death spawns resource pickups (not directly into inventory)
    /// </summary>
    public sealed class HarvestNode : MonoBehaviour
    {
        [Header("Harvest Rules")]
        [SerializeField] private ToolType requiredTool = ToolType.Axe;
        [SerializeField] private string yieldResourceId = "mat_wood";
        [SerializeField] private int yieldAmount = 5;

        [Header("HP")]
        [SerializeField] private int maxHp = 30;

        [Header("Pickup Spawn")]
        [SerializeField] private float pickupScatterRadius = 0.6f;
        [SerializeField] private float pickupSpawnHeight = 0.4f;

        public ToolType RequiredTool => requiredTool;
        public string YieldResourceId => yieldResourceId;
        public int YieldAmount => yieldAmount;

        public int MaxHp => maxHp;
        public int CurrentHp { get; private set; }
        public bool IsDead { get; private set; }

        private void Awake()
        {
            maxHp = Mathf.Max(1, maxHp);
            yieldAmount = Mathf.Max(1, yieldAmount);
            CurrentHp = maxHp;
        }

        public void Configure(ToolType required, string resourceId, int amount, int hp)
        {
            requiredTool = required;
            yieldResourceId = resourceId ?? "";
            yieldAmount = Mathf.Max(1, amount);
            maxHp = Mathf.Max(1, hp);
            CurrentHp = maxHp;
        }

        /// <summary>
        /// Applies a harvesting hit. Returns true only if the hit was valid (right tool + damage applied).
        /// Patch 7A: wrong tools may still work but slower (tree/ground only).
        /// </summary>
        public bool ApplyHarvestHit(ToolType toolType, int damage, Vector3 hitPoint)
        {
            if (IsDead)
                return false;
            if (damage <= 0)
                return false;

            var eff = GetEffectiveness(requiredTool, toolType);
            if (eff <= 0.0001f)
                return false;

            var scaled = Mathf.Max(1, Mathf.RoundToInt(damage * eff));

            CurrentHp = Mathf.Max(0, CurrentHp - scaled);
            if (CurrentHp == 0)
                Die(hitPoint);
            return true;
        }

        private static float GetEffectiveness(ToolType required, ToolType used)
        {
            if (used == ToolType.None)
                return 0f;
            if (used == required)
                return 1.0f;

            // Patch 7A scope:
            // - Axe nodes represent Trees.
            // - Shovel nodes represent Dig/Ground (stone/soil).
            if (required == ToolType.Axe)
            {
                if (used == ToolType.Shovel) return 0.35f;
                if (used == ToolType.Wrench) return 0.20f;
                return 0.0f;
            }

            if (required == ToolType.Shovel)
            {
                if (used == ToolType.Wrench) return 0.35f;
                // Spec does not define Axe vs ground; allow slow use.
                if (used == ToolType.Axe) return 0.35f;
                return 0.0f;
            }

            // Keep strict requirements for other node types (hammer, gas can, etc.).
            return 0.0f;
        }

        private void Die(Vector3 hitPoint)
        {
            if (IsDead)
                return;
            IsDead = true;

            // Prefer pickups for visibility.
            var count = Mathf.Max(1, yieldAmount);
            for (var i = 0; i < count; i++)
            {
                var offset = Random.insideUnitSphere;
                offset.y = 0f;
                offset = offset.normalized * Random.Range(0f, pickupScatterRadius);
                var pos = transform.position + Vector3.up * pickupSpawnHeight + offset;
                ResourcePickup.Spawn(pos, yieldResourceId, 1);
            }

            Destroy(gameObject);
        }
    }
}

