using System;
using System.Collections.Generic;
using System.Windows.Input;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Goal model - maps to the goals table in Supabase.
/// 
/// Philosophy: "Goals express intent, Metrics observe reality, Humans decide."
/// NO progress bars, percentages, or red/yellow/green status indicators.
/// 
/// Implements IDetailEntity for use in EntityDetailFlyout.
/// </summary>
[Table("goals")]
public class GoalDetail : BaseModel, IDetailEntity
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("owner_id")]
    public Guid OwnerTeamMemberId { get; set; }

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
    public int ProgressPercent { get; set; }

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

    #region Project Link
    
    /// <summary>
    /// ID of the linked project (populated from project_links table).
    /// Not a DB column - set by service when fetching goals.
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    /// <summary>
    /// Title of the linked project (for display).
    /// Not a DB column - set by service when fetching goals.
    /// </summary>
    public string? ProjectTitle { get; set; }
    
    /// <summary>
    /// Whether this goal is linked to a project.
    /// </summary>
    public bool HasProject => ProjectId.HasValue;
    
    #endregion

    #region Source Tracking

    /// <summary>
    /// Source entity type if goal was created from another entity (e.g., 'meeting', 'task').
    /// </summary>
    [Column("source_type")]
    public string? SourceType { get; set; }

    /// <summary>
    /// Source entity ID if goal was created from another entity.
    /// </summary>
    [Column("source_id")]
    public Guid? SourceId { get; set; }

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

    #region IDetailEntity Commands (wired up by parent ViewModel)

    /// <summary>
    /// Command to close the detail flyout. Wired up by parent ViewModel.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ICommand? CloseCommand { get; set; }

    /// <summary>
    /// Command to edit this goal. Wired up by parent ViewModel.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ICommand? EditCommand { get; set; }

    /// <summary>
    /// Command to delete this goal. Wired up by parent ViewModel.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ICommand? DeleteCommand { get; set; }

    #endregion

    #region Derived Health (Circle View - NOT in DB)

    /// <summary>
    /// Derived health from linked metric signals. Used by Circle view.
    /// Computed at load time using worst-state logic from linked metrics.
    /// NOT stored in DB - this is the authoritative health for Circle per GOALS_SPEC.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public GoalDerivedHealth DerivedHealth { get; set; } = GoalDerivedHealth.Unknown;

    /// <summary>
    /// Number of linked metrics (for Unknown state context).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int LinkedMetricsCount { get; set; }

    /// <summary>
    /// Display text for derived health.
    /// </summary>
    public string DerivedHealthDisplay => DerivedHealth switch
    {
        GoalDerivedHealth.OnTrack => "On Track",
        GoalDerivedHealth.AtRisk => "At Risk",
        GoalDerivedHealth.OffTrack => "Off Track",
        _ => LinkedMetricsCount == 0 ? "No Metrics" : "Unknown"
    };

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

/// <summary>
/// Derived health state for goals - computed from linked metric signals.
/// This is the authoritative health for Circle view per GOALS_SPEC.
/// Uses worst-state logic: any OffTrack metric = OffTrack goal.
/// </summary>
public enum GoalDerivedHealth
{
    /// <summary>
    /// No metrics linked or insufficient data to determine health.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// All linked metrics are On Track.
    /// </summary>
    OnTrack,
    
    /// <summary>
    /// At least one linked metric is At Risk/NeedsAttention, none Off Track.
    /// </summary>
    AtRisk,
    
    /// <summary>
    /// At least one linked metric is Off Track.
    /// </summary>
    OffTrack
}
