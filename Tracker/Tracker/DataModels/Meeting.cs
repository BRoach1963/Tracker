using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Meeting - unified model for all meeting types (1:1, team, all-hands, project, interview, other).
    /// Consolidates OneOnOne and Meeting classes into single entity.
    /// Maps to Supabase 'meetings' table.
    /// 
    /// Meeting type is determined by the 'Type' enum value:
    /// - OneOnOne: 1:1 meeting between manager and report (uses ManagerTeamMemberId + ReportTeamMemberId)
    /// - TeamMeeting: Team-level meeting (uses TeamId)
    /// - AllHands: Organization-wide meeting
    /// - Project: Project-related meeting (uses ProjectId)
    /// - Interview: Interview or assessment meeting
    /// - Other: Uncategorized meeting
    /// </summary>
    public class Meeting : AuditableEntity
    {
        #region Core Identity & Organization

        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this meeting belongs to (non-nullable).
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// User who created this meeting.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        #endregion

        #region Meeting Type & Details

        /// <summary>
        /// Meeting type (OneOnOne, TeamMeeting, AllHands, Project, Interview, Other).
        /// Maps to meeting_type enum in schema.
        /// </summary>
        public MeetingType Type { get; set; } = MeetingType.OneOnOne;

        /// <summary>
        /// Meeting title (VARCHAR 300 NOT NULL).
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Meeting description/details (TEXT, nullable).
        /// </summary>
        public string? Description { get; set; }

        #endregion

        #region Participants (Context-Dependent Based on Type)

        /// <summary>
        /// Manager in 1:1 context (UUID FK, nullable).
        /// Only populated for OneOnOne meetings.
        /// </summary>
        public Guid? ManagerTeamMemberId { get; set; }
        public TeamMember? Manager { get; set; }

        /// <summary>
        /// Report/Attendee in 1:1 context (UUID FK, nullable).
        /// Only populated for OneOnOne meetings.
        /// </summary>
        public Guid? ReportTeamMemberId { get; set; }
        public TeamMember? Report { get; set; }

        /// <summary>
        /// Team for team meetings (UUID FK, nullable).
        /// Only populated for TeamMeeting meetings.
        /// </summary>
        public Guid? TeamId { get; set; }
        public Team? Team { get; set; }

        /// <summary>
        /// Project for project-related meetings (UUID FK, nullable).
        /// </summary>
        public Guid? ProjectId { get; set; }
        public Project? Project { get; set; }

        #endregion

        #region Scheduling & Timing

        /// <summary>
        /// When the meeting is scheduled (TIMESTAMPTZ NOT NULL).
        /// Consolidates Date + StartTime from old models.
        /// </summary>
        public DateTime ScheduledAt { get; set; }

        /// <summary>
        /// Duration of the meeting in minutes (INTEGER, nullable).
        /// Replaces TimeSpan Duration from old models.
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Recurrence rule in iCal format (VARCHAR 200, nullable).
        /// Replaces IsRecurring bool from old models.
        /// Empty/null = single occurrence.
        /// </summary>
        public string? RecurrenceRule { get; set; }

        /// <summary>
        /// Meeting location or video conference link (VARCHAR 500, nullable).
        /// </summary>
        public string? Location { get; set; }

        #endregion

        #region Actual Meeting Execution

        /// <summary>
        /// When the meeting actually started (TIMESTAMPTZ, nullable).
        /// Only populated after meeting begins.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the meeting actually ended (TIMESTAMPTZ, nullable).
        /// Only populated after meeting concludes.
        /// </summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// Current status (Scheduled, InProgress, Completed, Cancelled).
        /// Maps to meeting_status enum in schema.
        /// </summary>
        public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;

        #endregion

        #region Related Content & Notes

        /// <summary>
        /// Action items/tasks created from this meeting.
        /// Replaces List<MeetingTask> from OneOnOne.
        /// Now uses List<Task> with MeetingId FK.
        /// </summary>
        public List<Task> Tasks { get; set; } = new();

        /// <summary>
        /// Agenda items for this meeting.
        /// </summary>
        public List<AgendaItem> AgendaItems { get; set; } = new();

        /// <summary>
        /// Meeting notes/summary (TEXT, nullable).
        /// Consolidates Agenda + Notes + Feedback from old models.
        /// </summary>
        public string? Notes { get; set; }

        #endregion

        #region Calendar Sync (from OneOnOne)

        /// <summary>
        /// Google Calendar event ID.
        /// </summary>
        public string? GoogleCalendarEventId { get; set; }

        /// <summary>
        /// Outlook/Microsoft Graph calendar event ID.
        /// </summary>
        public string? OutlookCalendarEventId { get; set; }

        /// <summary>
        /// Teams meeting join URL.
        /// </summary>
        public string? TeamsMeetingUrl { get; set; }

        /// <summary>
        /// Teams meeting ID for updates/cancellation.
        /// </summary>
        public string? TeamsMeetingId { get; set; }

        /// <summary>
        /// Google Meet URL.
        /// </summary>
        public string? GoogleMeetUrl { get; set; }

        /// <summary>
        /// When this meeting was last synced with external calendar.
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// Current sync status (Synced, Pending, Error, NotSynced).
        /// </summary>
        public string? SyncStatus { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Is this a recurring meeting? (check if RecurrenceRule is set)
        /// </summary>
        public bool IsRecurring => !string.IsNullOrEmpty(RecurrenceRule);

        /// <summary>
        /// Is the meeting completed?
        /// </summary>
        public bool IsCompleted => Status == MeetingStatus.Completed;

        /// <summary>
        /// Count of incomplete action items from this meeting.
        /// </summary>
        public int ActionItemCount => Tasks?.Count(t => !t.IsCompleted) ?? 0;

        /// <summary>
        /// Count of agenda items for this meeting.
        /// </summary>
        public int AgendaItemCount => AgendaItems?.Count ?? 0;

        /// <summary>
        /// Is synced to Google Calendar?
        /// </summary>
        public bool IsSyncedToGoogle => !string.IsNullOrEmpty(GoogleCalendarEventId);

        /// <summary>
        /// Is synced to Outlook/Microsoft?
        /// </summary>
        public bool IsSyncedToOutlook => !string.IsNullOrEmpty(OutlookCalendarEventId);

        /// <summary>
        /// Has a Teams meeting link?
        /// </summary>
        public bool HasTeamsMeeting => !string.IsNullOrEmpty(TeamsMeetingUrl);

        /// <summary>
        /// Has a Google Meet link?
        /// </summary>
        public bool HasGoogleMeet => !string.IsNullOrEmpty(GoogleMeetUrl);

        #endregion
    }
}
