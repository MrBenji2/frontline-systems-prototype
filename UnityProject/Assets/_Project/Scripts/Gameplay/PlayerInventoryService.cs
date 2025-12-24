using System;
using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Economy;
using UnityEngine;

namespace Frontline.Gameplay
{
    public sealed class PlayerInventoryService : MonoBehaviour
    {
        [Serializable]
        public sealed class ToolInstance
        {
            public string itemId = "";
            public ToolType toolType = ToolType.None;
            public ToolTier tier = ToolTier.None;
            public int maxDurability = 1;
            public int currentDurability = 1;
            public int hitDamage = 1;
        }

        public static PlayerInventoryService Instance { get; private set; }

        public event Action Changed;
        public event Action<ToolInstance> ToolBroken;

        [Header("Starter (to avoid tool/resource deadlock)")]
        [SerializeField] private bool grantStarterWood = true;
        [SerializeField] private int starterWoodAmount = 10;

        private readonly Dictionary<string, int> _resources = new(StringComparer.Ordinal);
        private readonly List<ToolInstance> _tools = new();
        private int _equippedToolIndex = -1;

        public IReadOnlyDictionary<string, int> Resources => _resources;
        public IReadOnlyList<ToolInstance> Tools => _tools;

        public ToolInstance EquippedTool =>
            _equippedToolIndex >= 0 && _equippedToolIndex < _tools.Count ? _tools[_equippedToolIndex] : null;

        public int EquippedToolIndex => _equippedToolIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (grantStarterWood && _resources.Count == 0 && _tools.Count == 0)
                AddResource(ToolRecipes.Wood, starterWoodAmount);
        }

        public int GetResource(string resourceId)
        {
            return _resources.TryGetValue(resourceId, out var c) ? c : 0;
        }

        public void AddResource(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return;
            if (amount <= 0)
                return;

            _resources[resourceId] = GetResource(resourceId) + amount;
            Changed?.Invoke();
        }

        public bool TryRemoveResource(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return false;
            if (amount <= 0)
                return false;

            var have = GetResource(resourceId);
            if (have < amount)
                return false;

            _resources[resourceId] = Mathf.Max(0, have - amount);
            Changed?.Invoke();
            return true;
        }

        public bool CanAfford(IEnumerable<ToolRecipe.Cost> costs)
        {
            if (costs == null)
                return true;

            foreach (var c in costs)
            {
                if (string.IsNullOrWhiteSpace(c.resourceId))
                    return false;
                if (c.amount <= 0)
                    continue;
                if (GetResource(c.resourceId) < c.amount)
                    return false;
            }

            return true;
        }

        public void Spend(IEnumerable<ToolRecipe.Cost> costs)
        {
            if (costs == null)
                return;

            // Assume affordability already checked.
            foreach (var c in costs)
            {
                if (string.IsNullOrWhiteSpace(c.resourceId))
                    continue;
                if (c.amount <= 0)
                    continue;

                var next = Mathf.Max(0, GetResource(c.resourceId) - c.amount);
                _resources[c.resourceId] = next;
            }

            Changed?.Invoke();
        }

        public void AddTool(string itemId, int maxDurability, ToolType type, ToolTier tier, int hitDamage)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            var tool = new ToolInstance
            {
                itemId = itemId,
                toolType = type,
                tier = tier,
                maxDurability = Mathf.Max(1, maxDurability),
                currentDurability = Mathf.Max(1, maxDurability),
                hitDamage = Mathf.Max(1, hitDamage),
            };

            _tools.Add(tool);

            if (_equippedToolIndex < 0)
                _equippedToolIndex = 0;

            Changed?.Invoke();
        }

        public bool EquipBestOfType(ToolType type)
        {
            if (type == ToolType.None || _tools.Count == 0)
                return false;

            var bestIdx = -1;
            ToolTier bestTier = ToolTier.None;

            for (var i = 0; i < _tools.Count; i++)
            {
                var t = _tools[i];
                if (t == null || t.toolType != type)
                    continue;
                if (bestIdx < 0 || t.tier > bestTier)
                {
                    bestIdx = i;
                    bestTier = t.tier;
                }
            }

            if (bestIdx < 0)
                return false;

            _equippedToolIndex = bestIdx;
            Changed?.Invoke();
            return true;
        }

        public void EquipIndex(int idx)
        {
            if (idx < 0 || idx >= _tools.Count)
            {
                _equippedToolIndex = -1;
                Changed?.Invoke();
                return;
            }

            _equippedToolIndex = idx;
            Changed?.Invoke();
        }

        public bool ConsumeEquippedDurability(int amount = 1)
        {
            var t = EquippedTool;
            if (t == null)
                return false;
            if (amount <= 0)
                return false;

            t.currentDurability = Mathf.Max(0, t.currentDurability - amount);
            if (t.currentDurability > 0)
            {
                Changed?.Invoke();
                return false;
            }

            BreakTool(t);
            return true;
        }

        private void BreakTool(ToolInstance t)
        {
            if (t == null)
                return;

            // Remove from inventory first.
            var idx = _tools.IndexOf(t);
            if (idx >= 0)
                _tools.RemoveAt(idx);

            // Maintain equipped index.
            if (_tools.Count == 0)
                _equippedToolIndex = -1;
            else if (_equippedToolIndex >= _tools.Count)
                _equippedToolIndex = _tools.Count - 1;

            // Pool integration:
            // - DestroyedPool counts broken tool item IDs as destroyed.
            if (DestroyedPoolService.Instance != null)
                DestroyedPoolService.Instance.RegisterDestroyed(t.itemId, 1);

            // - SalvagePool credits 50% of recipe cost (rounded down), NOT to the player.
            var recipe = ToolRecipes.Get(t.itemId);
            if (recipe != null && SalvagePoolService.Instance != null)
            {
                foreach (var c in recipe.SalvageCostsHalf())
                    SalvagePoolService.Instance.AddCredits(c.resourceId, c.amount);
            }

            ToolBroken?.Invoke(t);
            Changed?.Invoke();
        }

        public IEnumerable<string> OwnedToolItemIds()
        {
            return _tools.Where(t => t != null && !string.IsNullOrWhiteSpace(t.itemId)).Select(t => t.itemId);
        }
    }
}

