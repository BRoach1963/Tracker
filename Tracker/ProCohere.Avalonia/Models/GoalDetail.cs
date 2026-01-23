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

    [Column("parent_goal_id")]
    public Guid? ParentGoalId { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

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

    #region Dates

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    #endregion

    #region Priority

    [Column("priority")]
    public string? Priority { get; set; }

    #endregion

    #region Status

    [Column("status")]
    public string Status { get; set; } = "not_started";

    [Column("progress_percent")]
    public int? ProgressPercent { get; set; }

    /// <summary>
    /// Health is a computed view of Status for UI purposes.
    /// Maps status values to GoalHealth enum.
    /// </summary>
    public GoalHealth Health
    {
        get => Status?.ToLower() switch
        {
            "on_track" => GoalHealth.OnTrack,
            "needs_attention" => GoalHealth.NeedsAttention,
            "at_risk" => GoalHealth.AtRisk,
            "reframing_needed" => GoalHealth.ReframingNeeded,
            // Map legacy/alternative status values
            "in_progress" => GoalHealth.OnTrack,
            "not_started" => GoalHealth.OnTrack,
            "completed" => GoalHealth.OnTrack,
            _ => GoalHealth.OnTrack
        };
        set => Status = value switch
        {
            GoalHealth.OnTrack => "on_track",
            GoalHealth.NeedsAttention => "needs_attention",
            GoalHealth.AtRisk => "at_risk",
            GoalHealth.ReframingNeeded => "reframing_needed",
            _ => "on_track"
        };
    }

    /// <summary>
    /// Lifecycle is a computed view of Status for UI purposes.
    /// Maps status values to GoalLifecycle enum.
    /// </summary>
    public GoalLifecycle Lifecycle
    {
        get => Status?.ToLower() switch
        {
            "active" or "on_track" or "in_progress" or "needs_attention" or "at_risk" => GoalLifecycle.Active,
            "evolving" or "reframing_needed" => GoalLifecycle.Evolving,
            "paused" => GoalLifecycle.Paused,
            "superseded" => GoalLifecycle.Superseded,
            "retired" or "completed" => GoalLifecycle.Retired,
            "not_started" => GoalLifecycle.Active,
            _ => GoalLifecycle.Active
        };
        set => Status = value switch
        {
            GoalLifecycle.Active => "active",
            GoalLifecycle.Evolving => "evolving",
            GoalLifecycle.Paused => "paused",
            GoalLifecycle.Superseded => "superseded",
            GoalLifecycle.Retired => "retired",
            _ => "active"
        };
    }

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
    /// Avatar URL of the owner (set by service join).
    /// </summary>
    public string? OwnerAvatarUrl { get; set; }

    /// <summary>
    /// Initials of the owner (set by service join).
    /// </summary>
    public string? OwnerInitials { get; set; }

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
    /// Goal type display text.
    /// </summary>
    public string GoalTypeDisplay => GoalType.ToDisplayName();

    /// <summary>
    /// Health display text.
    /// </summary>
    public string HealthDisplay => Health.ToDisplayName();

    /// <summary>
    /// Lifecycle display text.
    /// </summary>
    public string LifecycleDisplay => Lifecycle.ToDisplayName();

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

    /// <summary>
    /// Display text for the goal's due date.
    /// </summary>
    public string DueDateDisplay
    {
        get
        {
            if (!DueDate.HasValue)
                return "No deadline";

            var today = DateTime.UtcNow.Date;
            var dueDate = DueDate.Value.Date;

            if (dueDate == today)
                return "Due today";
            if (dueDate == today.AddDays(1))
                return "Due tomorrow";
            if (dueDate < today)
                return "Past deadline";
            if ((dueDate - today).Days <= 7)
                return $"Due in {(dueDate - today).Days}d";
            return dueDate.ToString("MMM d");
        }
    }

    /// <summary>
    /// Status display text.
    /// </summary>
    public string StatusDisplay => Status?.ToLower() switch
    {
        "on_track" or "on-track" => "On Track",
        "at_risk" or "at-risk" => "At Risk",
        "off_track" or "off-track" => "Off Track",
        "in_progress" or "in-progress" => "In Progress",
        "completed" => "Completed",
        "not_started" or "not-started" => "Not Started",
        _ => Status ?? "Unknown"
    };

    #endregion
}
