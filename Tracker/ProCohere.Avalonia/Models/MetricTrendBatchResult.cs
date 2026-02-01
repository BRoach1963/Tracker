using System;
using System.Text.Json.Serialization;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// DTO for the procohere.get_metrics_with_trend_batch RPC response.
/// Returns metrics with computed trend in one query.
/// </summary>
public class MetricTrendBatchResult
{
    /// <summary>
    /// The metric ID.
    /// </summary>
    [JsonPropertyName("metric_id")]
    public Guid MetricId { get; set; }

    /// <summary>
    /// Current value of the metric.
    /// </summary>
    [JsonPropertyName("current_value")]
    public decimal? CurrentValue { get; set; }

    /// <summary>
    /// Trend as string from the RPC.
    /// Values: 'trending_up', 'trending_down', 'stable', 'more_variable', 'unknown'
    /// </summary>
    [JsonPropertyName("trend")]
    public string? TrendString { get; set; }

    /// <summary>
    /// Converts the string trend to the MetricTrend enum.
    /// </summary>
    public MetricTrend Trend => TrendString?.ToLowerInvariant() switch
    {
        "trending_up" => MetricTrend.TrendingUp,
        "trending_down" => MetricTrend.TrendingDown,
        "stable" => MetricTrend.Stable,
        "more_variable" => MetricTrend.MoreVariable,
        _ => MetricTrend.Unknown
    };
}
