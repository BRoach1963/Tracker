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

    [Column("owner_team_member_id")]
    public Guid? OwnerTeamMemberId { get; set; }

    [Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    #region Values (Hidden by default in UI)

    [Column("current_value")]
    public decimal CurrentValue { get; set; }

    [Column("target_value")]
    public decimal? TargetValue { get; set; }

    [Column("baseline_value")]
    public decimal? BaselineValue { get; set; }

    [Column("unit")]
    public string? Unit { get; set; }

    #endregion

    #region Direction & Trend

    /// <summary>
    /// Target direction: higher_is_better, lower_is_better, neutral
    /// </summary>
    [Column("target_direction")]
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

    #region Source & Scope

    /// <summary>
    /// Data source: system, survey, manual
    /// </summary>
    [Column("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Scope: individual, team, organization
    /// </summary>
    [Column("scope")]
    public string? Scope { get; set; }

    public MetricSource SourceEnum => MetricSourceExtensions.ParseMetricSource(Source);

    public MetricScope ScopeEnum => MetricScopeExtensions.ParseMetricScope(Scope);

    #endregion

    #region Lifecycle

    /// <summary>
    /// Lifecycle state: active, dormant, retired
    /// </summary>
    [Column("lifecycle")]
    public string Lifecycle { get; set; } = "active";

    public MetricLifecycle LifecycleEnum => MetricLifecycleExtensions.ParseMetricLifecycle(Lifecycle);

    public string LifecycleDisplay => LifecycleEnum.ToDisplayName();

    #endregion

    #region Frequency

    /// <summary>
    /// Update frequency: daily, weekly, monthly, quarterly
    /// </summary>
    [Column("frequency")]
    public string? Frequency { get; set; }

    [Column("last_updated_at")]
    public DateTime? LastUpdatedAt { get; set; }

    #endregion

    #region Sensitivity & Visibility

    [Column("is_sensitive")]
    public bool IsSensitive { get; set; }

    [Column("is_team_visible")]
    public bool IsTeamVisible { get; set; } = true;

    [Column("is_org_visible")]
    public bool IsOrgVisible { get; set; }

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
