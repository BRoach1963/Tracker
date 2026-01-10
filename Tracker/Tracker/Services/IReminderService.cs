using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.DataModels;

namespace Tracker.Services
{
    /// <summary>
    /// Interface for reminder management service.
    /// Enables unit testing by allowing mock implementations.
    /// </summary>
    public interface IReminderService : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when a reminder is triggered.
        /// </summary>
        event EventHandler<Reminder>? ReminderTriggered;

        /// <summary>
        /// Fired when engagement alerts are generated.
        /// </summary>
        event EventHandler<List<TeamMember>>? EngagementAlertTriggered;

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Starts the reminder service.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the reminder service.
        /// </summary>
        void Stop();

        /// <summary>
        /// Reloads settings and restarts if needed.
        /// </summary>
        void ReloadSettings();

        #endregion

        #region Reminder Creation Methods

        /// <summary>
        /// Creates a reminder for an upcoming meeting.
        /// </summary>
        Task CreateMeetingReminderAsync(OneOnOne meeting);

        /// <summary>
        /// Creates a reminder for a task deadline.
        /// </summary>
        Task CreateTaskReminderAsync(IndividualTask task);

        /// <summary>
        /// Creates a reminder for a goal deadline.
        /// </summary>
        Task CreateGoalReminderAsync(DevelopmentGoal goal);

        /// <summary>
        /// Creates a custom reminder.
        /// </summary>
        /// <param name="title">Reminder title</param>
        /// <param name="message">Reminder message</param>
        /// <param name="remindAt">When to trigger the reminder</param>
        /// <param name="teamMemberId">Optional associated team member</param>
        /// <param name="isRecurring">Whether the reminder repeats</param>
        /// <param name="recurrenceRule">RRULE format recurrence rule</param>
        /// <returns>The ID of the created reminder, or Guid.Empty on failure</returns>
        Task<Guid> CreateCustomReminderAsync(string title, string message, DateTime remindAt,
            Guid? teamMemberId = null, bool isRecurring = false, string? recurrenceRule = null);

        #endregion
    }
}
