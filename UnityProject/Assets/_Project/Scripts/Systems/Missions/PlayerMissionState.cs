using System;
using System.Collections.Generic;

namespace Frontline.Missions
{
    /// <summary>
    /// Represents the current state of a mission for a player.
    /// </summary>
    [Serializable]
    public sealed class PlayerMissionState
    {
        /// <summary>The mission ID this state tracks.</summary>
        public string missionId = "";

        /// <summary>Current status of the mission.</summary>
        public MissionStatus status = MissionStatus.NotStarted;

        /// <summary>UTC timestamp when mission was accepted.</summary>
        public long acceptedUtc;

        /// <summary>UTC timestamp when mission was completed (0 if not completed).</summary>
        public long completedUtc;

        /// <summary>Progress for each objective, keyed by objectiveId.</summary>
        public List<ObjectiveProgress> objectiveProgress = new();

        /// <summary>Number of times this mission has been completed (for repeatable missions).</summary>
        public int completionCount;
    }

    /// <summary>
    /// Tracks progress for a single objective within a mission.
    /// </summary>
    [Serializable]
    public sealed class ObjectiveProgress
    {
        /// <summary>The objective ID this progress tracks.</summary>
        public string objectiveId = "";

        /// <summary>Current count towards the objective goal.</summary>
        public int currentCount;

        /// <summary>Whether this objective is complete.</summary>
        public bool isComplete;
    }

    /// <summary>
    /// Possible states for a mission.
    /// </summary>
    public enum MissionStatus
    {
        /// <summary>Mission has not been started.</summary>
        NotStarted = 0,

        /// <summary>Mission is currently active.</summary>
        Active = 1,

        /// <summary>Mission has been completed successfully.</summary>
        Completed = 2,

        /// <summary>Mission was abandoned by the player.</summary>
        Abandoned = 3,

        /// <summary>Mission failed (time limit exceeded, etc.).</summary>
        Failed = 4
    }
}
