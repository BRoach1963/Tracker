using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Common.Enums;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for Reminder data access operations.
    /// Handles all reminder operations including status updates and snoozing.
    /// </summary>
    public class ReminderRepository : IReminderRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of ReminderRepository.
        /// </summary>
        public ReminderRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(ReminderRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all reminders that are due (status=Pending and RemindAt <= now).
        /// </summary>
        public async Task<List<Reminder>> GetDueRemindersAsync()
        {
            if (_context == null) return new List<Reminder>();

            try
            {
                var now = DateTime.Now;
                return await _context.Reminders
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted &&
                                r.Status == ReminderStatus.Pending &&
                                r.RemindAt <= now &&
                                (r.SnoozedUntil == null || r.SnoozedUntil <= now))
                    .OrderBy(r => r.RemindAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving due reminders");
                return new List<Reminder>();
            }
        }

        /// <summary>
        /// Gets all reminders for the current user.
        /// </summary>
        public async Task<List<Reminder>> GetAllRemindersAsync()
        {
            if (_context == null) return new List<Reminder>();

            try
            {
                return await _context.Reminders
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted)
                    .OrderBy(r => r.RemindAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving all reminders");
                return new List<Reminder>();
            }
        }

        /// <summary>
        /// Gets pending or snoozed reminders for display.
        /// </summary>
        public async Task<List<Reminder>> GetPendingRemindersAsync()
        {
            if (_context == null) return new List<Reminder>();

            try
            {
                return await _context.Reminders
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted &&
                                (r.Status == ReminderStatus.Pending || r.Status == ReminderStatus.Snoozed))
                    .OrderBy(r => r.RemindAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving pending reminders");
                return new List<Reminder>();
            }
        }

        /// <summary>
        /// Gets a specific reminder by ID.
        /// </summary>
        public async Task<Reminder?> GetReminderByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.Reminders
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving reminder with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new reminder.
        /// </summary>
        public async Task<Guid> AddReminderAsync(Reminder reminder)
        {
            if (_context == null)
            {
                _logger.Error("AddReminderAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                reminder.UserId = _userId;
                _context.Reminders.Add(reminder);
                await _context.SaveChangesAsync();
                _logger.Info("Added reminder ID: {0}", reminder.Id);
                return reminder.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding reminder");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates a reminder (e.g., after snooze or dismiss).
        /// </summary>
        public async Task<bool> UpdateReminderAsync(Reminder reminder)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Reminders.FindAsync(reminder.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateReminderAsync: Reminder ID {0} not found", reminder.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(reminder);
                await _context.SaveChangesAsync();
                _logger.Info("Updated reminder ID: {0}", reminder.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating reminder ID: {0}", reminder.Id);
                return false;
            }
        }

        /// <summary>
        /// Marks a reminder as triggered (shown to user).
        /// </summary>
        public async Task<bool> MarkReminderTriggeredAsync(Guid reminderId)
        {
            if (_context == null) return false;

            try
            {
                var reminder = await _context.Reminders.FindAsync(reminderId);
                if (reminder != null)
                {
                    reminder.Status = ReminderStatus.Triggered;
                    await _context.SaveChangesAsync();
                    _logger.Info("Marked reminder ID: {0} as triggered", reminderId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error marking reminder triggered");
                return false;
            }
        }

        /// <summary>
        /// Snoozes a reminder for the specified number of minutes.
        /// </summary>
        public async Task<bool> SnoozeReminderAsync(Guid reminderId, int snoozeMinutes)
        {
            if (_context == null) return false;

            try
            {
                var reminder = await _context.Reminders.FindAsync(reminderId);
                if (reminder != null)
                {
                    reminder.Status = ReminderStatus.Snoozed;
                    reminder.SnoozedUntil = DateTime.Now.AddMinutes(snoozeMinutes);
                    await _context.SaveChangesAsync();
                    _logger.Info("Snoozed reminder ID: {0} for {1} minutes", reminderId, snoozeMinutes);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error snoozing reminder");
                return false;
            }
        }

        /// <summary>
        /// Dismisses a reminder.
        /// </summary>
        public async Task<bool> DismissReminderAsync(Guid reminderId)
        {
            if (_context == null) return false;

            try
            {
                var reminder = await _context.Reminders.FindAsync(reminderId);
                if (reminder != null)
                {
                    reminder.Status = ReminderStatus.Dismissed;
                    await _context.SaveChangesAsync();
                    _logger.Info("Dismissed reminder ID: {0}", reminderId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error dismissing reminder");
                return false;
            }
        }

        /// <summary>
        /// Deletes a reminder.
        /// </summary>
        public async Task<bool> DeleteReminderAsync(Guid reminderId)
        {
            if (_context == null) return false;

            try
            {
                var reminder = await _context.Reminders.FindAsync(reminderId);
                if (reminder != null)
                {
                    _context.Reminders.Remove(reminder);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted reminder ID: {0}", reminderId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting reminder ID: {0}", reminderId);
                return false;
            }
        }

        /// <summary>
        /// Disposes the context if it was created by the factory.
        /// </summary>
        private void DisposeIfFactory(TrackerDbContext context)
        {
            if (context != _context && context is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}
