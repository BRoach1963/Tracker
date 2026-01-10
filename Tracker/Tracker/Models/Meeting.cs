using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a meeting (1:1, team meeting, all-hands, etc.).
/// Maps to Supabase meetings table.
/// </summary>
public class Meeting
{
    /// <summary>
    /// Unique identifier for the meeting.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this meeting belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// User who created this meeting.
    /// </summary>
    [Required]
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Type of meeting.
    /// </summary>
    [Required]
    public MeetingType MeetingType { get; set; } = MeetingType.OneOnOne;

    /// <summary>
    /// For 1:1s - the manager's team member ID.
    /// </summary>
    public Guid? ManagerTeamMemberId { get; set; }

    /// <summary>
    /// For 1:1s - the report's team member ID.
    /// </summary>
    public Guid? ReportTeamMemberId { get; set; }

    /// <summary>
    /// For team meetings - the team ID.
    /// </summary>
    public Guid? TeamId { get; set; }

    /// <summary>
    /// Meeting title.
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Meeting description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Scheduled date and time.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Duration of meeting in minutes.
    /// </summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// iCal RRULE format recurrence rule.
    /// </summary>
    [MaxLength(200)]
    public string? RecurrenceRule { get; set; }

    /// <summary>
    /// Location (room name or URL).
    /// </summary>
    [MaxLength(500)]
    public string? Location { get; set; }

    /// <summary>
    /// Meeting status.
    /// </summary>
    [Required]
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;

    /// <summary>
    /// When the meeting actually started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the meeting ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// When the meeting record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the meeting record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this meeting is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When the meeting was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who deleted the meeting.
    /// </summary>
    public Guid? DeletedBy { get; set; }

    // Sync metadata
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public int SyncVersion { get; set; } = 1;
    public DateTime SyncModifiedAt { get; set; } = DateTime.UtcNow;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(ManagerTeamMemberId))]
    public virtual TeamMember? Manager { get; set; }

    [ForeignKey(nameof(ReportTeamMemberId))]
    public virtual TeamMember? Report { get; set; }

    [ForeignKey(nameof(TeamId))]
    public virtual Team? Team { get; set; }

    public virtual ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();
    public virtual ICollection<MeetingAgendaItem> AgendaItems { get; set; } = new List<MeetingAgendaItem>();
    public virtual ICollection<MeetingNote> Notes { get; set; } = new List<MeetingNote>();
    public virtual ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    public virtual ICollection<TrackerTask> Tasks { get; set; } = new List<TrackerTask>();

    // Computed properties

    /// <summary>
    /// Whether the meeting is in the future.
    /// </summary>
    [NotMapped]
    public bool IsUpcoming => ScheduledAt.HasValue && ScheduledAt.Value > DateTime.UtcNow && Status == MeetingStatus.Scheduled;

    /// <summary>
    /// Whether the meeting is overdue (scheduled in the past but not completed).
    /// </summary>
    [NotMapped]
    public bool IsOverdue => ScheduledAt.HasValue && ScheduledAt.Value < DateTime.UtcNow && Status == MeetingStatus.Scheduled;

    /// <summary>
    /// Scheduled end time based on start time and duration.
    /// </summary>
    [NotMapped]
    public DateTime? ScheduledEndAt => ScheduledAt?.AddMinutes(DurationMinutes);

    /// <summary>
    /// Whether this is a 1:1 meeting.
    /// </summary>
    [NotMapped]
    public bool IsOneOnOne => MeetingType == MeetingType.OneOnOne;
}
