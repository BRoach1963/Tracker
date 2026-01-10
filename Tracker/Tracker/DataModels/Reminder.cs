using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a reminder/notification that will alert the user.
    /// Maps to Supabase 'reminders' table.
    /// Uses polymorphic entity references: entity_type + entity_id instead of separate FKs.
    /// </summary>
    public class Reminder : AuditableEntity
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The organization this reminder belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Who gets reminded.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Optional: Related team member.
        /// </summary>
        public Guid? TeamMemberId { get; set; }

        /// <summary>
        /// Type of reminder (e.g., Meeting, Task, Goal, Engagement, Custom).
        /// </summary>
        public ReminderType Type { get; set; }

        /// <summary>
        /// The type of entity being reminded about (meeting, task, goal, development_goal, etc.).
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the entity being reminded about.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Title shown in the notification.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed message shown in the notification.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// When the reminder should fire.
        /// </summary>
        public DateTime RemindAt { get; set; }

        /// <summary>
        /// For relative reminders (e.g., "15 min before meeting").
        /// </summary>
        public int? MinutesBefore { get; set; }

        /// <summary>
        /// Current status of the reminder (scheduled, sent, dismissed, snoozed).
        /// </summary>
        public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

        /// <summary>
        /// When the reminder was sent.
        /// </summary>
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// When the reminder was dismissed.
        /// </summary>
        public DateTime? DismissedAt { get; set; }

        /// <summary>
        /// If snoozed, when to remind again.
        /// </summary>
        public DateTime? SnoozedUntil { get; set; }

        /// <summary>
        /// Send as push notification.
        /// </summary>
        public bool SendPush { get; set; } = true;

        /// <summary>
        /// Send via email.
        /// </summary>
        public bool SendEmail { get; set; } = false;

        /// <summary>
        /// Send in-app notification.
        /// </summary>
        public bool SendInApp { get; set; } = true;

        /// <summary>
        /// Is this a recurring reminder.
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Recurrence rule in RRULE format (if recurring).
        /// </summary>
        public string? RecurrenceRule { get; set; }

        /// <summary>
        /// Computed: Is the reminder due now or overdue?
        /// </summary>
        public bool IsDue => Status == ReminderStatus.Pending && 
                            RemindAt <= DateTime.Now &&
                            (SnoozedUntil == null || SnoozedUntil <= DateTime.Now);

        /// <summary>
        /// Computed: Is snoozed and not yet due again?
        /// </summary>
        public bool IsSnoozed => Status == ReminderStatus.Snoozed && 
                                 SnoozedUntil.HasValue && 
                                 SnoozedUntil > DateTime.Now;
    }
}
