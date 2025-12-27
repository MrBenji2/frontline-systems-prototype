using System;
using System.Collections.Generic;

namespace Frontline.PlayerCard
{
    /// <summary>
    /// Comprehensive player statistics tracked across wars.
    /// These stats are displayed on the Player Card and used for promotion eligibility.
    /// </summary>
    [Serializable]
    public sealed class PlayerStats
    {
        // === Identity ===
        /// <summary>Current display name.</summary>
        public string displayName = "Soldier";

        /// <summary>History of name changes with timestamps.</summary>
        public List<NameChangeRecord> nameHistory = new();

        /// <summary>UTC timestamp of account creation.</summary>
        public long createdUtc;

        /// <summary>UUID for this player (persists across name changes).</summary>
        public string playerId = "";

        // === Combat Stats ===
        /// <summary>Total enemies killed (NPCs and players).</summary>
        public int kills;

        /// <summary>Total player kills specifically.</summary>
        public int playerKills;

        /// <summary>Total NPC kills.</summary>
        public int npcKills;

        /// <summary>Total deaths.</summary>
        public int deaths;

        /// <summary>Total friendly fire incidents (hitting allies).</summary>
        public int friendlyFireIncidents;

        /// <summary>Total damage dealt to enemies.</summary>
        public long damageDealt;

        /// <summary>Total damage taken.</summary>
        public long damageTaken;

        // === Medical Stats ===
        /// <summary>Total teammates revived.</summary>
        public int revives;

        /// <summary>Total healing done to teammates.</summary>
        public int healingDone;

        // === Logistics Stats ===
        /// <summary>Total resources gathered (any type).</summary>
        public long resourcesGathered;

        /// <summary>Total resources delivered to depots/front lines.</summary>
        public long resourcesDelivered;

        /// <summary>Total supply runs completed.</summary>
        public int supplyRunsCompleted;

        /// <summary>Total distance traveled while carrying cargo (meters).</summary>
        public float cargoDistanceTraveled;

        // === Engineering Stats ===
        /// <summary>Total structures built.</summary>
        public int structuresBuilt;

        /// <summary>Total structures repaired.</summary>
        public int structuresRepaired;

        /// <summary>Total structures demolished (controlled demolition).</summary>
        public int structuresDemolished;

        /// <summary>Total fortifications upgraded.</summary>
        public int fortificationsUpgraded;

        // === Vehicle Stats ===
        /// <summary>Total distance driven (meters).</summary>
        public float distanceDriven;

        /// <summary>Total vehicles operated.</summary>
        public int vehiclesOperated;

        /// <summary>Total vehicle collisions caused.</summary>
        public int vehicleCollisions;

        // === Mission Stats ===
        /// <summary>Total missions completed.</summary>
        public int missionsCompleted;

        /// <summary>Total missions abandoned.</summary>
        public int missionsAbandoned;

        /// <summary>Total missions failed.</summary>
        public int missionsFailed;

        /// <summary>Training missions completed.</summary>
        public int trainingMissionsCompleted;

        // === War Stats ===
        /// <summary>Total wars participated in.</summary>
        public int warsParticipated;

        /// <summary>Total wars won (on winning faction).</summary>
        public int warsWon;

        /// <summary>Total time served in seconds.</summary>
        public long timeServedSeconds;

        /// <summary>Current session start time (not persisted, calculated at runtime).</summary>
        [NonSerialized] public long sessionStartUtc;

        // === Leadership Stats ===
        /// <summary>Total orders issued (as commander).</summary>
        public int ordersIssued;

        /// <summary>Total squad members commanded.</summary>
        public int squadMembersCommanded;

        /// <summary>Largest division size achieved.</summary>
        public int largestDivisionSize;

        /// <summary>Total commendations received from peers.</summary>
        public int commendationsReceived;

        /// <summary>Total commendations given to others.</summary>
        public int commendationsGiven;

        // === Discipline Stats ===
        /// <summary>Total times imprisoned.</summary>
        public int timesImprisoned;

        /// <summary>Total prison sentences served (in hours).</summary>
        public float prisonTimeServedHours;

        /// <summary>Total certifications revoked.</summary>
        public int certificationsRevoked;

        /// <summary>Total griefing reports received.</summary>
        public int griefingReportsReceived;

        /// <summary>Total griefing reports upheld (confirmed).</summary>
        public int griefingReportsUpheld;

        // === Medals / Achievements ===
        /// <summary>List of medal IDs earned.</summary>
        public List<string> medals = new();

        /// <summary>List of achievement IDs unlocked.</summary>
        public List<string> achievements = new();
    }

    /// <summary>
    /// Records a name change event.
    /// </summary>
    [Serializable]
    public sealed class NameChangeRecord
    {
        /// <summary>The previous name.</summary>
        public string previousName = "";

        /// <summary>The new name.</summary>
        public string newName = "";

        /// <summary>UTC timestamp of the change.</summary>
        public long changedUtc;

        /// <summary>Reason for change (if provided).</summary>
        public string reason = "";

        /// <summary>Whether this was an admin-forced change.</summary>
        public bool adminForced;
    }

    /// <summary>
    /// Represents the full Player Card data for display.
    /// </summary>
    [Serializable]
    public sealed class PlayerCardData
    {
        public PlayerStats stats = new();

        /// <summary>Current rank ID.</summary>
        public string rankId = "e0";

        /// <summary>Current rank display name.</summary>
        public string rankDisplayName = "Recruit";

        /// <summary>Current trust score.</summary>
        public int trustScore;

        /// <summary>Current faction.</summary>
        public string faction = "Neutral";

        /// <summary>List of active certification IDs.</summary>
        public List<string> activeCertifications = new();

        /// <summary>Whether player is currently imprisoned.</summary>
        public bool isImprisoned;

        /// <summary>Prison release timestamp (if imprisoned).</summary>
        public long prisonReleaseUtc;
    }
}
