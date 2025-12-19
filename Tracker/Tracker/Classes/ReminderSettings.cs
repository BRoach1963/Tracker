namespace Tracker.Classes
{
    /// <summary>
    /// User preferences for reminders and notifications.
    /// </summary>
    public class ReminderSettings
    {
        /// <summary>
        /// Whether reminders are enabled at all.
        /// </summary>
        public bool EnableReminders { get; set; } = true;

        /// <summary>
        /// Start Tracker minimized to system tray on Windows login.
        /// </summary>
        public bool StartWithWindows { get; set; } = false;

        /// <summary>
        /// Minimize to system tray instead of closing.
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>
        /// Show meeting reminders.
        /// </summary>
        public bool ShowMeetingReminders { get; set; } = true;

        /// <summary>
        /// Minutes before meeting to show reminder.
        /// </summary>
        public int MeetingReminderMinutes { get; set; } = 15;

        /// <summary>
        /// Also show a reminder 1 day before meetings.
        /// </summary>
        public bool ShowMeetingReminderDayBefore { get; set; } = false;

        /// <summary>
        /// Show engagement alerts when team member hasn't had 1:1.
        /// </summary>
        public bool ShowEngagementAlerts { get; set; } = true;

        /// <summary>
        /// Weeks without 1:1 before showing engagement alert.
        /// </summary>
        public int EngagementAlertWeeks { get; set; } = 2;

        /// <summary>
        /// Show task deadline reminders.
        /// </summary>
        public bool ShowTaskReminders { get; set; } = true;

        /// <summary>
        /// Days before task due date to show reminder.
        /// </summary>
        public int TaskReminderDays { get; set; } = 1;

        /// <summary>
        /// Show goal deadline reminders.
        /// </summary>
        public bool ShowGoalReminders { get; set; } = true;

        /// <summary>
        /// Days before goal target date to show reminder.
        /// </summary>
        public int GoalReminderDays { get; set; } = 7;

        /// <summary>
        /// Play sound with notifications.
        /// </summary>
        public bool PlaySound { get; set; } = true;

        /// <summary>
        /// Default snooze duration in minutes.
        /// </summary>
        public int DefaultSnoozeMins { get; set; } = 15;
    }
}

