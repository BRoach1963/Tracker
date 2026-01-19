using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Goal model - maps to the goals table in Supabase.
/// 
/// Philosophy: "Goals express intent, Metrics observe reality, Humans decide."
/// NO progress bars, percentages, or red/yellow/green status indicators.
/// </summary>
[Table("goals")]
public class GoalDetail : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("owner_id")]
    public Guid? OwnerTeamMemberId { get; set; }

    [Column("created_by_user_id")]
    public Guid? CreatedByUserId { get; set; }

    #endregion

    #region Content

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    #endregion

    #region Goal Type (NEW)

    /// <summary>
    /// Type of goal: growth, execution, operational, directional
    /// </summary>
    [Column("goal_type")]
    public string? GoalTypeValue { get; set; }

    /// <summary>
    /// Parsed goal type enum.
    /// </summary>
    public GoalType GoalType
    {
        get => GoalTypeExtensions.ParseGoalType(GoalTypeValue);
        set => GoalTypeValue = value.ToString().ToLower();
    }

    #endregion

    #region Time Period

    [Column("time_period")]
    public string? TimePeriod { get; set; }

    [Column("year")]
    public int? Year { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    #endregion

    #region Health System (NEW - replaces old status)

    /// <summary>
    /// Health status: on_track, needs_attention, at_risk, reframing_needed
    /// </summary>
    [Column("health")]
    public string? HealthValue { get; set; }

    /// <summary>
    /// Parsed health enum.
    /// </summary>
    public GoalHealth Health
    {
        get => GoalHealthExtensions.ParseGoalHealth(HealthValue);
        set => HealthValue = value switch
        {
            GoalHealth.OnTrack => "on_track",
            GoalHealth.NeedsAttention => "needs_attention",
            GoalHealth.AtRisk => "at_risk",
            GoalHealth.ReframingNeeded => "reframing_needed",
            _ => "on_track"
        };
    }

    /// <summary>
    /// Reason/reflection for the current health status.
    /// Prompts: "What has changed?"
    /// </summary>
    [Column("health_reason")]
    public string? HealthReason { get; set; }

    #endregion

    #region Lifecycle (NEW)

    /// <summary>
    /// Lifecycle state: active, evolving, paused, superseded, retired
    /// </summary>
    [Column("lifecycle")]
    public string? LifecycleValue { get; set; }

    /// <summary>
    /// Parsed lifecycle enum.
    /// </summary>
    public GoalLifecycle Lifecycle
    {
        get => GoalLifecycleExtensions.ParseGoalLifecycle(LifecycleValue);
        set => LifecycleValue = value.ToString().ToLower();
    }

    /// <summary>
    /// Reason/reflection for the lifecycle change.
    /// </summary>
    [Column("lifecycle_reason")]
    public string? LifecycleReason { get; set; }

    /// <summary>
    /// If superseded, links to the replacement goal.
    /// </summary>
    [Column("superseded_by_id")]
    public Guid? SupersededById { get; set; }

    #endregion

    #region Legacy Status (kept for backward compatibility)

    /// <summary>
    /// Legacy status field - being phased out in favor of Health.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "not_started";

    /// <summary>
    /// Legacy progress percent - HIDDEN in UI by philosophy.
    /// </summary>
    [Column("progress_percent")]
    public int? ProgressPercent { get; set; }

    #endregion

    #region Visibility

    [Column("is_team_visible")]
    public bool IsTeamVisible { get; set; } = true;

    [Column("is_org_visible")]
    public bool IsOrgVisible { get; set; }

    /// <summary>
    /// Computed visibility level.
    /// </summary>
    public GoalVisibility Visibility => IsOrgVisible ? GoalVisibility.Organization
        : IsTeamVisible ? GoalVisibility.Team
        : GoalVisibility.Private;

    #endregion

    #region Relationships

    [Column("project_id")]
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// When this goal was last discussed in a meeting.
    /// </summary>
    [Column("last_discussed_at")]
    public DateTime? LastDiscussedAt { get; set; }

    /// <summary>
    /// The meeting where this goal was last discussed.
    /// </summary>
    [Column("last_discussed_meeting_id")]
    public Guid? LastDiscussedMeetingId { get; set; }

    #endregion

    #region Audit Fields

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties (Not in DB)

    /// <summary>
    /// Name of the owner (set by service join).
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Name of the meeting where last discussed (set by service join).
    /// </summary>
    public string? LastDiscussedMeetingName { get; set; }

    /// <summary>
    /// Associated metrics (loaded separately).
    /// </summary>
    public List<MetricDetail> AssociatedMetrics { get; set; } = new();

    /// <summary>
    /// Linked tasks (loaded separately).
    /// </summary>
    public List<TaskDetail> LinkedTasks { get; set; } = new();

    #endregion

    #region Computed Display Properties

    /// <summary>
    /// Health display text - neutral language only.
    /// </summary>
    public string HealthDisplay => Health.ToDisplayName();

    /// <summary>
    /// Lifecycle display text.
    /// </summary>
    public string LifecycleDisplay => Lifecycle.ToDisplayName();

    /// <summary>
    /// Goal type display text.
    /// </summary>
    public string GoalTypeDisplay => GoalType.ToDisplayName();

    /// <summary>
    /// Visibility display text.
    /// </summary>
    public string VisibilityDisplay => Visibility.ToDisplayName();

    /// <summary>
    /// Whether this goal is currently actionable.
    /// </summary>
    public bool IsActionable => Lifecycle.IsActionable();

    /// <summary>
    /// Whether this goal is in a terminal lifecycle state.
    /// </summary>
    public bool IsTerminal => Lifecycle.IsTerminal();

    /// <summary>
    /// Number of associated metrics.
    /// </summary>
    public int AssociatedMetricsCount => AssociatedMetrics?.Count ?? 0;

    /// <summary>
    /// Number of linked tasks.
    /// </summary>
    public int LinkedTasksCount => LinkedTasks?.Count ?? 0;

    #endregion

    #region Backward Compatibility (To Be Removed)

    /// <summary>
    /// DEPRECATED: Legacy progress property for backward compatibility.
    /// Will be hidden in new UI per philosophy: "NO progress bars, percentages".
    /// </summary>
    [Obsolete("Progress percentages are being phased out. Use Health instead.")]
    public int Progress => ProgressPercent ?? 0;

    /// <summary>
    /// DEPRECATED: Legacy status display with emoji colors.
    /// Will be replaced with neutral HealthDisplay.
    /// </summary>
    [Obsolete("Colored status indicators are being phased out. Use HealthDisplay instead.")]
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
    /// DEPRECATED: Whether this goal is considered "on track".
    /// Use Health == GoalHealth.OnTrack instead.
    /// </summary>
    [Obsolete("Use Health == GoalHealth.OnTrack instead.")]
    public bool IsOnTrack => Health == GoalHealth.OnTrack;

    #endregion
}
