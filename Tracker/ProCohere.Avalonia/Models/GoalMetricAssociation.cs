using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Join table linking goals to metrics.
/// Maps to the goal_metrics table in Supabase procohere schema.
/// 
/// Philosophy: Goals and Metrics are linked but independent.
/// Metrics INFORM goal discussions but never DETERMINE goal health.
/// </summary>
[Table("goal_metrics")]
public class GoalMetricAssociation : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("goal_id")]
    public Guid GoalId { get; set; }

    [Column("metric_id")]
    public Guid MetricId { get; set; }

    #endregion

    #region Association Settings

    /// <summary>
    /// Whether this is the primary metric for the goal.
    /// </summary>
    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Display order when showing linked metrics.
    /// </summary>
    [Column("sort_order")]
    public int SortOrder { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion
}
