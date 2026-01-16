using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Core.DataModels;

/// <summary>
/// Represents notes from a meeting.
/// Maps to Supabase meeting_notes table.
/// Note: This table does NOT have soft delete columns.
/// </summary>
[Table("meeting_notes")]
public class MeetingNote
{
    /// <summary>
    /// Primary key (UUID).
    /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The meeting this note belongs to.
    /// Maps to: meeting_id UUID NOT NULL
    /// </summary>
    [Required]
    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    /// <summary>
    /// Team member who wrote this note.
    /// Maps to: author_team_member_id UUID NULL
    /// </summary>
    [Column("author_team_member_id")]
    public Guid? AuthorTeamMemberId { get; set; }

    /// <summary>
    /// Content of the note.
    /// Maps to: content TEXT NOT NULL
    /// </summary>
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether this note is private (only visible to author).
    /// Maps to: is_private BOOLEAN NOT NULL DEFAULT false
    /// </summary>
    [Column("is_private")]
    public bool IsPrivate { get; set; }

    /// <summary>
    /// AI-generated summary of the note.
    /// Maps to: ai_summary TEXT NULL
    /// </summary>
    [Column("ai_summary")]
    public string? AiSummary { get; set; }

    /// <summary>
    /// When this record was created.
    /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    #region Navigation Properties

    /// <summary>
    /// Navigation property for Meeting.
    /// </summary>
    [NotMapped]
    public virtual Meeting? Meeting { get; set; }

    /// <summary>
    /// Navigation property for Author team member.
    /// </summary>
    [NotMapped]
    public virtual TeamMember? Author { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this note has an AI summary.
    /// </summary>
    [NotMapped]
    public bool HasAiSummary => !string.IsNullOrWhiteSpace(AiSummary);

    /// <summary>
    /// A truncated preview of the content.
    /// </summary>
    [NotMapped]
    public string ContentPreview => Content.Length > 100 ? Content[..97] + "..." : Content;

    #endregion
}
