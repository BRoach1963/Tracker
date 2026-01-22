using System;
using System.Collections.Generic;
using System.Linq;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Meeting model - maps to procohere.meetings table in Supabase.
/// </summary>
[Table("meetings")]
public class MeetingDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("meeting_type")]
    public string MeetingType { get; set; } = "one_on_one";

    [Column("status")]
    public string Status { get; set; } = "scheduled";

    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; } = 30;

    [Column("location")]
    public string? Location { get; set; }

    [Column("video_link")]
    public string? VideoLink { get; set; }

    [Column("recurrence_rule")]
    public string? RecurrenceRule { get; set; }

    [Column("parent_meeting_id")]
    public Guid? ParentMeetingId { get; set; }

    [Column("meeting_series_id")]
    public Guid? MeetingSeriesId { get; set; }

    [Column("created_by")]
    public Guid CreatedByTeamMemberId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #region Legacy/Compatibility Properties

    /// <summary>
    /// LEGACY: For backward compatibility with old 1:1 meeting UI code.
    /// Use Attendees collection instead for multi-attendee support.
    /// Returns the first non-organizer attendee's TeamMemberId, or null.
    /// </summary>
    public Guid? TeamMemberId => 
        Attendees.FirstOrDefault(a => a.Role != "organizer")?.TeamMemberId;

    /// <summary>
    /// LEGACY: Alias for Description - old code used Notes.
    /// </summary>
    public string? Notes
    {
        get => Description;
        set => Description = value;
    }

    #endregion

    #region Non-DB Properties (set by service)

    /// <summary>
    /// Name of the team member this meeting is with (set by service).
    /// </summary>
    public string? TeamMemberName { get; set; }

    /// <summary>
    /// Attendee names (for display).
    /// </summary>
    public List<MeetingAttendee> Attendees { get; set; } = new();

    /// <summary>
    /// Agenda items for this meeting.
    /// </summary>
    public List<MeetingAgendaItem> AgendaItems { get; set; } = new();

    /// <summary>
    /// All prep items for this meeting (populated by service, then filtered into groups).
    /// </summary>
    public List<MeetingPrepItem> PrepItems { get; set; } = new();

    /// <summary>
    /// The current user's team member ID (set by ViewModel for ownership checks).
    /// </summary>
    public Guid? CurrentUserTeamMemberId { get; set; }

    /// <summary>
    /// Whether the current user is the creator/owner of this meeting.
    /// </summary>
    public bool IsOwnedByCurrentUser => 
        CurrentUserTeamMemberId.HasValue && 
        CurrentUserTeamMemberId.Value == CreatedByTeamMemberId;

    /// <summary>
    /// Whether the meeting has agenda items.
    /// </summary>
    public bool HasAgendaItems => AgendaItems.Count > 0;

    #region Personal Prep Properties (for Me view)

    /// <summary>
    /// My agenda items - items I added (AddedBy == me).
    /// </summary>
    public List<MeetingAgendaItem> MyAgendaItems =>
        CurrentUserTeamMemberId.HasValue
            ? AgendaItems.Where(a => a.AddedBy == CurrentUserTeamMemberId.Value).ToList()
            : new List<MeetingAgendaItem>();

    /// <summary>
    /// Team agenda items - items added by others.
    /// </summary>
    public List<MeetingAgendaItem> TeamAgendaItems =>
        CurrentUserTeamMemberId.HasValue
            ? AgendaItems.Where(a => a.AddedBy != CurrentUserTeamMemberId.Value).ToList()
            : AgendaItems;

    /// <summary>
    /// Count of my agenda items.
    /// </summary>
    public int MyAgendaCount => MyAgendaItems.Count;

    /// <summary>
    /// Count of team agenda items.
    /// </summary>
    public int TeamAgendaCount => TeamAgendaItems.Count;

    /// <summary>
    /// My private notes for this meeting (filtered from MeetingNotes).
    /// Note: This collection must be populated by the service.
    /// </summary>
    public List<MeetingNote> MyNotes { get; set; } = new();

    /// <summary>
    /// Count of my private notes.
    /// </summary>
    public int MyNotesCount => MyNotes.Count;

    /// <summary>
    /// Shared notes for this meeting (non-private notes from all attendees).
    /// Note: This collection must be populated by the service.
    /// </summary>
    public List<MeetingNote> SharedNotes { get; set; } = new();

    /// <summary>
    /// Count of shared notes.
    /// </summary>
    public int SharedNotesCount => SharedNotes.Count;

    /// <summary>
    /// My follow-up tasks for this meeting (assigned to me, linked to this meeting).
    /// Note: This collection must be populated by the service.
    /// </summary>
    public List<TaskDetail> MyFollowUps { get; set; } = new();

    /// <summary>
    /// Team follow-ups created from this meeting (assigned to others).
    /// Note: This collection must be populated by the service.
    /// </summary>
    public List<TaskDetail> TeamFollowUps { get; set; } = new();

    /// <summary>
    /// Count of my open follow-up tasks.
    /// </summary>
    public int MyFollowUpsOpenCount => MyFollowUps.Count(t => !t.IsCompleted);

    /// <summary>
    /// Count of team follow-ups created from this meeting.
    /// </summary>
    public int TeamFollowUpsCount => TeamFollowUps.Count;

    /// <summary>
    /// Prep state for Me view - derived from personal items.
    /// PrepNotStarted: No personal prep items created.
    /// PrepInProgress: Has at least one prep item, note, or follow-up.
    /// </summary>
    public string PrepState =>
        (MyAgendaCount == 0 && MyNotesCount == 0 && MyFollowUpsOpenCount == 0)
            ? "PrepNotStarted"
            : "PrepInProgress";

    /// <summary>
    /// Human-friendly prep state display.
    /// </summary>
    public string PrepStateDisplay => PrepState switch
    {
        "PrepNotStarted" => "No prep yet",
        "PrepInProgress" => "Prep in progress",
        _ => ""
    };

    /// <summary>
    /// Icon for prep state indicator.
    /// </summary>
    public string PrepStateIcon => PrepState switch
    {
        "PrepNotStarted" => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z", // Empty circle
        "PrepInProgress" => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20M16.24,7.76L10.5,13.5L7.76,10.76L6.34,12.17L10.5,16.34L17.66,9.17L16.24,7.76Z", // Checkmark circle
        _ => ""
    };

    /// <summary>
    /// Color for prep state indicator.
    /// </summary>
    public string PrepStateColor => PrepState switch
    {
        "PrepNotStarted" => "#9CA3AF", // Gray - not started
        "PrepInProgress" => "#10B981", // Green - in progress
        _ => "#D1D5DB"
    };

    /// <summary>
    /// Whether prep has been started (for UI visibility).
    /// </summary>
    public bool HasPrepStarted => PrepState == "PrepInProgress";

    /// <summary>
    /// Whether the current user is an organizer of this meeting.
    /// </summary>
    public bool IsCurrentUserOrganizer =>
        CurrentUserTeamMemberId.HasValue &&
        Attendees.Any(a => a.TeamMemberId == CurrentUserTeamMemberId.Value && a.IsOrganizer);

    /// <summary>
    /// Current user's role in this meeting.
    /// </summary>
    public string MyRoleDisplay =>
        IsCurrentUserOrganizer ? "Organizer" : "Attendee";

    #region Prep Item Groups (for Me view Prep tab)

    /// <summary>
    /// My Prep - personal prep items I created for myself (visibility='personal', unassigned, requested_by=me).
    /// </summary>
    public List<MeetingPrepItem> MyPrepItems =>
        CurrentUserTeamMemberId.HasValue
            ? PrepItems.Where(p => 
                p.VisibilityScope == "personal" && 
                !p.AssignedToTeamMemberId.HasValue && 
                p.RequestedByTeamMemberId == CurrentUserTeamMemberId.Value).ToList()
            : new List<MeetingPrepItem>();

    /// <summary>
    /// Prep Assigned To Me - prep items someone else assigned to me (visibility='assigned', assigned_to=me).
    /// </summary>
    public List<MeetingPrepItem> PrepAssignedToMe =>
        CurrentUserTeamMemberId.HasValue
            ? PrepItems.Where(p => 
                p.VisibilityScope == "assigned" && 
                p.AssignedToTeamMemberId == CurrentUserTeamMemberId.Value).ToList()
            : new List<MeetingPrepItem>();

    /// <summary>
    /// Prep I Assigned - prep items I assigned to others (visibility='assigned', requested_by=me, assigned_to is not null).
    /// </summary>
    public List<MeetingPrepItem> PrepIAssigned =>
        CurrentUserTeamMemberId.HasValue
            ? PrepItems.Where(p => 
                p.VisibilityScope == "assigned" && 
                p.RequestedByTeamMemberId == CurrentUserTeamMemberId.Value &&
                p.AssignedToTeamMemberId.HasValue &&
                p.AssignedToTeamMemberId != CurrentUserTeamMemberId).ToList()
            : new List<MeetingPrepItem>();

    /// <summary>
    /// Team Prep (Everyone) - shared prep for all attendees (visibility='meeting', unassigned).
    /// </summary>
    public List<MeetingPrepItem> TeamPrepItems =>
        PrepItems.Where(p => 
            p.VisibilityScope == "meeting" && 
            !p.AssignedToTeamMemberId.HasValue).ToList();

    /// <summary>
    /// Count of my personal prep items.
    /// </summary>
    public int MyPrepCount => MyPrepItems.Count;

    /// <summary>
    /// Count of prep items assigned to me.
    /// </summary>
    public int PrepAssignedToMeCount => PrepAssignedToMe.Count;

    /// <summary>
    /// Count of prep items I assigned to others.
    /// </summary>
    public int PrepIAssignedCount => PrepIAssigned.Count;

    /// <summary>
    /// Count of team prep items.
    /// </summary>
    public int TeamPrepCount => TeamPrepItems.Count;

    /// <summary>
    /// Total open prep items for current user (personal + assigned to me).
    /// </summary>
    public int TotalOpenPrepCount => 
        MyPrepItems.Count(p => p.Status != "done") + 
        PrepAssignedToMe.Count(p => p.Status != "done");

    /// <summary>
    /// Whether user has any prep items (for empty state).
    /// </summary>
    public bool HasAnyPrepItems => PrepItems.Count > 0;

    #endregion

    #endregion

    #endregion

    #region Computed Properties

    /// <summary>
    /// ScheduledAt converted to local time. Handles case where Kind is Unspecified
    /// by treating it as UTC (Supabase stores timestamps in UTC).
    /// </summary>
    public DateTime? ScheduledAtLocal
    {
        get
        {
            if (!ScheduledAt.HasValue) return null;
            var dt = ScheduledAt.Value;
            // If Kind is Unspecified, treat as UTC since Supabase stores in UTC
            if (dt.Kind == DateTimeKind.Unspecified)
            {
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return dt.ToLocalTime();
        }
    }

    /// <summary>
    /// Local date of the meeting for filtering/grouping.
    /// </summary>
    public DateTime? LocalDate => ScheduledAtLocal?.Date;

    /// <summary>
    /// Whether this is a 1:1 meeting.
    /// </summary>
    public bool IsOneOnOne => MeetingType?.ToLower() == "one_on_one" || MeetingType?.ToLower() == "1:1";

    /// <summary>
    /// Meeting type display text.
    /// </summary>
    public string TypeDisplay => MeetingType?.ToLower() switch
    {
        "one_on_one" or "1:1" => "1:1",
        "team" => "Team",
        "project" => "Project",
        "standup" => "Standup",
        "review" => "Review",
        _ => "Meeting"
    };

    /// <summary>
    /// Type color for badges.
    /// </summary>
    public string TypeColor => MeetingType?.ToLower() switch
    {
        "one_on_one" or "1:1" => "#3B82F6", // Blue
        "team" => "#10B981", // Green
        "standup" => "#8B5CF6", // Purple
        "review" => "#F59E0B", // Amber
        _ => "#6B7280" // Gray
    };

    /// <summary>
    /// Friendly scheduled time text.
    /// </summary>
    public string ScheduledText
    {
        get
        {
            if (!ScheduledAtLocal.HasValue)
                return "Not scheduled";

            var now = DateTime.Now;
            var scheduled = ScheduledAtLocal.Value;

            if (scheduled.Date == now.Date)
                return $"Today at {scheduled:h:mm tt}";
            if (scheduled.Date == now.Date.AddDays(1))
                return $"Tomorrow at {scheduled:h:mm tt}";
            if (scheduled.Date < now.Date.AddDays(7))
                return $"{scheduled:dddd} at {scheduled:h:mm tt}";
            return scheduled.ToString("MMM d at h:mm tt");
        }
    }

    /// <summary>
    /// Start time display (e.g. "9:00 AM").
    /// </summary>
    public string StartTimeDisplay => ScheduledAtLocal?.ToString("h:mm tt") ?? "";

    /// <summary>
    /// End time display.
    /// </summary>
    public string EndTimeDisplay => ScheduledAtLocal?.AddMinutes(DurationMinutes ?? 30).ToString("h:mm tt") ?? "";

    /// <summary>
    /// Time range display (e.g. "9:00 AM - 10:00 AM").
    /// </summary>
    public string TimeRangeDisplay => $"{StartTimeDisplay} - {EndTimeDisplay}";

    /// <summary>
    /// Duration text.
    /// </summary>
    public string DurationText
    {
        get
        {
            var mins = DurationMinutes ?? 30;
            return mins switch
            {
                < 60 => $"{mins} min",
                60 => "1 hour",
                _ => mins % 60 == 0
                    ? $"{mins / 60} hours"
                    : $"{mins / 60}h {mins % 60}m"
            };
        }
    }

    /// <summary>
    /// Date display for list grouping.
    /// </summary>
    public string DateGroupDisplay
    {
        get
        {
            if (!ScheduledAtLocal.HasValue)
                return "Unscheduled";

            var now = DateTime.Now.Date;
            var scheduled = ScheduledAtLocal.Value.Date;

            if (scheduled == now)
                return "Today";
            if (scheduled == now.AddDays(1))
                return "Tomorrow";
            if (scheduled >= now && scheduled < now.AddDays(7))
                return scheduled.ToString("dddd, MMM d");
            return scheduled.ToString("MMMM d, yyyy");
        }
    }

    /// <summary>
    /// Short date for calendar month view.
    /// </summary>
    public string ShortTimeDisplay => ScheduledAtLocal?.ToString("h:mm tt") ?? "";

    /// <summary>
    /// Hour of day (0-23) for positioning in day/week view.
    /// </summary>
    public int StartHour => ScheduledAtLocal?.Hour ?? 0;

    /// <summary>
    /// Minutes past the hour (0-59).
    /// </summary>
    public int StartMinute => ScheduledAtLocal?.Minute ?? 0;

    /// <summary>
    /// Start hour for calendar display (5 AM = hour 5).
    /// This must match CalendarHours starting hour in CircleViewModel.
    /// </summary>
    private const int CalendarStartHour = 5;

    /// <summary>
    /// Top offset for day/week calendar view (pixels from top of calendar).
    /// Assuming 60px per hour. Offset from CalendarStartHour (8 AM).
    /// Returns 0 for meetings before the calendar start hour.
    /// </summary>
    public double CalendarTopOffset => Math.Max(0, ((StartHour - CalendarStartHour) * 60) + StartMinute);

    /// <summary>
    /// Height in day/week calendar based on duration.
    /// </summary>
    public double CalendarHeight => Math.Max(DurationMinutes ?? 30, 15); // Minimum 15px

    /// <summary>
    /// Day of week index (0=Sunday, 6=Saturday).
    /// </summary>
    public int DayOfWeekIndex => ScheduledAtLocal.HasValue ? (int)ScheduledAtLocal.Value.DayOfWeek : 0;

    /// <summary>
    /// Whether meeting has a video link.
    /// </summary>
    public bool HasVideoLink => !string.IsNullOrWhiteSpace(VideoLink);

    /// <summary>
    /// Whether meeting has location.
    /// </summary>
    public bool HasLocation => !string.IsNullOrWhiteSpace(Location);

    /// <summary>
    /// Whether this meeting is in the past (end time has passed).
    /// </summary>
    public bool IsPast
    {
        get
        {
            if (!ScheduledAtLocal.HasValue) return false;
            var endTime = ScheduledAtLocal.Value.AddMinutes(DurationMinutes ?? 30);
            return DateTime.Now > endTime;
        }
    }

    /// <summary>
    /// Whether this meeting is in the future.
    /// </summary>
    public bool IsFuture => !IsPast;

    /// <summary>
    /// Whether this meeting starts within the next 2 hours.
    /// </summary>
    public bool IsSoon
    {
        get
        {
            if (!ScheduledAtLocal.HasValue || IsPast) return false;
            var hoursUntilStart = (ScheduledAtLocal.Value - DateTime.Now).TotalHours;
            return hoursUntilStart <= 2 && hoursUntilStart > 0;
        }
    }

    /// <summary>
    /// Attendee count display.
    /// </summary>
    public string AttendeeCountDisplay => Attendees.Count switch
    {
        0 => "No attendees",
        1 => "1 attendee",
        _ => $"{Attendees.Count} attendees"
    };

    /// <summary>
    /// Alias for TypeDisplay for XAML binding compatibility.
    /// </summary>
    public string MeetingTypeName => TypeDisplay;

    /// <summary>
    /// Cadence/frequency display (stub - will be enhanced when cadence is tracked).
    /// </summary>
    public string? CadenceDisplay => null; // Currently meetings don't track cadence

    #endregion
}

/// <summary>
/// Meeting attendee - maps to procohere.meeting_attendees table in Supabase.
/// CRITICAL: When creating a meeting, the creator MUST be inserted as an attendee
/// with role='organizer' immediately after, or RLS will prevent them from seeing it.
/// Treat (meeting_id, team_member_id) as unique - upsert accordingly.
/// </summary>
[Table("meeting_attendees")]
public class MeetingAttendee : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    [Column("role")]
    public string Role { get; set; } = "attendee";

    [Column("response_status")]
    public string ResponseStatus { get; set; } = "pending";

    [Column("attended")]
    public bool? Attended { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #region Non-DB Properties (set by service)

    /// <summary>
    /// Team member name (set by service after join with team_members).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Team member email (set by service).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Avatar URL for the attendee (set by service from team member).
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Initials for avatar display.
    /// </summary>
    public string Initials => string.Join("", Name.Split(' ').Where(p => p.Length > 0).Take(2).Select(p => p[0])).ToUpper();

    /// <summary>
    /// Whether this attendee is the organizer.
    /// </summary>
    public bool IsOrganizer => Role?.ToLower() == "organizer";

    /// <summary>
    /// Display text for role badge. Returns empty for regular attendees.
    /// Shows: Organizer, Optional (nothing for regular attendee).
    /// </summary>
    public string RoleDisplay => Role?.ToLower() switch
    {
        "organizer" => "Organizer",
        "optional" => "Optional",
        _ => string.Empty
    };

    /// <summary>
    /// Whether to show the role badge (only for non-standard attendees).
    /// </summary>
    public bool ShowRoleBadge => !string.IsNullOrEmpty(RoleDisplay);

    #endregion
}

/// <summary>
/// Meeting agenda item - maps to the meeting_agenda_items table in Supabase.
/// </summary>
[Table("meeting_agenda_items")]
public class MeetingAgendaItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("added_by")]
    public Guid AddedBy { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Agenda item status: 'open', 'discussed', 'action_created', 'deferred', 'dropped'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "open";

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_private")]
    public bool IsPrivate { get; set; }

    [Column("is_completed")]
    public bool IsCompleted { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("linked_entity_type")]
    public string? LinkedEntityType { get; set; }

    [Column("linked_entity_id")]
    public Guid? LinkedEntityId { get; set; }

    #region Carry-Forward Properties

    /// <summary>
    /// Person this carry-forward is anchored to. Required when status=deferred.
    /// </summary>
    [Column("anchor_team_member_id")]
    public Guid? AnchorTeamMemberId { get; set; }

    /// <summary>
    /// Lifecycle state for carried-forward items: pending, surfaced, resolved, converted, expired.
    /// </summary>
    [Column("carry_forward_state")]
    public string? CarryForwardState { get; set; }

    /// <summary>
    /// When this carry-forward expires (30 days from deferral or after 2 meetings).
    /// </summary>
    [Column("carry_forward_expires_at")]
    public DateTime? CarryForwardExpiresAt { get; set; }

    /// <summary>
    /// Number of meeting opportunities since deferral. Expires at 2.
    /// </summary>
    [Column("carry_forward_meeting_count")]
    public int CarryForwardMeetingCount { get; set; }

    /// <summary>
    /// If this item was carried forward, points to the original agenda item.
    /// </summary>
    [Column("source_agenda_item_id")]
    public Guid? SourceAgendaItemId { get; set; }

    #endregion

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region Computed Properties

    /// <summary>
    /// Display text for the status.
    /// </summary>
    public string StatusDisplay => Status?.ToLower() switch
    {
        "open" => "Open",
        "discussed" => "Discussed",
        "action_created" => "Action Created",
        "deferred" => "Deferred",
        "dropped" => "Dropped",
        _ => "Open"
    };

    /// <summary>
    /// Status color for badges.
    /// </summary>
    public string StatusColor => Status?.ToLower() switch
    {
        "open" => "#6B7280",        // Gray
        "discussed" => "#3B82F6",    // Blue
        "action_created" => "#10B981", // Green
        "deferred" => "#F59E0B",     // Amber
        "dropped" => "#EF4444",      // Red
        _ => "#6B7280"               // Gray
    };

    /// <summary>
    /// Whether this agenda item has a linked entity (task, goal, etc.).
    /// </summary>
    public bool HasLinkedEntity => !string.IsNullOrEmpty(LinkedEntityType) && LinkedEntityId.HasValue;

    /// <summary>
    /// Display text for the linked entity type.
    /// </summary>
    public string LinkedEntityTypeDisplay => LinkedEntityType?.ToLower() switch
    {
        "task" => "Task",
        "goal" => "Goal",
        "metric" => "Metric",
        "project" => "Project",
        _ => ""
    };

    /// <summary>
    /// Title of the linked entity (populated from join or lookup).
    /// Not persisted - computed at runtime.
    /// </summary>
    public string? LinkedEntityTitle { get; set; }

    /// <summary>
    /// Icon for the linked entity type (SVG path data).
    /// </summary>
    public string LinkedEntityIcon => LinkedEntityType?.ToLower() switch
    {
        "task" => "M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M10,17L5,12L6.41,10.59L10,14.17L17.59,6.58L19,8L10,17Z", // Checkbox
        "goal" => "M12,2C6.47,2 2,6.47 2,12C2,17.53 6.47,22 12,22C17.53,22 22,17.53 22,12C22,6.47 17.53,2 12,2M12,20C7.58,20 4,16.42 4,12C4,7.58 7.58,4 12,4C16.42,4 20,7.58 20,12C20,16.42 16.42,20 12,20M15,12A3,3 0 0,1 12,15A3,3 0 0,1 9,12A3,3 0 0,1 12,9A3,3 0 0,1 15,12Z", // Target
        "metric" => "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z", // Chart
        "project" => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z", // Folder
        _ => ""
    };

    /// <summary>
    /// Full linkage display text (e.g., "→ Task: Fix auth bug").
    /// </summary>
    public string LinkedEntityDisplay => HasLinkedEntity && !string.IsNullOrEmpty(LinkedEntityTitle)
        ? $"→ {LinkedEntityTypeDisplay}: \"{LinkedEntityTitle}\""
        : HasLinkedEntity
        ? $"→ Linked {LinkedEntityTypeDisplay}"
        : string.Empty;

    /// <summary>
    /// Whether this is a deferred/carry-forward item.
    /// </summary>
    public bool IsCarryForward => Status?.ToLower() == "deferred" || !string.IsNullOrEmpty(CarryForwardState);

    /// <summary>
    /// Whether this item was carried forward from another meeting.
    /// </summary>
    public bool IsCarriedForward => SourceAgendaItemId.HasValue;

    /// <summary>
    /// Display text for the carry-forward state.
    /// </summary>
    public string CarryForwardStateDisplay => Models.CarryForwardState.GetDisplayName(CarryForwardState);

    /// <summary>
    /// Color for the carry-forward state badge.
    /// </summary>
    public string CarryForwardStateColor => Models.CarryForwardState.GetColor(CarryForwardState);

    /// <summary>
    /// Whether this carry-forward item is expired (past expiration date or 2+ meetings).
    /// </summary>
    public bool IsExpired => CarryForwardState == Models.CarryForwardState.Expired ||
                             (CarryForwardExpiresAt.HasValue && DateTime.UtcNow > CarryForwardExpiresAt.Value) ||
                             CarryForwardMeetingCount >= 2;

    /// <summary>
    /// Days remaining until expiration, or null if not applicable.
    /// </summary>
    public int? DaysUntilExpiration => CarryForwardExpiresAt.HasValue
        ? Math.Max(0, (int)(CarryForwardExpiresAt.Value - DateTime.UtcNow).TotalDays)
        : null;

    /// <summary>
    /// Expiration display text (e.g., "Expires in 5 days" or "2 meetings").
    /// </summary>
    public string ExpirationDisplay
    {
        get
        {
            if (CarryForwardMeetingCount >= 2)
                return "Meeting limit reached";
            if (CarryForwardExpiresAt.HasValue)
            {
                var days = DaysUntilExpiration ?? 0;
                if (days <= 0) return "Expired";
                if (days == 1) return "Expires tomorrow";
                return $"Expires in {days} days";
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Formatted creation date for display.
    /// </summary>
    public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("MMM d, yyyy");

    /// <summary>
    /// Short time since creation.
    /// </summary>
    public string CreatedAtShort => CreatedAt.ToLocalTime().ToString("MMM d");

    #endregion
}
