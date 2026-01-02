namespace Tracker.Common.Enums
{
    /// <summary>
    /// Types of sections that can appear in meeting prep.
    /// </summary>
    public enum PrepSectionType
    {
        /// <summary>
        /// Urgent items requiring immediate attention (critical overdue tasks, blockers).
        /// </summary>
        Urgent,

        /// <summary>
        /// Follow-up items from previous meetings.
        /// </summary>
        FollowUp,

        /// <summary>
        /// Current task status and progress.
        /// </summary>
        TaskStatus,

        /// <summary>
        /// OKR and KPI goal progress.
        /// </summary>
        GoalProgress,

        /// <summary>
        /// Recent survey feedback and responses.
        /// </summary>
        SurveyFeedback,

        /// <summary>
        /// Recognition opportunities (birthdays, anniversaries, achievements).
        /// </summary>
        Recognition,

        /// <summary>
        /// Recent feedback given to the team member.
        /// </summary>
        RecentFeedback,

        /// <summary>
        /// AI-suggested discussion topics.
        /// </summary>
        Suggested
    }

    /// <summary>
    /// Priority levels for prep items.
    /// </summary>
    public enum PrepItemPriority
    {
        /// <summary>
        /// Low priority - informational only.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority - worth discussing.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority - should be addressed.
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority - must be addressed immediately.
        /// </summary>
        Critical = 3
    }
}
