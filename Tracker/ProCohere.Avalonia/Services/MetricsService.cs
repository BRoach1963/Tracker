using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing metrics in Supabase.
/// 
/// Philosophy: "Metrics are signals that tell a story, NOT targets to chase."
/// - Display DIRECTIONAL TRENDS (↗ → ↘), not numeric values
/// - Metrics inform but never determine goal health
/// - Human interpretation is always required
/// </summary>
public class MetricsService : IMetricsService
{
    #region Singleton

    private static readonly Lazy<MetricsService> _instance =
        new(() => new MetricsService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static MetricsService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "metrics_service.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion

    #region Properties

    /// <inheritdoc />
    public string? LastError { get; private set; }

    #endregion

    private MetricsService() { }

    #region Library Queries

    /// <inheritdoc />
    public async Task<List<MetricDetail>> GetAllMetricsAsync(CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MetricDetail>();
        }

        try
        {
            Log("Loading all metrics");

            // Load metrics and trends in parallel using batch RPC
            var metricsTask = client.From<MetricDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("name", Ordering.Ascending)
                .Get();

            var trendsTask = GetMetricsTrendBatchAsync(null, ct);

            await Task.WhenAll(metricsTask, trendsTask);

            var metrics = metricsTask.Result.Models ?? new List<MetricDetail>();
            var trends = trendsTask.Result;

            // Apply trends from batch result
            ApplyTrendBatchResults(metrics, trends);

            Log($"All metrics returned: {metrics.Count} (with batch trends)");
            return metrics;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetAllMetrics ERROR: {ex.Message}");
            return new List<MetricDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<MetricDetail?> GetMetricByIdAsync(Guid metricId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Loading metric: {metricId}");

            var result = await client.From<MetricDetail>()
                .Filter("id", Operator.Equals, metricId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (result != null)
            {
                result.Trend = await CalculateTrendAsync(result.Id, ct);
            }

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetMetricById ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Note: The 'lifecycle' column does not exist in the procohere.metrics schema.
    /// This method returns all metrics. Use client-side filtering if needed.
    /// </remarks>
    public async Task<List<MetricDetail>> GetMetricsByLifecycleAsync(MetricLifecycle lifecycle, CancellationToken ct = default)
    {
        // lifecycle column doesn't exist in procohere.metrics schema
        // Return all metrics - client-side filtering can be applied by caller
        Log($"GetMetricsByLifecycleAsync called with {lifecycle} - returning all metrics (lifecycle column not in DB)");
        return await GetAllMetricsAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Note: The 'scope' column does not exist in the procohere.metrics schema.
    /// This method returns all metrics. Use client-side filtering if needed.
    /// </remarks>
    public async Task<List<MetricDetail>> GetMetricsByScopeAsync(MetricScope scope, CancellationToken ct = default)
    {
        // scope column doesn't exist in procohere.metrics schema
        // Return all metrics - client-side filtering can be applied by caller
        Log($"GetMetricsByScopeAsync called with {scope} - returning all metrics (scope column not in DB)");
        return await GetAllMetricsAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Note: The 'source' column does not exist in the procohere.metrics schema.
    /// This method returns all metrics. Use client-side filtering if needed.
    /// </remarks>
    public async Task<List<MetricDetail>> GetMetricsBySourceAsync(MetricSource source, CancellationToken ct = default)
    {
        // source column doesn't exist in procohere.metrics schema
        // Return all metrics - client-side filtering can be applied by caller
        Log($"GetMetricsBySourceAsync called with {source} - returning all metrics (source column not in DB)");
        return await GetAllMetricsAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<MetricDetail>> SearchMetricsAsync(string query, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MetricDetail>();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllMetricsAsync(ct);
        }

        try
        {
            Log($"Searching metrics: '{query}'");

            // Search by name (case-insensitive)
            var result = await client.From<MetricDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("name", Operator.ILike, $"%{query}%")
                .Order("name", Ordering.Ascending)
                .Get();

            var metrics = result.Models ?? new List<MetricDetail>();
            
            // Use batch RPC for trends if we have metrics
            if (metrics.Count > 0)
            {
                var metricIds = metrics.Select(m => m.Id);
                var trends = await GetMetricsTrendBatchAsync(metricIds, ct);
                ApplyTrendBatchResults(metrics, trends);
            }

            Log($"Search '{query}' returned: {metrics.Count} metrics (with batch trends)");
            return metrics;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"SearchMetrics ERROR: {ex.Message}");
            return new List<MetricDetail>();
        }
    }

    #endregion

    #region Goal Association

    /// <inheritdoc />
    public async Task<List<MetricDetail>> GetMetricsForGoalAsync(Guid goalId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MetricDetail>();
        }

        try
        {
            Log($"Loading metrics for goal: {goalId}");

            // First get the goal_metric associations
            var associations = await client.From<GoalMetricAssociation>()
                .Filter("goal_id", Operator.Equals, goalId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var metricIds = associations.Models?.Select(a => a.MetricId).ToList() ?? new List<Guid>();

            if (!metricIds.Any())
            {
                Log($"No metrics associated with goal {goalId}");
                return new List<MetricDetail>();
            }

            // Then get the metrics
            var metrics = new List<MetricDetail>();
            foreach (var metricId in metricIds)
            {
                var metric = await GetMetricByIdAsync(metricId, ct);
                if (metric != null)
                {
                    metrics.Add(metric);
                }
            }

            Log($"Metrics for goal {goalId}: {metrics.Count}");
            return metrics;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetMetricsForGoal ERROR: {ex.Message}");
            return new List<MetricDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<List<MetricDetail>> GetAvailableMetricsForAssociationAsync(Guid goalId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MetricDetail>();
        }

        try
        {
            Log($"Loading available metrics for goal association: {goalId}");

            // Get already associated metric IDs
            var associations = await client.From<GoalMetricAssociation>()
                .Filter("goal_id", Operator.Equals, goalId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var associatedIds = associations.Models?.Select(a => a.MetricId).ToHashSet() ?? new HashSet<Guid>();

            // Get all active metrics
            var allMetrics = await GetMetricsByLifecycleAsync(MetricLifecycle.Active, ct);

            // Filter out already associated ones
            var available = allMetrics.Where(m => !associatedIds.Contains(m.Id)).ToList();

            Log($"Available metrics for goal {goalId}: {available.Count}");
            return available;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetAvailableMetricsForAssociation ERROR: {ex.Message}");
            return new List<MetricDetail>();
        }
    }

    #endregion

    #region CRUD

    /// <inheritdoc />
    public async Task<MetricDetail?> CreateMetricAsync(MetricDetail metric, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;
        var profile = AuthService.Instance.CurrentProfile;

        if (client == null || session?.TeamMember == null || profile == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Creating metric: {metric.Name}");

            // Set required fields
            metric.Id = Guid.NewGuid();
            metric.OrganizationId = session.TeamMember.OrganizationId;
            metric.CreatedAt = DateTime.UtcNow;
            metric.UpdatedAt = DateTime.UtcNow;
            metric.IsDeleted = false;

            // Note: CreatedByUserId and Lifecycle don't exist in DB schema

            var result = await client.From<MetricDetail>()
                .Insert(metric);

            var created = result.Models?.FirstOrDefault();
            Log($"Metric created: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateMetric ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Only updates columns that exist in procohere.metrics schema:
    /// name, description, metric_type, unit, target_value, current_value, direction, frequency
    /// </remarks>
    public async Task<MetricDetail?> UpdateMetricAsync(MetricDetail metric, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Updating metric: {metric.Id}");

            metric.UpdatedAt = DateTime.UtcNow;

            // Only update columns that exist in procohere.metrics schema
            var result = await client.From<MetricDetail>()
                .Where(m => m.Id == metric.Id)
                .Set(m => m.Name!, metric.Name)
                .Set(m => m.Description!, metric.Description)
                .Set(m => m.MetricType!, metric.MetricType)
                .Set(m => m.Unit!, metric.Unit)
                .Set(m => m.TargetValue!, metric.TargetValue)
                .Set(m => m.CurrentValue!, metric.CurrentValue)
                .Set(m => m.TargetDirection!, metric.TargetDirection)
                .Set(m => m.Frequency!, metric.Frequency)
                .Set(m => m.UpdatedAt, metric.UpdatedAt)
                .Update();

            var updated = result.Models?.FirstOrDefault();
            Log($"Metric updated: {updated?.Id}");
            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateMetric ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMetricAsync(Guid metricId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var profile = AuthService.Instance.CurrentProfile;

        if (client == null || profile == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Soft-deleting metric: {metricId}");

            await client.From<MetricDetail>()
                .Where(m => m.Id == metricId)
                .Set(m => m.IsDeleted, true)
                .Set(m => m.DeletedAt!, DateTime.UtcNow)
                .Set(m => m.DeletedBy!, profile.Id)
                .Set(m => m.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log($"Metric deleted: {metricId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteMetric ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Value Updates

    /// <inheritdoc />
    public async Task<MetricDetail?> UpdateValueAsync(
        Guid metricId, 
        decimal newValue, 
        string? whatChanged, 
        CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;
        var profile = AuthService.Instance.CurrentProfile;

        if (client == null || session?.TeamMember == null || profile == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Updating metric value: {metricId} = {newValue}");

            // Get the current metric to capture previous value
            var current = await GetMetricByIdAsync(metricId, ct);
            if (current == null)
            {
                LastError = "Metric not found";
                return null;
            }

            var previousValue = current.CurrentValue;
            var now = DateTime.UtcNow;

            // Create history entry
            var historyEntry = new MetricHistoryEntry
            {
                Id = Guid.NewGuid(),
                MetricId = metricId,
                OrganizationId = current.OrganizationId,
                Value = newValue,
                PreviousValue = previousValue,
                WhatChanged = whatChanged,
                Source = "manual", // Default since Source doesn't exist in MetricDetail
                RecordedByUserId = profile.Id,
                RecordedAt = now,
                CreatedAt = now
            };

            await client.From<MetricHistoryEntry>()
                .Insert(historyEntry);

            // Update the metric's current value
            var result = await client.From<MetricDetail>()
                .Where(m => m.Id == metricId)
                .Set(m => m.CurrentValue!, newValue)
                .Set(m => m.UpdatedAt, now)
                .Update();

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
            {
                updated.Trend = await CalculateTrendAsync(metricId, ct);
            }

            Log($"Metric value updated: {metricId}, {previousValue} → {newValue}");
            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateValue ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    /// <remarks>
    /// Note: The 'lifecycle' column does not exist in procohere.metrics schema.
    /// This method returns the metric without persisting lifecycle changes.
    /// Lifecycle is a UI-only concept for metrics until schema is updated.
    /// </remarks>
    public async Task<MetricDetail?> UpdateLifecycleAsync(
        Guid metricId, 
        MetricLifecycle lifecycle, 
        CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"UpdateLifecycleAsync: lifecycle column doesn't exist in metrics table - returning current metric");

            // Just fetch the current metric since we can't persist lifecycle
            var result = await client.From<MetricDetail>()
                .Filter("id", Operator.Equals, metricId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (result != null)
            {
                result.Trend = await CalculateTrendAsync(metricId, ct);
            }

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateLifecycle ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region History & Trends

    /// <inheritdoc />
    public async Task<List<MetricHistoryEntry>> GetHistoryAsync(
        Guid metricId, 
        int limit = 12, 
        CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MetricHistoryEntry>();
        }

        try
        {
            Log($"Loading history for metric: {metricId}, limit: {limit}");

            var result = await client.From<MetricHistoryEntry>()
                .Filter("metric_id", Operator.Equals, metricId.ToString())
                .Order("recorded_at", Ordering.Descending)
                .Limit(limit)
                .Get();

            var history = result.Models ?? new List<MetricHistoryEntry>();
            Log($"History entries for {metricId}: {history.Count}");
            return history;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetHistory ERROR: {ex.Message}");
            return new List<MetricHistoryEntry>();
        }
    }

    /// <inheritdoc />
    public async Task<MetricTrend> CalculateTrendAsync(Guid metricId, CancellationToken ct = default)
    {
        try
        {
            // Get recent history (last 3 entries for trend calculation)
            var history = await GetHistoryAsync(metricId, 3, ct);

            if (history.Count < 2)
            {
                return MetricTrend.Unknown;
            }

            // Calculate trend based on recent values
            // Newest first, so compare [0] (newest) to [1] (previous)
            var newest = history[0].Value;
            var previous = history[1].Value;
            var diff = newest - previous;

            // Check for variability if we have 3+ entries
            if (history.Count >= 3)
            {
                var oldest = history[2].Value;
                var diff1 = previous - oldest;
                var diff2 = newest - previous;

                // If direction changed significantly, it's variable
                if ((diff1 > 0 && diff2 < 0) || (diff1 < 0 && diff2 > 0))
                {
                    var variance = Math.Abs(diff1) + Math.Abs(diff2);
                    if (variance > Math.Abs(diff) * 2)
                    {
                        return MetricTrend.MoreVariable;
                    }
                }
            }

            // Simple threshold for "stable" (within 1%)
            var threshold = Math.Abs(previous) * 0.01m;
            if (threshold < 0.01m) threshold = 0.01m;

            if (Math.Abs(diff) <= threshold)
            {
                return MetricTrend.Stable;
            }

            return diff > 0 ? MetricTrend.TrendingUp : MetricTrend.TrendingDown;
        }
        catch (Exception ex)
        {
            Log($"CalculateTrend ERROR: {ex.Message}");
            return MetricTrend.Unknown;
        }
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Gets metrics with their computed trends in a single batch RPC call.
    /// Uses procohere.get_metrics_with_trend_batch to replace N+1 trend queries.
    /// </summary>
    /// <param name="metricIds">Metric IDs to get trends for. Pass null for all visible metrics.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of results with metric_id, current_value, and trend.</returns>
    public async Task<List<MetricTrendBatchResult>> GetMetricsTrendBatchAsync(
        IEnumerable<Guid>? metricIds = null,
        CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MetricTrendBatchResult>();
        }

        try
        {
            var idsArray = metricIds?.ToArray();
            Log($"Getting metrics trend batch for {idsArray?.Length ?? 0} metrics (null = all)");

            var rpcResult = await client.Rpc("get_metrics_with_trend_batch", new
            {
                p_metric_ids = idsArray
            });

            ct.ThrowIfCancellationRequested();

            if (rpcResult?.Content == null)
            {
                Log("RPC returned no content");
                return new List<MetricTrendBatchResult>();
            }

            Log($"RPC response length: {rpcResult.Content.Length}");

            var results = JsonSerializer.Deserialize<List<MetricTrendBatchResult>>(
                rpcResult.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<MetricTrendBatchResult>();

            Log($"Metrics trend batch returned: {results.Count} results");
            return results;
        }
        catch (OperationCanceledException)
        {
            Log("GetMetricsTrendBatch cancelled");
            throw;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetMetricsTrendBatch ERROR: {ex.Message}");
            return new List<MetricTrendBatchResult>();
        }
    }

    /// <summary>
    /// Applies batch trend results to a list of metrics in memory.
    /// </summary>
    public void ApplyTrendBatchResults(List<MetricDetail> metrics, List<MetricTrendBatchResult> trendResults)
    {
        var trendLookup = trendResults.ToDictionary(r => r.MetricId);
        
        foreach (var metric in metrics)
        {
            if (trendLookup.TryGetValue(metric.Id, out var result))
            {
                metric.Trend = result.Trend;
            }
            else
            {
                metric.Trend = MetricTrend.Unknown;
            }
        }
    }

    #endregion
}
