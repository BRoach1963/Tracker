using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing reminders in Supabase.
/// Handles CRUD operations for reminders, including scheduled notifications for
/// meetings, tasks, goals, and custom reminders.
/// 
/// This service provides the data access layer. The actual reminder triggering
/// is handled by ReminderSchedulerService (which uses timers to check due reminders).
/// </summary>
public class ReminderDataService
{
    #region Singleton

    private static readonly Lazy<ReminderDataService> _instance =
        new(() => new ReminderDataService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static ReminderDataService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "reminder_service.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
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

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private ReminderDataService() { }

    #region Read Operations

    /// <summary>
    /// Gets all pending (scheduled) reminders for the current user.
    /// </summary>
    public async Task<List<Reminder>> GetPendingRemindersAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.User == null)
        {
            LastError = "Not authenticated";
            return new List<Reminder>();
        }

        try
        {
            Log("Getting pending reminders");

            var result = await client.From<Reminder>()
                .Filter("user_id", Operator.Equals, session.User.Id.ToString())
                .Filter("status", Operator.Equals, "scheduled")
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("remind_at", Ordering.Ascending)
                .Get();

            var reminders = result.Models?.ToList() ?? new List<Reminder>();
            Log($"Found {reminders.Count} pending reminders");
            return reminders;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetPendingReminders ERROR: {ex.Message}");
            return new List<Reminder>();
        }
    }

    /// <summary>
    /// Gets all reminders that are due (scheduled and past their remind_at time).
    /// Also includes snoozed reminders whose snooze time has passed.
    /// </summary>
    public async Task<List<Reminder>> GetDueRemindersAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.User == null)
        {
            LastError = "Not authenticated";
            return new List<Reminder>();
        }

        try
        {
            var now = DateTime.UtcNow;
            Log($"Getting due reminders (now = {now:u})");

            // Get scheduled reminders that are due
            var scheduledResult = await client.From<Reminder>()
                .Filter("user_id", Operator.Equals, session.User.Id.ToString())
                .Filter("status", Operator.Equals, "scheduled")
                .Filter("remind_at", Operator.LessThanOrEqual, now.ToString("o"))
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var dueReminders = scheduledResult.Models?.ToList() ?? new List<Reminder>();

            // Get snoozed reminders whose snooze time has passed
            var snoozedResult = await client.From<Reminder>()
                .Filter("user_id", Operator.Equals, session.User.Id.ToString())
                .Filter("status", Operator.Equals, "snoozed")
                .Filter("snoozed_until", Operator.LessThanOrEqual, now.ToString("o"))
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var snoozedDue = snoozedResult.Models?.ToList() ?? new List<Reminder>();
            dueReminders.AddRange(snoozedDue);

            Log($"Found {dueReminders.Count} due reminders ({scheduledResult.Models?.Count() ?? 0} scheduled, {snoozedDue.Count} snoozed)");
            return dueReminders;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetDueReminders ERROR: {ex.Message}");
            return new List<Reminder>();
        }
    }

    /// <summary>
    /// Gets reminders for a specific entity (meeting, task, goal).
    /// </summary>
    public async Task<List<Reminder>> GetRemindersForEntityAsync(string entityType, Guid entityId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.User == null)
        {
            LastError = "Not authenticated";
            return new List<Reminder>();
        }

        try
        {
            Log($"Getting reminders for {entityType}/{entityId}");

            var result = await client.From<Reminder>()
                .Filter("user_id", Operator.Equals, session.User.Id.ToString())
                .Filter("entity_type", Operator.Equals, entityType)
                .Filter("entity_id", Operator.Equals, entityId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var reminders = result.Models?.ToList() ?? new List<Reminder>();
            Log($"Found {reminders.Count} reminders for {entityType}/{entityId}");
            return reminders;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetRemindersForEntity ERROR: {ex.Message}");
            return new List<Reminder>();
        }
    }

    /// <summary>
    /// Gets a single reminder by ID.
    /// </summary>
    public async Task<Reminder?> GetReminderAsync(Guid reminderId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Getting reminder: {reminderId}");

            var reminder = await client.From<Reminder>()
                .Filter("id", Operator.Equals, reminderId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            return reminder;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetReminder ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Create Operations

    /// <summary>
    /// Creates a new reminder.
    /// </summary>
    public async Task<Reminder?> CreateReminderAsync(Reminder reminder)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null || session?.User == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            // Set required fields
            reminder.Id = Guid.NewGuid();
            reminder.OrganizationId = session.TeamMember.OrganizationId;
            reminder.UserId = session.User.Id;
            reminder.StatusString = "scheduled";
            reminder.IsDeleted = false;
            reminder.CreatedAt = DateTime.UtcNow;
            reminder.UpdatedAt = DateTime.UtcNow;

            Log($"Creating reminder: {reminder.Title} for {reminder.EntityType}/{reminder.EntityId} at {reminder.RemindAt:u}");

            var result = await client.From<Reminder>()
                .Insert(reminder);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Created reminder: {created.Id}");
            }

            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateReminder ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a reminder for an upcoming meeting.
    /// </summary>
    public async Task<Reminder?> CreateMeetingReminderAsync(MeetingDetail meeting, int minutesBefore)
    {
        if (meeting.ScheduledAt == null)
        {
            LastError = "Meeting has no scheduled time";
            return null;
        }

        var remindAt = meeting.ScheduledAt.Value.AddMinutes(-minutesBefore);
        if (remindAt <= DateTime.UtcNow)
        {
            // Already past the reminder time
            Log($"Skipping meeting reminder - already past remind time ({remindAt:u})");
            return null;
        }

        var reminder = new Reminder
        {
            Type = ReminderType.Meeting,
            EntityType = "meeting",
            EntityId = meeting.Id,
            Title = $"Upcoming: {meeting.Title}",
            Message = $"Meeting starts in {minutesBefore} minutes",
            RemindAt = remindAt,
            MinutesBefore = minutesBefore,
            TeamMemberId = meeting.TeamMemberId,
            SendInApp = true,
            SendPush = true
        };

        return await CreateReminderAsync(reminder);
    }

    /// <summary>
    /// Creates a reminder for a task deadline.
    /// </summary>
    public async Task<Reminder?> CreateTaskReminderAsync(TaskDetail task, int daysBefore)
    {
        if (task.DueDate == null)
        {
            LastError = "Task has no due date";
            return null;
        }

        var remindAt = task.DueDate.Value.AddDays(-daysBefore);
        if (remindAt <= DateTime.UtcNow)
        {
            // Already past the reminder time
            Log($"Skipping task reminder - already past remind time ({remindAt:u})");
            return null;
        }

        var reminder = new Reminder
        {
            Type = ReminderType.Task,
            EntityType = "task",
            EntityId = task.Id,
            Title = $"Task Due Soon: {task.Title}",
            Message = daysBefore == 1 
                ? "Due tomorrow" 
                : $"Due in {daysBefore} days",
            RemindAt = remindAt,
            TeamMemberId = task.OwnerTeamMemberId,
            SendInApp = true,
            SendPush = true
        };

        return await CreateReminderAsync(reminder);
    }

    /// <summary>
    /// Creates a reminder for a goal deadline.
    /// </summary>
    public async Task<Reminder?> CreateGoalReminderAsync(GoalDetail goal, int daysBefore)
    {
        if (goal.DueDate == null)
        {
            LastError = "Goal has no due date";
            return null;
        }

        var remindAt = goal.DueDate.Value.AddDays(-daysBefore);
        if (remindAt <= DateTime.UtcNow)
        {
            // Already past the reminder time
            Log($"Skipping goal reminder - already past remind time ({remindAt:u})");
            return null;
        }

        var reminder = new Reminder
        {
            Type = ReminderType.Goal,
            EntityType = "goal",
            EntityId = goal.Id,
            Title = $"Goal Deadline: {goal.Title}",
            Message = daysBefore == 1 
                ? "Due date is tomorrow" 
                : $"Due date in {daysBefore} days",
            RemindAt = remindAt,
            TeamMemberId = goal.OwnerTeamMemberId,
            SendInApp = true,
            SendPush = true
        };

        return await CreateReminderAsync(reminder);
    }

    /// <summary>
    /// Creates a custom reminder.
    /// </summary>
    public async Task<Reminder?> CreateCustomReminderAsync(string title, string? message, DateTime remindAt)
    {
        var session = AuthService.Instance.CurrentSession_ProCohere;
        
        var reminder = new Reminder
        {
            Type = ReminderType.Custom,
            EntityType = "custom",
            EntityId = Guid.NewGuid(), // Self-referencing for custom reminders
            Title = title,
            Message = message,
            RemindAt = remindAt,
            TeamMemberId = session?.TeamMember?.Id,
            SendInApp = true,
            SendPush = true
        };

        return await CreateReminderAsync(reminder);
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Marks a reminder as sent/triggered.
    /// </summary>
    public async Task<bool> MarkReminderSentAsync(Guid reminderId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Marking reminder as sent: {reminderId}");

            await client.From<Reminder>()
                .Filter("id", Operator.Equals, reminderId.ToString())
                .Set(r => r.StatusString!, "sent")
                .Set(r => r.SentAt!, DateTime.UtcNow)
                .Set(r => r.UpdatedAt!, DateTime.UtcNow)
                .Update();

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"MarkReminderSent ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Dismisses a reminder.
    /// </summary>
    public async Task<bool> DismissReminderAsync(Guid reminderId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Dismissing reminder: {reminderId}");

            await client.From<Reminder>()
                .Filter("id", Operator.Equals, reminderId.ToString())
                .Set(r => r.StatusString!, "dismissed")
                .Set(r => r.DismissedAt!, DateTime.UtcNow)
                .Set(r => r.UpdatedAt!, DateTime.UtcNow)
                .Update();

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DismissReminder ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Snoozes a reminder until a later time.
    /// </summary>
    public async Task<bool> SnoozeReminderAsync(Guid reminderId, int snoozeMinutes)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            var snoozeUntil = DateTime.UtcNow.AddMinutes(snoozeMinutes);
            Log($"Snoozing reminder {reminderId} until {snoozeUntil:u}");

            await client.From<Reminder>()
                .Filter("id", Operator.Equals, reminderId.ToString())
                .Set(r => r.StatusString!, "snoozed")
                .Set(r => r.SnoozedUntil!, snoozeUntil)
                .Set(r => r.UpdatedAt!, DateTime.UtcNow)
                .Update();

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"SnoozeReminder ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Soft deletes a reminder.
    /// </summary>
    public async Task<bool> DeleteReminderAsync(Guid reminderId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.User == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Deleting reminder: {reminderId}");

            await client.From<Reminder>()
                .Filter("id", Operator.Equals, reminderId.ToString())
                .Set(r => r.IsDeleted!, true)
                .Set(r => r.DeletedAt!, DateTime.UtcNow)
                .Set(r => r.DeletedBy!, session.User.Id)
                .Set(r => r.UpdatedAt!, DateTime.UtcNow)
                .Update();

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteReminder ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cancels all reminders for an entity (when entity is deleted).
    /// </summary>
    public async Task<int> CancelRemindersForEntityAsync(string entityType, Guid entityId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.User == null)
        {
            LastError = "Not authenticated";
            return 0;
        }

        try
        {
            Log($"Cancelling reminders for {entityType}/{entityId}");

            // Get reminders for this entity first (to count them)
            var reminders = await GetRemindersForEntityAsync(entityType, entityId);
            var pendingReminders = reminders.Where(r => r.Status == ReminderStatus.Pending).ToList();

            if (pendingReminders.Count == 0)
            {
                return 0;
            }

            // Update status to cancelled
            await client.From<Reminder>()
                .Filter("entity_type", Operator.Equals, entityType)
                .Filter("entity_id", Operator.Equals, entityId.ToString())
                .Filter("status", Operator.Equals, "scheduled")
                .Set(r => r.StatusString!, "cancelled")
                .Set(r => r.UpdatedAt!, DateTime.UtcNow)
                .Update();

            Log($"Cancelled {pendingReminders.Count} reminders");
            return pendingReminders.Count;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CancelRemindersForEntity ERROR: {ex.Message}");
            return 0;
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if a reminder already exists for the specified entity and reminder type.
    /// Used to prevent duplicate reminders.
    /// </summary>
    public async Task<bool> ReminderExistsAsync(string entityType, Guid entityId, ReminderType reminderType)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.User == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            var typeString = reminderType.ToString().ToLowerInvariant();

            var result = await client.From<Reminder>()
                .Filter("user_id", Operator.Equals, session.User.Id.ToString())
                .Filter("entity_type", Operator.Equals, entityType)
                .Filter("entity_id", Operator.Equals, entityId.ToString())
                .Filter("reminder_type", Operator.Equals, typeString)
                .Filter("status", Operator.Equals, "scheduled")
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            return result.Models?.Any() == true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ReminderExists ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion
}
