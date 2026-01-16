namespace ProCohere.Avalonia.Models;

/// <summary>
/// Dashboard statistics for the stat cards.
/// </summary>
public class DashboardStats
{
    /// <summary>
    /// Total number of team members managed by this user.
    /// </summary>
    public int TeamMemberCount { get; set; }

    /// <summary>
    /// Total number of tasks.
    /// </summary>
    public int TotalTasks { get; set; }

    /// <summary>
    /// Number of completed tasks.
    /// </summary>
    public int CompletedTasks { get; set; }

    /// <summary>
    /// Task completion percentage (0-100).
    /// </summary>
    public int TaskCompletionPercent => TotalTasks > 0 
        ? (int)((double)CompletedTasks / TotalTasks * 100) 
        : 0;

    /// <summary>
    /// Total number of goals.
    /// </summary>
    public int TotalGoals { get; set; }

    /// <summary>
    /// Number of goals on track (on_track or completed status).
    /// </summary>
    public int GoalsOnTrack { get; set; }

    /// <summary>
    /// Percentage of goals on track (0-100).
    /// </summary>
    public int GoalsOnTrackPercent => TotalGoals > 0 
        ? (int)((double)GoalsOnTrack / TotalGoals * 100) 
        : 0;

    /// <summary>
    /// Number of active projects (status = in_progress).
    /// </summary>
    public int ActiveProjectCount { get; set; }
}
