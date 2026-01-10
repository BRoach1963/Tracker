using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a recurring talking point for 1:1s between manager and report.
/// Maps to Supabase talking_points table.
/// </summary>
public class TalkingPoint
{
    /// <summary>
    /// Unique identifier for this talking point.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Manager's team member ID for this 1:1 relationship.
    /// </summary>
    [Required]
    public Guid ManagerTeamMemberId { get; set; }

    /// <summary>
    /// Report's team member ID for this 1:1 relationship.
    /// </summary>
    [Required]
    public Guid ReportTeamMemberId { get; set; }

    /// <summary>
    /// Team member who added this talking point.
    /// </summary>
    public Guid? AddedByTeamMemberId { get; set; }

    /// <summary>
    /// Title of the talking point.
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notes or details about this talking point.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Category (career, feedback, project, personal, etc.).
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Whether this is a recurring topic that should persist.
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Whether this talking point is still active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this topic was last discussed.
    /// </summary>
    public DateTime? LastDiscussedAt { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ManagerTeamMemberId))]
    public virtual TeamMember? Manager { get; set; }

    [ForeignKey(nameof(ReportTeamMemberId))]
    public virtual TeamMember? Report { get; set; }

    [ForeignKey(nameof(AddedByTeamMemberId))]
    public virtual TeamMember? AddedBy { get; set; }

    // Computed properties

    /// <summary>
    /// Whether this talking point has never been discussed.
    /// </summary>
    [NotMapped]
    public bool NeverDiscussed => !LastDiscussedAt.HasValue;

    /// <summary>
    /// Days since this topic was last discussed.
    /// </summary>
    [NotMapped]
    public int? DaysSinceLastDiscussion => LastDiscussedAt.HasValue
        ? (int)(DateTime.UtcNow - LastDiscussedAt.Value).TotalDays
        : null;
}
