using System;
using System.Collections.Generic;

namespace Frontline.Missions
{
    /// <summary>
    /// Container for all mission definitions loaded from JSON.
    /// </summary>
    [Serializable]
    public sealed class MissionsDefinitions
    {
        public List<MissionDefinition> missions = new();
    }

    /// <summary>
    /// Defines a mission that players can accept and complete.
    /// Missions are the primary way players earn certifications and trust.
    /// </summary>
    [Serializable]
    public sealed class MissionDefinition
    {
        /// <summary>Unique identifier for this mission (e.g., "training_basic_rifle").</summary>
        public string missionId = "";

        /// <summary>Display name shown to players.</summary>
        public string displayName = "";

        /// <summary>Description of the mission objectives and rewards.</summary>
        public string description = "";

        /// <summary>Category for UI grouping (e.g., "training", "logistics", "combat").</summary>
        public string category = "";

        /// <summary>If true, mission is automatically assigned to new players.</summary>
        public bool autoAssign;

        /// <summary>If true, mission can be repeated after completion.</summary>
        public bool repeatable;

        /// <summary>Required certification to accept this mission (empty = no requirement).</summary>
        public string requiredCertId = "";

        /// <summary>Required permission to accept this mission (empty = no requirement).</summary>
        public string requiredPermission = "";

        /// <summary>List of objectives that must be completed.</summary>
        public List<MissionObjectiveDefinition> objectives = new();

        /// <summary>Rewards granted upon mission completion.</summary>
        public MissionRewardsDefinition rewards = new();
    }

    /// <summary>
    /// Defines a single objective within a mission.
    /// </summary>
    [Serializable]
    public sealed class MissionObjectiveDefinition
    {
        /// <summary>Unique identifier within the mission (e.g., "hit_targets").</summary>
        public string objectiveId = "";

        /// <summary>Display text for this objective.</summary>
        public string description = "";

        /// <summary>
        /// Type of objective. Supported types:
        /// - "hit_target": Hit a specific number of targets
        /// - "kill_npc": Kill a specific number of NPCs
        /// - "gather_resource": Gather a specific amount of a resource
        /// - "deliver_resource": Deliver resources to a location
        /// - "craft_item": Craft a specific item
        /// - "build_structure": Build a specific structure
        /// - "reach_location": Reach a specific location
        /// </summary>
        public string type = "";

        /// <summary>Target ID for objectives that require a specific target (e.g., resource ID).</summary>
        public string targetId = "";

        /// <summary>Required count to complete this objective.</summary>
        public int requiredCount = 1;

        /// <summary>If true, this objective is optional (mission completes without it).</summary>
        public bool optional;

        /// <summary>Order in which objectives should be displayed/completed (lower = first).</summary>
        public int order;
    }

    /// <summary>
    /// Defines rewards granted upon mission completion.
    /// </summary>
    [Serializable]
    public sealed class MissionRewardsDefinition
    {
        /// <summary>Trust points awarded.</summary>
        public int trustPoints;

        /// <summary>Certification ID to grant (empty = none).</summary>
        public string grantCertification = "";

        /// <summary>Items awarded (list of item IDs with quantities).</summary>
        public List<MissionItemReward> items = new();
    }

    /// <summary>
    /// Defines an item reward for mission completion.
    /// </summary>
    [Serializable]
    public sealed class MissionItemReward
    {
        public string itemId = "";
        public int quantity = 1;
    }
}
