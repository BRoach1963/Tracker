using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Outcome record for an agenda item discussion.
/// Maps to procohere.agenda_item_outcomes table.
/// </summary>
[Table("agenda_item_outcomes")]
public class AgendaItemOutcomeDetail : BaseModel
{
    #region Identity

    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this outcome belongs to.
    /// </summary>
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The agenda item this outcome is for.
    /// </summary>
    [Column("agenda_item_id")]
    public Guid AgendaItemId { get; set; }

    #endregion

    #region Outcome Data

    /// <summary>
    /// Type of outcome: task_created, goal_created, goal_updated, follow_up_scheduled,
    /// decision_recorded, feedback_captured, notes_added.
    /// </summary>
    [Column("outcome_type")]
    public string OutcomeType { get; set; } = string.Empty;

    /// <summary>
    /// For entity-creating outcomes, the type of entity created (task, goal, meeting).
    /// </summary>
    [Column("linked_entity_type")]
    public string? LinkedEntityType { get; set; }

    /// <summary>
    /// For entity-creating outcomes, the ID of the created entity.
    /// </summary>
    [Column("linked_entity_id")]
    public Guid? LinkedEntityId { get; set; }

    /// <summary>
    /// For content outcomes (decision, feedback, notes), the actual content.
    /// </summary>
    [Column("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Who can see this outcome: private, attendees, team, organization.
    /// </summary>
    [Column("visibility")]
    public string Visibility { get; set; } = OutcomeVisibility.Attendees;

    #endregion

    #region Metadata

    /// <summary>
    /// Team member who created this outcome.
    /// </summary>
    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Whether this outcome has been soft-deleted.
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this outcome was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this outcome was last updated.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Display-friendly name for the outcome type.
    /// </summary>
    public string OutcomeTypeDisplay => Models.OutcomeType.GetDisplayName(OutcomeType);

    /// <summary>
    /// Icon path data for the outcome type.
    /// </summary>
    public string OutcomeTypeIcon => Models.OutcomeType.GetIcon(OutcomeType);

    /// <summary>
    /// Color for the outcome type badge.
    /// </summary>
    public string OutcomeTypeColor => Models.OutcomeType.GetColor(OutcomeType);

    /// <summary>
    /// Whether this outcome links to another entity.
    /// </summary>
    public bool HasLinkedEntity => !string.IsNullOrEmpty(LinkedEntityType) && LinkedEntityId.HasValue;

    /// <summary>
    /// Whether this outcome has inline content.
    /// </summary>
    public bool HasContent => !string.IsNullOrEmpty(Content);

    /// <summary>
    /// Display-friendly name for the visibility level.
    /// </summary>
    public string VisibilityDisplay => OutcomeVisibility.GetDisplayName(Visibility);

    /// <summary>
    /// Display-friendly name for the linked entity type.
    /// </summary>
    public string LinkedEntityTypeDisplay => LinkedEntityType?.ToLower() switch
    {
        "task" => "Task",
        "goal" => "Goal",
        "meeting" => "Meeting",
        "metric" => "Metric",
        _ => LinkedEntityType ?? string.Empty
    };

    /// <summary>
    /// Formatted creation time for display.
    /// </summary>
    public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("MMM d, yyyy h:mm tt");

    /// <summary>
    /// Short formatted creation time.
    /// </summary>
    public string CreatedAtShort => CreatedAt.ToLocalTime().ToString("MMM d");

    /// <summary>
    /// Content preview (first 100 chars) for list display.
    /// </summary>
    public string ContentPreview => Content?.Length > 100 
        ? Content.Substring(0, 100) + "..." 
        : Content ?? string.Empty;

    /// <summary>
    /// Whether this is a content-type outcome (decision, feedback, notes).
    /// </summary>
    public bool IsContentOutcome => OutcomeType is 
        Models.OutcomeType.DecisionRecorded or 
        Models.OutcomeType.FeedbackCaptured or 
        Models.OutcomeType.NotesAdded;

    /// <summary>
    /// Whether this is an entity-creating outcome (task, goal, meeting).
    /// </summary>
    public bool IsEntityOutcome => OutcomeType is 
        Models.OutcomeType.TaskCreated or 
        Models.OutcomeType.GoalCreated or 
        Models.OutcomeType.GoalUpdated or 
        Models.OutcomeType.FollowUpScheduled;

    #endregion
}
