using System;
using System.Collections.Generic;
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

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("author_id")]
    public Guid AuthorId { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("is_shared")]
    public bool IsShared { get; set; }

    /// <summary>
    /// Tags/categories assigned to this note for filtering.
    /// Stored as JSONB array of category strings (e.g., ["action", "decision"]).
    /// </summary>
    [Column("tags")]
    public List<string>? Tags { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #region Computed Properties

    /// <summary>
    /// Whether this note has content.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Preview of the content (first 100 chars).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string ContentPreview => Content.Length > 100 
        ? Content.Substring(0, 100) + "..." 
        : Content;

    /// <summary>
    /// Last updated display text.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string LastUpdatedDisplay => UpdatedAt.ToLocalTime().ToString("MMM d, h:mm tt");

    #endregion
}
