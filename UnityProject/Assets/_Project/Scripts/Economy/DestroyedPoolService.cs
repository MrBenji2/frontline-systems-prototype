using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Definitions;
using UnityEngine;

namespace Frontline.Economy
{
    /// <summary>
    /// Central authoritative closed-economy gate:
    /// - Only items that were crafted/built at least once AND destroyed in-world become eligible for NPC loadouts/loot.
    /// - DestroyedPool holds the "eligible destroyed" counts.
    /// </summary>
    public sealed class DestroyedPoolService : MonoBehaviour
    {
        public static DestroyedPoolService Instance { get; private set; }

        public event Action Changed;

        private readonly Dictionary<string, int> _destroyedCounts = new(StringComparer.Ordinal);
        private readonly HashSet<string> _craftedEver = new(StringComparer.Ordinal);

        // Diagnostics: tracks destruction events for things not yet crafted.
        private readonly Dictionary<string, int> _destroyedButUncraftedCounts = new(StringComparer.Ordinal);

        private string SavePath => Path.Combine(Application.persistentDataPath, "destroyed_pool.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadFromDisk();
        }

        public IReadOnlyDictionary<string, int> GetAllDestroyedCounts() => _destroyedCounts;

        public IReadOnlyDictionary<string, int> GetDestroyedButUncraftedCounts() => _destroyedButUncraftedCounts;

        public bool HasCraftedEver(string definitionId) => _craftedEver.Contains(definitionId);

        public int GetDestroyedCount(string definitionId)
        {
            return _destroyedCounts.TryGetValue(definitionId, out var c) ? c : 0;
        }

        /// <summary>
        /// Closed-economy consumption step (Milestone 4 loot):
        /// Decrements the eligible destroyed count (craftedEver && destroyed) if available.
        /// Returns true only if the pool was decremented.
        /// </summary>
        public bool TryConsumeDestroyed(string definitionId, int amount = 1)
        {
            if (!IsValidId(definitionId))
                return false;
            if (amount <= 0)
                return false;

            var before = GetDestroyedCount(definitionId);
            if (before < amount)
                return false;

            var after = Math.Max(0, before - amount);
            _destroyedCounts[definitionId] = after;
            SaveToDisk();
            Changed?.Invoke();
            return true;
        }

        public void MarkCrafted(string definitionId)
        {
            if (!IsValidId(definitionId))
                return;

            if (_craftedEver.Add(definitionId))
            {
                SaveToDisk();
                Changed?.Invoke();
            }
        }

        public void RegisterDestroyed(string definitionId, int amount = 1)
        {
            if (!IsValidId(definitionId))
                return;
            if (amount <= 0)
                return;

            if (!_craftedEver.Contains(definitionId))
            {
                _destroyedButUncraftedCounts[definitionId] = GetFrom(_destroyedButUncraftedCounts, definitionId) + amount;
                SaveToDisk();
                Changed?.Invoke();
                return;
            }

            _destroyedCounts[definitionId] = GetFrom(_destroyedCounts, definitionId) + amount;
            SaveToDisk();
            Changed?.Invoke();
        }

        public void ResetAll()
        {
            _destroyedCounts.Clear();
            _craftedEver.Clear();
            _destroyedButUncraftedCounts.Clear();
            SaveToDisk();
            Changed?.Invoke();
        }

        public void SeedAllKnownDefinitionsAsCraftedThenDestroyed(int destroyedEach = 1)
        {
            var ids = DefinitionRegistry.Instance != null
                ? DefinitionRegistry.Instance.AllDefinitionIds().ToList()
                : new List<string>();

            foreach (var id in ids)
            {
                MarkCrafted(id);
                RegisterDestroyed(id, destroyedEach);
            }
        }

        private static int GetFrom(Dictionary<string, int> dict, string id)
        {
            return dict.TryGetValue(id, out var c) ? c : 0;
        }

        private static bool IsValidId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            if (DefinitionRegistry.Instance == null)
                return true; // allow early calls during bootstrap
            return DefinitionRegistry.Instance.IsKnownId(id);
        }

        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return;

                var json = File.ReadAllText(SavePath);
                var snap = JsonUtility.FromJson<DestroyedPoolSnapshot>(json);
                if (snap == null)
                    return;

                _destroyedCounts.Clear();
                foreach (var e in snap.destroyedCounts)
                    _destroyedCounts[e.id] = Math.Max(0, e.count);

                _craftedEver.Clear();
                foreach (var id in snap.craftedEver)
                    _craftedEver.Add(id);

                _destroyedButUncraftedCounts.Clear();
                foreach (var e in snap.destroyedButUncraftedCounts)
                    _destroyedButUncraftedCounts[e.id] = Math.Max(0, e.count);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DestroyedPool: failed to load '{SavePath}': {ex.Message}");
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var snap = new DestroyedPoolSnapshot
                {
                    craftedEver = _craftedEver.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                    destroyedCounts = _destroyedCounts
                        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .Select(kv => new DestroyedPoolSnapshot.Entry { id = kv.Key, count = kv.Value })
                        .ToList(),
                    destroyedButUncraftedCounts = _destroyedButUncraftedCounts
                        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .Select(kv => new DestroyedPoolSnapshot.Entry { id = kv.Key, count = kv.Value })
                        .ToList()
                };

                var json = JsonUtility.ToJson(snap, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DestroyedPool: failed to save '{SavePath}': {ex.Message}");
            }
        }
    }
}

