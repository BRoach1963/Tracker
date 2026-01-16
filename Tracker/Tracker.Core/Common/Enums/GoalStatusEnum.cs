namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Status of a goal or target.
    /// Maps to Supabase goal_status enum.
    /// </summary>
    public enum GoalStatus
    {
        NotStarted,
        OnTrack,
        AtRisk,
        OffTrack,
        Completed,
        Cancelled
    }
}
