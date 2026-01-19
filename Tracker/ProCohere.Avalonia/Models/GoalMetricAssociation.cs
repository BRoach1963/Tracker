using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Join table linking goals to metrics.
/// Maps to the goal_metrics table in Supabase.
/// 
/// Philosophy: Goals and Metrics are linked but independent.
/// Metrics INFORM goal discussions but never DETERMINE goal health.
/// </summary>
[Table("goal_metrics")]
public class GoalMetricAssociation : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("goal_id")]
    public Guid GoalId { get; set; }

    [Column("metric_id")]
    public Guid MetricId { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Optional note about why this metric is relevant to this goal.
    /// </summary>
    [Column("context_note")]
    public string? ContextNote { get; set; }

    /// <summary>
    /// Who created this association.
    /// </summary>
    [Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }
}
