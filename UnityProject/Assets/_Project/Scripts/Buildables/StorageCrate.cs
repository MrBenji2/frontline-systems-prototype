using System;
using System.Collections.Generic;
using System.Linq;
using Frontline.Economy;
using Frontline.Gameplay;
using Frontline.Loot;
using Frontline.World;
using UnityEngine;

namespace Frontline.Buildables
{
    [RequireComponent(typeof(Buildable))]
    [RequireComponent(typeof(Health))]
    public sealed class StorageCrate : MonoBehaviour
    {
        [SerializeField] private int maxSlots = 12;
        [SerializeField] private int maxTotalCount = 80;

        [Header("Milestone 7.3: Weight System")]
        [SerializeField] private float maxWeight = 200f;
        [Tooltip("Upgrade tiers add this much capacity per level.")]
        [SerializeField] private float upgradeWeightPerLevel = 50f;

        [Header("Milestone 7.3: Label")]
        [SerializeField] private string crateLabel = "Storage Crate";

        private readonly Dictionary<string, int> _items = new(StringComparer.Ordinal);
        private Health _health;
        private bool _registered;
        private int _upgradeLevel = 0;

        public int MaxSlots => maxSlots;
        public int MaxTotalCount => maxTotalCount;

        /// <summary>
        /// Milestone 7.3: Maximum weight capacity (including upgrades).
        /// </summary>
        public float MaxWeight => maxWeight + (_upgradeLevel * upgradeWeightPerLevel);

        /// <summary>
        /// Milestone 7.3: Current upgrade level (0 = base).
        /// </summary>
        public int UpgradeLevel => _upgradeLevel;

        /// <summary>
        /// Milestone 7.3: Gets or sets the crate label/name.
        /// </summary>
        public string Label
        {
            get => string.IsNullOrWhiteSpace(crateLabel) ? "Storage Crate" : crateLabel;
            set => crateLabel = value ?? "Storage Crate";
        }

        public IReadOnlyDictionary<string, int> Items => _items;

        /// <summary>
        /// Milestone 7.5: Slots used = distinct item types in inventory.
        /// </summary>
        public int SlotsUsed => _items.Count;

        /// <summary>
        /// Milestone 7.5: Distinct item count (same as SlotsUsed).
        /// </summary>
        public int DistinctItemCount => _items.Count;

        /// <summary>
        /// Total quantity of all items (for display purposes).
        /// </summary>
        public int TotalQuantity => _items.Values.Sum();

        /// <summary>
        /// Milestone 7.3: Calculates current total weight of items.
        /// </summary>
        public float CurrentWeight => CalculateWeight();

        /// <summary>
        /// Milestone 7.3: Returns true if over weight capacity.
        /// </summary>
        public bool IsOverWeight => CurrentWeight > MaxWeight;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        public bool CanAdd(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            if (count <= 0)
                return false;

            // Milestone 7.5: Check weight limit (primary constraint for resources).
            var itemWeight = GetItemWeight(itemId) * count;
            if (CurrentWeight + itemWeight > MaxWeight)
                return false;

            // Milestone 7.5: Check slots (distinct item types).
            // If item already exists, no new slot needed.
            if (_items.ContainsKey(itemId))
                return true;

            // New item type requires a free slot.
            return SlotsUsed + 1 <= maxSlots;
        }

        /// <summary>
        /// Milestone 7.3: Calculate total weight of stored items.
        /// </summary>
        private float CalculateWeight()
        {
            var total = 0f;
            foreach (var kv in _items)
            {
                if (kv.Value <= 0)
                    continue;
                total += GetItemWeight(kv.Key) * kv.Value;
            }
            return total;
        }

        /// <summary>
        /// Milestone 7.3: Get weight of an item type.
        /// </summary>
        public static float GetItemWeight(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0f;

            // Resources are lighter per unit.
            if (itemId.StartsWith("mat_"))
                return PlayerInventoryService.RESOURCE_WEIGHT;

            // Default weight for tools/other items.
            return 2f;
        }

        /// <summary>
        /// Milestone 7.3: Upgrade the crate's capacity.
        /// Returns true if upgrade was successful.
        /// </summary>
        public bool TryUpgrade()
        {
            // Hook for future upgrade cost checking.
            // For now, just increment the level.
            _upgradeLevel++;
            return true;
        }

        /// <summary>
        /// Milestone 7.3: Set upgrade level (for save/load).
        /// </summary>
        public void SetUpgradeLevelForLoad(int level)
        {
            _upgradeLevel = Mathf.Max(0, level);
        }

        public bool TryAdd(string itemId, int count)
        {
            if (!CanAdd(itemId, count))
                return false;

            _items[itemId] = GetCount(itemId) + count;
            return true;
        }

        public bool TryRemove(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            if (count <= 0)
                return false;

            var have = GetCount(itemId);
            if (have < count)
                return false;

            var next = have - count;
            if (next <= 0)
                _items.Remove(itemId);
            else
                _items[itemId] = next;
            return true;
        }

        public int GetCount(string itemId)
        {
            return _items.TryGetValue(itemId, out var c) ? c : 0;
        }

        public void LoadFromSnapshot(IEnumerable<BuildablesWorldSnapshot.ItemStack> stacks)
        {
            _items.Clear();
            if (stacks == null)
                return;

            foreach (var s in stacks)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.itemId) || s.count <= 0)
                    continue;
                if (_items.ContainsKey(s.itemId))
                    _items[s.itemId] += s.count;
                else
                    _items[s.itemId] = s.count;
            }
        }

        public List<BuildablesWorldSnapshot.ItemStack> ToSnapshot()
        {
            return _items
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new BuildablesWorldSnapshot.ItemStack { itemId = kv.Key, count = kv.Value })
                .ToList();
        }

        private void OnDied(Health h)
        {
            if (_registered)
                return;
            _registered = true;

            // Milestone 7.3: On crate destruction, drop all contents as loot.
            DropContentsAsLoot();
            _items.Clear();

            // Register the crate itself as destroyed.
            if (DestroyedPoolService.Instance != null)
            {
                DestroyedPoolService.Instance.RegisterDestroyed("build_storage", 1);
            }
        }

        /// <summary>
        /// Milestone 7.3: Drop all contents as loot pickups.
        /// </summary>
        private void DropContentsAsLoot()
        {
            if (_items.Count == 0)
                return;

            var basePos = transform.position;
            var count = 0;

            foreach (var kv in _items)
            {
                if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value <= 0)
                    continue;

                // Spread loot in a circle around the crate position.
                var angle = count * (360f / Mathf.Max(1, _items.Count)) * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.8f;
                var spawnPos = basePos + offset;

                LootPickup.Spawn(spawnPos, kv.Key, kv.Value);
                count++;
            }
        }

        /// <summary>
        /// Milestone 7.3: Force-destroy the crate (used by UI button).
        /// </summary>
        public void DestroyCrate()
        {
            if (_health != null && !_health.IsDead)
            {
                _health.Kill();
            }
        }
    }
}

