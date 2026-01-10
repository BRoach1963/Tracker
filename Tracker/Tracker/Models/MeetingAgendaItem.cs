using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents an agenda item for a meeting.
/// Maps to Supabase meeting_agenda_items table.
/// </summary>
public class MeetingAgendaItem
{
    /// <summary>
    /// Unique identifier for this agenda item.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The meeting this agenda item belongs to.
    /// </summary>
    [Required]
    public Guid MeetingId { get; set; }

    /// <summary>
    /// Team member who added this agenda item.
    /// </summary>
    public Guid? AddedByTeamMemberId { get; set; }

    /// <summary>
    /// Agenda item title.
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notes or details for this agenda item.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Order of this item in the agenda.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this item has been discussed.
    /// </summary>
    public bool IsDiscussed { get; set; }

    /// <summary>
    /// When this item was discussed.
    /// </summary>
    public DateTime? DiscussedAt { get; set; }

    /// <summary>
    /// Estimated time for this item in minutes.
    /// </summary>
    public int? TimeEstimateMinutes { get; set; }

    /// <summary>
    /// Actual time spent on this item in minutes.
    /// </summary>
    public int? ActualDurationMinutes { get; set; }

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

    [ForeignKey(nameof(AddedByTeamMemberId))]
    public virtual TeamMember? AddedBy { get; set; }

    // Computed properties

    /// <summary>
    /// Whether this item is still pending discussion.
    /// </summary>
    [NotMapped]
    public bool IsPending => !IsDiscussed;

    /// <summary>
    /// Difference between estimated and actual time (positive = over time).
    /// </summary>
    [NotMapped]
    public int? TimeVariance => ActualDurationMinutes.HasValue && TimeEstimateMinutes.HasValue
        ? ActualDurationMinutes.Value - TimeEstimateMinutes.Value
        : null;
}
