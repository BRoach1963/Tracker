using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents a scheduled reminder that will alert the user.
/// Maps to Supabase 'reminders' table.
/// Uses polymorphic entity references: entity_type + entity_id.
/// </summary>
[Table("reminders")]
public class Reminder : BaseModel
{
    /// <summary>
    /// Primary key (UUID).
    /// </summary>
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Organization this reminder belongs to.
    /// </summary>
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// User who gets reminded.
    /// </summary>
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional: Related team member (for engagement reminders).
    /// </summary>
    [Column("team_member_id")]
    public Guid? TeamMemberId { get; set; }

    /// <summary>
    /// Type of reminder (stored as string in database).
    /// </summary>
    [Column("reminder_type")]
    public string ReminderTypeString { get; set; } = "custom";

    /// <summary>
    /// Reminder type as enum.
    /// </summary>
    [JsonIgnore]
    public ReminderType Type
    {
        get => Enum.TryParse<ReminderType>(ReminderTypeString, true, out var result) ? result : ReminderType.Custom;
        set => ReminderTypeString = value.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// The type of entity being reminded about (meeting, task, goal, custom).
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity being reminded about.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Title shown in the notification.
    /// </summary>
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed message shown in the notification.
    /// </summary>
    [Column("message")]
    public string? Message { get; set; }

    /// <summary>
    /// When the reminder should fire (UTC).
    /// </summary>
    [Column("remind_at")]
    public DateTime RemindAt { get; set; }

    /// <summary>
    /// Minutes before the event to trigger the reminder.
    /// </summary>
    [Column("minutes_before")]
    public int? MinutesBefore { get; set; }

    /// <summary>
    /// Current status (stored as string in database).
    /// </summary>
    [Column("status")]
    public string StatusString { get; set; } = "scheduled";

    /// <summary>
    /// Status as enum.
    /// </summary>
    [JsonIgnore]
    public ReminderStatus Status
    {
        get => StatusString switch
        {
            "scheduled" => ReminderStatus.Pending,
            "sent" => ReminderStatus.Sent,
            "triggered" => ReminderStatus.Triggered,
            "dismissed" => ReminderStatus.Dismissed,
            "snoozed" => ReminderStatus.Snoozed,
            "cancelled" => ReminderStatus.Cancelled,
            _ => ReminderStatus.Pending
        };
        set => StatusString = value switch
        {
            ReminderStatus.Pending => "scheduled",
            ReminderStatus.Sent => "sent",
            ReminderStatus.Triggered => "triggered",
            ReminderStatus.Dismissed => "dismissed",
            ReminderStatus.Snoozed => "snoozed",
            ReminderStatus.Cancelled => "cancelled",
            _ => "scheduled"
        };
    }

    /// <summary>
    /// When the reminder was sent.
    /// </summary>
    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the reminder was dismissed.
    /// </summary>
    [Column("dismissed_at")]
    public DateTime? DismissedAt { get; set; }

    /// <summary>
    /// If snoozed, when to remind again.
    /// </summary>
    [Column("snoozed_until")]
    public DateTime? SnoozedUntil { get; set; }

    /// <summary>
    /// Send as push notification (native Windows toast).
    /// </summary>
    [Column("send_push")]
    public bool SendPush { get; set; } = true;

    /// <summary>
    /// Send via email.
    /// </summary>
    [Column("send_email")]
    public bool SendEmail { get; set; } = false;

    /// <summary>
    /// Send in-app notification (ProCohereToast).
    /// </summary>
    [Column("send_in_app")]
    public bool SendInApp { get; set; } = true;

    /// <summary>
    /// Is this a recurring reminder.
    /// </summary>
    [Column("is_recurring")]
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Recurrence rule in RRULE format (e.g., "FREQ=DAILY;INTERVAL=1").
    /// </summary>
    [Column("recurrence_rule")]
    public string? RecurrenceRule { get; set; }

    /// <summary>
    /// When created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When last modified.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When soft deleted.
    /// </summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who soft deleted.
    /// </summary>
    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #region Computed Properties

    /// <summary>
    /// Is the reminder due (pending and past remind_at time).
    /// </summary>
    [JsonIgnore]
    public bool IsDue => Status == ReminderStatus.Pending && RemindAt <= DateTime.UtcNow;

    /// <summary>
    /// Is the reminder currently snoozed and snooze time not yet reached.
    /// </summary>
    [JsonIgnore]
    public bool IsActivelySnoozed => Status == ReminderStatus.Snoozed && SnoozedUntil.HasValue && SnoozedUntil.Value > DateTime.UtcNow;

    /// <summary>
    /// Should the snoozed reminder fire now (snooze time has passed).
    /// </summary>
    [JsonIgnore]
    public bool IsSnoozeDue => Status == ReminderStatus.Snoozed && SnoozedUntil.HasValue && SnoozedUntil.Value <= DateTime.UtcNow;

    #endregion
}
