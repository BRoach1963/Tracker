using System.Windows;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services
{
    /// <summary>
    /// Background service that monitors and triggers reminders.
    /// Runs on a timer and shows toast notifications for due reminders.
    /// </summary>
    public class ReminderService : IReminderService
    {
        #region Singleton

        private static readonly Lazy<ReminderService> _lazyInstance = 
            new(() => new ReminderService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of ReminderService.
        /// </summary>
        public static ReminderService Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private Timer? _reminderTimer;
        private Timer? _engagementTimer;
        private bool _isRunning;
        private bool _disposed;
        private ReminderSettings _settings;

        // Check interval in milliseconds (60 seconds)
        private const int CHECK_INTERVAL_MS = 60000;
        
        // Engagement check interval (4 hours)
        private const int ENGAGEMENT_CHECK_INTERVAL_MS = 4 * 60 * 60 * 1000;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a reminder is triggered.
        /// </summary>
        public event EventHandler<Reminder>? ReminderTriggered;

        /// <summary>
        /// Fired when engagement alerts are generated.
        /// </summary>
        public event EventHandler<List<TeamMember>>? EngagementAlertTriggered;

        #endregion

        #region Constructor

        private ReminderService()
        {
            _logger = LoggingManager.GetComponentLogger("ReminderService");
            _settings = UserSettingsManager.Instance.ReminderSettings ?? new ReminderSettings();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the reminder service.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _logger.Info("Starting ReminderService");
            _isRunning = true;

            // Reload settings
            _settings = UserSettingsManager.Instance.ReminderSettings ?? new ReminderSettings();

            if (!_settings.EnableReminders)
            {
                _logger.Info("Reminders are disabled in settings");
                return;
            }

            // Start the reminder check timer
            _reminderTimer = new Timer(
                CheckRemindersCallback,
                null,
                TimeSpan.FromSeconds(10), // Initial delay
                TimeSpan.FromMilliseconds(CHECK_INTERVAL_MS)
            );

            // Start the engagement check timer (less frequent)
            if (_settings.ShowEngagementAlerts)
            {
                _engagementTimer = new Timer(
                    CheckEngagementCallback,
                    null,
                    TimeSpan.FromMinutes(5), // Initial delay
                    TimeSpan.FromMilliseconds(ENGAGEMENT_CHECK_INTERVAL_MS)
                );
            }

            _logger.Info("ReminderService started");
        }

        /// <summary>
        /// Stops the reminder service.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _logger.Info("Stopping ReminderService");
            _isRunning = false;

            _reminderTimer?.Dispose();
            _reminderTimer = null;

            _engagementTimer?.Dispose();
            _engagementTimer = null;

            _logger.Info("ReminderService stopped");
        }

        /// <summary>
        /// Reloads settings and restarts if needed.
        /// </summary>
        public void ReloadSettings()
        {
            _settings = UserSettingsManager.Instance.ReminderSettings ?? new ReminderSettings();
            
            if (_isRunning)
            {
                Stop();
                Start();
            }
        }

        /// <summary>
        /// Creates a reminder for an upcoming meeting.
        /// </summary>
        public async Task CreateMeetingReminderAsync(Meeting meeting)
        {
            if (!_settings.ShowMeetingReminders) return;

            try
            {
                var reminderTime = meeting.ScheduledAt.AddMinutes(-_settings.MeetingReminderMinutes);

                // Don't create if meeting is in the past
                if (reminderTime <= DateTime.Now) return;

                // Get meeting title based on meeting type
                string meetingTitle = meeting.Title;
                if (string.IsNullOrWhiteSpace(meetingTitle))
                {
                    // Fallback for 1:1 meetings if title is empty
                    if (meeting.Type == MeetingType.OneOnOne && meeting.Report != null)
                    {
                        meetingTitle = $"1:1 with {meeting.Report.FirstName} {meeting.Report.LastName}".Trim();
                    }
                    else
                    {
                        meetingTitle = "Upcoming Meeting";
                    }
                }

                var reminder = new Reminder
                {
                    Type = ReminderType.Meeting,
                    Status = ReminderStatus.Pending,
                    Title = $"Meeting: {meetingTitle}",
                    Message = $"Your meeting starts in {_settings.MeetingReminderMinutes} minutes",
                    RemindAt = reminderTime,
                    EntityType = "meeting",
                    EntityId = meeting.Id,
                    TeamMemberId = meeting.Type == MeetingType.OneOnOne ? meeting.ReportTeamMemberId : null
                };

                await AddReminderAsync(reminder);
                _logger.Info("Created meeting reminder for Meeting ID: {0}", meeting.Id);

                // Also create day-before reminder if enabled
                if (_settings.ShowMeetingReminderDayBefore)
                {
                    var dayBeforeTime = meeting.ScheduledAt.AddDays(-1);
                    if (dayBeforeTime > DateTime.Now)
                    {
                        var dayBeforeReminder = new Reminder
                        {
                            Type = ReminderType.Meeting,
                            Status = ReminderStatus.Pending,
                            Title = $"Upcoming: {meetingTitle}",
                            Message = $"Tomorrow at {meeting.ScheduledAt:hh\\:mm}",
                            RemindAt = dayBeforeTime,
                            EntityType = "meeting",
                            EntityId = meeting.Id,
                            TeamMemberId = meeting.Type == MeetingType.OneOnOne ? meeting.ReportTeamMemberId : null
                        };
                        await AddReminderAsync(dayBeforeReminder);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error creating meeting reminder");
            }
        }

        /// <summary>
        /// Creates a reminder for a task deadline.
        /// </summary>
        public async Task CreateTaskReminderAsync(TrackerTask task)
        {
            if (!_settings.ShowTaskReminders) return;

            try
            {
                var reminderTime = task.DueDate.Value.AddDays(-_settings.TaskReminderDays);

                // Don't create if already past
                if (reminderTime <= DateTime.Now) return;

                var reminder = new Reminder
                {
                    Type = ReminderType.Task,
                    Status = ReminderStatus.Pending,
                    Title = $"Task Due Soon: {task.Title ?? task.Description}",
                    Message = $"Due {task.DueDate:MMM dd} - Assigned to {task.Owner?.FullName ?? "Unassigned"}",
                    RemindAt = reminderTime,
                    EntityType = "task",
                    EntityId = task.Id,
                    TeamMemberId = task.OwnerTeamMemberId
                };

                await AddReminderAsync(reminder);
                _logger.Info("Created task reminder for Task ID: {0}", task.Id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error creating task reminder");
            }
        }

        /// <summary>
        /// Creates a reminder for a goal deadline.
        /// </summary>
        public async Task CreateGoalReminderAsync(DevelopmentGoal goal)
        {
            if (!_settings.ShowGoalReminders || !goal.TargetDate.HasValue) return;

            try
            {
                var reminderTime = goal.TargetDate.Value.AddDays(-_settings.GoalReminderDays);

                // Don't create if already past
                if (reminderTime <= DateTime.Now) return;

                var reminder = new Reminder
                {
                    Type = ReminderType.Goal,
                    Status = ReminderStatus.Pending,
                    Title = $"Goal Deadline Approaching",
                    Message = $"\"{goal.Title}\" - Target date: {goal.TargetDate:MMM dd}",
                    RemindAt = reminderTime,
                    EntityType = "development_goal",
                    EntityId = goal.Id,
                    TeamMemberId = goal.TeamMemberId
                };

                await AddReminderAsync(reminder);
                _logger.Info("Created goal reminder for Goal ID: {0}", goal.Id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error creating goal reminder");
            }
        }

        /// <summary>
        /// Creates a custom reminder.
        /// </summary>
        public async Task<Guid> CreateCustomReminderAsync(string title, string message, DateTime remindAt, 
            Guid? teamMemberId = null, bool isRecurring = false, string? recurrenceRule = null)
        {
            try
            {
                var reminder = new Reminder
                {
                    Type = ReminderType.Custom,
                    Status = ReminderStatus.Pending,
                    Title = title,
                    Message = message,
                    RemindAt = remindAt,
                    EntityType = "custom",
                    EntityId = Guid.NewGuid(),
                    TeamMemberId = teamMemberId,
                    IsRecurring = isRecurring,
                    RecurrenceRule = recurrenceRule
                };

                var id = await AddReminderAsync(reminder);
                _logger.Info("Created custom reminder ID: {0}", id);
                return id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error creating custom reminder");
                return Guid.Empty;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Converts a 32-bit integer ID to a deterministic Guid.
        /// Used for polymorphic entity references where int IDs need to be stored as UUIDs.
        /// </summary>
        private static Guid GuidFromInt(int id)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(id).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        private async Task<Guid> AddReminderAsync(Reminder reminder)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue || userId.Value == Guid.Empty)
            {
                _logger.Warn("AddReminderAsync called but no current user is set");
                return Guid.Empty;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var reminderRepository = new ReminderRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            return await reminderRepository.AddReminderAsync(reminder);
        }

        private async void CheckRemindersCallback(object? state)
        {
            if (!_isRunning || !_settings.EnableReminders) return;

            try
            {
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue || userId.Value == Guid.Empty)
                {
                    _logger.Warn("CheckRemindersCallback called but no current user is set");
                    return;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var reminderRepository = new ReminderRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var dueReminders = await reminderRepository.GetDueRemindersAsync();

                foreach (var reminder in dueReminders)
                {
                    // Mark as triggered first to prevent duplicate notifications
                    await reminderRepository.MarkReminderTriggeredAsync(reminder.Id);

                    // Show notification
                    ShowReminderNotification(reminder);

                    // Fire event
                    ReminderTriggered?.Invoke(this, reminder);

                    // Handle recurring reminders
                    if (reminder.IsRecurring)
                    {
                        var nextReminder = new Reminder
                        {
                            Type = reminder.Type,
                            Status = ReminderStatus.Pending,
                            Title = reminder.Title,
                            Message = reminder.Message,
                            RemindAt = reminder.RemindAt.AddDays(1), // Default: daily recurrence
                            EntityType = reminder.EntityType,
                            EntityId = reminder.EntityId,
                            TeamMemberId = reminder.TeamMemberId,
                            IsRecurring = true,
                            RecurrenceRule = reminder.RecurrenceRule
                        };
                        await reminderRepository.AddReminderAsync(nextReminder);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error in reminder check callback");
            }
        }

        private async void CheckEngagementCallback(object? state)
        {
            if (!_isRunning || !_settings.ShowEngagementAlerts) return;

            try
            {
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue || userId.Value == Guid.Empty)
                {
                    _logger.Warn("CheckEngagementCallback called but no current user is set");
                    return;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var teamMemberRepository = new TeamMemberRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var teamMembersWithoutMeeting = await teamMemberRepository
                    .GetTeamMembersWithoutRecentOneOnOneAsync(_settings.EngagementAlertWeeks);

                if (teamMembersWithoutMeeting.Count > 0)
                {
                    // Show aggregated notification
                    var message = teamMembersWithoutMeeting.Count == 1
                        ? $"{teamMembersWithoutMeeting[0].FirstName} {teamMembersWithoutMeeting[0].LastName} hasn't had a 1:1 in {_settings.EngagementAlertWeeks}+ weeks"
                        : $"{teamMembersWithoutMeeting.Count} team members haven't had a 1:1 in {_settings.EngagementAlertWeeks}+ weeks";

                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        NotificationManager.Instance.ShowWarning("Team Engagement", message);
                    });

                    EngagementAlertTriggered?.Invoke(this, teamMembersWithoutMeeting);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error in engagement check callback");
            }
        }

        private void ShowReminderNotification(Reminder reminder)
        {
            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    var icon = reminder.Type switch
                    {
                        ReminderType.Meeting => "📅",
                        ReminderType.Task => "✅",
                        ReminderType.Goal => "🎯",
                        ReminderType.Engagement => "👥",
                        _ => "🔔"
                    };

                    var title = $"{icon} {reminder.Title}";
                    
                    // Use toast notification
                    NotificationManager.Instance.ShowInfo(title, reminder.Message);

                    // Also send native toast if app is in background
                    NotificationManager.Instance.SendNativeToast(title, reminder.Message);
                });
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error showing reminder notification");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Stop();
            }

            _disposed = true;
        }

        #endregion
    }
}

