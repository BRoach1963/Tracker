using System;
using System.Collections.Generic;
using System.Linq;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Meeting model - maps to the meetings table in Supabase.
/// Used for dashboard upcoming meetings.
/// </summary>
[Table("meetings")]
public class MeetingDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("meeting_type")]
    public string MeetingType { get; set; } = "one_on_one";

    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; } = 30;

    [Column("location")]
    public string? Location { get; set; }

    [Column("video_link")]
    public string? VideoLink { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("team_member_id")]
    public Guid? TeamMemberId { get; set; }

    [Column("created_by")]
    public Guid? CreatedByTeamMemberId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

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
    /// The current user's team member ID (set by ViewModel for ownership checks).
    /// </summary>
    public Guid? CurrentUserTeamMemberId { get; set; }

    /// <summary>
    /// Whether the current user is the creator/owner of this meeting.
    /// </summary>
    public bool IsOwnedByCurrentUser => 
        CurrentUserTeamMemberId.HasValue && 
        CreatedByTeamMemberId.HasValue && 
        CurrentUserTeamMemberId.Value == CreatedByTeamMemberId.Value;

    /// <summary>
    /// Whether the meeting has agenda items.
    /// </summary>
    public bool HasAgendaItems => AgendaItems.Count > 0;

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
/// Meeting attendee - maps to the meeting_attendees table in Supabase.
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
