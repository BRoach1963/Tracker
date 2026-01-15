using System.Collections.Concurrent;
using System.Windows.Threading;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Helpers;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Database;
using Tracker.Services.Data.Repositories;
using Tracker.Services.Subscription;

namespace Tracker.Services.Microsoft365
{
    /// <summary>
    /// Manages bidirectional synchronization between Tracker meetings and Microsoft Calendar.
    /// Implements the "Optimistic Push + Delta Pull" strategy documented in Teams-Calendar-Sync-Strategy.md
    /// </summary>
    public class CalendarSyncService : IDisposable
    {
        #region Singleton

        private static CalendarSyncService? _instance;
        private static readonly object _lock = new();

        public static CalendarSyncService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new CalendarSyncService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly ConcurrentQueue<SyncOperation> _offlineQueue;
        private Timer? _periodicSyncTimer;
        private bool _isSyncing;
        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private DateTime _lastSyncTime = DateTime.MinValue;

        #endregion

        #region Properties

        /// <summary>
        /// Whether sync is currently in progress.
        /// </summary>
        public bool IsSyncing => _isSyncing;

        /// <summary>
        /// Whether Microsoft 365 is connected and ready for sync.
        /// </summary>
        public bool IsReady => MicrosoftGraphAuthService.Instance.IsAuthenticated &&
                              MicrosoftGraphAuthService.Instance.CalendarAvailable;

        /// <summary>
        /// Number of pending operations in offline queue.
        /// </summary>
        public int PendingOperations => _offlineQueue.Count;

        /// <summary>
        /// Current sync status for UI display.
        /// </summary>
        public SyncStatus Status { get; private set; } = SyncStatus.NotConnected;

        /// <summary>
        /// Last sync error message, if any.
        /// </summary>
        public string? LastError { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when sync status changes.
        /// </summary>
        public event Action<SyncStatus>? StatusChanged;

        /// <summary>
        /// Raised when a calendar event is synced (for UI updates).
        /// </summary>
        public event Action<Meeting, SyncDirection>? MeetingSynced;

        #endregion

        #region Constructor

        private CalendarSyncService()
        {
            _logger = LoggingManager.GetComponentLogger("CalendarSync");
            _offlineQueue = new ConcurrentQueue<SyncOperation>();

            MicrosoftGraphAuthService.Instance.AuthenticationStateChanged += OnAuthStateChanged;
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Starts the periodic sync timer.
        /// Should be called after successful Microsoft login.
        /// </summary>
        public void StartPeriodicSync()
        {
            // Check subscription tier
            if (!SubscriptionService.Instance.HasFeature("calendar_sync"))
            {
                _logger.Info("Calendar sync not available in current subscription tier");
                return;
            }

            if (!IsReady)
            {
                _logger.Warn("Cannot start sync - Microsoft 365 not connected");
                return;
            }

            var settings = UserSettingsManager.Instance.Settings.Microsoft365;
            var interval = TimeSpan.FromMinutes(settings.SyncIntervalMinutes);

            _periodicSyncTimer?.Dispose();
            _periodicSyncTimer = new Timer(
                async _ => await SyncDeltaAsync(),
                null,
                TimeSpan.FromSeconds(5), // Initial delay
                interval);

            _logger.Info($"Periodic sync started (interval: {interval.TotalMinutes} minutes)");
            UpdateStatus(SyncStatus.Idle);
        }

        /// <summary>
        /// Stops the periodic sync timer.
        /// </summary>
        public void StopPeriodicSync()
        {
            _periodicSyncTimer?.Dispose();
            _periodicSyncTimer = null;
            _logger.Info("Periodic sync stopped");
        }

        #endregion

        #region Sync Triggers

        /// <summary>
        /// Triggers a sync when app window gains focus.
        /// </summary>
        public async Task OnAppFocusedAsync()
        {
            var settings = UserSettingsManager.Instance.Settings.Microsoft365;
            if (!settings.SyncOnFocus || !IsReady)
                return;

            // Don't sync if we just synced within the last 30 seconds
            if ((DateTime.Now - _lastSyncTime).TotalSeconds < 30)
                return;

            await SyncDeltaAsync();
        }

        /// <summary>
        /// Called when a meeting is created in Tracker.
        /// Immediately pushes to calendar.
        /// </summary>
        public async Task OnMeetingCreatedAsync(Meeting meeting)
        {
            if (!IsReady || !ShouldSync(meeting))
                return;

            _logger.Info($"Pushing new meeting to calendar: {meeting.Description}");
            await PushCreateAsync(meeting);
        }

        /// <summary>
        /// Called when a meeting is updated in Tracker.
        /// Immediately pushes changes to calendar.
        /// </summary>
        public async Task OnMeetingUpdatedAsync(Meeting meeting)
        {
            if (!IsReady || !ShouldSync(meeting))
                return;

            if (string.IsNullOrEmpty(meeting.CalendarEventId))
            {
                // Not yet synced, create instead
                await PushCreateAsync(meeting);
                return;
            }

            _logger.Info($"Pushing meeting update to calendar: {meeting.Title}");
            await PushUpdateAsync(meeting);
        }

        /// <summary>
        /// Called when a meeting is deleted in Tracker.
        /// Immediately deletes from calendar.
        /// </summary>
        public async Task OnMeetingDeletedAsync(Meeting meeting)
        {
            if (!IsReady || string.IsNullOrEmpty(meeting.CalendarEventId))
                return;

            _logger.Info($"Removing meeting from calendar: {meeting.Title}");
            await PushDeleteAsync(meeting);
        }

        /// <summary>
        /// Forces a full sync (not delta).
        /// </summary>
        public async Task ForceSyncAsync()
        {
            var settings = UserSettingsManager.Instance.Settings.Microsoft365;
            settings.CalendarDeltaLink = null; // Clear delta link to force full sync
            UserSettingsManager.Instance.SaveSettings();
            
            await SyncDeltaAsync();
        }

        /// <summary>
        /// Fetches the latest calendar event and updates the meeting's time fields.
        /// Call this before opening a meeting for edit to ensure time is current.
        /// </summary>
        /// <param name="meeting">The meeting to refresh from calendar.</param>
        /// <returns>True if calendar was fetched and time was updated; false if no calendar link or fetch failed.</returns>
        public async Task<bool> RefreshTimeFromCalendarAsync(Meeting meeting)
        {
            if (!IsReady || string.IsNullOrEmpty(meeting.CalendarEventId))
            {
                return false;
            }

            try
            {
                var calEvent = await MicrosoftGraphService.Instance.GetCalendarEventAsync(meeting.CalendarEventId);
                if (calEvent == null)
                {
                    _logger.Warn($"Calendar event not found for meeting {meeting.Id}, may have been deleted");
                    return false;
                }

                // Check if calendar event has changed
                var calendarLastModified = calEvent.LastModifiedDateTime?.UtcDateTime ?? DateTime.UtcNow;
                if (meeting.LastSyncedAt.HasValue && meeting.LastSyncedAt >= calendarLastModified)
                {
                    _logger.Debug($"Calendar event unchanged for meeting {meeting.Id}");
                    return false; // No changes
                }

                // Update time fields from calendar (calendar is authoritative for time)
                bool timeChanged = false;

                if (calEvent.Start != null)
                {
                    var startLocal = calEvent.Start.ToLocalDateTime();
                    if (meeting.ScheduledAt.Date != startLocal.Date || meeting.ScheduledAt.TimeOfDay != startLocal.TimeOfDay)
                    {
                        meeting.ScheduledAt = startLocal;
                        timeChanged = true;
                    }
                }

                if (calEvent.End != null && calEvent.Start != null)
                {
                    var duration = (int)(calEvent.End.ToLocalDateTime() - calEvent.Start.ToLocalDateTime()).TotalMinutes;
                    if (meeting.DurationMinutes != duration)
                    {
                        meeting.DurationMinutes = duration;
                        timeChanged = true;
                    }
                }

                // Update sync timestamp
                meeting.LastSyncedAt = DateTime.UtcNow;

                if (timeChanged)
                {
                    _logger.Info($"Updated meeting {meeting.Id} time from calendar: {meeting.ScheduledAt:g} ({meeting.DurationMinutes}min)");
                    
                    // Save the time changes to database
                    var meetingRepository = CreateMeetingRepository();
                    if (meetingRepository != null)
                    {
                        await meetingRepository.UpdateMeetingAsync(meeting);
                    }
                }

                return timeChanged;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Failed to refresh time from calendar for meeting {meeting.Id}");
                return false;
            }
        }

        #endregion

        #region Push Operations (Tracker → Calendar)

        private async Task PushCreateAsync(Meeting meeting)
        {
            try
            {
                UpdateStatus(SyncStatus.Syncing);

                var calendarEvent = ConvertToCalendarEvent(meeting);
                var created = await MicrosoftGraphService.Instance.CreateCalendarEventAsync(calendarEvent);

                if (created != null)
                {
                    // Store the calendar event ID on the meeting (generic approach)
                    meeting.CalendarEventId = created.Id;
                    meeting.CalendarProviderString = "microsoft";
                    meeting.CalendarEtag = created.ETag ?? created.ChangeKey;
                    meeting.LastSyncedAt = DateTime.UtcNow;
                    meeting.CalendarSyncStatus = "synced";

                    // Save to database
                    await SaveMeetingSyncDataAsync(meeting);

                    _logger.Info($"Created calendar event: {created.Id}");
                    MeetingSynced?.Invoke(meeting, SyncDirection.Push);
                }
                else
                {
                    QueueOfflineOperation(SyncOperationType.Create, meeting);
                }

                UpdateStatus(SyncStatus.Idle);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to push create to calendar");
                QueueOfflineOperation(SyncOperationType.Create, meeting);
                UpdateStatus(SyncStatus.Error, ex.Message);
            }
        }

        private async Task PushUpdateAsync(Meeting meeting)
        {
            try
            {
                UpdateStatus(SyncStatus.Syncing);

                var calendarEvent = ConvertToCalendarEvent(meeting);
                var updated = await MicrosoftGraphService.Instance.UpdateCalendarEventAsync(
                    meeting.CalendarEventId!, calendarEvent);

                if (updated != null)
                {
                    meeting.CalendarEtag = updated.ETag ?? updated.ChangeKey;
                    meeting.LastSyncedAt = DateTime.UtcNow;
                    meeting.CalendarSyncStatus = "synced";

                    await SaveMeetingSyncDataAsync(meeting);

                    _logger.Info($"Updated calendar event: {meeting.CalendarEventId}");
                    MeetingSynced?.Invoke(meeting, SyncDirection.Push);
                }
                else
                {
                    QueueOfflineOperation(SyncOperationType.Update, meeting);
                }

                UpdateStatus(SyncStatus.Idle);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to push update to calendar");
                QueueOfflineOperation(SyncOperationType.Update, meeting);
                UpdateStatus(SyncStatus.Error, ex.Message);
            }
        }

        private async Task PushDeleteAsync(Meeting meeting)
        {
            try
            {
                UpdateStatus(SyncStatus.Syncing);

                var success = await MicrosoftGraphService.Instance.DeleteCalendarEventAsync(
                    meeting.CalendarEventId!);

                if (success)
                {
                    _logger.Info($"Deleted calendar event: {meeting.CalendarEventId}");
                }
                else
                {
                    QueueOfflineOperation(SyncOperationType.Delete, meeting);
                }

                UpdateStatus(SyncStatus.Idle);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to push delete to calendar");
                QueueOfflineOperation(SyncOperationType.Delete, meeting);
                UpdateStatus(SyncStatus.Error, ex.Message);
            }
        }

        #endregion

        #region Pull Operations (Calendar → Tracker)

        private async Task SyncDeltaAsync()
        {
            if (!IsReady || !await _syncLock.WaitAsync(0))
                return; // Already syncing or not ready

            try
            {
                _isSyncing = true;
                UpdateStatus(SyncStatus.Syncing);

                var settings = UserSettingsManager.Instance.Settings.Microsoft365;
                var startDate = DateTime.Today.AddDays(-settings.SyncDaysBack);
                var endDate = DateTime.Today.AddDays(settings.SyncDaysForward);

                var (events, deletedIds, nextDeltaLink) = 
                    await MicrosoftGraphService.Instance.GetCalendarDeltaAsync(
                        settings.CalendarDeltaLink, startDate, endDate);

                // Process changed events
                foreach (var calEvent in events)
                {
                    await ProcessIncomingEventAsync(calEvent);
                }

                // Process deleted events
                foreach (var deletedId in deletedIds)
                {
                    await ProcessDeletedEventAsync(deletedId);
                }

                // Save delta link for next sync
                if (!string.IsNullOrEmpty(nextDeltaLink))
                {
                    settings.CalendarDeltaLink = nextDeltaLink;
                    settings.LastCalendarSync = DateTime.Now;
                    UserSettingsManager.Instance.SaveSettings();
                }

                _lastSyncTime = DateTime.Now;
                LastError = null;
                UpdateStatus(SyncStatus.Idle);

                _logger.Info($"Delta sync complete: {events.Count} events, {deletedIds.Count} deleted");

                // Process any queued offline operations
                await ProcessOfflineQueueAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Delta sync failed");
                LastError = ex.Message;
                UpdateStatus(SyncStatus.Error, ex.Message);
            }
            finally
            {
                _isSyncing = false;
                _syncLock.Release();
            }
        }

        private async Task ProcessIncomingEventAsync(GraphCalendarEvent calEvent)
        {
            try
            {
                // Check if we already have this event linked to a meeting
                var existingMeeting = await FindMeetingByCalendarEventIdAsync(calEvent.Id!);

                if (existingMeeting != null)
                {
                    // Check for conflicts
                    if (HasConflict(existingMeeting, calEvent))
                    {
                        // Calendar wins - update Tracker
                        _logger.Info($"Conflict detected for {calEvent.Subject}, calendar wins");
                        await UpdateMeetingFromCalendarAsync(existingMeeting, calEvent);
                        
                        // Notify user of the change
                        ShowConflictNotification(existingMeeting, calEvent);
                    }
                    else
                    {
                        // Just update if changed
                        await UpdateMeetingFromCalendarAsync(existingMeeting, calEvent);
                    }
                }
                else
                {
                    // This is an external calendar event - optionally import
                    // For now, we only sync events created from Tracker
                    _logger.Debug($"Ignoring external calendar event: {calEvent.Subject}");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Failed to process incoming event: {calEvent.Id}");
            }
        }

        private async Task ProcessDeletedEventAsync(string calendarEventId)
        {
            try
            {
                var meeting = await FindMeetingByCalendarEventIdAsync(calendarEventId);
                if (meeting != null)
                {
                    var memberName = meeting.Report != null
                        ? $"{meeting.Report.FirstName} {meeting.Report.LastName}".Trim()
                        : meeting.Title;

                    // Show dialog asking user what to do
                    // Must dispatch to UI thread
                    bool deleteInTracker = false;
                    
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var result = MessageBoxHelper.Show(
                            $"The calendar event for \"{meeting.Title}\" on {meeting.ScheduledAt:MMM d} was deleted in Outlook.\n\n" +
                            "Do you also want to delete this meeting in Tracker?\n\n" +
                            "• Yes - Delete the meeting (moves to recycle bin)\n" +
                            "• No - Keep the meeting in Tracker (unlink from calendar)",
                            "Calendar Event Deleted",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);

                        deleteInTracker = (result == System.Windows.MessageBoxResult.Yes);
                    });

                    if (deleteInTracker)
                    {
                        // Soft delete the meeting (goes to recycle bin)
                        var meetingRepository = CreateMeetingRepository();
                        if (meetingRepository != null)
                        {
                            await meetingRepository.DeleteMeetingAsync(meeting.Id);
                        }
                        
                        _logger.Info($"Calendar event deleted in Outlook, user chose to delete in Tracker: {meeting.Title}");
                        NotificationManager.Instance.ShowInfo("Meeting Deleted", 
                            $"Meeting \"{meeting.Title}\" has been moved to the recycle bin.");
                    }
                    else
                    {
                        // Just unlink from calendar
                        meeting.CalendarEventId = null;
                        meeting.CalendarProviderString = null;
                        meeting.CalendarEtag = null;
                        meeting.CalendarSyncStatus = "not_synced";
                        await SaveMeetingSyncDataAsync(meeting);

                        _logger.Info($"Calendar event deleted in Outlook, user chose to keep in Tracker: {meeting.Title}");
                        NotificationManager.Instance.ShowInfo("Meeting Unlinked", 
                            $"Meeting \"{meeting.Title}\" is no longer synced to Outlook.");
                    }

                    MeetingSynced?.Invoke(meeting, SyncDirection.Pull);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Failed to process deleted event: {calendarEventId}");
            }
        }

        #endregion

        #region Offline Queue

        private void QueueOfflineOperation(SyncOperationType type, Meeting meeting)
        {
            var operation = new SyncOperation
            {
                Type = type,
                MeetingId = meeting.Id,
                QueuedAt = DateTime.Now
            };

            _offlineQueue.Enqueue(operation);
            meeting.CalendarSyncStatus = "pending";

            _logger.Info($"Queued offline operation: {type} for meeting {meeting.Id}");
        }

        private async Task ProcessOfflineQueueAsync()
        {
            int processed = 0;
            int maxRetries = 3;

            while (_offlineQueue.TryDequeue(out var operation))
            {
                try
                {
                    var meeting = await GetMeetingByIdAsync(operation.MeetingId);
                    if (meeting == null)
                        continue;

                    bool success = operation.Type switch
                    {
                        SyncOperationType.Create => await RetryPushCreateAsync(meeting),
                        SyncOperationType.Update => await RetryPushUpdateAsync(meeting),
                        SyncOperationType.Delete => await RetryPushDeleteAsync(meeting),
                        _ => false
                    };

                    if (!success && operation.RetryCount < maxRetries)
                    {
                        operation.RetryCount++;
                        _offlineQueue.Enqueue(operation);
                    }

                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex, $"Failed to process offline operation");
                }
            }

            if (processed > 0)
                _logger.Info($"Processed {processed} offline operations");
        }

        private async Task<bool> RetryPushCreateAsync(Meeting meeting)
        {
            var calendarEvent = ConvertToCalendarEvent(meeting);
            var created = await MicrosoftGraphService.Instance.CreateCalendarEventAsync(calendarEvent);
            
            if (created != null)
            {
                meeting.CalendarEventId = created.Id;
                meeting.CalendarProviderString = "microsoft";
                meeting.CalendarEtag = created.ETag ?? created.ChangeKey;
                meeting.LastSyncedAt = DateTime.UtcNow;
                meeting.CalendarSyncStatus = "synced";
                await SaveMeetingSyncDataAsync(meeting);
                return true;
            }
            return false;
        }

        private async Task<bool> RetryPushUpdateAsync(Meeting meeting)
        {
            if (string.IsNullOrEmpty(meeting.CalendarEventId))
                return await RetryPushCreateAsync(meeting);

            var calendarEvent = ConvertToCalendarEvent(meeting);
            var updated = await MicrosoftGraphService.Instance.UpdateCalendarEventAsync(
                meeting.CalendarEventId, calendarEvent);
            
            if (updated != null)
            {
                meeting.CalendarEtag = updated.ETag ?? updated.ChangeKey;
                meeting.LastSyncedAt = DateTime.UtcNow;
                meeting.CalendarSyncStatus = "synced";
                await SaveMeetingSyncDataAsync(meeting);
                return true;
            }
            return false;
        }

        private async Task<bool> RetryPushDeleteAsync(Meeting meeting)
        {
            if (string.IsNullOrEmpty(meeting.CalendarEventId))
                return true; // Nothing to delete

            return await MicrosoftGraphService.Instance.DeleteCalendarEventAsync(meeting.CalendarEventId);
        }

        #endregion

        #region Conversion & Helpers

        private GraphCalendarEvent ConvertToCalendarEvent(Meeting meeting)
        {
            var teamMember = meeting.Report;
            var subject = meeting.Title ?? $"Meeting with {(teamMember != null ? $"{teamMember.FirstName} {teamMember.LastName}".Trim() : "Team")}";
            
            if (!string.IsNullOrEmpty(meeting.Description))
                subject = $"{meeting.Title} - {meeting.Description}";

            var startTime = meeting.ScheduledAt;
            var endTime = meeting.ScheduledAt.AddMinutes(meeting.DurationMinutes ?? 60);

            var calEvent = new GraphCalendarEvent
            {
                Subject = subject,
                Start = GraphDateTimeTimeZone.FromLocalDateTime(startTime),
                End = GraphDateTimeTimeZone.FromLocalDateTime(endTime),
                Body = new GraphItemBody
                {
                    ContentType = "text",
                    Content = BuildEventBody(meeting)
                }
            };

            // Add attendee if we have their email
            if (!string.IsNullOrEmpty(teamMember?.Email))
            {
                calEvent.Attendees = new List<GraphAttendee>
                {
                    new GraphAttendee
                    {
                        Type = "required",
                        EmailAddress = new GraphEmailAddress
                        {
                            Name = $"{teamMember.FirstName} {teamMember.LastName}".Trim(),
                            Address = teamMember.Email
                        }
                    }
                };
            }

            return calEvent;
        }

        private string BuildEventBody(Meeting meeting)
        {
            var body = new System.Text.StringBuilder();
            
            body.AppendLine("📋 Agenda:");
            if (meeting.AgendaItems?.Any() == true)
            {
                foreach (var item in meeting.AgendaItems)
                {
                    body.AppendLine($"• {item.Title}");
                }
            }
            else
            {
                body.AppendLine("• No agenda items set");
            }

            body.AppendLine();
            body.AppendLine("---");
            body.AppendLine("Managed by Tracker");

            return body.ToString();
        }

        private bool HasConflict(Meeting meeting, GraphCalendarEvent calEvent)
        {
            // Check if etag changed since last sync
            var currentEtag = calEvent.ETag ?? calEvent.ChangeKey;
            return meeting.CalendarEtag != currentEtag && 
                   meeting.LastSyncedAt.HasValue &&
                   meeting.LastSyncedAt < DateTime.UtcNow.AddMinutes(-1); // Ignore very recent changes
        }

        private bool ShouldSync(Meeting meeting)
        {
            // Only sync if calendar sync is enabled
            var settings = UserSettingsManager.Instance.Settings.Microsoft365;
            if (!settings.CalendarSyncEnabled)
                return false;

            // Don't sync completed/canceled meetings
            if (meeting.Status == MeetingStatus.Completed ||
                meeting.Status == MeetingStatus.Cancelled)
                return false;

            return true;
        }

        private void ShowConflictNotification(Meeting meeting, GraphCalendarEvent calEvent)
        {
            var memberName = meeting.Report != null 
                ? $"{meeting.Report.FirstName} {meeting.Report.LastName}".Trim()
                : meeting.Title;
            
            // Use NotificationManager for toast notification
            var message = $"Meeting with {memberName} was moved in Outlook to {calEvent.Start?.ToLocalDateTime():g}";
            NotificationManager.Instance.ShowInfo("Calendar Update", message);
            _logger.Info($"Conflict notification: {message}");
        }

        #endregion

        #region Database Operations

        private async Task<Meeting?> FindMeetingByCalendarEventIdAsync(string calendarEventId)
        {
            var meetingRepository = CreateMeetingRepository();
            if (meetingRepository == null)
            {
                return null;
            }

            // Query database for meeting with this calendar event ID via CalendarLinks table
            return await meetingRepository.FindMeetingByCalendarEventIdAsync("outlook", calendarEventId);
        }

        private async Task<Meeting?> GetMeetingByIdAsync(Guid meetingId)
        {
            var meetingRepository = CreateMeetingRepository();
            if (meetingRepository == null)
            {
                return null;
            }

            return await meetingRepository.GetMeetingByIdAsync(meetingId);
        }

        private async Task SaveMeetingSyncDataAsync(Meeting meeting)
        {
            // Update meeting's sync fields in database
            var meetingRepository = CreateMeetingRepository();
            if (meetingRepository != null)
            {
                await meetingRepository.UpdateMeetingSyncDataAsync(
                    meeting.Id,
                    meeting.CalendarEventId,
                    meeting.CalendarProviderString,
                    meeting.CalendarEtag,
                    meeting.CalendarSyncStatus);
            }
        }

        private async Task UpdateMeetingFromCalendarAsync(Meeting meeting, GraphCalendarEvent calEvent)
        {
            // Update meeting with calendar data (calendar wins for scheduling)
            if (calEvent.Start != null)
            {
                var startLocal = calEvent.Start.ToLocalDateTime();
                meeting.ScheduledAt = startLocal;
            }

            if (calEvent.End != null)
            {
                var endLocal = calEvent.End.ToLocalDateTime();
                meeting.DurationMinutes = (int)(endLocal - meeting.ScheduledAt).TotalMinutes;
            }

            meeting.CalendarEtag = calEvent.ETag ?? calEvent.ChangeKey;
            meeting.LastSyncedAt = DateTime.UtcNow;
            meeting.CalendarSyncStatus = "synced";

            await SaveMeetingSyncDataAsync(meeting);
            MeetingSynced?.Invoke(meeting, SyncDirection.Pull);
        }

        private static MeetingRepository? CreateMeetingRepository()
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                return null;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            var context = contextFactory.CreateContext();
            return new MeetingRepository(context, userId.Value, () => contextFactory.CreateContext());
        }

        #endregion

        #region Status Management

        private void UpdateStatus(SyncStatus status, string? error = null)
        {
            Status = status;
            LastError = error;

            // Ensure we're on UI thread for event
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusChanged?.Invoke(status);
                });
            }
            else
            {
                StatusChanged?.Invoke(status);
            }
        }

        private void OnAuthStateChanged(bool isAuthenticated)
        {
            if (isAuthenticated)
            {
                UpdateStatus(SyncStatus.Idle);
            }
            else
            {
                StopPeriodicSync();
                UpdateStatus(SyncStatus.NotConnected);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopPeriodicSync();
            _syncLock.Dispose();
            MicrosoftGraphAuthService.Instance.AuthenticationStateChanged -= OnAuthStateChanged;
        }

        #endregion
    }

    #region Supporting Types

    public enum SyncStatus
    {
        NotConnected,
        Idle,
        Syncing,
        Error
    }

    public enum SyncDirection
    {
        Push, // Tracker → Calendar
        Pull  // Calendar → Tracker
    }

    public enum SyncOperationType
    {
        Create,
        Update,
        Delete
    }

    public class SyncOperation
    {
        public SyncOperationType Type { get; set; }
        public Guid MeetingId { get; set; }
        public DateTime QueuedAt { get; set; }
        public int RetryCount { get; set; }
    }

    #endregion
}

