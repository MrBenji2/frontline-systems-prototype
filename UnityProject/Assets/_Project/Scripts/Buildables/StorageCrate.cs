using System;
using System.Collections.Generic;
using System.Linq;
using Frontline.Economy;
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

        private readonly Dictionary<string, int> _items = new(StringComparer.Ordinal);
        private Health _health;
        private bool _registered;

        public int MaxSlots => maxSlots;
        public int MaxTotalCount => maxTotalCount;

        public IReadOnlyDictionary<string, int> Items => _items;

        public int SlotsUsed => _items.Count;
        public int TotalCount => _items.Values.Sum();

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
            if (TotalCount + count > maxTotalCount)
                return false;
            if (_items.ContainsKey(itemId))
                return true;
            return SlotsUsed + 1 <= maxSlots;
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

            // On crate destruction, all contents are destroyed (no world spill).
            if (DestroyedPoolService.Instance != null)
            {
                foreach (var kv in _items)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value <= 0)
                        continue;
                    DestroyedPoolService.Instance.RegisterDestroyed(kv.Key, kv.Value);
                }
            }

            _items.Clear();
        }
    }
}

