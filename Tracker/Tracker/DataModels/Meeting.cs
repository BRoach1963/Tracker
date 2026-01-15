using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Meeting - unified model for all meeting types (1:1, team, all-hands, project, interview, other).
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
    [Table("meetings")]
    public class Meeting : AuditableEntity
    {
        #region Core Identity & Organization

        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this meeting belongs to (non-nullable).
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// User who created this meeting.
        /// Maps to: created_by_user_id UUID NOT NULL
        /// </summary>
        [Column("created_by_user_id")]
        public Guid CreatedByUserId { get; set; }

        #endregion

        #region Meeting Type & Details

        /// <summary>
        /// Meeting type stored as string for PostgreSQL enum.
        /// Maps to: meeting_type meeting_type (enum) NOT NULL
        /// </summary>
        [Column("meeting_type")]
        [MaxLength(50)]
        public string TypeString { get; set; } = "one_on_one";

        /// <summary>
        /// Meeting type as C# enum.
        /// </summary>
        [NotMapped]
        public MeetingType Type
        {
            get => TypeString switch
            {
                "one_on_one" => MeetingType.OneOnOne,
                "team_meeting" => MeetingType.TeamMeeting,
                "all_hands" => MeetingType.AllHands,
                "project" => MeetingType.Project,
                "interview" => MeetingType.Interview,
                _ => MeetingType.Other
            };
            set => TypeString = value switch
            {
                MeetingType.OneOnOne => "one_on_one",
                MeetingType.TeamMeeting => "team_meeting",
                MeetingType.AllHands => "all_hands",
                MeetingType.Project => "project",
                MeetingType.Interview => "interview",
                _ => "other"
            };
        }

        /// <summary>
        /// Meeting title (VARCHAR 300 NOT NULL).
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Meeting description/details (TEXT, nullable).
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        #endregion

        #region Participants (Context-Dependent Based on Type)

        /// <summary>
        /// Manager in 1:1 context (UUID FK, nullable).
        /// Only populated for OneOnOne meetings.
        /// Maps to: manager_team_member_id UUID NULL
        /// </summary>
        [Column("manager_team_member_id")]
        public Guid? ManagerTeamMemberId { get; set; }

        /// <summary>
        /// Navigation property for Manager.
        /// </summary>
        [NotMapped]
        public TeamMember? Manager { get; set; }

        /// <summary>
        /// Report/Attendee in 1:1 context (UUID FK, nullable).
        /// Only populated for OneOnOne meetings.
        /// Maps to: report_team_member_id UUID NULL
        /// </summary>
        [Column("report_team_member_id")]
        public Guid? ReportTeamMemberId { get; set; }

        /// <summary>
        /// Navigation property for Report.
        /// </summary>
        [NotMapped]
        public TeamMember? Report { get; set; }

        /// <summary>
        /// Team for team meetings (UUID FK, nullable).
        /// Only populated for TeamMeeting meetings.
        /// Maps to: team_id UUID NULL
        /// </summary>
        [Column("team_id")]
        public Guid? TeamId { get; set; }

        /// <summary>
        /// Navigation property for Team.
        /// </summary>
        [NotMapped]
        public Team? Team { get; set; }

        /// <summary>
        /// Project for project-related meetings (UUID FK, nullable).
        /// Maps to: project_id UUID NULL (added via ALTER)
        /// </summary>
        [Column("project_id")]
        public Guid? ProjectId { get; set; }

        /// <summary>
        /// Navigation property for Project.
        /// </summary>
        [NotMapped]
        public Project? Project { get; set; }

        #endregion

        #region Scheduling & Timing

        /// <summary>
        /// When the meeting is scheduled (TIMESTAMPTZ).
        /// Maps to: scheduled_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("scheduled_at")]
        public DateTime ScheduledAt { get; set; }

        /// <summary>
        /// Duration of the meeting in minutes.
        /// Maps to: duration_minutes INT4 NOT NULL
        /// </summary>
        [Column("duration_minutes")]
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Recurrence rule in iCal RRULE format (VARCHAR 200, nullable).
        /// Empty/null = single occurrence.
        /// Maps to: recurrence_rule VARCHAR(200) NULL
        /// </summary>
        [Column("recurrence_rule")]
        [MaxLength(200)]
        public string? RecurrenceRule { get; set; }

        /// <summary>
        /// Meeting location (physical address or note).
        /// Maps to: location VARCHAR(500) NULL
        /// </summary>
        [Column("location")]
        [MaxLength(500)]
        public string? Location { get; set; }

        #endregion

        #region Actual Meeting Execution

        /// <summary>
        /// When the meeting actually started (TIMESTAMPTZ, nullable).
        /// Only populated after meeting begins.
        /// Maps to: started_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the meeting actually ended (TIMESTAMPTZ, nullable).
        /// Only populated after meeting concludes.
        /// Maps to: ended_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// Meeting status stored as string for PostgreSQL enum.
        /// Maps to: status meeting_status (enum) NOT NULL
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string StatusString { get; set; } = "scheduled";

        /// <summary>
        /// Meeting status as C# enum.
        /// </summary>
        [NotMapped]
        public MeetingStatus Status
        {
            get => StatusString switch
            {
                "scheduled" => MeetingStatus.Scheduled,
                "in_progress" => MeetingStatus.InProgress,
                "completed" => MeetingStatus.Completed,
                "cancelled" => MeetingStatus.Cancelled,
                _ => MeetingStatus.Scheduled
            };
            set => StatusString = value switch
            {
                MeetingStatus.Scheduled => "scheduled",
                MeetingStatus.InProgress => "in_progress",
                MeetingStatus.Completed => "completed",
                MeetingStatus.Cancelled => "cancelled",
                _ => "scheduled"
            };
        }

        #endregion

        #region Related Content & Notes

        /// <summary>
        /// Action items/tasks created from this meeting.
        /// </summary>
        [NotMapped]
        public List<TrackerTask> Tasks { get; set; } = new();

        /// <summary>
        /// Agenda items for this meeting.
        /// </summary>
        [NotMapped]
        public List<AgendaItem> AgendaItems { get; set; } = new();

        /// <summary>
        /// Meeting notes/summary (TEXT, nullable).
        /// Maps to: notes TEXT NULL (if exists in schema)
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }

        #endregion

        #region Calendar Sync (Generic - One Provider at a Time)

        /// <summary>
        /// External calendar event ID (Google or Outlook).
        /// Maps to: calendar_event_id VARCHAR(255) NULL
        /// </summary>
        [Column("calendar_event_id")]
        [MaxLength(255)]
        public string? CalendarEventId { get; set; }

        /// <summary>
        /// Calendar provider as string for PostgreSQL.
        /// Maps to: calendar_provider VARCHAR(50) NULL
        /// </summary>
        [Column("calendar_provider")]
        [MaxLength(50)]
        public string? CalendarProviderString { get; set; }

        /// <summary>
        /// Calendar provider as C# enum.
        /// </summary>
        [NotMapped]
        public CalendarProviderType? CalendarProvider
        {
            get => CalendarProviderString switch
            {
                "google" => CalendarProviderType.Google,
                "microsoft" => CalendarProviderType.Microsoft,
                "apple" => CalendarProviderType.Apple,
                _ => null
            };
            set => CalendarProviderString = value switch
            {
                CalendarProviderType.Google => "google",
                CalendarProviderType.Microsoft => "microsoft",
                CalendarProviderType.Apple => "apple",
                _ => null
            };
        }

        /// <summary>
        /// ETag/change token from calendar provider for efficient sync.
        /// Maps to: calendar_etag VARCHAR(500) NULL
        /// </summary>
        [Column("calendar_etag")]
        [MaxLength(500)]
        public string? CalendarEtag { get; set; }

        /// <summary>
        /// FK to calendar_links - which OAuth connection was used to sync this meeting.
        /// Maps to: calendar_link_id UUID NULL
        /// </summary>
        [Column("calendar_link_id")]
        public Guid? CalendarLinkId { get; set; }

        /// <summary>
        /// Calendar sync status as string.
        /// Maps to: calendar_sync_status VARCHAR(50) DEFAULT 'not_synced'
        /// </summary>
        [Column("calendar_sync_status")]
        [MaxLength(50)]
        public string? CalendarSyncStatus { get; set; } = "not_synced";

        /// <summary>
        /// When this meeting was last synced with external calendar.
        /// Maps to: last_synced_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("last_synced_at")]
        public DateTime? LastSyncedAt { get; set; }

        #endregion

        #region Video Conference (Generic - One Platform at a Time)

        /// <summary>
        /// Join URL for the video meeting (Teams, Google Meet, Zoom, etc.).
        /// Maps to: video_conference_url VARCHAR(500) NULL
        /// </summary>
        [Column("video_conference_url")]
        [MaxLength(500)]
        public string? VideoConferenceUrl { get; set; }

        /// <summary>
        /// Video conference provider as string.
        /// Maps to: video_conference_provider VARCHAR(50) NULL
        /// </summary>
        [Column("video_conference_provider")]
        [MaxLength(50)]
        public string? VideoConferenceProviderString { get; set; }

        /// <summary>
        /// Video conference provider as C# enum.
        /// </summary>
        [NotMapped]
        public Common.Enums.VideoConferenceProvider? VideoConferenceProvider
        {
            get => VideoConferenceProviderString switch
            {
                "teams" => Common.Enums.VideoConferenceProvider.Teams,
                "google_meet" => Common.Enums.VideoConferenceProvider.GoogleMeet,
                "zoom" => Common.Enums.VideoConferenceProvider.Zoom,
                "webex" => Common.Enums.VideoConferenceProvider.WebEx,
                _ => null
            };
            set => VideoConferenceProviderString = value switch
            {
                Common.Enums.VideoConferenceProvider.Teams => "teams",
                Common.Enums.VideoConferenceProvider.GoogleMeet => "google_meet",
                Common.Enums.VideoConferenceProvider.Zoom => "zoom",
                Common.Enums.VideoConferenceProvider.WebEx => "webex",
                _ => null
            };
        }

        /// <summary>
        /// Provider-specific meeting ID for API operations (update/cancel).
        /// Maps to: video_conference_id VARCHAR(255) NULL
        /// </summary>
        [Column("video_conference_id")]
        [MaxLength(255)]
        public string? VideoConferenceId { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Is this a recurring meeting? (check if RecurrenceRule is set)
        /// </summary>
        [NotMapped]
        public bool IsRecurring => !string.IsNullOrEmpty(RecurrenceRule);

        /// <summary>
        /// Is the meeting completed?
        /// </summary>
        [NotMapped]
        public bool IsCompleted => Status == MeetingStatus.Completed;

        /// <summary>
        /// Count of incomplete action items from this meeting.
        /// </summary>
        [NotMapped]
        public int ActionItemCount => Tasks?.Count(t => t.Status != WorkItemStatus.Completed) ?? 0;

        /// <summary>
        /// Count of agenda items for this meeting.
        /// </summary>
        [NotMapped]
        public int AgendaItemCount => AgendaItems?.Count ?? 0;

        /// <summary>
        /// Is synced to any external calendar?
        /// </summary>
        [NotMapped]
        public bool IsSyncedToCalendar => !string.IsNullOrEmpty(CalendarEventId);

        /// <summary>
        /// Has a video conference link?
        /// </summary>
        [NotMapped]
        public bool HasVideoConference => !string.IsNullOrEmpty(VideoConferenceUrl);

        #endregion
    }
}
