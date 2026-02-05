using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service interface for metric operations.
/// 
/// Philosophy: "Metrics are signals that tell a story, NOT targets to chase."
/// - Metrics are displayed as DIRECTIONAL TRENDS (↗ → ↘), not numeric values
/// - Metrics inform but never determine goal health
/// - Human interpretation is always required
/// </summary>
public interface IMetricsService
{
    #region Library Queries

    /// <summary>
    /// Gets all metrics visible to the current user (respects RLS).
    /// </summary>
    Task<List<MetricDetail>> GetAllMetricsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a single metric by ID.
    /// </summary>
    Task<MetricDetail?> GetMetricByIdAsync(Guid metricId, CancellationToken ct = default);

    /// <summary>
    /// Gets metrics filtered by lifecycle state.
    /// </summary>
    Task<List<MetricDetail>> GetMetricsByLifecycleAsync(MetricLifecycle lifecycle, CancellationToken ct = default);

    /// <summary>
    /// Gets metrics filtered by scope (Individual, Team, Organization).
    /// </summary>
    Task<List<MetricDetail>> GetMetricsByScopeAsync(MetricScope scope, CancellationToken ct = default);

    /// <summary>
    /// Gets metrics filtered by source (System, Survey, Manual).
    /// </summary>
    Task<List<MetricDetail>> GetMetricsBySourceAsync(MetricSource source, CancellationToken ct = default);

    /// <summary>
    /// Searches metrics by name or description.
    /// </summary>
    Task<List<MetricDetail>> SearchMetricsAsync(string query, CancellationToken ct = default);

    #endregion

    #region Goal Association

    /// <summary>
    /// Gets metrics associated with a specific goal.
    /// </summary>
    Task<List<MetricDetail>> GetMetricsForGoalAsync(Guid goalId, CancellationToken ct = default);

    /// <summary>
    /// Gets metrics available for association with a goal (not already associated).
    /// </summary>
    Task<List<MetricDetail>> GetAvailableMetricsForAssociationAsync(Guid goalId, CancellationToken ct = default);

    #endregion

    #region CRUD

    /// <summary>
    /// Creates a new metric. Always requires explicit user action.
    /// </summary>
    Task<MetricDetail?> CreateMetricAsync(MetricDetail metric, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing metric (name, description, attributes, etc.).
    /// Does NOT update value - use UpdateValueAsync for that.
    /// </summary>
    Task<MetricDetail?> UpdateMetricAsync(MetricDetail metric, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a metric.
    /// </summary>
    Task<bool> DeleteMetricAsync(Guid metricId, CancellationToken ct = default);

    #endregion

    #region Value Updates (Manual Metrics)

    /// <summary>
    /// Updates the current value of a metric.
    /// For manual metrics, requires a "what changed" note.
    /// Creates a history entry.
    /// </summary>
    /// <param name="metricId">The metric to update</param>
    /// <param name="newValue">New value</param>
    /// <param name="whatChanged">Note about what caused this change (required for Manual source)</param>
    Task<MetricDetail?> UpdateValueAsync(
        Guid metricId, 
        decimal newValue, 
        string? whatChanged, 
        CancellationToken ct = default);

    #endregion

    #region Lifecycle

    /// <summary>
    /// Updates the lifecycle state of a metric.
    /// </summary>
    /// <param name="metricId">The metric to update</param>
    /// <param name="lifecycle">New lifecycle state (Active, Dormant, Retired)</param>
    Task<MetricDetail?> UpdateLifecycleAsync(
        Guid metricId, 
        MetricLifecycle lifecycle, 
        CancellationToken ct = default);

    #endregion

    #region History & Trends

    /// <summary>
    /// Gets the history entries for a metric.
    /// </summary>
    /// <param name="metricId">The metric</param>
    /// <param name="limit">Max entries to return (default 12 for sparkline)</param>
    Task<List<MetricHistoryEntry>> GetHistoryAsync(
        Guid metricId, 
        int limit = 12, 
        CancellationToken ct = default);

    /// <summary>
    /// Calculates the trend direction for a metric based on its history.
    /// Returns only directional indicator (↗ → ↘), NOT numeric values.
    /// </summary>
    Task<MetricTrend> CalculateTrendAsync(Guid metricId, CancellationToken ct = default);

    /// <summary>
    /// Calculates detailed trend analysis using linear regression for a metric.
    /// Uses TrendAnalyzer for more sophisticated analysis than simple comparison.
    /// </summary>
    /// <param name="metricId">Metric ID to analyze.</param>
    /// <param name="lookbackDays">Number of days to analyze (default: 30).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Detailed trend analysis result.</returns>
    Task<TrendResult> GetTrendAnalysisAsync(
        Guid metricId,
        int lookbackDays = 30,
        CancellationToken ct = default);

    /// <summary>
    /// Projects a metric's value at a future date based on current trend.
    /// </summary>
    /// <param name="metricId">Metric ID to project.</param>
    /// <param name="targetDate">Date to project to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Projected value, or null if trend analysis is insufficient.</returns>
    Task<double?> ProjectValueAsync(
        Guid metricId,
        DateTime targetDate,
        CancellationToken ct = default);

    /// <summary>
    /// Projects when a metric will reach a target value based on current trend.
    /// </summary>
    /// <param name="metricId">Metric ID to analyze.</param>
    /// <param name="targetValue">Target value to reach.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Projected date, or null if not reachable with current trend.</returns>
    Task<DateTime?> ProjectTargetDateAsync(
        Guid metricId,
        double targetValue,
        CancellationToken ct = default);

    #endregion

    #region Error Handling

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    string? LastError { get; }

    #endregion
}
