using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Task model - maps to the tasks table in Supabase.
/// Used for dashboard upcoming tasks.
/// </summary>
[Table("tasks")]
public class TaskDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string Status { get; set; } = "not_started";

    [Column("priority")]
    public string? Priority { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("assigned_to")]
    public Guid? OwnerTeamMemberId { get; set; }

    [Column("created_by")]
    public Guid? CreatedByTeamMemberId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    #region Computed Properties

    /// <summary>
    /// Name of the owner (set by DashboardService join).
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Whether the task is overdue.
    /// </summary>
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.UtcNow && Status != "completed";

    /// <summary>
    /// Whether the task is completed.
    /// </summary>
    public bool IsCompleted => Status == "completed";

    /// <summary>
    /// Friendly due date text.
    /// </summary>
    public string DueDateText
    {
        get
        {
            if (!DueDate.HasValue)
                return "No due date";

            var today = DateTime.UtcNow.Date;
            var dueDate = DueDate.Value.Date;

            if (dueDate == today)
                return "Due today";
            if (dueDate == today.AddDays(1))
                return "Due tomorrow";
            if (dueDate < today)
                return "Overdue";
            if ((dueDate - today).Days <= 7)
                return $"Due in {(dueDate - today).Days}d";
            return dueDate.ToString("MMM d");
        }
    }

    /// <summary>
    /// Priority display text with emoji.
    /// </summary>
    public string PriorityDisplay => Priority?.ToLower() switch
    {
        "high" => "🔴 High",
        "medium" => "🟡 Medium",
        "low" => "🟢 Low",
        _ => "⚪ None"
    };

    #endregion
}
