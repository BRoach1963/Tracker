using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a reminder/notification that will alert the user.
    /// </summary>
    public class Reminder : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Type of reminder (Meeting, Task, Goal, Engagement, Custom).
        /// </summary>
        public ReminderType Type { get; set; }

        /// <summary>
        /// Current status of the reminder.
        /// </summary>
        public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

        /// <summary>
        /// Title shown in the notification.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed message shown in the notification.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// When the reminder should fire.
        /// </summary>
        public DateTime DueDateTime { get; set; }

        /// <summary>
        /// If snoozed, when to remind again.
        /// </summary>
        public DateTime? SnoozedUntil { get; set; }

        /// <summary>
        /// Optional: Related 1:1 meeting ID.
        /// </summary>
        public int? OneOnOneId { get; set; }

        /// <summary>
        /// Optional: Related team member ID.
        /// </summary>
        public int? TeamMemberId { get; set; }

        /// <summary>
        /// Optional: Related task ID.
        /// </summary>
        public int? TaskId { get; set; }

        /// <summary>
        /// Optional: Related goal ID.
        /// </summary>
        public int? GoalId { get; set; }

        /// <summary>
        /// Whether this is a recurring reminder (e.g., engagement checks).
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// For recurring reminders, how often to repeat (in days).
        /// </summary>
        public int? RecurrenceIntervalDays { get; set; }

        /// <summary>
        /// Computed: Is the reminder due now or overdue?
        /// </summary>
        public bool IsDue => Status == ReminderStatus.Pending && 
                            DueDateTime <= DateTime.Now &&
                            (SnoozedUntil == null || SnoozedUntil <= DateTime.Now);

        /// <summary>
        /// Computed: Is snoozed and not yet due again?
        /// </summary>
        public bool IsSnoozed => Status == ReminderStatus.Snoozed && 
                                 SnoozedUntil.HasValue && 
                                 SnoozedUntil > DateTime.Now;
    }
}

