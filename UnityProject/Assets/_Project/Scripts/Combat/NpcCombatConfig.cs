using UnityEngine;

namespace Frontline.Combat
{
    public enum NpcDifficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2
    }

    public enum NpcAttackType
    {
        Ranged = 0,
        Melee = 1
    }

    public static class NpcCombatConfig
    {
        public static string NpcTypeId(NpcDifficulty d, NpcAttackType t) => $"{d}_{t}";

        public static void Get(
            NpcDifficulty difficulty,
            NpcAttackType attackType,
            out int hp,
            out float moveSpeed,
            out float aggroRange,
            out float attackInterval,
            out float aimErrorDegrees,
            out float meleeRange)
        {
            meleeRange = 2.0f;
            aimErrorDegrees = 0f;

            switch (difficulty)
            {
                case NpcDifficulty.Easy:
                    hp = 40;
                    moveSpeed = 5.5f;
                    break;
                case NpcDifficulty.Medium:
                    hp = 75;
                    moveSpeed = 5.5f;
                    break;
                case NpcDifficulty.Hard:
                default:
                    hp = 120;
                    moveSpeed = 7.5f;
                    break;
            }

            if (attackType == NpcAttackType.Ranged)
            {
                aggroRange = difficulty switch
                {
                    NpcDifficulty.Easy => 10f,
                    NpcDifficulty.Medium => 20f,
                    _ => 25f,
                };

                attackInterval = difficulty switch
                {
                    NpcDifficulty.Easy => 1.25f,
                    NpcDifficulty.Medium => 0.75f,
                    _ => 0.75f,
                };

                aimErrorDegrees = difficulty switch
                {
                    NpcDifficulty.Easy => 14f,
                    NpcDifficulty.Medium => 7f,
                    _ => 2f,
                };
            }
            else
            {
                aggroRange = difficulty switch
                {
                    NpcDifficulty.Easy => 5f,
                    NpcDifficulty.Medium => 10f,
                    _ => 10f,
                };

                attackInterval = difficulty switch
                {
                    NpcDifficulty.Easy => 1.20f,
                    NpcDifficulty.Medium => 0.70f,
                    _ => 0.70f,
                };
            }
        }

        public static Color GetTint(NpcDifficulty d, NpcAttackType t)
        {
            var baseColor = d switch
            {
                NpcDifficulty.Easy => new Color(0.35f, 0.85f, 0.35f),
                NpcDifficulty.Medium => new Color(0.95f, 0.75f, 0.20f),
                _ => new Color(0.90f, 0.25f, 0.25f),
            };

            return t == NpcAttackType.Ranged ? baseColor : Color.Lerp(baseColor, new Color(0.25f, 0.25f, 0.95f), 0.35f);
        }
    }
}

