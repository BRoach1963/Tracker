using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// A single history entry for a metric value change.
/// Maps to the metric_history table in Supabase.
/// 
/// Philosophy: History shows the DIRECTION of change over time.
/// UI displays trend arrows (↗ → ↘), not specific numbers by default.
/// </summary>
[Table("metric_history")]
public class MetricHistoryEntry : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("metric_id")]
    public Guid MetricId { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("value")]
    public decimal Value { get; set; }

    [Column("previous_value")]
    public decimal? PreviousValue { get; set; }

    /// <summary>
    /// Note about what caused this change (especially for manual metrics).
    /// </summary>
    [Column("what_changed")]
    public string? WhatChanged { get; set; }

    /// <summary>
    /// Source of this value update: system, survey, manual
    /// </summary>
    [Column("source")]
    public string? Source { get; set; }

    [Column("recorded_by_user_id")]
    public Guid? RecordedByUserId { get; set; }

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

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
