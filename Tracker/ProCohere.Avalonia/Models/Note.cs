using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Note model - maps to the notes table in Supabase procohere schema.
/// Represents a journal entry that can optionally be linked to various entities.
/// </summary>
[Table("notes")]
public class Note : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The team member who authored this note.
    /// Maps to author_team_member_id in database.
    /// </summary>
    [Column("author_team_member_id")]
    public Guid AuthorTeamMemberId { get; set; }

    #endregion

    #region Content

    [Column("title")]
    public string? Title { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Format of the content: 'plain', 'markdown', 'html', etc.
    /// </summary>
    [Column("content_format")]
    public string ContentFormat { get; set; } = "plain";

    /// <summary>
    /// Category for organizing notes.
    /// </summary>
    [Column("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Tags stored as JSONB array in database.
    /// </summary>
    [Column("tags")]
    public List<string>? Tags { get; set; }

    #endregion

    #region Entity Links

    /// <summary>
    /// Link to a team member (e.g., note about a direct report).
    /// </summary>
    [Column("linked_team_member_id")]
    public Guid? LinkedTeamMemberId { get; set; }

    /// <summary>
    /// Link to a meeting.
    /// </summary>
    [Column("linked_meeting_id")]
    public Guid? LinkedMeetingId { get; set; }

    /// <summary>
    /// Link to a project.
    /// </summary>
    [Column("linked_project_id")]
    public Guid? LinkedProjectId { get; set; }

    /// <summary>
    /// Link to a goal.
    /// </summary>
    [Column("linked_goal_id")]
    public Guid? LinkedGoalId { get; set; }

    /// <summary>
    /// Link to a task.
    /// </summary>
    [Column("linked_task_id")]
    public Guid? LinkedTaskId { get; set; }

    #endregion

    #region Status Flags

    [Column("is_private")]
    public bool IsPrivate { get; set; } = true;

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("pinned_at")]
    public DateTime? PinnedAt { get; set; }

    [Column("is_archived")]
    public bool IsArchived { get; set; }

    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }

    #endregion

    #region AI Fields

    [Column("ai_summary")]
    public string? AiSummary { get; set; }

    [Column("ai_suggested_actions")]
    public List<string>? AiSuggestedActions { get; set; }

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

    #region Sync Fields

    [Column("sync_id")]
    public Guid? SyncId { get; set; }

    [Column("sync_version")]
    public int SyncVersion { get; set; } = 1;

    [Column("sync_modified_at")]
    public DateTime? SyncModifiedAt { get; set; }

    [Column("sync_status")]
    public string SyncStatus { get; set; } = "synced";

    #endregion

    #region Computed Properties (not mapped to DB)

    /// <summary>
    /// Whether this note has any entity links.
    /// </summary>
    public bool HasLinks => LinkedTeamMemberId.HasValue ||
                           LinkedMeetingId.HasValue ||
                           LinkedProjectId.HasValue ||
                           LinkedGoalId.HasValue ||
                           LinkedTaskId.HasValue;

    /// <summary>
    /// Count of linked entities.
    /// </summary>
    public int LinkCount => (LinkedTeamMemberId.HasValue ? 1 : 0) +
                           (LinkedMeetingId.HasValue ? 1 : 0) +
                           (LinkedProjectId.HasValue ? 1 : 0) +
                           (LinkedGoalId.HasValue ? 1 : 0) +
                           (LinkedTaskId.HasValue ? 1 : 0);

    /// <summary>
    /// Display title - uses title if set, otherwise first 50 chars of content.
    /// </summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title)
        ? (Content.Length > 50 ? Content[..50] + "..." : Content)
        : Title;

    /// <summary>
    /// Content preview - first 200 characters.
    /// </summary>
    public string ContentPreview => Content.Length > 200
        ? Content[..200] + "..."
        : Content;

    /// <summary>
    /// Display name for the author (populated by service layer).
    /// </summary>
    public string? AuthorName { get; set; }

    #endregion
}
