using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Core.Common.Enums;

namespace Tracker.Core.DataModels;

/// <summary>
/// Represents an attendee/participant in a meeting.
/// Maps to Supabase meeting_attendees table (7 base columns + 3 added via ALTER).
/// Note: This table does NOT have soft delete or updated_at columns.
/// </summary>
[Table("meeting_attendees")]
public class MeetingAttendee
{
    /// <summary>
    /// Unique identifier for this record.
    /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The meeting this attendee belongs to.
    /// Maps to: meeting_id UUID NOT NULL
    /// </summary>
    [Required]
    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    /// <summary>
    /// The team member who is attending.
    /// Maps to: team_member_id UUID NOT NULL
    /// </summary>
    [Required]
    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// Response to the meeting invitation (stored as string for PostgreSQL).
    /// Maps to: response VARCHAR(50) NULL
    /// </summary>
    [Column("response")]
    [MaxLength(50)]
    public string? ResponseString { get; set; } = "pending";

    /// <summary>
    /// Response to the meeting invitation as C# enum.
    /// </summary>
    [NotMapped]
    public AttendeeResponse Response
    {
        get => ResponseString switch
        {
            "pending" => AttendeeResponse.Pending,
            "accepted" => AttendeeResponse.Accepted,
            "declined" => AttendeeResponse.Declined,
            "tentative" => AttendeeResponse.Tentative,
            _ => AttendeeResponse.Pending
        };
        set => ResponseString = value switch
        {
            AttendeeResponse.Pending => "pending",
            AttendeeResponse.Accepted => "accepted",
            AttendeeResponse.Declined => "declined",
            AttendeeResponse.Tentative => "tentative",
            _ => "pending"
        };
    }

    /// <summary>
    /// When the attendee responded.
    /// Maps to: response_at TIMESTAMPTZ NULL
    /// </summary>
    [Column("response_at")]
    public DateTime? ResponseAt { get; set; }

    /// <summary>
    /// Whether the attendee actually attended the meeting.
    /// Maps to: attended BOOLEAN NULL
    /// </summary>
    [Column("attended")]
    public bool? Attended { get; set; }

    /// <summary>
    /// When this record was created.
    /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    #region Calendar Sync Fields (Added via ALTER)

    /// <summary>
    /// Email address used in external calendar system (if synced).
    /// Maps to: external_attendee_email VARCHAR(255) NULL (added via ALTER)
    /// </summary>
    [Column("external_attendee_email")]
    [MaxLength(255)]
    public string? ExternalAttendeeEmail { get; set; }

    /// <summary>
    /// When the attendee removed this meeting from their calendar.
    /// Maps to: removed_from_calendar_at TIMESTAMPTZ NULL (added via ALTER)
    /// </summary>
    [Column("removed_from_calendar_at")]
    public DateTime? RemovedFromCalendarAt { get; set; }

    /// <summary>
    /// Sync status with external calendar ("synced", "out_of_sync", "pending", "error").
    /// Maps to: sync_status VARCHAR(50) DEFAULT 'synced' (added via ALTER)
    /// </summary>
    [Column("sync_status")]
    [MaxLength(50)]
    public string? SyncStatus { get; set; } = "synced";

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Navigation property for Meeting.
    /// </summary>
    [NotMapped]
    public virtual Meeting? Meeting { get; set; }

    /// <summary>
    /// Navigation property for TeamMember.
    /// </summary>
    [NotMapped]
    public virtual TeamMember? TeamMember { get; set; }

    #endregion

    #region Computed Properties

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

    /// <summary>
    /// Whether the attendee has a sync issue.
    /// </summary>
    [NotMapped]
    public bool HasSyncIssue => SyncStatus != "synced" && SyncStatus != null;

    #endregion
}
