using UnityEngine;

namespace Frontline.Core
{
    /// <summary>
    /// Shared world-scale constants (Patch Edition).
    /// Single source of truth for vertical snapping/stacking units.
    /// </summary>
    public static class WorldConstants
    {
        // One vertical "level" in world meters.
        // Used for stacking buildables and future vehicles/air layers.
        public const float WORLD_LEVEL_HEIGHT = 1.0f;
    }
}

