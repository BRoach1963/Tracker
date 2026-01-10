using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents an action item from a meeting.
/// A simpler version of a task, specific to meeting follow-ups.
/// Maps to Supabase action_items table.
/// </summary>
public class ActionItem
{
    /// <summary>
    /// Unique identifier for this action item.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The meeting this action item came from.
    /// </summary>
    [Required]
    public Guid MeetingId { get; set; }

    /// <summary>
    /// Team member assigned to this action item.
    /// </summary>
    public Guid? AssigneeTeamMemberId { get; set; }

    /// <summary>
    /// Action item title.
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the action item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Due date for this action item.
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// Whether this action item is completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// When this action item was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// If converted to a task, the ID of that task.
    /// </summary>
    public Guid? ConvertedTaskId { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(MeetingId))]
    public virtual Meeting? Meeting { get; set; }

    [ForeignKey(nameof(AssigneeTeamMemberId))]
    public virtual TeamMember? Assignee { get; set; }

    [ForeignKey(nameof(ConvertedTaskId))]
    public virtual TrackerTask? ConvertedTask { get; set; }

    // Computed properties

    /// <summary>
    /// Whether this action item has been converted to a task.
    /// </summary>
    [NotMapped]
    public bool IsConvertedToTask => ConvertedTaskId.HasValue;

    /// <summary>
    /// Whether this action item is overdue.
    /// </summary>
    [NotMapped]
    public bool IsOverdue => !IsCompleted && DueDate.HasValue && DueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Days remaining until due date (negative if overdue).
    /// </summary>
    [NotMapped]
    public int? DaysRemaining => DueDate.HasValue
        ? (DueDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days
        : null;
}
