using System;
using System.Collections.Generic;

namespace Frontline.Trust
{
    [Serializable]
    public sealed class CertificationTierDefinition
    {
        public int tier;
        public string certId = "";
        public string displayName = "";
        public List<string> permissions = new();
        public CertificationRequirementsDefinition requirements = new();
        public int version = 1;
        public CertificationExpiresDefinition expires = new();
    }

    [Serializable]
    public sealed class CertificationRequirementsDefinition
    {
        public string type = "";
    }

    [Serializable]
    public sealed class CertificationExpiresDefinition
    {
        public string mode = "";

        // JsonUtility cannot reliably represent nullable primitives (null -> default).
        // Represent as string to allow JSON "days": null.
        public string days = null;
    }
}

