using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Meeting agenda item link - maps to meeting_agenda_item_links table.
/// Join table linking agenda items to other entities (goals, tasks, metrics).
/// Note: This table has NO soft-delete columns.
/// </summary>
[Table("meeting_agenda_item_links")]
public class MeetingAgendaItemLink : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_agenda_item_id")]
    public Guid MeetingAgendaItemId { get; set; }

    /// <summary>
    /// Type of link relationship.
    /// </summary>
    [Column("link_kind")]
    public string LinkKind { get; set; } = string.Empty;

    /// <summary>
    /// Type of linked entity: 'task', 'goal', 'metric', etc.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the linked entity.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Meeting prep item link - maps to meeting_prep_item_links table.
/// Join table linking prep items to other entities.
/// Note: This table has NO soft-delete columns.
/// </summary>
[Table("meeting_prep_item_links")]
public class MeetingPrepItemLink : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_prep_item_id")]
    public Guid MeetingPrepItemId { get; set; }

    /// <summary>
    /// Type of link relationship.
    /// </summary>
    [Column("link_kind")]
    public string LinkKind { get; set; } = string.Empty;

    /// <summary>
    /// Type of linked entity: 'task', 'goal', 'metric', etc.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the linked entity.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Meeting agenda scaffold - maps to meeting_agenda_scaffolds table.
/// Pre-built agenda structures that can be applied to meetings.
/// </summary>
[Table("meeting_agenda_scaffolds")]
public class MeetingAgendaScaffold : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Content

    /// <summary>
    /// Meeting type this scaffold applies to: 'one_on_one', 'team', etc.
    /// </summary>
    [Column("meeting_type")]
    public string MeetingType { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Scope: 'system', 'organization', 'personal'.
    /// </summary>
    [Column("scope")]
    public string Scope { get; set; } = "organization";

    /// <summary>
    /// FK to team_members who created this scaffold.
    /// </summary>
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

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
    /// Items in this scaffold (populated by service).
    /// </summary>
    public List<MeetingAgendaScaffoldItem> Items { get; set; } = new();

    #endregion

    #region Computed

    public bool IsSystemScaffold => Scope == "system";
    public bool CanEdit => Scope != "system";

    #endregion
}

/// <summary>
/// Meeting agenda scaffold item - maps to meeting_agenda_scaffold_items table.
/// Individual items within an agenda scaffold template.
/// </summary>
[Table("meeting_agenda_scaffold_items")]
public class MeetingAgendaScaffoldItem : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// FK to parent scaffold.
    /// </summary>
    [Column("scaffold_id")]
    public Guid ScaffoldId { get; set; }

    #endregion

    #region Content

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Display order within scaffold.
    /// </summary>
    [Column("sort_order")]
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether items created from this are private by default.
    /// </summary>
    [Column("default_is_private")]
    public bool DefaultIsPrivate { get; set; }

    /// <summary>
    /// Target kind for the agenda item.
    /// </summary>
    [Column("target_kind")]
    public string TargetKind { get; set; } = string.Empty;

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
/// Vector embedding - maps to vector_embeddings table.
/// Stores vector embeddings for semantic search (RAG).
/// Infrastructure table - typically not used directly by app code.
/// </summary>
[Table("vector_embeddings")]
public class VectorEmbedding : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Entity Reference

    /// <summary>
    /// Type of embedded entity: 'note', 'meeting_note', 'feedback', etc.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the embedded entity.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Index of this chunk (for long content split into multiple embeddings).
    /// </summary>
    [Column("chunk_index")]
    public int ChunkIndex { get; set; }

    #endregion

    #region Content

    /// <summary>
    /// Hash of the content to detect changes.
    /// </summary>
    [Column("content_hash")]
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Preview of the content (first N characters).
    /// </summary>
    [Column("content_preview")]
    public string? ContentPreview { get; set; }

    /// <summary>
    /// Full text content that was embedded.
    /// </summary>
    [Column("content")]
    public string? Content { get; set; }

    #endregion

    #region Embedding

    /// <summary>
    /// The vector embedding (pgvector type).
    /// Note: Postgrest returns this as string, handle conversion in service.
    /// </summary>
    [Column("embedding")]
    public string? Embedding { get; set; }

    /// <summary>
    /// Number of dimensions in the embedding.
    /// </summary>
    [Column("embedding_dimensions")]
    public int EmbeddingDimensions { get; set; }

    /// <summary>
    /// Name of the embedding model used.
    /// </summary>
    [Column("model_name")]
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Version of the embedding model.
    /// </summary>
    [Column("model_version")]
    public string? ModelVersion { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    [Column("metadata")]
    public string? Metadata { get; set; }

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
