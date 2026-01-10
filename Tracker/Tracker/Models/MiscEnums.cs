namespace Tracker.Models;

/// <summary>
/// Category of a note.
/// Maps to Supabase note_category enum.
/// </summary>
public enum NoteCategory
{
    General,
    Meeting,
    Goal,
    Metric,
    Task,
    TeamMember,
    Project,
    Idea,
    FollowUp
}

/// <summary>
/// Content format for notes.
/// </summary>
public enum ContentFormat
{
    Plain,
    Markdown,
    Html
}

/// <summary>
/// Priority of a notification.
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// Frequency for email digests.
/// </summary>
public enum EmailFrequency
{
    Immediate,
    Daily,
    Weekly,
    Never
}

/// <summary>
/// Calendar provider.
/// Maps to Supabase calendar_provider enum.
/// </summary>
public enum CalendarProvider
{
    Google,
    Microsoft,
    Apple,
    Other
}

/// <summary>
/// Status of calendar sync.
/// Maps to Supabase calendar_sync_status enum.
/// </summary>
public enum CalendarSyncStatus
{
    Pending,
    Synced,
    Failed,
    Cancelled
}

/// <summary>
/// Type of reminder.
/// Maps to Supabase reminder_type enum.
/// </summary>
public enum ReminderType
{
    Meeting,
    Task,
    Goal,
    Review,
    Survey,
    Feedback,
    OneOnOnePrep,
    Custom
}

/// <summary>
/// Status of a reminder.
/// Maps to Supabase reminder_status enum.
/// </summary>
public enum ReminderStatus
{
    Scheduled,
    Sent,
    Dismissed,
    Snoozed,
    Cancelled
}
