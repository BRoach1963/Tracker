using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Background service that monitors and triggers reminders.
/// Runs on a timer and shows toast notifications when reminders are due.
/// 
/// Usage:
///   - Call Start() after user authenticates
///   - Call Stop() when user signs out or app exits
///   - Call ReloadSettings() if reminder settings change
/// </summary>
public class ReminderSchedulerService : IDisposable
{
    #region Singleton

    private static readonly Lazy<ReminderSchedulerService> _instance =
        new(() => new ReminderSchedulerService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static ReminderSchedulerService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "reminder_scheduler.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine($"[ReminderScheduler] {message}");
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion

    #region Constants

    /// <summary>
    /// How often to check for due reminders (60 seconds).
    /// </summary>
    private const int CHECK_INTERVAL_MS = 60_000;

    /// <summary>
    /// Initial delay before first check (10 seconds - allows app to fully load).
    /// </summary>
    private const int INITIAL_DELAY_MS = 10_000;

    /// <summary>
    /// Default snooze duration in minutes.
    /// </summary>
    public const int DEFAULT_SNOOZE_MINUTES = 10;

    #endregion

    #region Fields

    private Timer? _reminderTimer;
    private bool _isRunning;
    private bool _disposed;
    private ReminderSettings _settings;
    private readonly object _lock = new();

    // Track recently shown reminders to prevent duplicates within a short window
    private readonly HashSet<Guid> _recentlyShownReminders = new();
    private DateTime _lastCleanup = DateTime.UtcNow;

    #endregion

    #region Events

    /// <summary>
    /// Fired when a reminder is triggered (for UI updates, logging, etc.).
    /// </summary>
    public event EventHandler<Reminder>? ReminderTriggered;

    #endregion

    #region Constructor

    private ReminderSchedulerService()
    {
        _settings = ReminderSettings.Default;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets whether the scheduler is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the current reminder settings.
    /// </summary>
    public ReminderSettings Settings => _settings;

    #endregion

    #region Public Methods - Lifecycle

    /// <summary>
    /// Starts the reminder scheduler.
    /// Call this after user authenticates.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                Log("Already running, ignoring Start()");
                return;
            }

            Log("Starting reminder scheduler...");
            _isRunning = true;

            // Load settings (could be from local storage in future)
            _settings = ReminderSettings.Default;

            if (!_settings.EnableReminders)
            {
                Log("Reminders are disabled in settings");
                return;
            }

            // Start the timer
            _reminderTimer = new Timer(
                CheckRemindersCallback,
                null,
                INITIAL_DELAY_MS,
                CHECK_INTERVAL_MS
            );

            Log($"Reminder scheduler started (check every {CHECK_INTERVAL_MS / 1000}s)");
        }
    }

    /// <summary>
    /// Stops the reminder scheduler.
    /// Call this when user signs out or app exits.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                Log("Not running, ignoring Stop()");
                return;
            }

            Log("Stopping reminder scheduler...");
            _isRunning = false;

            _reminderTimer?.Dispose();
            _reminderTimer = null;

            _recentlyShownReminders.Clear();

            Log("Reminder scheduler stopped");
        }
    }

    /// <summary>
    /// Reloads settings and restarts the scheduler if needed.
    /// </summary>
    public void ReloadSettings(ReminderSettings? newSettings = null)
    {
        lock (_lock)
        {
            _settings = newSettings ?? ReminderSettings.Default;
            Log($"Settings reloaded: EnableReminders={_settings.EnableReminders}");

            if (_isRunning)
            {
                Stop();
                Start();
            }
        }
    }

    /// <summary>
    /// Forces an immediate check for due reminders.
    /// Useful for testing or when user opens reminder-related UI.
    /// </summary>
    public async Task CheckNowAsync()
    {
        if (!_isRunning || !_settings.EnableReminders)
        {
            Log("CheckNowAsync skipped - not running or disabled");
            return;
        }

        Log("Manual check requested");
        await CheckRemindersAsync();
    }

    #endregion

    #region Public Methods - Snooze & Dismiss

    /// <summary>
    /// Snoozes a reminder for the specified duration.
    /// </summary>
    public async Task SnoozeReminderAsync(Guid reminderId, int? snoozeMinutes = null)
    {
        var minutes = snoozeMinutes ?? _settings.DefaultSnoozeDurationMinutes;
        Log($"Snoozing reminder {reminderId} for {minutes} minutes");

        var success = await ReminderDataService.Instance.SnoozeReminderAsync(reminderId, minutes);
        if (!success)
        {
            Log($"Failed to snooze reminder: {ReminderDataService.Instance.LastError}");
        }
    }

    /// <summary>
    /// Dismisses a reminder.
    /// </summary>
    public async Task DismissReminderAsync(Guid reminderId)
    {
        Log($"Dismissing reminder {reminderId}");

        var success = await ReminderDataService.Instance.DismissReminderAsync(reminderId);
        if (!success)
        {
            Log($"Failed to dismiss reminder: {ReminderDataService.Instance.LastError}");
        }
    }

    #endregion

    #region Private Methods - Timer Callback

    private async void CheckRemindersCallback(object? state)
    {
        if (!_isRunning || !_settings.EnableReminders) return;

        try
        {
            await CheckRemindersAsync();
        }
        catch (Exception ex)
        {
            Log($"Error in reminder check callback: {ex.Message}");
        }
    }

    private async Task CheckRemindersAsync()
    {
        // Ensure we're authenticated
        var session = AuthService.Instance.CurrentSession_ProCohere;
        if (session?.User == null)
        {
            Log("Not authenticated, skipping reminder check");
            return;
        }

        Log("Checking for due reminders...");

        // Get due reminders from data service
        var dueReminders = await ReminderDataService.Instance.GetDueRemindersAsync();

        if (dueReminders.Count == 0)
        {
            Log("No due reminders found");
            return;
        }

        Log($"Found {dueReminders.Count} due reminders");

        // Clean up recently-shown cache periodically (every 5 minutes)
        CleanupRecentlyShownCache();

        foreach (var reminder in dueReminders)
        {
            // Skip if we've recently shown this reminder (prevent duplicates)
            if (_recentlyShownReminders.Contains(reminder.Id))
            {
                Log($"Skipping recently shown reminder: {reminder.Id}");
                continue;
            }

            // Mark as sent first to prevent duplicate notifications on next check
            var markSuccess = await ReminderDataService.Instance.MarkReminderSentAsync(reminder.Id);
            if (!markSuccess)
            {
                Log($"Failed to mark reminder as sent: {reminder.Id}");
                continue;
            }

            // Track that we've shown this reminder
            _recentlyShownReminders.Add(reminder.Id);

            // Show the notification
            ShowReminderNotification(reminder);

            // Fire event for any listeners
            ReminderTriggered?.Invoke(this, reminder);
        }
    }

    private void CleanupRecentlyShownCache()
    {
        // Only clean up every 5 minutes
        if ((DateTime.UtcNow - _lastCleanup).TotalMinutes < 5) return;

        _lastCleanup = DateTime.UtcNow;
        
        // Clear the cache - reminders that were marked as "sent" won't be returned
        // by GetDueRemindersAsync anyway, so this is just a safety measure
        _recentlyShownReminders.Clear();
        Log("Cleaned up recently-shown reminders cache");
    }

    #endregion

    #region Private Methods - Notifications

    private void ShowReminderNotification(Reminder reminder)
    {
        try
        {
            // Determine icon based on reminder type
            var icon = reminder.Type switch
            {
                ReminderType.Meeting => "📅",
                ReminderType.Task => "✅",
                ReminderType.Goal => "🎯",
                ReminderType.Engagement => "👥",
                ReminderType.Custom => "🔔",
                _ => "🔔"
            };

            var title = $"{icon} {reminder.Title}";
            var message = reminder.Message ?? string.Empty;

            Log($"Showing notification: {title}");

            // Show notification on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                // Check if main window is visible
                var isWindowVisible = NotificationService.Instance.IsMainWindowVisible?.Invoke() ?? true;
                
                if (isWindowVisible)
                {
                    // Show in-app toast
                    NotificationService.Instance.ShowInfo(title, message, durationSeconds: 10);
                }
                else
                {
                    // Show native toast with Dismiss/Snooze buttons
                    NotificationService.Instance.SendReminderToast(
                        title, 
                        message, 
                        reminder.Id, 
                        _settings.DefaultSnoozeDurationMinutes);
                }
            });

            // Play sound if enabled (future enhancement)
            if (_settings.PlaySound)
            {
                PlayNotificationSound();
            }
        }
        catch (Exception ex)
        {
            Log($"Error showing reminder notification: {ex.Message}");
        }
    }

    private void PlayNotificationSound()
    {
        // Future: Play system notification sound
        // For now, Windows native toasts play their own sound
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
