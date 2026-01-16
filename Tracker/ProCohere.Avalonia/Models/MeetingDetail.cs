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
    public int DurationMinutes { get; set; } = 30;

    [Column("location")]
    public string? Location { get; set; }

    [Column("video_link")]
    public string? VideoLink { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("team_member_id")]
    public Guid? TeamMemberId { get; set; }

    [Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

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

    #endregion

    #region Computed Properties

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
            if (!ScheduledAt.HasValue)
                return "Not scheduled";

            var now = DateTime.Now;
            var scheduled = ScheduledAt.Value.ToLocalTime();

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
    public string StartTimeDisplay => ScheduledAt?.ToLocalTime().ToString("h:mm tt") ?? "";

    /// <summary>
    /// End time display.
    /// </summary>
    public string EndTimeDisplay => ScheduledAt?.ToLocalTime().AddMinutes(DurationMinutes).ToString("h:mm tt") ?? "";

    /// <summary>
    /// Time range display (e.g. "9:00 AM - 10:00 AM").
    /// </summary>
    public string TimeRangeDisplay => $"{StartTimeDisplay} - {EndTimeDisplay}";

    /// <summary>
    /// Duration text.
    /// </summary>
    public string DurationText => DurationMinutes switch
    {
        < 60 => $"{DurationMinutes} min",
        60 => "1 hour",
        _ => DurationMinutes % 60 == 0 
            ? $"{DurationMinutes / 60} hours" 
            : $"{DurationMinutes / 60}h {DurationMinutes % 60}m"
    };

    /// <summary>
    /// Date display for list grouping.
    /// </summary>
    public string DateGroupDisplay
    {
        get
        {
            if (!ScheduledAt.HasValue)
                return "Unscheduled";

            var now = DateTime.Now.Date;
            var scheduled = ScheduledAt.Value.ToLocalTime().Date;

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
    public string ShortTimeDisplay => ScheduledAt?.ToLocalTime().ToString("h:mm tt") ?? "";

    /// <summary>
    /// Hour of day (0-23) for positioning in day/week view.
    /// </summary>
    public int StartHour => ScheduledAt?.ToLocalTime().Hour ?? 0;

    /// <summary>
    /// Minutes past the hour (0-59).
    /// </summary>
    public int StartMinute => ScheduledAt?.ToLocalTime().Minute ?? 0;

    /// <summary>
    /// Top offset for day/week calendar view (pixels from top of hour block).
    /// Assuming 60px per hour.
    /// </summary>
    public double CalendarTopOffset => (StartHour * 60) + StartMinute;

    /// <summary>
    /// Height in day/week calendar based on duration.
    /// </summary>
    public double CalendarHeight => Math.Max(DurationMinutes, 15); // Minimum 15px

    /// <summary>
    /// Day of week index (0=Sunday, 6=Saturday).
    /// </summary>
    public int DayOfWeekIndex => ScheduledAt.HasValue ? (int)ScheduledAt.Value.ToLocalTime().DayOfWeek : 0;

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

    #endregion
}

/// <summary>
/// Meeting attendee for display.
/// </summary>
public class MeetingAttendee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Initials => string.Join("", Name.Split(' ').Where(p => p.Length > 0).Take(2).Select(p => p[0])).ToUpper();
    public bool IsOrganizer { get; set; }
    public string ResponseStatus { get; set; } = "pending"; // accepted, declined, tentative, pending
}

/// <summary>
/// Meeting agenda item.
/// </summary>
public class MeetingAgendaItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }
}
