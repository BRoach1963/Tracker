namespace Tracker.Common.Enums
{
    /// <summary>
    /// Status of a meeting.
    /// Maps to PostgreSQL meeting_status enum.
    /// </summary>
    public enum MeetingStatus
    {
        /// <summary>Meeting is scheduled but not started.</summary>
        Scheduled = 0,

        /// <summary>Meeting is currently in progress.</summary>
        InProgress = 1,

        /// <summary>Meeting has been completed.</summary>
        Completed = 2,

        /// <summary>Meeting has been cancelled.</summary>
        Cancelled = 3,

        /// <summary>Meeting has been rescheduled.</summary>
        Rescheduled = 4
    }
}
