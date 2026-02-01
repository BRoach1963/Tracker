using System;
using System.Text.Json.Serialization;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// DTO for the procohere.get_project_signals_batch RPC response.
/// Returns task and goal signal counts for projects in one query.
/// </summary>
public class ProjectSignalsBatchResult
{
    /// <summary>
    /// The project ID.
    /// </summary>
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Count of overdue tasks linked to this project.
    /// </summary>
    [JsonPropertyName("overdue_task_count")]
    public int OverdueTaskCount { get; set; }

    /// <summary>
    /// Count of goals needing attention (at_risk, needs_attention, blocked).
    /// </summary>
    [JsonPropertyName("goals_needing_attention")]
    public int GoalsNeedingAttention { get; set; }
}
