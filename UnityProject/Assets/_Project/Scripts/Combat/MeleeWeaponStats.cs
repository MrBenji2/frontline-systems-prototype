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

            if (string.Equals(itemId, KnifeId, StringComparison.Ordinal))
            {
                stats = new Stats("Knife", rangeMeters: 1f, damage: 6, speed: 8f);
                return true;
            }

            if (string.Equals(itemId, SwordId, StringComparison.Ordinal))
            {
                stats = new Stats("Sword", rangeMeters: 2f, damage: 2, speed: 5f);
                return true;
            }

            if (string.Equals(itemId, PoleId, StringComparison.Ordinal))
            {
                stats = new Stats("Pole", rangeMeters: 3f, damage: 3, speed: 3f);
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

