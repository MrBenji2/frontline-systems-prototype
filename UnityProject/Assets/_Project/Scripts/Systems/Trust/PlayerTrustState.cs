using System;
using System.Collections.Generic;

namespace Frontline.Trust
{
    [Serializable]
    public sealed class PlayerTrustState
    {
        public string playerId = "local";
        public FactionId faction = FactionId.Neutral;
        public int trustScore;
        public string rankId = "r0";

        // Runtime convenience index (not persisted directly by JsonUtility).
        [NonSerialized] public Dictionary<string, PlayerCertificationState> certsById = new(StringComparer.Ordinal);

        public void RebuildIndexFromList(List<PlayerCertificationState> certs)
        {
            certsById = new Dictionary<string, PlayerCertificationState>(StringComparer.Ordinal);
            if (certs == null)
                return;
            foreach (var c in certs)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.certId))
                    continue;
                certsById[c.certId] = c;
            }
        }
    }
}

