using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// NoteLink model - maps to the note_links table in Supabase procohere schema.
/// Represents a polymorphic link between a note and any entity type.
/// This enables a single note to link to multiple entities of different types.
/// </summary>
[Table("note_links")]
public class NoteLink : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("note_id")]
    public Guid NoteId { get; set; }

    #endregion

    #region Entity Reference

    /// <summary>
    /// The type of entity being linked. Uses note_link_entity_type enum in database.
    /// Valid values: meeting, team_member, goal, task, metric, target, project
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the linked entity.
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Snapshot of the entity's title at time of linking.
    /// Useful for display without needing to fetch the entity.
    /// </summary>
    [Column("entity_title_snapshot")]
    public string? EntityTitleSnapshot { get; set; }

    /// <summary>
    /// Semantic relationship type (e.g., "mentioned", "action_item", "reference").
    /// Optional - used for AI context and filtering.
    /// </summary>
    [Column("relationship_type")]
    public string? RelationshipType { get; set; }

    /// <summary>
    /// Sort order for UI display. Lower values appear first.
    /// </summary>
    [Column("sort_order")]
    public short SortOrder { get; set; }

    #endregion

    #region Audit

    /// <summary>
    /// The team member who created this link.
    /// References team_members.id (not auth.users.id).
    /// </summary>
    [Column("created_by_team_member_id")]
    public Guid CreatedByTeamMemberId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion
}

/// <summary>
/// Constants for entity types used in note_links.
/// These must match the note_link_entity_type enum in the database.
/// </summary>
public static class NoteLinkEntityTypes
{
    public const string Meeting = "meeting";
    public const string TeamMember = "team_member";
    public const string Goal = "goal";
    public const string Task = "task";
    public const string Metric = "metric";
    public const string Target = "target";
    public const string Project = "project";
}

/// <summary>
/// Constants for relationship types used in note_links.
/// These are semantic descriptors for how the entity relates to the note.
/// </summary>
public static class NoteLinkRelationshipTypes
{
    /// <summary>Entity was mentioned in the note content.</summary>
    public const string Mentioned = "mentioned";
    
    /// <summary>Note contains an action item for this entity.</summary>
    public const string ActionItem = "action_item";
    
    /// <summary>Note references this entity for context.</summary>
    public const string Reference = "reference";
    
    /// <summary>Note is a follow-up from this entity (e.g., meeting follow-up).</summary>
    public const string FollowUp = "follow_up";
}

