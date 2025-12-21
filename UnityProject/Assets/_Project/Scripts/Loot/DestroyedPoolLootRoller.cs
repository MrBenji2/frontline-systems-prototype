using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Combat;
using Frontline.Crafting;
using Frontline.Economy;
using UnityEngine;

namespace Frontline.Loot
{
    public static class DestroyedPoolLootRoller
    {
        private static string LogPath => Path.Combine(Application.persistentDataPath, "loot_roll_log.txt");

        public static bool TryRollAndSpawn(string npcType, NpcDifficulty difficulty, Vector3 pos)
        {
            // Baseline material loot (ALWAYS DROPS; not governed by DestroyedPool).
            var baselineDrops = RollBaselineMaterialDrops(difficulty);
            SpawnBaselineDrops(pos, baselineDrops);

            var service = DestroyedPoolService.Instance;
            if (service == null)
            {
                AppendLog(DateTime.UtcNow, npcType, 0, "NONE", 0, 0, 0, FormatBaselineDrops(baselineDrops));
                return false;
            }

            // Roll 0-1 drops (max 1).
            var rollDrops = UnityEngine.Random.Range(0, 2); // 0 or 1

            var eligible = service.GetAllDestroyedCounts()
                .Where(kv => kv.Value > 0)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            var eligibleCount = eligible.Count;

            if (rollDrops == 0)
            {
                AppendLog(DateTime.UtcNow, npcType, eligibleCount, "NONE", 0, 0, 0, FormatBaselineDrops(baselineDrops));
                return false;
            }

            if (eligibleCount == 0)
            {
                AppendLog(DateTime.UtcNow, npcType, 0, "NONE", 0, 0, 0, FormatBaselineDrops(baselineDrops));
                return false;
            }

            var totalWeight = eligible.Sum(kv => Mathf.Max(0, kv.Value));
            if (totalWeight <= 0)
            {
                AppendLog(DateTime.UtcNow, npcType, eligibleCount, "NONE", 0, 0, 0, FormatBaselineDrops(baselineDrops));
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
                AppendLog(DateTime.UtcNow, npcType, eligibleCount, "NONE", 0, 0, 0, FormatBaselineDrops(baselineDrops));
                return false;
            }

            var poolBefore = service.GetDestroyedCount(chosenId);
            var consumed = service.TryConsumeDestroyed(chosenId, 1);
            var poolAfter = service.GetDestroyedCount(chosenId);

            if (!consumed)
            {
                AppendLog(DateTime.UtcNow, npcType, eligibleCount, "NONE", 0, poolBefore, poolBefore, FormatBaselineDrops(baselineDrops));
                return false;
            }

            LootPickup.Spawn(pos, chosenId);
            AppendLog(DateTime.UtcNow, npcType, eligibleCount, chosenId, chosenWeight, poolBefore, poolAfter, FormatBaselineDrops(baselineDrops));
            return true;
        }

        private static List<(string materialId, int quantity)> RollBaselineMaterialDrops(NpcDifficulty difficulty)
        {
            // On EVERY NPC death:
            // - Drop 1 material bundle (guaranteed).
            // - 50% chance to drop a second material bundle.
            var drops = new List<(string materialId, int quantity)>(2);

            var bundles = 1 + (UnityEngine.Random.value < 0.5f ? 1 : 0);
            for (var i = 0; i < bundles; i++)
            {
                var matId = RollBaselineMaterialId(difficulty);
                var qty = UnityEngine.Random.Range(1, 4); // 1-3
                drops.Add((matId, qty));
            }

            return drops;
        }

        private static string RollBaselineMaterialId(NpcDifficulty difficulty)
        {
            // Allowed materials ONLY:
            // mat_wood, mat_stone, mat_coal, mat_iron, mat_diesel
            // Tier weighting by NPC difficulty.
            // (Weights are intentionally simple and small-qty capped.)
            var roll = UnityEngine.Random.Range(0, 100);
            switch (difficulty)
            {
                case NpcDifficulty.Easy:
                    // Mostly wood/stone; rare coal; very rare iron/diesel.
                    // wood 45, stone 45, coal 8, iron 1, diesel 1
                    if (roll < 45) return ToolRecipes.Wood;
                    if (roll < 90) return ToolRecipes.Stone;
                    if (roll < 98) return ToolRecipes.Coal;
                    if (roll < 99) return ToolRecipes.Iron;
                    return ToolRecipes.Diesel;
                case NpcDifficulty.Medium:
                    // wood/stone/coal common; iron/diesel rare.
                    // wood 30, stone 30, coal 30, iron 5, diesel 5
                    if (roll < 30) return ToolRecipes.Wood;
                    if (roll < 60) return ToolRecipes.Stone;
                    if (roll < 90) return ToolRecipes.Coal;
                    if (roll < 95) return ToolRecipes.Iron;
                    return ToolRecipes.Diesel;
                case NpcDifficulty.Hard:
                default:
                    // iron/diesel more frequent; still some basics.
                    // wood 15, stone 15, coal 20, iron 25, diesel 25
                    if (roll < 15) return ToolRecipes.Wood;
                    if (roll < 30) return ToolRecipes.Stone;
                    if (roll < 50) return ToolRecipes.Coal;
                    if (roll < 75) return ToolRecipes.Iron;
                    return ToolRecipes.Diesel;
            }
        }

        private static void SpawnBaselineDrops(Vector3 center, List<(string materialId, int quantity)> drops)
        {
            if (drops == null || drops.Count == 0)
                return;

            // Spawn each bundle as its own loot pickup (so quantities remain visible + lootable).
            // Slight offsets avoid overlapping cubes.
            for (var i = 0; i < drops.Count; i++)
            {
                var d = drops[i];
                var offset = UnityEngine.Random.insideUnitSphere;
                offset.y = 0f;
                offset = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.right;
                var pos = center + offset * (0.6f + 0.15f * i);
                LootPickup.Spawn(pos, d.materialId, d.quantity);
            }
        }

        private static string FormatBaselineDrops(List<(string materialId, int quantity)> drops)
        {
            if (drops == null || drops.Count == 0)
                return "[]";

            return "[" + string.Join(", ", drops.Select(d => $"({d.materialId},{d.quantity})")) + "]";
        }

        private static void AppendLog(
            DateTime utc,
            string npcType,
            int eligibleCount,
            string chosenCraftedItemId,
            int weight,
            int poolBefore,
            int poolAfter,
            string baselineDrops)
        {
            try
            {
                var line =
                    $"timestamp={utc:O} npcType={npcType} eligibleCount={eligibleCount} chosenCraftedItemId={chosenCraftedItemId} weight={weight} poolBefore={poolBefore} poolAfter={poolAfter} baselineDrops={baselineDrops}\n";
                File.AppendAllText(LogPath, line);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LootRollLog: failed to append: {ex.Message}");
            }
        }
    }
}

