using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A 1:1 meeting between manager and team member.
    /// Simplified model focused on: who, when, what to discuss, what came out, and linked items.
    /// </summary>
    public class OneOnOne : AuditableEntity
    {
        #region Core Properties

        public int Id { get; set; }

        /// <summary>
        /// Brief title/description of the meeting.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The team member this 1:1 is with.
        /// </summary>
        public TeamMember TeamMember { get; set; } = new();

        /// <summary>
        /// Display name for the team member.
        /// </summary>
        public string TeamMemberName => $"{TeamMember.FirstName} {TeamMember.LastName}";

        #endregion

        #region When (Date/Time)

        public DateTime Date { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        public bool IsRecurring { get; set; }

        /// <summary>
        /// Meeting status (Scheduled, Completed, Cancelled).
        /// </summary>
        public MeetingStatusEnum Status { get; set; }

        #endregion

        #region What to Discuss (Agenda Items)

        /// <summary>
        /// Agenda items for this meeting (topics, concerns, questions, blockers, decisions).
        /// </summary>
        public List<AgendaItem> AgendaItems { get; set; } = new();

        #endregion

        #region What Came Out (Tasks)

        /// <summary>
        /// Tasks created from this meeting (replaces ActionItems + FollowUpItems).
        /// </summary>
        public List<MeetingTask> Tasks { get; set; } = new();

        #endregion

        #region Notes & Feedback

        /// <summary>
        /// High-level agenda text (used for calendar sync).
        /// </summary>
        public string Agenda { get; set; } = string.Empty;

        /// <summary>
        /// Free-form meeting notes.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Feedback given during the meeting.
        /// </summary>
        public string Feedback { get; set; } = string.Empty;

        #endregion

        #region Linked Items (Existing Tasks/OKRs/KPIs Discussed)

        /// <summary>
        /// Links to existing IndividualTasks that were discussed.
        /// </summary>
        public List<OneOnOneLinkedTask> LinkedTasks { get; set; } = new();

        /// <summary>
        /// Links to existing OKRs that were discussed.
        /// </summary>
        public List<OneOnOneLinkedOkr> LinkedOkrs { get; set; } = new();

        /// <summary>
        /// Links to existing KPIs that were discussed.
        /// </summary>
        public List<OneOnOneLinkedKpi> LinkedKpis { get; set; } = new();

        #endregion

        #region Calendar & Meeting Sync

        /// <summary>
        /// Google Calendar event ID (for Google Calendar sync).
        /// </summary>
        public string? GoogleCalendarEventId { get; set; }

        /// <summary>
        /// Microsoft Graph calendar event ID (for Outlook/Teams sync).
        /// </summary>
        public string? CalendarEventId { get; set; }

        /// <summary>
        /// Teams meeting join URL (if a Teams meeting was created).
        /// </summary>
        public string? TeamsMeetingUrl { get; set; }

        /// <summary>
        /// Teams meeting ID for updates/cancellation.
        /// </summary>
        public string? TeamsMeetingId { get; set; }

        /// <summary>
        /// Whether a Teams meeting link has been generated.
        /// </summary>
        public bool HasTeamsMeeting => !string.IsNullOrEmpty(TeamsMeetingUrl);

        /// <summary>
        /// Google Meet URL (if a Google Meet was created).
        /// </summary>
        public string? GoogleMeetUrl { get; set; }

        /// <summary>
        /// Whether a Google Meet link has been generated.
        /// </summary>
        public bool HasGoogleMeet => !string.IsNullOrEmpty(GoogleMeetUrl);

        /// <summary>
        /// ETag for conflict detection (Microsoft Graph).
        /// </summary>
        public string? CalendarEventEtag { get; set; }

        /// <summary>
        /// When this meeting was last synced with external calendar.
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// Current sync status: NotSynced, Synced, Pending, Error.
        /// </summary>
        public string SyncStatus { get; set; } = "NotSynced";

        public bool IsSyncedToGoogle { get; set; }
        
        /// <summary>
        /// Whether this meeting is synced to Microsoft Calendar.
        /// </summary>
        public bool IsSyncedToOutlook => !string.IsNullOrEmpty(CalendarEventId);

        #endregion

        #region Computed Display Properties

        /// <summary>
        /// Number of tasks/action items from this meeting.
        /// </summary>
        public int TaskCount => Tasks?.Count ?? 0;

        /// <summary>
        /// Number of agenda items for this meeting.
        /// </summary>
        public int AgendaCount => AgendaItems?.Count ?? 0;

        /// <summary>
        /// Number of incomplete tasks from this meeting.
        /// </summary>
        public int IncompleteTaskCount => Tasks?.Count(t => !t.IsCompleted) ?? 0;

        /// <summary>
        /// Display string for task count with incomplete indicator.
        /// </summary>
        public string TasksDisplay => IncompleteTaskCount > 0 
            ? $"{TaskCount} ({IncompleteTaskCount} pending)" 
            : TaskCount.ToString();

        /// <summary>
        /// Short description preview (first 50 chars).
        /// </summary>
        public string DescriptionPreview => string.IsNullOrEmpty(Description) 
            ? "—" 
            : (Description.Length > 50 ? Description.Substring(0, 47) + "..." : Description);

        /// <summary>
        /// Formatted date and time for display.
        /// </summary>
        public string DateTimeDisplay => $"{Date:MMM dd} @ {StartTime:hh\\:mm}";

        #endregion
    }
}
