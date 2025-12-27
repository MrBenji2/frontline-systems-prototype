using System;
using System.Collections.Generic;

namespace Frontline.Trust
{
    [Serializable]
    public sealed class RanksDefinitions
    {
        public List<RankDefinition> factionRanks = new();
    }

    [Serializable]
    public sealed class RankDefinition
    {
        public string rankId = "";
        public string displayName = "";
        public int minTrust;
        public string track = "enlisted"; // "enlisted" or "officer"
    }
}

