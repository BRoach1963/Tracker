using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Meeting note - personal notes for a meeting.
/// Maps to meeting_notes table in Supabase.
/// </summary>
[Table("meeting_notes")]
public class MeetingNote : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("author_team_member_id")]
    public Guid? AuthorTeamMemberId { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("is_private")]
    public bool IsPrivate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region Computed Properties

    /// <summary>
    /// Whether this note has content.
    /// </summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Preview of the content (first 100 chars).
    /// </summary>
    public string ContentPreview => Content.Length > 100 
        ? Content.Substring(0, 100) + "..." 
        : Content;

    /// <summary>
    /// Last updated display text.
    /// </summary>
    public string LastUpdatedDisplay => UpdatedAt.ToLocalTime().ToString("MMM d, h:mm tt");

    #endregion
}
