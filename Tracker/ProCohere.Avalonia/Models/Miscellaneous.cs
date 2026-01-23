using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Activity feed entry - maps to the activity_feed table in Supabase procohere schema.
/// User-facing activity stream (different from audit_log which is system-level).
/// </summary>
[Table("activity_feed")]
public class ActivityFeed : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// FK to team_members - who performed the action.
    /// </summary>
    [Column("actor_id")]
    public Guid ActorId { get; set; }

    #endregion

    #region Activity Details

    /// <summary>
    /// Action: 'created', 'updated', 'completed', 'commented', etc.
    /// </summary>
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity: 'goal', 'task', 'meeting', 'feedback', etc.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Display title of the entity at time of activity.
    /// </summary>
    [Column("entity_title")]
    public string? EntityTitle { get; set; }

    /// <summary>
    /// Additional context as JSON.
    /// </summary>
    [Column("metadata")]
    public string? Metadata { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    #endregion

    #region Timestamp

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    #endregion

    #region Computed Properties

    public string ActionDisplay => Action switch
    {
        "created" => "created",
        "updated" => "updated",
        "completed" => "completed",
        "commented" => "commented on",
        "assigned" => "assigned",
        _ => Action
    };

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
/// Comment model - maps to the comments table in Supabase procohere schema.
/// Polymorphic comments on any entity type. Supports threaded replies.
/// </summary>
[Table("comments")]
public class Comment : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// FK to team_members - comment author.
    /// </summary>
    [Column("author_id")]
    public Guid AuthorId { get; set; }

    #endregion

    #region Entity Link

    /// <summary>
    /// Type of entity: 'goal', 'task', 'meeting', 'note', etc.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity this comment is on.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    #endregion

    #region Threading

    /// <summary>
    /// Parent comment for replies (null if top-level).
    /// </summary>
    [Column("parent_comment_id")]
    public Guid? ParentCommentId { get; set; }

    #endregion

    #region Content

    [Column("content")]
    public string Content { get; set; } = string.Empty;

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

    #region Navigation (not mapped)

    /// <summary>
    /// Replies to this comment (populated by service).
    /// </summary>
    public List<Comment> Replies { get; set; } = new();

    #endregion

    #region Computed Properties

    public bool IsReply => ParentCommentId.HasValue;

    public bool HasReplies => Replies.Count > 0;

    #endregion
}

/// <summary>
/// Entity tag association - maps to entity_tags table in Supabase procohere schema.
/// Join table linking tags to any entity type (polymorphic).
/// </summary>
[Table("entity_tags")]
public class EntityTag : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("tag_id")]
    public Guid TagId { get; set; }

    #endregion

    #region Entity Link

    /// <summary>
    /// Type of entity: 'goal', 'task', 'meeting', 'note', etc.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the tagged entity.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

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

    #region Navigation (not mapped)

    /// <summary>
    /// The tag (populated by service).
    /// </summary>
    public Tag? Tag { get; set; }

    #endregion
}

/// <summary>
/// Goal category - maps to goal_categories table in Supabase procohere schema.
/// Organization-defined categories for grouping goals.
/// </summary>
[Table("goal_categories")]
public class GoalCategory : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Content

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Color hex code for display.
    /// </summary>
    [Column("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    [Column("sort_order")]
    public int SortOrder { get; set; }

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
}

/// <summary>
/// Goal template - maps to goal_templates table in Supabase procohere schema.
/// Reusable templates for creating goals with predefined structure.
/// </summary>
[Table("goal_templates")]
public class GoalTemplate : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// FK to goal_categories.
    /// </summary>
    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    #endregion

    #region Content

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Goal type: 'individual', 'team', 'company'.
    /// </summary>
    [Column("goal_type")]
    public string GoalType { get; set; } = "individual";

    /// <summary>
    /// Default targets as JSON array.
    /// </summary>
    [Column("default_targets")]
    public string? DefaultTargets { get; set; }

    /// <summary>
    /// Whether this is a system template (cannot be deleted).
    /// </summary>
    [Column("is_system_template")]
    public bool IsSystemTemplate { get; set; }

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

    public bool CanEdit => !IsSystemTemplate;
    public bool CanDelete => !IsSystemTemplate;

    #endregion
}
