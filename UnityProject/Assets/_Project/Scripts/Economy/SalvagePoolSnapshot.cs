using System;
using System.Collections.Generic;

namespace Frontline.Economy
{
    [Serializable]
    public sealed class SalvagePoolSnapshot
    {
        public List<Entry> credits = new();

        [Serializable]
        public sealed class Entry
        {
            public string id = "";
            public int amount;
        }
    }
}

