using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontline.Buildables
{
    [Serializable]
    public sealed class BuildablesWorldSnapshot
    {
        [Serializable]
        public sealed class ItemStack
        {
            public string itemId = "";
            public int count;
        }

        [Serializable]
        public sealed class BuildableEntry
        {
            public string itemId = "";
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
            public int currentHp;
            public int ownerTeam;
            public string ownerId = "";

            // Buildable-specific state (additive; safe defaults on old saves).
            public bool gateOpen;

            // Only used for storage crates.
            public List<ItemStack> stored = new();
        }

        public List<BuildableEntry> buildables = new();
    }
}

