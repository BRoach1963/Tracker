using Tracker.DataModels;
using Tracker.Common.Enums;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for Reminder data access operations.
    /// Handles all reminder operations including status updates and snoozing.
    /// </summary>
    public interface IReminderRepository
    {
        /// <summary>
        /// Gets all reminders that are due (status=Pending and RemindAt <= now).
        /// Used by the reminder notification service.
        /// </summary>
        Task<List<Reminder>> GetDueRemindersAsync();

        /// <summary>
        /// Gets all reminders for the current user.
        /// </summary>
        Task<List<Reminder>> GetAllRemindersAsync();

        /// <summary>
        /// Gets pending or snoozed reminders for display.
        /// </summary>
        Task<List<Reminder>> GetPendingRemindersAsync();

        /// <summary>
        /// Gets a specific reminder by ID.
        /// </summary>
        Task<Reminder?> GetReminderByIdAsync(Guid id);

        /// <summary>
        /// Adds a new reminder.
        /// </summary>
        Task<Guid> AddReminderAsync(Reminder reminder);

        /// <summary>
        /// Updates a reminder (e.g., after snooze or dismiss).
        /// </summary>
        Task<bool> UpdateReminderAsync(Reminder reminder);

        /// <summary>
        /// Marks a reminder as triggered (shown to user).
        /// </summary>
        Task<bool> MarkReminderTriggeredAsync(Guid reminderId);

        /// <summary>
        /// Snoozes a reminder for the specified number of minutes.
        /// </summary>
        Task<bool> SnoozeReminderAsync(Guid reminderId, int snoozeMinutes);

        /// <summary>
        /// Dismisses a reminder.
        /// </summary>
        Task<bool> DismissReminderAsync(Guid reminderId);

        /// <summary>
        /// Deletes a reminder.
        /// </summary>
        Task<bool> DeleteReminderAsync(Guid reminderId);
    }
}
