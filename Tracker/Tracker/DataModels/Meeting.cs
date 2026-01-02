using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a meeting or 1:1 discussion between team members.
    /// </summary>
    [Table("Meetings")]
    public class Meeting : AuditableEntity
    {
        /// <summary>Gets or sets the meeting type (OneOnOne, TeamMeeting, etc.).</summary>
        [Required]
        public MeetingType Type { get; set; }

        /// <summary>Gets or sets the meeting title.</summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the primary attendee (the employee being met with).</summary>
        [Required]
        public int PrimaryAttendeeId { get; set; }

        [ForeignKey(nameof(PrimaryAttendeeId))]
        public TeamMember? PrimaryAttendee { get; set; }

        /// <summary>Gets the primary attendee name.</summary>
        [NotMapped]
        public string PrimaryAttendeeName => PrimaryAttendee?.Name ?? "Unknown";

        /// <summary>Gets or sets the meeting date.</summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>Gets or sets the start time.</summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>Gets or sets the end time.</summary>
        public TimeSpan? EndTime { get; set; }

        /// <summary>Gets or sets the meeting duration in minutes.</summary>
        public int? Duration { get; set; }

        /// <summary>Gets or sets the meeting status.</summary>
        public MeetingStatusEnum Status { get; set; } = MeetingStatusEnum.Scheduled;

        /// <summary>Gets or sets whether this is a recurring meeting.</summary>
        public bool IsRecurring { get; set; }

        /// <summary>Gets or sets the recurring series ID if this is part of a series.</summary>
        public int? RecurringSeriesId { get; set; }

        /// <summary>Gets or sets the associated project ID if applicable.</summary>
        public int? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        /// <summary>Gets or sets the meeting notes.</summary>
        [StringLength(4000)]
        public string? Notes { get; set; }

        /// <summary>Gets or sets the meeting location or video conference link.</summary>
        [StringLength(500)]
        public string? Location { get; set; }
    }
}
