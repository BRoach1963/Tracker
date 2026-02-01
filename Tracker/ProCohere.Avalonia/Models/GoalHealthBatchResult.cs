using System;
using System.Text.Json.Serialization;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// DTO for the procohere.get_goal_health_batch_v2 RPC response.
/// Returns computed health for goals based on linked metrics with trend analysis.
/// </summary>
public class GoalHealthBatchResult
{
    /// <summary>
    /// The goal ID.
    /// </summary>
    [JsonPropertyName("goal_id")]
    public Guid GoalId { get; set; }

    /// <summary>
    /// Number of metrics linked to this goal.
    /// </summary>
    [JsonPropertyName("linked_metrics_count")]
    public int LinkedMetricsCount { get; set; }

    /// <summary>
    /// Derived health status as string from the RPC.
    /// Values: 'unknown', 'on_track', 'at_risk', 'off_track'
    /// </summary>
    [JsonPropertyName("derived_health")]
    public string? DerivedHealthString { get; set; }

    /// <summary>
    /// Converts the string health to the GoalDerivedHealth enum.
    /// </summary>
    public GoalDerivedHealth DerivedHealth => DerivedHealthString?.ToLowerInvariant() switch
    {
        "on_track" => GoalDerivedHealth.OnTrack,
        "at_risk" => GoalDerivedHealth.AtRisk,
        "off_track" => GoalDerivedHealth.OffTrack,
        _ => GoalDerivedHealth.Unknown
    };
}
