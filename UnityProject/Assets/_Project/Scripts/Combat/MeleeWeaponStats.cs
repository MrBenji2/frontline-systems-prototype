using System;

namespace Frontline.Combat
{
    /// <summary>
    /// Milestone 6.1: minimal melee weapon stat lookup by itemId.
    /// </summary>
    public static class MeleeWeaponStats
    {
        public readonly struct Stats
        {
            public readonly string displayName;
            public readonly float rangeMeters;
            public readonly int damage;
            public readonly float speed;

            public Stats(string displayName, float rangeMeters, int damage, float speed)
            {
                this.displayName = displayName ?? "";
                this.rangeMeters = rangeMeters;
                this.damage = damage;
                this.speed = speed;
            }
        }

        public const string KnifeId = "weapon_knife";
        public const string SwordId = "weapon_sword";
        public const string PoleId = "weapon_pole";

        public static bool TryGet(string itemId, out Stats stats)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                stats = default;
                return false;
            }

            // Patch 7.1F: Rebalanced melee weapon stats.
            // Damage scales with range (longer reach = harder to balance, so slightly less DPS).
            // Speed indicates attacks per second.
            if (string.Equals(itemId, KnifeId, StringComparison.Ordinal))
            {
                // Knife: fast, short range, moderate damage.
                stats = new Stats("Knife", rangeMeters: 1.2f, damage: 12, speed: 3f);
                return true;
            }

            if (string.Equals(itemId, SwordId, StringComparison.Ordinal))
            {
                // Sword: medium speed, medium range, good damage.
                stats = new Stats("Sword", rangeMeters: 1.8f, damage: 18, speed: 2f);
                return true;
            }

            if (string.Equals(itemId, PoleId, StringComparison.Ordinal))
            {
                // Pole: slow, long range, high damage per hit.
                stats = new Stats("Pole", rangeMeters: 2.5f, damage: 25, speed: 1.2f);
                return true;
            }

            stats = default;
            return false;
        }

        public static bool TryGetUpgradeTarget(string itemId, out string upgradeToItemId)
        {
            upgradeToItemId = "";
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (string.Equals(itemId, KnifeId, StringComparison.Ordinal))
            {
                upgradeToItemId = SwordId;
                return true;
            }

            if (string.Equals(itemId, SwordId, StringComparison.Ordinal))
            {
                upgradeToItemId = PoleId;
                return true;
            }

            return false;
        }
    }
}

