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
    /// 
    /// Inspired by Star Wars Galaxies mission terminal system:
    /// - Terminals display available missions with title, description, time limit, rewards
    /// - Missions scale in difficulty with increasing rewards
    /// - Location data enables waypoint display and world integration
    /// </summary>
    [Serializable]
    public sealed class MissionDefinition
    {
        // === Identification ===

        /// <summary>Unique identifier for this mission (e.g., "training_basic_rifle").</summary>
        public string missionId = "";

        /// <summary>Display name shown to players and terminals.</summary>
        public string displayName = "";

        /// <summary>Description of the mission objectives and context.</summary>
        public string description = "";

        /// <summary>Category for UI grouping (e.g., "training", "logistics", "combat", "recon").</summary>
        public string category = "";

        /// <summary>Difficulty level (1-10). Higher difficulty = better rewards. Used for terminal filtering.</summary>
        public int difficulty = 1;

        // === Prerequisites ===

        /// <summary>Required certifications to accept this mission (empty = no requirement).</summary>
        public List<string> requiredCertifications = new();

        /// <summary>Required permissions to accept this mission (empty = no requirement).</summary>
        public List<string> requiredPermissions = new();

        /// <summary>Minimum trust score required to accept this mission.</summary>
        public int minTrustScore;

        /// <summary>Minimum rank ID required (e.g., "e3" for Lance Corporal).</summary>
        public string minRankId = "";

        // === Timing ===

        /// <summary>Time limit in seconds to complete the mission (0 = no limit).</summary>
        public float timeLimit;

        // === Assignment ===

        /// <summary>If true, mission is automatically assigned to new players.</summary>
        public bool autoAssign;

        /// <summary>If true, mission can be repeated after completion.</summary>
        public bool repeatable;

        /// <summary>Cooldown in seconds before a repeatable mission can be accepted again.</summary>
        public float repeatCooldown;

        // === Terminal & Faction ===

        /// <summary>If true, this mission appears at mission terminals.</summary>
        public bool terminalAvailable;

        /// <summary>Faction alignment required (empty = neutral/any, "FactionA", "FactionB").</summary>
        public string faction = "";

        /// <summary>Terminal types that can offer this mission (empty = all terminals).</summary>
        public List<string> terminalTypes = new();

        // === Objectives ===

        /// <summary>List of objectives that must be completed.</summary>
        public List<MissionObjectiveDefinition> objectives = new();

        // === Rewards ===

        /// <summary>Rewards granted upon mission completion.</summary>
        public MissionRewardsDefinition rewards = new();

        // === Legacy Compatibility ===
        // These fields maintain backward compatibility with existing mission definitions

        /// <summary>[Legacy] Single required certification. Use requiredCertifications list instead.</summary>
        public string requiredCertId
        {
            get => requiredCertifications.Count > 0 ? requiredCertifications[0] : "";
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (requiredCertifications.Count == 0)
                        requiredCertifications.Add(value);
                    else
                        requiredCertifications[0] = value;
                }
            }
        }

        /// <summary>[Legacy] Single required permission. Use requiredPermissions list instead.</summary>
        public string requiredPermission
        {
            get => requiredPermissions.Count > 0 ? requiredPermissions[0] : "";
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (requiredPermissions.Count == 0)
                        requiredPermissions.Add(value);
                    else
                        requiredPermissions[0] = value;
                }
            }
        }
    }

    /// <summary>
    /// Objective type enumeration for type-safe handling.
    /// </summary>
    public enum MissionObjectiveType
    {
        Unknown = 0,
        ReachLocation,
        HitTarget,
        GatherResource,
        DeliverResource,
        KillNpc,
        CraftItem,
        BuildStructure,
        DefendLocation,
        EscortTarget,
        SabotageTarget,
        RepairStructure
    }

    /// <summary>
    /// Defines a single objective within a mission.
    /// Includes location data for world integration and waypoint display.
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
        /// - "reach_location": Reach a specific location
        /// - "hit_target": Hit a specific number of targets
        /// - "kill_npc": Kill a specific number of NPCs
        /// - "gather_resource": Gather a specific amount of a resource
        /// - "deliver_resource": Deliver resources to a location
        /// - "craft_item": Craft a specific item
        /// - "build_structure": Build a specific structure
        /// - "defend_location": Defend a location for a duration
        /// - "escort_target": Escort a target to a destination
        /// - "sabotage_target": Sabotage an enemy structure
        /// - "repair_structure": Repair a damaged structure
        /// </summary>
        public string type = "";

        /// <summary>Target ID for objectives that require a specific target (e.g., resource ID, NPC type).</summary>
        public string targetId = "";

        /// <summary>Required count to complete this objective.</summary>
        public int requiredCount = 1;

        /// <summary>If true, this objective is optional (mission completes without it).</summary>
        public bool optional;

        /// <summary>Order in which objectives should be displayed/completed (lower = first).</summary>
        public int order;

        /// <summary>World location for this objective (x, y, z coordinates). Used for waypoints.</summary>
        public MissionLocation location = new();

        /// <summary>Radius around location that satisfies reach_location objectives (meters).</summary>
        public float locationRadius = 5f;

        /// <summary>Duration in seconds for time-based objectives (e.g., defend_location).</summary>
        public float duration;

        /// <summary>Parses the type string to MissionObjectiveType enum.</summary>
        public MissionObjectiveType GetObjectiveType()
        {
            return type?.ToLowerInvariant() switch
            {
                "reach_location" => MissionObjectiveType.ReachLocation,
                "hit_target" => MissionObjectiveType.HitTarget,
                "gather_resource" => MissionObjectiveType.GatherResource,
                "deliver_resource" => MissionObjectiveType.DeliverResource,
                "kill_npc" => MissionObjectiveType.KillNpc,
                "craft_item" => MissionObjectiveType.CraftItem,
                "build_structure" => MissionObjectiveType.BuildStructure,
                "defend_location" => MissionObjectiveType.DefendLocation,
                "escort_target" => MissionObjectiveType.EscortTarget,
                "sabotage_target" => MissionObjectiveType.SabotageTarget,
                "repair_structure" => MissionObjectiveType.RepairStructure,
                _ => MissionObjectiveType.Unknown
            };
        }
    }

    /// <summary>
    /// Represents a world location with x, y, z coordinates.
    /// Used for objective locations, waypoints, and spatial queries.
    /// </summary>
    [Serializable]
    public sealed class MissionLocation
    {
        public float x;
        public float y;
        public float z;

        /// <summary>If true, this location has valid coordinates.</summary>
        public bool IsValid => x != 0 || y != 0 || z != 0;

        /// <summary>Converts to Unity Vector3.</summary>
        public UnityEngine.Vector3 ToVector3() => new(x, y, z);

        /// <summary>Creates from Unity Vector3.</summary>
        public static MissionLocation FromVector3(UnityEngine.Vector3 v) => new() { x = v.x, y = v.y, z = v.z };

        /// <summary>Distance to another location.</summary>
        public float DistanceTo(MissionLocation other)
        {
            var dx = x - other.x;
            var dy = y - other.y;
            var dz = z - other.z;
            return UnityEngine.Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Distance to a Vector3 position.</summary>
        public float DistanceTo(UnityEngine.Vector3 pos)
        {
            var dx = x - pos.x;
            var dy = y - pos.y;
            var dz = z - pos.z;
            return UnityEngine.Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    /// <summary>
    /// Defines rewards granted upon mission completion.
    /// Rewards scale with mission difficulty.
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

        /// <summary>Credits/currency awarded.</summary>
        public int credits;

        /// <summary>Experience points awarded (for future XP system, not stat bonuses).</summary>
        public int experience;
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
