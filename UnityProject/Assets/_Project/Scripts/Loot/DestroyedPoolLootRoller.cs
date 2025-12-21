using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Economy;
using UnityEngine;

namespace Frontline.Loot
{
    public static class DestroyedPoolLootRoller
    {
        private static string LogPath => Path.Combine(Application.persistentDataPath, "loot_roll_log.txt");

        public static bool TryRollAndSpawn(string npcType, Vector3 pos)
        {
            var service = DestroyedPoolService.Instance;
            if (service == null)
                return false;

            // Roll 0-1 drops (max 1).
            var rollDrops = UnityEngine.Random.Range(0, 2); // 0 or 1

            var eligible = service.GetAllDestroyedCounts()
                .Where(kv => kv.Value > 0)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            var eligibleCount = eligible.Count;

            if (rollDrops == 0)
            {
                AppendLog(DateTime.UtcNow, npcType, eligibleCount, "(none)", 0, 0, 0);
                return false;
            }

            if (eligibleCount == 0)
            {
                AppendLog(DateTime.UtcNow, npcType, 0, "(none)", 0, 0, 0);
                return false;
            }

            var totalWeight = eligible.Sum(kv => Mathf.Max(0, kv.Value));
            if (totalWeight <= 0)
            {
                AppendLog(DateTime.UtcNow, npcType, eligibleCount, "(none)", 0, 0, 0);
                return false;
            }

            var pick = UnityEngine.Random.Range(0, totalWeight);
            string chosenId = null;
            var chosenWeight = 0;
            foreach (var kv in eligible)
            {
                var w = Mathf.Max(0, kv.Value);
                if (w <= 0)
                    continue;
                if (pick < w)
                {
                    chosenId = kv.Key;
                    chosenWeight = w;
                    break;
                }
                pick -= w;
            }

            if (string.IsNullOrWhiteSpace(chosenId))
            {
                AppendLog(DateTime.UtcNow, npcType, eligibleCount, "(none)", 0, 0, 0);
                return false;
            }

            var poolBefore = service.GetDestroyedCount(chosenId);
            var consumed = service.TryConsumeDestroyed(chosenId, 1);
            var poolAfter = service.GetDestroyedCount(chosenId);

            if (!consumed)
            {
                AppendLog(DateTime.UtcNow, npcType, eligibleCount, "(none)", 0, poolBefore, poolBefore);
                return false;
            }

            LootPickup.Spawn(pos, chosenId);
            AppendLog(DateTime.UtcNow, npcType, eligibleCount, chosenId, chosenWeight, poolBefore, poolAfter);
            return true;
        }

        private static void AppendLog(DateTime utc, string npcType, int eligibleCount, string chosenItemId, int weight, int poolBefore, int poolAfter)
        {
            try
            {
                var line =
                    $"timestamp={utc:O} npcType={npcType} eligibleCount={eligibleCount} chosenItemId={chosenItemId} weight={weight} poolBefore={poolBefore} poolAfter={poolAfter}\n";
                File.AppendAllText(LogPath, line);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LootRollLog: failed to append: {ex.Message}");
            }
        }
    }
}

