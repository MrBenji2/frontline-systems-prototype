using System;
using System.Collections.Generic;
using Frontline.Missions;
using Frontline.Trust;

namespace Frontline.Definitions
{
    [Serializable]
    public sealed class GameDefinitions
    {
        public List<MaterialDef> materials = new();
        public List<WeaponDef> weapons = new();
        public List<StructureDef> structures = new();
        public List<VehicleDef> vehicles = new();
        public List<ItemDef> items = new();

        // Milestone 7 (Trust): data-driven certifications + ranks.
        public CertificationsDefinitions certifications = new();
        public RanksDefinitions ranks = new();

        // Milestone 8 (Missions): data-driven mission system.
        public MissionsDefinitions missions = new();
    }

    [Serializable]
    public sealed class CraftCost
    {
        public string materialId = "";
        public int amount;
    }

    [Serializable]
    public sealed class MaterialDef
    {
        public string id = "";
        public string displayName = "";
        public int tier;
    }

    [Serializable]
    public sealed class WeaponDef
    {
        public string id = "";
        public string displayName = "";
        public List<CraftCost> craftCosts = new();

        // Milestone 6.1 melee stats (safe defaults for older defs).
        public float rangeMeters;
        public int damage;
        public float speed;
        public int maxDurability;
    }

    [Serializable]
    public sealed class StructureDef
    {
        public string id = "";
        public string displayName = "";
        public List<CraftCost> craftCosts = new();

        // Milestone 5.3 foundation metadata (additive).
        public string description = "";
        public string requiredSkillId = "";
        public List<string> builtAtStations = new();
    }

    [Serializable]
    public sealed class VehicleUpgradeSlotDef
    {
        public string slotId = "";
        public string displayName = "";
    }

    [Serializable]
    public sealed class VehicleDef
    {
        public string id = "";
        public string displayName = "";
        public List<VehicleUpgradeSlotDef> upgradeSlots = new();
        public List<CraftCost> craftCosts = new();
    }

    [Serializable]
    public sealed class ItemDef
    {
        public string id = "";
        public string displayName = "";
    }
}

