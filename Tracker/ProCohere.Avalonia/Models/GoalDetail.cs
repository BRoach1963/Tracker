using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Goal model - maps to the goals table in Supabase.
/// Used for dashboard goal tracking.
/// </summary>
[Table("goals")]
public class GoalDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string Status { get; set; } = "not_started";

    [Column("progress_percent")]
    public int? ProgressPercent { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("due_date")]
    public DateTime? EndDate { get; set; }

    [Column("owner_id")]
    public Guid? OwnerTeamMemberId { get; set; }

    // Note: goals table doesn't have created_by_user_id, only owner_id

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    #region Computed Properties

    /// <summary>
    /// Name of the owner (set by DashboardService join).
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Whether this goal is considered "on track".
    /// </summary>
    public bool IsOnTrack => Status?.ToLower() switch
    {
        "on_track" => true,
        "on-track" => true,
        "completed" => true,
        _ => false
    };

    /// <summary>
    /// Status display text with emoji.
    /// </summary>
    public string StatusDisplay => Status?.ToLower() switch
    {
        "on_track" or "on-track" => "🟢 On Track",
        "at_risk" or "at-risk" => "🟡 At Risk",
        "off_track" or "off-track" => "🔴 Off Track",
        "in_progress" or "in-progress" => "🔵 In Progress",
        "completed" => "✅ Completed",
        "not_started" or "not-started" => "⚪ Not Started",
        _ => "⚪ " + (Status ?? "Unknown")
    };

    /// <summary>
    /// Progress as percentage (0-100).
    /// </summary>
    public int Progress => ProgressPercent ?? 0;

    #endregion
}
