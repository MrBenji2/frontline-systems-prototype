using System;
using System.Collections.Generic;

namespace Frontline.Economy
{
    [Serializable]
    public sealed class DestroyedPoolSnapshot
    {
        public List<Entry> destroyedCounts = new();
        public List<string> craftedEver = new();
        public List<Entry> destroyedButUncraftedCounts = new();

        [Serializable]
        public sealed class Entry
        {
            public string id = "";
            public int count;
        }
    }
}

