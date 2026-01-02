namespace Tracker.DataModels
{
    /// <summary>
    /// Types of meetings.
    /// </summary>
    public enum MeetingType
    {
        /// <summary>One-on-one meeting between two people.</summary>
        OneOnOne = 0,

        /// <summary>Team meeting with multiple people.</summary>
        TeamMeeting = 1,

        /// <summary>All-hands meeting.</summary>
        AllHands = 2,

        /// <summary>Project kick-off meeting.</summary>
        ProjectKickoff = 3,

        /// <summary>Review meeting.</summary>
        Review = 4,

        /// <summary>Planning session.</summary>
        Planning = 5,

        /// <summary>Other type of meeting.</summary>
        Other = 6
    }
}
