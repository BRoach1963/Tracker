namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Categories for development goals.
    /// Maps to Supabase dev_goal_category enum.
    /// </summary>
    public enum DevelopmentGoalCategory
    {
        SkillDevelopment,
        Certification,
        Leadership,
        CareerGrowth,
        Education,
        Networking,
        Wellness,
        Other
    }

    /// <summary>
    /// Status for development goals.
    /// Maps to Supabase dev_goal_status enum.
    /// </summary>
    public enum DevelopmentGoalStatus
    {
        Draft,
        Active,
        OnHold,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Status for development goal milestones.
    /// Maps to Supabase milestone_status enum.
    /// </summary>
    public enum MilestoneStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Skipped
    }
}
