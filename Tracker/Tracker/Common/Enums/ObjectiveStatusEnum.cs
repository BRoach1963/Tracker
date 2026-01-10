namespace Tracker.Common.Enums
{
    /// <summary>
    /// Status of a goal (OKR) or target.
    /// Maps to Supabase goal_status enum.
    /// </summary>
    public enum OkrStatus
    {
        NotStarted,
        OnTrack,
        AtRisk,
        OffTrack,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Legacy alias for backwards compatibility during migration.
    /// </summary>
    [Obsolete("Use OkrStatus instead")]
    public enum ObjectiveStatusEnum
    {
        OnTrack = OkrStatus.OnTrack,
        AtRisk = OkrStatus.AtRisk,
        OffTrack = OkrStatus.OffTrack
    }
}
