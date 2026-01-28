namespace ProCohere.Avalonia.Models;

/// <summary>
/// User preferences for the reminder system.
/// Stored locally via LocalSettingsService.
/// </summary>
public class ReminderSettings
{
    #region Master Toggle

    /// <summary>
    /// Master on/off switch for all reminders.
    /// </summary>
    public bool EnableReminders { get; set; } = true;

    #endregion

    #region Meeting Reminders

    /// <summary>
    /// Show reminders for upcoming meetings.
    /// </summary>
    public bool ShowMeetingReminders { get; set; } = true;

    /// <summary>
    /// Minutes before a meeting to show the reminder.
    /// </summary>
    public int MeetingReminderMinutes { get; set; } = 15;

    /// <summary>
    /// Show a day-before warning for meetings.
    /// </summary>
    public bool ShowDayBeforeWarning { get; set; } = false;

    #endregion

    #region Task Reminders

    /// <summary>
    /// Show reminders for task deadlines.
    /// </summary>
    public bool ShowTaskReminders { get; set; } = true;

    /// <summary>
    /// Days before a task deadline to show the reminder.
    /// </summary>
    public int TaskReminderDays { get; set; } = 1;

    #endregion

    #region Goal Reminders

    /// <summary>
    /// Show reminders for goal deadlines.
    /// </summary>
    public bool ShowGoalReminders { get; set; } = true;

    /// <summary>
    /// Days before a goal deadline to show the reminder.
    /// </summary>
    public int GoalReminderDays { get; set; } = 7;

    #endregion

    #region Engagement Alerts

    /// <summary>
    /// Show alerts when team members haven't had a 1:1 recently.
    /// </summary>
    public bool ShowEngagementAlerts { get; set; } = true;

    /// <summary>
    /// Weeks without a 1:1 before showing an engagement alert.
    /// </summary>
    public int EngagementAlertWeeks { get; set; } = 2;

    #endregion

    #region Notification Preferences

    /// <summary>
    /// Play a sound with notifications.
    /// </summary>
    public bool PlaySound { get; set; } = true;

    /// <summary>
    /// Default snooze duration in minutes.
    /// </summary>
    public int DefaultSnoozeDurationMinutes { get; set; } = 15;

    #endregion

    #region Factory

    /// <summary>
    /// Gets the default reminder settings.
    /// </summary>
    public static ReminderSettings Default => new();

    #endregion
}
