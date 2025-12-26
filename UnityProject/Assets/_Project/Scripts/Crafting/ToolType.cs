namespace Frontline.Crafting
{
    public enum ToolType
    {
        None = 0,
        Axe = 1,
        Shovel = 2,
        Wrench = 3,
        Hammer = 4,
        GasCan = 5,

        // Milestone 6.1: melee weapons are treated as durable tools for inventory/equip/repair.
        MeleeWeapon = 6,

        // Patch 7.1D: Additional tool types for the 5-slot equipment system.
        Pickaxe = 7,
        Knife = 8,           // Secondary slot weapon
        Throwable = 9,       // Grenades, throwing knives, etc.
        Deployable = 10,     // Portable shield, camp/tent, workbench
        Medical = 11         // Medkits, bandages, etc.
    }
}

