using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontline.Vehicles
{
    [Serializable]
    public sealed class TransportTruckWorldSnapshot
    {
        [Serializable]
        public sealed class ItemStack
        {
            public string itemId = "";
            public int count;
        }

        [Serializable]
        public sealed class TruckEntry
        {
            public bool exists;
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
            public int currentHp;
            public List<ItemStack> stored = new();
        }

        public TruckEntry truck = new();
    }
}

