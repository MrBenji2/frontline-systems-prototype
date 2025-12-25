using System;

namespace Frontline.Trust
{
    [Serializable]
    public sealed class PlayerCertificationState
    {
        public string certId = "";
        public int tier;
        public int versionEarned;
        public bool isActive = true;
        public bool isExpired;
        public long earnedUtc;
        public long lastUsedUtc;
        public string ladderId = "";
    }
}

