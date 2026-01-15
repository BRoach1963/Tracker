using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a reminder/notification that will alert the user.
    /// Maps to Supabase 'reminders' table.
    /// Uses polymorphic entity references: entity_type + entity_id.
    /// </summary>
    [Table("reminders")]
    public class Reminder
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this reminder belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// User who gets reminded.
        /// Maps to: user_id UUID NOT NULL
        /// </summary>
        [Column("user_id")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Optional: Related team member.
        /// Maps to: team_member_id UUID NULL
        /// </summary>
        [Column("team_member_id")]
        public Guid? TeamMemberId { get; set; }

        /// <summary>
        /// Type of reminder (stored as string).
        /// Maps to: reminder_type reminder_type (enum) NOT NULL
        /// </summary>
        [Column("reminder_type")]
        [MaxLength(50)]
        public string ReminderTypeString { get; set; } = "custom";

        /// <summary>
        /// Reminder type as enum.
        /// </summary>
        [NotMapped]
        public ReminderType Type
        {
            get => Enum.TryParse<ReminderType>(ReminderTypeString, true, out var result) ? result : ReminderType.Custom;
            set => ReminderTypeString = value.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// The type of entity being reminded about.
        /// Maps to: entity_type VARCHAR(50) NOT NULL
        /// </summary>
        [Column("entity_type")]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the entity being reminded about.
        /// Maps to: entity_id UUID NOT NULL
        /// </summary>
        [Column("entity_id")]
        public Guid EntityId { get; set; }

        /// <summary>
        /// Title shown in the notification.
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed message shown in the notification.
        /// Maps to: message TEXT NULL
        /// </summary>
        [Column("message")]
        public string? Message { get; set; }

        /// <summary>
        /// When the reminder should fire.
        /// Maps to: remind_at TIMESTAMPTZ NOT NULL
        /// </summary>
        [Column("remind_at")]
        public DateTime RemindAt { get; set; }

        /// <summary>
        /// Minutes before the event to trigger the reminder.
        /// Maps to: minutes_before INT4 NULL
        /// </summary>
        [Column("minutes_before")]
        public int? MinutesBefore { get; set; }

        /// <summary>
        /// Current status (stored as string).
        /// Maps to: status reminder_status (enum) NOT NULL DEFAULT 'scheduled'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string StatusString { get; set; } = "scheduled";

        /// <summary>
        /// Status as enum.
        /// </summary>
        [NotMapped]
        public ReminderStatus Status
        {
            get => StatusString switch
            {
                "scheduled" => ReminderStatus.Pending,
                "sent" => ReminderStatus.Sent,
                "dismissed" => ReminderStatus.Dismissed,
                "snoozed" => ReminderStatus.Snoozed,
                _ => ReminderStatus.Pending
            };
            set => StatusString = value switch
            {
                ReminderStatus.Pending => "scheduled",
                ReminderStatus.Sent => "sent",
                ReminderStatus.Dismissed => "dismissed",
                ReminderStatus.Snoozed => "snoozed",
                _ => "scheduled"
            };
        }

        /// <summary>
        /// When the reminder was sent.
        /// Maps to: sent_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// When the reminder was dismissed.
        /// Maps to: dismissed_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("dismissed_at")]
        public DateTime? DismissedAt { get; set; }

        /// <summary>
        /// If snoozed, when to remind again.
        /// Maps to: snoozed_until TIMESTAMPTZ NULL
        /// </summary>
        [Column("snoozed_until")]
        public DateTime? SnoozedUntil { get; set; }

        /// <summary>
        /// Send as push notification.
        /// Maps to: send_push BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("send_push")]
        public bool SendPush { get; set; } = true;

        /// <summary>
        /// Send via email.
        /// Maps to: send_email BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("send_email")]
        public bool SendEmail { get; set; } = false;

        /// <summary>
        /// Send in-app notification.
        /// Maps to: send_in_app BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("send_in_app")]
        public bool SendInApp { get; set; } = true;

        /// <summary>
        /// Is this a recurring reminder.
        /// Maps to: is_recurring BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_recurring")]
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Recurrence rule in RRULE format.
        /// Maps to: recurrence_rule VARCHAR(200) NULL
        /// </summary>
        [Column("recurrence_rule")]
        [MaxLength(200)]
        public string? RecurrenceRule { get; set; }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        [NotMapped]
        public Organization? Organization { get; set; }

        [NotMapped]
        public User? User { get; set; }

        [NotMapped]
        public TeamMember? TeamMember { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Is the reminder due now or overdue?
        /// </summary>
        [NotMapped]
        public bool IsDue => Status == ReminderStatus.Pending && 
                            RemindAt <= DateTime.Now &&
                            (SnoozedUntil == null || SnoozedUntil <= DateTime.Now);

        /// <summary>
        /// Is snoozed and not yet due again?
        /// </summary>
        [NotMapped]
        public bool IsSnoozed => Status == ReminderStatus.Snoozed && 
                                 SnoozedUntil.HasValue && 
                                 SnoozedUntil > DateTime.Now;

        #endregion
    }
}
