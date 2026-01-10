using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Models;

/// <summary>
/// Represents a user's connected calendar account.
/// Maps to Supabase calendar_links table.
/// </summary>
public class CalendarLink
{
    /// <summary>
    /// Unique identifier for this calendar link.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// User who owns this calendar link.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Calendar provider.
    /// </summary>
    [Required]
    public CalendarProvider Provider { get; set; }

    /// <summary>
    /// Account email.
    /// </summary>
    [MaxLength(255)]
    public string? AccountEmail { get; set; }

    /// <summary>
    /// Account display name.
    /// </summary>
    [MaxLength(200)]
    public string? AccountName { get; set; }

    /// <summary>
    /// Access token (encrypted).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token (encrypted).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// When the token expires.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// Whether this link is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether sync is enabled.
    /// </summary>
    public bool SyncEnabled { get; set; } = true;

    /// <summary>
    /// Whether to sync meetings to calendar.
    /// </summary>
    public bool SyncMeetingsToCalendar { get; set; } = true;

    /// <summary>
    /// Whether to sync tasks to calendar.
    /// </summary>
    public bool SyncTasksToCalendar { get; set; }

    /// <summary>
    /// Whether to create meetings from calendar events.
    /// </summary>
    public bool CreateMeetingFromCalendar { get; set; }

    /// <summary>
    /// Default calendar ID.
    /// </summary>
    [MaxLength(255)]
    public string? DefaultCalendarId { get; set; }

    /// <summary>
    /// Default calendar name.
    /// </summary>
    [MaxLength(200)]
    public string? DefaultCalendarName { get; set; }

    /// <summary>
    /// When last sync occurred.
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Status of last sync.
    /// </summary>
    public CalendarSyncStatus? LastSyncStatus { get; set; }

    /// <summary>
    /// Error from last sync (if failed).
    /// </summary>
    public string? LastSyncError { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties

    /// <summary>
    /// Whether the token is expired.
    /// </summary>
    [NotMapped]
    public bool IsTokenExpired => TokenExpiresAt.HasValue && TokenExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Whether sync can be performed.
    /// </summary>
    [NotMapped]
    public bool CanSync => IsActive && SyncEnabled && !IsTokenExpired;
}

/// <summary>
/// Represents a reminder for various entities.
/// Maps to Supabase reminders table.
/// </summary>
public class Reminder
{
    /// <summary>
    /// Unique identifier for this reminder.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// User to receive the reminder.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of reminder.
    /// </summary>
    [Required]
    public ReminderType ReminderType { get; set; }

    /// <summary>
    /// Reminder title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Reminder message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Type of related entity.
    /// </summary>
    [MaxLength(50)]
    public string? EntityType { get; set; }

    /// <summary>
    /// ID of related entity.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// When the reminder should be sent.
    /// </summary>
    [Required]
    public DateTime RemindAt { get; set; }

    /// <summary>
    /// Status of the reminder.
    /// </summary>
    [Required]
    public ReminderStatus Status { get; set; } = ReminderStatus.Scheduled;

    /// <summary>
    /// When the reminder was sent.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the reminder was dismissed.
    /// </summary>
    public DateTime? DismissedAt { get; set; }

    /// <summary>
    /// Snooze until time.
    /// </summary>
    public DateTime? SnoozedUntil { get; set; }

    /// <summary>
    /// How many times snoozed.
    /// </summary>
    public int SnoozeCount { get; set; }

    /// <summary>
    /// Whether to send notification in app.
    /// </summary>
    public bool NotifyInApp { get; set; } = true;

    /// <summary>
    /// Whether to send email notification.
    /// </summary>
    public bool NotifyEmail { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties

    /// <summary>
    /// Whether the reminder is due.
    /// </summary>
    [NotMapped]
    public bool IsDue => Status == ReminderStatus.Scheduled && RemindAt <= DateTime.UtcNow;

    /// <summary>
    /// Whether the reminder is snoozed and still pending.
    /// </summary>
    [NotMapped]
    public bool IsSnoozed => Status == ReminderStatus.Snoozed && 
                             SnoozedUntil.HasValue && 
                             SnoozedUntil.Value > DateTime.UtcNow;

    /// <summary>
    /// Whether the reminder is active (scheduled or snoozed).
    /// </summary>
    [NotMapped]
    public bool IsActive => Status == ReminderStatus.Scheduled || Status == ReminderStatus.Snoozed;
}
