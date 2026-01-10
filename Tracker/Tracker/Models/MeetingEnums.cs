namespace Tracker.Models;

/// <summary>
/// Type of meeting.
/// Maps to Supabase meeting_type enum.
/// </summary>
public enum MeetingType
{
    OneOnOne,
    TeamMeeting,
    AllHands,
    Project,
    Interview,
    Other
}

/// <summary>
/// Status of a meeting.
/// Maps to Supabase meeting_status enum.
/// </summary>
public enum MeetingStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    Rescheduled
}

/// <summary>
/// Response status for a meeting attendee.
/// </summary>
public enum AttendeeResponse
{
    Pending,
    Accepted,
    Declined,
    Tentative
}
