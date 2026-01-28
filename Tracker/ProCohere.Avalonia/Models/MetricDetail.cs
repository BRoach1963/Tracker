using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Metric model - maps to the metrics table in Supabase.
/// 
/// Philosophy: "Metrics are signals that tell a story, NOT targets to chase."
/// UI displays trends and direction, NOT numeric values by default.
/// </summary>
[Table("metrics")]
public class MetricDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("owner_id")]
    public Guid? OwnerTeamMemberId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("metric_type")]
    public string MetricType { get; set; } = "number";

    #region Values (Hidden by default in UI)

    [Column("current_value")]
    public decimal? CurrentValue { get; set; }

    [Column("target_value")]
    public decimal? TargetValue { get; set; }

    [Column("unit")]
    public string? Unit { get; set; }

    #endregion

    #region Direction & Trend

    /// <summary>
    /// Target direction: higher_is_better, lower_is_better, neutral
    /// </summary>
    [Column("direction")]
    public string? TargetDirection { get; set; }

    /// <summary>
    /// Computed trend based on metric history: trending_up, stable, trending_down, variable, unknown
    /// This is NOT stored in DB - computed from metric_history table.
    /// </summary>
    public MetricTrend Trend { get; set; } = MetricTrend.Unknown;

    /// <summary>
    /// Trend display with directional arrow.
    /// </summary>
    public string TrendDisplay => Trend.GetArrow();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Source defaults to Manual since column doesn't exist in DB.
    /// </summary>
    public MetricSource SourceEnum => MetricSource.Manual;

    /// <summary>
    /// Scope defaults to Individual since column doesn't exist in DB.
    /// </summary>
    public MetricScope ScopeEnum => MetricScope.Individual;

    /// <summary>
    /// Lifecycle defaults to Active since column doesn't exist in DB.
    /// </summary>
    public MetricLifecycle LifecycleEnum => MetricLifecycle.Active;

    public string LifecycleDisplay => LifecycleEnum.ToDisplayName();

    #endregion

    #region Frequency

    /// <summary>
    /// Update frequency: daily, weekly, monthly, quarterly
    /// </summary>
    [Column("frequency")]
    public string? Frequency { get; set; }

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
    /// Not a DB column - set by service when fetching metrics.
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    /// <summary>
    /// Title of the linked project (for display).
    /// Not a DB column - set by service when fetching metrics.
    /// </summary>
    public string? ProjectTitle { get; set; }
    
    /// <summary>
    /// Whether this metric is linked to a project.
    /// </summary>
    public bool HasProject => ProjectId.HasValue;
    
    #endregion

    #region Navigation Properties (Not in DB)

    /// <summary>
    /// Name of the steward (set by service join).
    /// </summary>
    public string? StewardName { get; set; }

    /// <summary>
    /// Count of goals this metric is linked to.
    /// </summary>
    public int LinkedGoalsCount { get; set; }

    #endregion
}

// Enums are defined in separate files:
// - MetricSource.cs
// - MetricScope.cs  
// - MetricLifecycle.cs
// - MetricTrend.cs
