using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents an attendee/participant in a meeting.
/// Maps to Supabase meeting_attendees table.
/// </summary>
public class MeetingAttendee
{
    /// <summary>
    /// Unique identifier for this record.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The meeting this attendee belongs to.
    /// </summary>
    [Required]
    public Guid MeetingId { get; set; }

    /// <summary>
    /// The team member who is attending.
    /// </summary>
    [Required]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// Response to the meeting invitation.
    /// </summary>
    public AttendeeResponse Response { get; set; } = AttendeeResponse.Pending;

    /// <summary>
    /// When the attendee responded.
    /// </summary>
    public DateTime? ResponseAt { get; set; }

    /// <summary>
    /// Whether the attendee actually attended the meeting.
    /// </summary>
    public bool? Attended { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(MeetingId))]
    public virtual Meeting? Meeting { get; set; }

    [ForeignKey(nameof(TeamMemberId))]
    public virtual TeamMember? TeamMember { get; set; }

    // Computed properties

    /// <summary>
    /// Whether the attendee has accepted the meeting.
    /// </summary>
    [NotMapped]
    public bool HasAccepted => Response == AttendeeResponse.Accepted;

    /// <summary>
    /// Whether the attendee has declined the meeting.
    /// </summary>
    [NotMapped]
    public bool HasDeclined => Response == AttendeeResponse.Declined;
}
