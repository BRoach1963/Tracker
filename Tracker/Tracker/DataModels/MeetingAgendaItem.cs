using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels;

/// <summary>
/// Represents an agenda item for a meeting.
/// Maps to Supabase meeting_agenda_items table (14 columns).
/// Note: This table does NOT have soft delete columns.
/// </summary>
[Table("meeting_agenda_items")]
public class MeetingAgendaItem
{
    /// <summary>
    /// Unique identifier for this agenda item.
    /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The meeting this agenda item belongs to.
    /// Maps to: meeting_id UUID NOT NULL
    /// </summary>
    [Required]
    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    /// <summary>
    /// Team member who added this agenda item.
    /// Maps to: added_by_team_member_id UUID NULL
    /// </summary>
    [Column("added_by_team_member_id")]
    public Guid? AddedByTeamMemberId { get; set; }

    /// <summary>
    /// Agenda item title.
    /// Maps to: title VARCHAR(300) NOT NULL
    /// </summary>
    [Required]
    [Column("title")]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notes or details for this agenda item.
    /// Maps to: notes TEXT NULL
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Order of this item in the agenda.
    /// Maps to: sort_order INT4 NOT NULL DEFAULT 0
    /// </summary>
    [Column("sort_order")]
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this item has been discussed.
    /// Maps to: is_discussed BOOLEAN NOT NULL DEFAULT false
    /// </summary>
    [Column("is_discussed")]
    public bool IsDiscussed { get; set; }

    /// <summary>
    /// When this item was discussed.
    /// Maps to: discussed_at TIMESTAMPTZ NULL
    /// </summary>
    [Column("discussed_at")]
    public DateTime? DiscussedAt { get; set; }

    /// <summary>
    /// Estimated time for this item in minutes.
    /// Maps to: time_estimate_minutes INT4 NULL
    /// </summary>
    [Column("time_estimate_minutes")]
    public int? TimeEstimateMinutes { get; set; }

    /// <summary>
    /// Actual time spent on this item in minutes.
    /// Maps to: actual_duration_minutes INT4 NULL
    /// </summary>
    [Column("actual_duration_minutes")]
    public int? ActualDurationMinutes { get; set; }

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

    #region Linked Entity (for discussing existing tasks/goals/metrics/projects)

    /// <summary>
    /// Type of entity being discussed in this agenda item.
    /// Maps to: linked_entity_type VARCHAR(50) NULL
    /// Values: 'task', 'goal', 'metric', 'project', or NULL for standalone items.
    /// </summary>
    [Column("linked_entity_type")]
    public string? LinkedEntityType { get; set; }

    /// <summary>
    /// ID of the entity being discussed.
    /// Maps to: linked_entity_id UUID NULL
    /// </summary>
    [Column("linked_entity_id")]
    public Guid? LinkedEntityId { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Navigation property for Meeting.
    /// </summary>
    [NotMapped]
    public virtual Meeting? Meeting { get; set; }

    /// <summary>
    /// Navigation property for AddedBy team member.
    /// </summary>
    [NotMapped]
    public virtual TeamMember? AddedBy { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this item is still pending discussion.
    /// </summary>
    [NotMapped]
    public bool IsPending => !IsDiscussed;

    /// <summary>
    /// Difference between estimated and actual time (positive = over time).
    /// </summary>
    [NotMapped]
    public int? TimeVariance => ActualDurationMinutes.HasValue && TimeEstimateMinutes.HasValue
        ? ActualDurationMinutes.Value - TimeEstimateMinutes.Value
        : null;

    /// <summary>
    /// Whether this agenda item is linked to an existing entity.
    /// </summary>
    [NotMapped]
    public bool HasLinkedEntity => LinkedEntityId.HasValue && !string.IsNullOrEmpty(LinkedEntityType);

    /// <summary>
    /// Whether this is a standalone agenda item (not linked to any entity).
    /// </summary>
    [NotMapped]
    public bool IsStandalone => !HasLinkedEntity;

    #endregion
}
