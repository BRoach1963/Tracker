using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents an activity log entry (audit trail).
/// Maps to Supabase activity_log table.
/// </summary>
public class ActivityLog
{
    /// <summary>
    /// Unique identifier for this log entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this activity belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// User who performed the action.
    /// </summary>
    [Required]
    public Guid ActorUserId { get; set; }

    /// <summary>
    /// Team member who performed the action (if applicable).
    /// </summary>
    public Guid? ActorTeamMemberId { get; set; }

    /// <summary>
    /// Action performed (created, updated, deleted, assigned, completed, etc.).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected (task, goal, feedback, meeting, etc.).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity.
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Snapshot of entity name for display.
    /// </summary>
    [MaxLength(300)]
    public string? EntityName { get; set; }

    /// <summary>
    /// Previous values (stored as JSON).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (stored as JSON).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Context type (bulk_update, api, ui, automation).
    /// </summary>
    [MaxLength(50)]
    public string? ContextType { get; set; }

    /// <summary>
    /// IP address of the request.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When this activity occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(ActorTeamMemberId))]
    public virtual TeamMember? ActorTeamMember { get; set; }
}

/// <summary>
/// Represents an in-app notification.
/// Maps to Supabase notifications table.
/// </summary>
public class Notification
{
    /// <summary>
    /// Unique identifier for this notification.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this notification belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// User to receive the notification.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of notification (task_assigned, feedback_received, etc.).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Notification title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

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
    /// Deep link in app.
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Notification priority.
    /// </summary>
    [Required]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Whether the notification has been read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When the notification was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Whether the notification has been dismissed.
    /// </summary>
    public bool IsDismissed { get; set; }

    /// <summary>
    /// When the notification was dismissed.
    /// </summary>
    public DateTime? DismissedAt { get; set; }

    /// <summary>
    /// Whether an email was sent.
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// When the email was sent.
    /// </summary>
    public DateTime? EmailSentAt { get; set; }

    /// <summary>
    /// When the notification expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When this notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    // Computed properties

    /// <summary>
    /// Whether the notification is expired.
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Whether the notification is active (unread and not dismissed).
    /// </summary>
    [NotMapped]
    public bool IsActive => !IsRead && !IsDismissed && !IsExpired;
}

/// <summary>
/// Represents user preferences for notifications.
/// Maps to Supabase notification_preferences table.
/// </summary>
public class NotificationPreference
{
    /// <summary>
    /// Unique identifier for this preference.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// User this preference belongs to.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of notification (task_assigned, feedback_received, etc.).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Whether in-app notifications are enabled.
    /// </summary>
    public bool InAppEnabled { get; set; } = true;

    /// <summary>
    /// Whether email notifications are enabled.
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// Whether push notifications are enabled.
    /// </summary>
    public bool PushEnabled { get; set; }

    /// <summary>
    /// Frequency for email digests.
    /// </summary>
    public EmailFrequency EmailFrequency { get; set; } = EmailFrequency.Immediate;

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an organization-wide announcement.
/// Maps to Supabase announcements table.
/// </summary>
public class Announcement
{
    /// <summary>
    /// Unique identifier for this announcement.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this announcement belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// User who created the announcement.
    /// </summary>
    [Required]
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Announcement title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Announcement content.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Target type (organization, team, role).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TargetType { get; set; } = "organization";

    /// <summary>
    /// Target team ID (if targeting a team).
    /// </summary>
    public Guid? TargetTeamId { get; set; }

    /// <summary>
    /// Target role IDs (stored as JSON array).
    /// </summary>
    public string? TargetRoleIds { get; set; }

    /// <summary>
    /// When to publish the announcement.
    /// </summary>
    public DateTime PublishAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the announcement expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this is pinned.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Whether this is a draft.
    /// </summary>
    public bool IsDraft { get; set; } = true;

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this announcement is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this announcement was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(TargetTeamId))]
    public virtual Team? TargetTeam { get; set; }

    // Computed properties

    /// <summary>
    /// Whether the announcement is currently published.
    /// </summary>
    [NotMapped]
    public bool IsPublished => !IsDraft && PublishAt <= DateTime.UtcNow;

    /// <summary>
    /// Whether the announcement is expired.
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Whether the announcement is active (published and not expired).
    /// </summary>
    [NotMapped]
    public bool IsActive => IsPublished && !IsExpired && !IsDeleted;
}
