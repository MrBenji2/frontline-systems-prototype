using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Frontline.Crafting;
using Frontline.Economy;
using UnityEngine;

namespace Frontline.Gameplay
{
    /// <summary>
    /// Patch 7.1D: Equipment slot categories for the 5-slot system.
    /// Slots are 1-indexed to match UI (Slot 1-5 correspond to number keys 1-5).
    /// </summary>
    public enum EquipmentSlot
    {
        None = 0,
        Primary = 1,      // Slot 1: Primary weapons (rifles, melee weapons)
        Secondary = 2,    // Slot 2: Secondary weapons (pistols, knives)
        Throwable = 3,    // Slot 3: Throwable items (grenades, throwing weapons)
        Deployable = 4,   // Slot 4: Deployable items (portable shield, camp/tent, workbench)
        Medical = 5       // Slot 5: Medical pocket (medkits, bandages)
    }

    public sealed class PlayerInventoryService : MonoBehaviour
    {
        // Patch 7B: repairing tools/weapons costs a fraction of craft cost.
        public const float REPAIR_FRACTION = 0.25f;

        // Patch 7.1D: Number of equipment slots (1-5).
        public const int SLOT_COUNT = 5;

        // Milestone 7.2: Weight system constants.
        public const float DEFAULT_MAX_CARRY_WEIGHT = 100f;
        public const float RESOURCE_WEIGHT = 0.5f; // Weight per unit of resource
        public const float PACKAGED_ITEM_WEIGHT_MULTIPLIER = 1.5f; // Extra weight for unslotted tools

        [Serializable]
        public sealed class ToolInstance
        {
            public string itemId = "";
            public ToolType toolType = ToolType.None;
            public ToolTier tier = ToolTier.None;
            public int maxDurability = 1;
            public int currentDurability = 1;
            public int hitDamage = 1;
            // Patch 7.1D: Track which slot this tool occupies (1-5, or 0 for unslotted).
            public int slotNumber = 0;
        }

        public static PlayerInventoryService Instance { get; private set; }

        public event Action Changed;
        public event Action<ToolInstance> ToolBroken;

        [Header("Starter (to avoid tool/resource deadlock)")]
        [SerializeField] private bool grantStarterWood = true;
        [SerializeField] private int starterWoodAmount = 10;

        [Header("Milestone 7.2: Weight System")]
        [SerializeField] private float maxCarryWeight = DEFAULT_MAX_CARRY_WEIGHT;

        private readonly Dictionary<string, int> _resources = new(StringComparer.Ordinal);
        private readonly List<ToolInstance> _tools = new();

        // Patch 7.1D: Fixed 5-slot equipment system (1-indexed, index 0 unused).
        // Each slot holds a reference to a tool in _tools, or null if empty.
        private readonly ToolInstance[] _equippedSlots = new ToolInstance[SLOT_COUNT + 1]; // Index 0 unused
        private int _activeSlot = 1; // Currently selected slot (1-5)

        private string ToolsSavePath => Path.Combine(Application.persistentDataPath, "player_tools.json");

        public IReadOnlyDictionary<string, int> Resources => _resources;
        public IReadOnlyList<ToolInstance> Tools => _tools;

        /// <summary>
        /// Gets the currently equipped tool from the active slot (1-5).
        /// Returns null if active slot is empty.
        /// </summary>
        public ToolInstance EquippedTool =>
            _activeSlot >= 1 && _activeSlot <= SLOT_COUNT ? _equippedSlots[_activeSlot] : null;

        /// <summary>
        /// Gets the 1-based active slot number (1-5).
        /// </summary>
        public int ActiveSlot => _activeSlot;

        /// <summary>
        /// Legacy compatibility: returns the index of the equipped tool in the tools list.
        /// Returns -1 if no tool is equipped.
        /// </summary>
        public int EquippedToolIndex
        {
            get
            {
                var tool = EquippedTool;
                return tool != null ? _tools.IndexOf(tool) : -1;
            }
        }

        /// <summary>
        /// Gets the tool in a specific slot (1-5). Returns null if slot is empty.
        /// </summary>
        public ToolInstance GetToolInSlot(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > SLOT_COUNT)
                return null;
            return _equippedSlots[slotNumber];
        }

        /// <summary>
        /// Gets the appropriate equipment slot for a tool type.
        /// </summary>
        public static EquipmentSlot GetSlotForToolType(ToolType type)
        {
            return type switch
            {
                // Primary slot (1): main weapons and tools
                ToolType.MeleeWeapon => EquipmentSlot.Primary,
                ToolType.Pickaxe => EquipmentSlot.Primary,
                ToolType.Axe => EquipmentSlot.Primary,
                ToolType.Hammer => EquipmentSlot.Primary,
                ToolType.Shovel => EquipmentSlot.Primary,
                ToolType.Wrench => EquipmentSlot.Primary,

                // Secondary slot (2): sidearms and small weapons
                ToolType.Knife => EquipmentSlot.Secondary,

                // Throwable slot (3): grenades, throwing weapons
                ToolType.Throwable => EquipmentSlot.Throwable,

                // Deployable slot (4): portable structures, equipment
                ToolType.Deployable => EquipmentSlot.Deployable,
                ToolType.GasCan => EquipmentSlot.Deployable,

                // Medical slot (5): healing items
                ToolType.Medical => EquipmentSlot.Medical,

                _ => EquipmentSlot.Primary
            };
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize slots to null.
            for (var i = 0; i <= SLOT_COUNT; i++)
                _equippedSlots[i] = null;

            if (grantStarterWood && _resources.Count == 0 && _tools.Count == 0)
                AddResource(ToolRecipes.Wood, starterWoodAmount);
        }

        private void Update()
        {
            // Patch 7.1D: Handle slot switching with number keys 1-5.
            // Don't process while in build mode (those keys select buildables instead).
            if (Buildables.BuildablesService.Instance != null && Buildables.BuildablesService.Instance.IsBuildModeActive)
                return;

            // Don't process while any modal UI is open.
            if (UI.UiModalManager.Instance != null && UI.UiModalManager.Instance.HasOpenModal)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) SetActiveSlot(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SetActiveSlot(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SetActiveSlot(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SetActiveSlot(4);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) SetActiveSlot(5);
        }

        /// <summary>
        /// Sets the active equipment slot (1-5).
        /// </summary>
        public void SetActiveSlot(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > SLOT_COUNT)
                return;
            if (_activeSlot == slotNumber)
                return;

            _activeSlot = slotNumber;
            Changed?.Invoke();
        }

        [Serializable]
        private sealed class PlayerToolsSnapshot
        {
            public int activeSlot = 1;
            public List<ToolInstance> tools = new();
            // Legacy field for backwards compatibility.
            public int equippedToolIndex = -1;
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>
        /// Milestone 6.1: minimal persistence for tool/weapons durability via explicit Save/Load actions.
        /// Patch 7.1D: Updated to save slot assignments.
        /// </summary>
        public void SaveToolsToDisk()
        {
            try
            {
                var snap = new PlayerToolsSnapshot
                {
                    activeSlot = _activeSlot,
                    tools = _tools.Where(t => t != null).ToList()
                };

                var json = JsonUtility.ToJson(snap, true);
                File.WriteAllText(ToolsSavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PlayerInventory: failed to save tools '{ToolsSavePath}': {ex.Message}");
            }
        }

        public void LoadToolsFromDisk()
        {
            try
            {
                if (!File.Exists(ToolsSavePath))
                    return;

                var json = File.ReadAllText(ToolsSavePath);
                var snap = JsonUtility.FromJson<PlayerToolsSnapshot>(json);
                if (snap == null)
                    return;

                _tools.Clear();
                for (var i = 0; i <= SLOT_COUNT; i++)
                    _equippedSlots[i] = null;

                if (snap.tools != null)
                {
                    foreach (var t in snap.tools)
                    {
                        if (t == null || string.IsNullOrWhiteSpace(t.itemId))
                            continue;
                        t.maxDurability = Mathf.Max(1, t.maxDurability);
                        t.currentDurability = Mathf.Clamp(t.currentDurability, 0, t.maxDurability);
                        t.hitDamage = Mathf.Max(1, t.hitDamage);

                        // Patch 7.1D: Ensure slot number is valid (1-5).
                        if (t.slotNumber < 1 || t.slotNumber > SLOT_COUNT)
                            t.slotNumber = (int)GetSlotForToolType(t.toolType);

                        _tools.Add(t);

                        // Assign to slot if not already occupied.
                        if (t.slotNumber >= 1 && t.slotNumber <= SLOT_COUNT && _equippedSlots[t.slotNumber] == null)
                            _equippedSlots[t.slotNumber] = t;
                    }
                }

                _activeSlot = Mathf.Clamp(snap.activeSlot, 1, SLOT_COUNT);
                Changed?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PlayerInventory: failed to load tools '{ToolsSavePath}': {ex.Message}");
            }
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

            // Patch 7.1D: Determine the appropriate slot for this tool type.
            var targetSlot = (int)GetSlotForToolType(type);

            var tool = new ToolInstance
            {
                itemId = itemId,
                toolType = type,
                tier = tier,
                maxDurability = Mathf.Max(1, maxDurability),
                currentDurability = Mathf.Max(1, maxDurability),
                hitDamage = Mathf.Max(1, hitDamage),
                slotNumber = targetSlot
            };

            _tools.Add(tool);

            // Patch 7.1D: Auto-equip to the appropriate slot if empty.
            if (targetSlot >= 1 && targetSlot <= SLOT_COUNT && _equippedSlots[targetSlot] == null)
            {
                _equippedSlots[targetSlot] = tool;
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// Patch 7.1D: Adds a tool to a specific slot (1-5).
        /// If the slot is occupied, the existing tool is replaced (moved to unslotted).
        /// </summary>
        public void AddToolToSlot(string itemId, int maxDurability, ToolType type, ToolTier tier, int hitDamage, int slotNumber)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;
            if (slotNumber < 1 || slotNumber > SLOT_COUNT)
            {
                AddTool(itemId, maxDurability, type, tier, hitDamage);
                return;
            }

            var tool = new ToolInstance
            {
                itemId = itemId,
                toolType = type,
                tier = tier,
                maxDurability = Mathf.Max(1, maxDurability),
                currentDurability = Mathf.Max(1, maxDurability),
                hitDamage = Mathf.Max(1, hitDamage),
                slotNumber = slotNumber
            };

            _tools.Add(tool);

            // Replace existing tool in slot (mark old as unslotted).
            var existingTool = _equippedSlots[slotNumber];
            if (existingTool != null)
                existingTool.slotNumber = 0;

            _equippedSlots[slotNumber] = tool;
            Changed?.Invoke();
        }

        public bool EquipBestOfType(ToolType type)
        {
            if (type == ToolType.None || _tools.Count == 0)
                return false;

            // Patch 7.1D: Find the best tool of this type and equip it to its appropriate slot.
            var targetSlot = (int)GetSlotForToolType(type);
            ToolInstance best = null;
            ToolTier bestTier = ToolTier.None;

            foreach (var t in _tools)
            {
                if (t == null || t.toolType != type)
                    continue;
                if (best == null || t.tier > bestTier)
                {
                    best = t;
                    bestTier = t.tier;
                }
            }

            if (best == null)
                return false;

            // Equip to the appropriate slot.
            EquipToolToSlot(best, targetSlot);
            return true;
        }

        /// <summary>
        /// Legacy method: equips a tool by its index in the tools list.
        /// Patch 7.1D: This now assigns the tool to its appropriate slot.
        /// </summary>
        public void EquipIndex(int idx)
        {
            if (idx < 0 || idx >= _tools.Count)
            {
                // Clear the active slot.
                if (_activeSlot >= 1 && _activeSlot <= SLOT_COUNT)
                {
                    var existingTool = _equippedSlots[_activeSlot];
                    if (existingTool != null)
                        existingTool.slotNumber = 0;
                    _equippedSlots[_activeSlot] = null;
                }
                Changed?.Invoke();
                return;
            }

            var tool = _tools[idx];
            if (tool == null)
                return;

            var targetSlot = (int)GetSlotForToolType(tool.toolType);
            EquipToolToSlot(tool, targetSlot);
        }

        /// <summary>
        /// Patch 7.1D: Equips a tool to a specific slot (1-5).
        /// </summary>
        public void EquipToolToSlot(ToolInstance tool, int slotNumber)
        {
            if (tool == null)
                return;
            if (slotNumber < 1 || slotNumber > SLOT_COUNT)
                return;

            // Remove tool from its current slot if any.
            if (tool.slotNumber >= 1 && tool.slotNumber <= SLOT_COUNT && _equippedSlots[tool.slotNumber] == tool)
                _equippedSlots[tool.slotNumber] = null;

            // Clear existing tool from target slot.
            var existingTool = _equippedSlots[slotNumber];
            if (existingTool != null && existingTool != tool)
                existingTool.slotNumber = 0;

            // Assign tool to new slot.
            _equippedSlots[slotNumber] = tool;
            tool.slotNumber = slotNumber;

            // Switch to the newly equipped slot.
            _activeSlot = slotNumber;
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

        /// <summary>
        /// Patch 7B: Repairs a tool back to max durability at fractional recipe cost:
        /// cost = craftCost * REPAIR_FRACTION * missingPercent (per resource, rounded up).
        /// </summary>
        public bool TryRepairTool(int toolIndex, float repairFraction = REPAIR_FRACTION)
        {
            if (toolIndex < 0 || toolIndex >= _tools.Count)
                return false;

            var t = _tools[toolIndex];
            if (t == null)
                return false;

            var max = Mathf.Max(1, t.maxDurability);
            var cur = Mathf.Clamp(t.currentDurability, 0, max);
            if (cur >= max)
                return false; // nothing to repair

            var recipe = ToolRecipes.Get(t.itemId);
            if (recipe == null || recipe.costs == null || recipe.costs.Count == 0)
                return false;

            repairFraction = Mathf.Clamp01(repairFraction);
            var missingPercent = (max - cur) / (float)max;
            if (missingPercent <= 0.0001f)
                return false;

            // Compute repair costs.
            var costs = new List<ToolRecipe.Cost>();
            foreach (var c in recipe.costs)
            {
                if (string.IsNullOrWhiteSpace(c.resourceId) || c.amount <= 0)
                    continue;
                var amt = Mathf.CeilToInt(c.amount * repairFraction * missingPercent);
                if (amt > 0)
                    costs.Add(new ToolRecipe.Cost { resourceId = c.resourceId, amount = amt });
            }

            if (!CanAfford(costs))
                return false;

            Spend(costs);
            t.currentDurability = max;
            Changed?.Invoke();
            return true;
        }

        private void BreakTool(ToolInstance t)
        {
            if (t == null)
                return;

            // Patch 7.1D: Clear from slot first.
            if (t.slotNumber >= 1 && t.slotNumber <= SLOT_COUNT && _equippedSlots[t.slotNumber] == t)
                _equippedSlots[t.slotNumber] = null;

            // Remove from inventory.
            var idx = _tools.IndexOf(t);
            if (idx >= 0)
                _tools.RemoveAt(idx);

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

        #region Milestone 7.2: Weight System

        /// <summary>
        /// Gets the maximum carry weight for the player.
        /// </summary>
        public float MaxCarryWeight => maxCarryWeight;

        /// <summary>
        /// Calculates the total weight of all carried items (resources + tools).
        /// </summary>
        public float GetTotalWeight()
        {
            var totalWeight = 0f;

            // Calculate resource weight.
            foreach (var kv in _resources)
            {
                if (kv.Value <= 0)
                    continue;
                totalWeight += kv.Value * GetResourceWeight(kv.Key);
            }

            // Calculate tool/weapon weight.
            foreach (var tool in _tools)
            {
                if (tool == null)
                    continue;
                var baseWeight = GetToolWeight(tool.toolType);

                // Unslotted (packaged) items are heavier.
                if (tool.slotNumber == 0)
                    baseWeight *= PACKAGED_ITEM_WEIGHT_MULTIPLIER;

                totalWeight += baseWeight;
            }

            return totalWeight;
        }

        /// <summary>
        /// Gets the weight ratio (0.0 to 1.0+).
        /// </summary>
        public float GetWeightRatio()
        {
            if (maxCarryWeight <= 0f)
                return 1f;
            return GetTotalWeight() / maxCarryWeight;
        }

        /// <summary>
        /// Returns true if the player is carrying more than max capacity.
        /// </summary>
        public bool IsOverEncumbered()
        {
            return GetTotalWeight() > maxCarryWeight;
        }

        /// <summary>
        /// Gets the movement speed multiplier based on carried weight.
        /// 0-30% = 1.0 (no penalty)
        /// 30-60% = slight slowdown (0.85)
        /// 60-90% = heavy slowdown (0.65)
        /// >90% = extreme slowdown (0.4)
        /// </summary>
        public float GetWeightSpeedMultiplier()
        {
            var ratio = GetWeightRatio();
            if (ratio <= 0.3f) return 1.0f;
            if (ratio <= 0.6f) return 0.85f;
            if (ratio <= 0.9f) return 0.65f;
            return 0.4f;
        }

        /// <summary>
        /// Gets the maximum step height based on carried weight.
        /// Lighter = can climb higher. Heavier = reduced climb ability.
        /// </summary>
        public float GetWeightMaxStepHeight(float baseStepHeight)
        {
            var ratio = GetWeightRatio();
            if (ratio <= 0.3f) return baseStepHeight;
            if (ratio <= 0.6f) return baseStepHeight * 0.8f;
            if (ratio <= 0.9f) return baseStepHeight * 0.5f;
            return baseStepHeight * 0.25f;
        }

        /// <summary>
        /// Gets the stamina drain multiplier based on carried weight (hook for future stamina system).
        /// </summary>
        public float GetWeightStaminaDrainMultiplier()
        {
            var ratio = GetWeightRatio();
            if (ratio <= 0.3f) return 1.0f;
            if (ratio <= 0.6f) return 1.5f;
            if (ratio <= 0.9f) return 2.5f;
            return 4.0f;
        }

        /// <summary>
        /// Gets the weight of a resource type.
        /// </summary>
        public static float GetResourceWeight(string resourceId)
        {
            // All resources have the same weight for now.
            // Can be expanded with a lookup table if needed.
            return RESOURCE_WEIGHT;
        }

        /// <summary>
        /// Gets the base weight of a tool type.
        /// Large weapons are heavier.
        /// </summary>
        public static float GetToolWeight(ToolType toolType)
        {
            return toolType switch
            {
                ToolType.None => 0f,
                // Large weapons (rifles, melee weapons): heavier
                ToolType.MeleeWeapon => 5f,
                ToolType.Axe => 4f,
                ToolType.Shovel => 3f,
                ToolType.Pickaxe => 4f,
                ToolType.Hammer => 3f,
                ToolType.Wrench => 2f,
                // Small items: lighter
                ToolType.Knife => 1f,
                ToolType.Throwable => 0.5f,
                ToolType.GasCan => 8f,  // Heavy when full
                ToolType.Deployable => 10f, // Portable structures are heavy
                ToolType.Medical => 1f,
                _ => 2f
            };
        }

        /// <summary>
        /// Debug: logs current weight status.
        /// </summary>
        public void DebugLogWeightStatus()
        {
            var total = GetTotalWeight();
            var ratio = GetWeightRatio();
            var speedMult = GetWeightSpeedMultiplier();
            Debug.Log($"[Weight] Total: {total:F1}/{maxCarryWeight:F1} ({ratio * 100:F0}%) | Speed: {speedMult:F2}x | Encumbered: {IsOverEncumbered()}");
        }

        #endregion
    }
}

