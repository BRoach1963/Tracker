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
    /// Entity links loaded from note_links table.
    /// Not mapped to database - populated by service layer.
    /// </summary>
    public List<NoteLink> Links { get; set; } = new();

    #endregion

    #region Project Link
    
    /// <summary>
    /// ID of the linked project (populated from project_links table).
    /// Not a DB column - set by service when fetching notes.
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    /// <summary>
    /// Title of the linked project (for display).
    /// Not a DB column - set by service when fetching notes.
    /// </summary>
    public string? ProjectTitle { get; set; }
    
    /// <summary>
    /// Whether this note is linked to a project.
    /// </summary>
    public bool HasProject => ProjectId.HasValue;
    
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
    /// Whether this note has any tags.
    /// </summary>
    public bool HasTags => Tags != null && Tags.Count > 0;

    /// <summary>
    /// Whether this note has any entity links.
    /// </summary>
    public bool HasLinks => Links.Count > 0;

    /// <summary>
    /// Count of linked entities.
    /// </summary>
    public int LinkCount => Links.Count;

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

    /// <summary>
    /// Human-friendly timestamp like "2h ago", "Yesterday", "Jan 15".
    /// </summary>
    public string DisplayTimestamp
    {
        get
        {
            var now = DateTime.UtcNow;
            var diff = now - CreatedAt;

            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 2)
                return "Yesterday";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";
            if (CreatedAt.Year == now.Year)
                return CreatedAt.ToString("MMM d");
            return CreatedAt.ToString("MMM d, yyyy");
        }
    }

    /// <summary>
    /// Extended content preview for tooltip (first 500 chars).
    /// </summary>
    public string ContentPreviewExtended => Content.Length > 500
        ? Content[..500] + "..."
        : Content;

    #endregion
}
