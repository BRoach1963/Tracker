using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// A single history entry for a metric value change.
/// Maps to the metric_values table in Supabase (NOT metric_history which doesn't exist).
/// 
/// Philosophy: History shows the DIRECTION of change over time.
/// UI displays trend arrows (↗ → ↘), not specific numbers by default.
/// </summary>
[Table("metric_values")]
public class MetricHistoryEntry : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("metric_id")]
    public Guid MetricId { get; set; }

    [Column("recorded_by")]
    public Guid? RecordedByUserId { get; set; }

    [Column("value")]
    public decimal Value { get; set; }

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Notes about this value entry.
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

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

    #region Non-DB Properties (computed or set by service)

    /// <summary>
    /// Previous value - computed from previous entry in service.
    /// NOT stored in DB.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public decimal? PreviousValue { get; set; }

    /// <summary>
    /// Note about what caused this change (alias for Notes, for backward compatibility).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? WhatChanged
    {
        get => Notes;
        set => Notes = value;
    }

    /// <summary>
    /// Source of this value update - NOT in DB, computed/default.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? Source { get; set; }

    #endregion

    #region Computed Properties (Not in DB)

    /// <summary>
    /// Change direction indicator based on previous value.
    /// </summary>
    public MetricTrend ChangeDirection
    {
        get
        {
            if (!PreviousValue.HasValue) return MetricTrend.Unknown;
            
            var diff = Value - PreviousValue.Value;
            if (Math.Abs(diff) < 0.001m) return MetricTrend.Stable;
            return diff > 0 ? MetricTrend.TrendingUp : MetricTrend.TrendingDown;
        }
    }

    /// <summary>
    /// Arrow representation of change direction.
    /// </summary>
    public string ChangeArrow => ChangeDirection.GetArrow();

    /// <summary>
    /// Formatted date for display.
    /// </summary>
    public string RecordedDateDisplay => RecordedAt.ToString("MMM d, yyyy");

    /// <summary>
    /// Time since recording (for recent entries).
    /// </summary>
    public string RecordedTimeAgo
    {
        get
        {
            var diff = DateTime.UtcNow - RecordedAt;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return RecordedAt.ToString("MMM d");
        }
    }

    #endregion
}
