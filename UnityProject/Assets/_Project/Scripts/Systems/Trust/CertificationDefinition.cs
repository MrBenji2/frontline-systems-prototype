using System;
using System.Collections.Generic;

namespace Frontline.Trust
{
    [Serializable]
    public sealed class CertificationsDefinitions
    {
        public List<CertificationDefinition> ladders = new();
    }

    [Serializable]
    public sealed class CertificationDefinition
    {
        public string ladderId = "";
        public string displayName = "";
        public string category = "";
        public List<CertificationTierDefinition> tiers = new();
    }
}

