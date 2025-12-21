using System;
using System.Collections.Generic;

namespace Frontline.Economy
{
    [Serializable]
    public sealed class CreatedPoolSnapshot
    {
        public List<Entry> createdCounts = new();

        [Serializable]
        public sealed class Entry
        {
            public string id = "";
            public int count;
        }
    }
}

