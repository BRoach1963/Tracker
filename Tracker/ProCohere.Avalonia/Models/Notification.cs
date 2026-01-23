using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Notification model - maps to the notifications table in Supabase procohere schema.
/// In-app notifications for team members.
/// </summary>
[Table("notifications")]
public class Notification : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    #endregion

    #region Content

    /// <summary>
    /// Notification type: 'meeting_reminder', 'task_due', 'feedback_received', 'goal_update', etc.
    /// </summary>
    [Column("notification_type")]
    public string NotificationType { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    public string? Message { get; set; }

    #endregion

    #region Entity Link

    /// <summary>
    /// Type of related entity: 'meeting', 'task', 'goal', 'feedback', etc.
    /// </summary>
    [Column("entity_type")]
    public string? EntityType { get; set; }

    /// <summary>
    /// ID of the related entity for navigation.
    /// </summary>
    [Column("entity_id")]
    public Guid? EntityId { get; set; }

    #endregion

    #region Read Status

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this notification has a linked entity for navigation.
    /// </summary>
    public bool HasEntity => !string.IsNullOrEmpty(EntityType) && EntityId.HasValue;

    /// <summary>
    /// Relative time display (e.g., "5 minutes ago").
    /// </summary>
    public string TimeAgo
    {
        get
        {
            var elapsed = DateTime.UtcNow - CreatedAt;
            if (elapsed.TotalMinutes < 1) return "Just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
            return CreatedAt.ToString("MMM d");
        }
    }

    #endregion
}

/// <summary>
/// Notification type constants.
/// </summary>
public static class NotificationTypes
{
    public const string MeetingReminder = "meeting_reminder";
    public const string MeetingInvite = "meeting_invite";
    public const string TaskDue = "task_due";
    public const string TaskAssigned = "task_assigned";
    public const string FeedbackReceived = "feedback_received";
    public const string GoalUpdate = "goal_update";
    public const string ReviewPending = "review_pending";
    public const string KudosReceived = "kudos_received";
}
