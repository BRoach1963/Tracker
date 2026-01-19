using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Target (Key Result) model - maps to the targets table in Supabase.
/// Represents measurable outcomes for goals.
/// </summary>
[Table("targets")]
public class TargetDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("goal_id")]
    public Guid GoalId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("target_type")]
    public string TargetType { get; set; } = "numeric";

    [Column("target_value")]
    public decimal? TargetValue { get; set; }

    [Column("current_value")]
    public decimal CurrentValue { get; set; }

    [Column("unit")]
    public string? Unit { get; set; }

    [Column("status")]
    public string Status { get; set; } = "not_started";

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    #region Computed Properties

    /// <summary>
    /// Progress as percentage (0-100) based on current vs target value.
    /// </summary>
    public int Progress
    {
        get
        {
            if (!TargetValue.HasValue || TargetValue.Value == 0)
                return 0;
            var percent = (int)Math.Round((CurrentValue / TargetValue.Value) * 100);
            return Math.Clamp(percent, 0, 100);
        }
    }

    /// <summary>
    /// Whether the target is completed.
    /// </summary>
    public bool IsCompleted => Status?.ToLower() == "completed" || CompletedAt.HasValue;

    /// <summary>
    /// Status display text.
    /// </summary>
    public string StatusDisplay => Status?.ToLower() switch
    {
        "not_started" => "Not Started",
        "in_progress" => "In Progress",
        "completed" => "Completed",
        "at_risk" => "At Risk",
        _ => Status ?? "Unknown"
    };

    #endregion
}
