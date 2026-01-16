namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Types of AI-generated insights.
    /// </summary>
    public enum InsightType
    {
        /// <summary>Goal is at risk of not meeting its target.</summary>
        GoalAtRisk,

        /// <summary>Goal is ending soon and needs attention.</summary>
        GoalEndingSoon,

        /// <summary>Metric is off target.</summary>
        MetricOffTarget,

        /// <summary>Meeting cadence gap detected.</summary>
        MeetingGap,

        /// <summary>Task action item is stale/not progressing.</summary>
        StaleActionItem,

        /// <summary>Task is overdue.</summary>
        TaskOverdue,

        /// <summary>Team member birthday coming up.</summary>
        UpcomingBirthday,

        /// <summary>Team member work anniversary coming up.</summary>
        UpcomingAnniversary,

        /// <summary>Survey response indicates concern.</summary>
        SurveyAlert,

        /// <summary>General recommendation.</summary>
        Recommendation,

        /// <summary>Trend detected in data.</summary>
        Trend,

        /// <summary>Opportunity identified.</summary>
        Opportunity,

        /// <summary>General insight (default/fallback).</summary>
        General
    }
}
