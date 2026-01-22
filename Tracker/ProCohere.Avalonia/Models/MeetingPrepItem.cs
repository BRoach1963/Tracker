using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Meeting prep item - maps to procohere.meeting_prep_items table.
/// Prep items support personal, assigned, and team-wide visibility scopes.
/// 
/// Editing rules (enforce in UI):
/// - Requester can edit title/body and can change assignment and status
/// - Assignee can update status and assignee_notes, but cannot edit title/body
/// - If user is neither requester nor assignee, show read-only
/// </summary>
[Table("meeting_prep_items")]
public class MeetingPrepItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    /// <summary>
    /// Who created/requested this prep item.
    /// </summary>
    [Column("requested_by_team_member_id")]
    public Guid RequestedByTeamMemberId { get; set; }

    /// <summary>
    /// Who this prep item is assigned to (null = unassigned).
    /// For 'assigned' visibility, this is the person who needs to complete it.
    /// </summary>
    [Column("assigned_to_team_member_id")]
    public Guid? AssignedToTeamMemberId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description/body of the prep item.
    /// </summary>
    [Column("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Notes from the assignee (only assignee can edit this field).
    /// </summary>
    [Column("assignee_notes")]
    public string? AssigneeNotes { get; set; }

    /// <summary>
    /// Visibility scope: 'personal', 'assigned', 'meeting'.
    /// - personal: Only visible to the requester
    /// - assigned: Visible to requester AND assignee only
    /// - meeting: Visible to all attendees (team prep)
    /// </summary>
    [Column("visibility_scope")]
    public string VisibilityScope { get; set; } = "personal";

    /// <summary>
    /// Status: 'open', 'in_progress', 'done', 'dismissed'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "open";

    /// <summary>
    /// When the status was last updated.
    /// </summary>
    [Column("status_updated_at")]
    public DateTime? StatusUpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the status.
    /// </summary>
    [Column("status_updated_by_team_member_id")]
    public Guid? StatusUpdatedByTeamMemberId { get; set; }

    /// <summary>
    /// Whether the status was manually overridden (e.g., requester marked done on behalf of assignee).
    /// </summary>
    [Column("overridden_status")]
    public bool OverriddenStatus { get; set; }

    /// <summary>
    /// When this prep item is due.
    /// </summary>
    [Column("due_at")]
    public DateTime? DueAt { get; set; }

    /// <summary>
    /// When this prep item was completed.
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Who completed this prep item.
    /// </summary>
    [Column("completed_by_team_member_id")]
    public Guid? CompletedByTeamMemberId { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this item should be carried forward to future meetings.
    /// </summary>
    [Column("carry_forward")]
    public bool CarryForward { get; set; }

    /// <summary>
    /// If this was carried forward, points to the original prep item.
    /// </summary>
    [Column("carried_from_prep_item_id")]
    public Guid? CarriedFromPrepItemId { get; set; }

    /// <summary>
    /// Source type for provenance (e.g., 'manual', 'scaffold', 'ai').
    /// </summary>
    [Column("source_type")]
    public string? SourceType { get; set; }

    /// <summary>
    /// Snapshot of source data for provenance tracking.
    /// </summary>
    [Column("source_snapshot")]
    public string? SourceSnapshot { get; set; }

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

    #region Non-DB Properties (set by service)

    /// <summary>
    /// Name of the person who requested this item (set by service).
    /// </summary>
    public string RequestedByName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the assignee (set by service). Empty if unassigned.
    /// </summary>
    public string AssignedToName { get; set; } = string.Empty;

    /// <summary>
    /// Alias for RequestedByName (for XAML binding convenience).
    /// </summary>
    public string RequesterName => RequestedByName;

    /// <summary>
    /// Alias for AssignedToName (for XAML binding convenience).
    /// </summary>
    public string AssigneeName => AssignedToName;

    /// <summary>
    /// Alias for Body (for XAML binding convenience - consistent with other models using Description).
    /// </summary>
    public string? Description => Body;

    /// <summary>
    /// Whether this prep item has a linked entity (future feature - currently always false).
    /// Prep items don't support entity linking yet; use this stub for XAML binding.
    /// </summary>
    public bool HasLinkedEntity => false;

    /// <summary>
    /// Display text for linked entity type (future feature - currently empty).
    /// </summary>
    public string LinkedEntityTypeDisplay => string.Empty;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this is a personal prep item (only visible to requester).
    /// </summary>
    public bool IsPersonal => VisibilityScope == "personal";

    /// <summary>
    /// Whether this is an assigned prep item (visible to requester + assignee).
    /// </summary>
    public bool IsAssigned => VisibilityScope == "assigned";

    /// <summary>
    /// Whether this is team/meeting prep (visible to all attendees).
    /// </summary>
    public bool IsTeamPrep => VisibilityScope == "meeting";

    /// <summary>
    /// Display text for status.
    /// </summary>
    public string StatusDisplay => Status switch
    {
        "open" => "Open",
        "in_progress" => "In Progress",
        "done" => "Done",
        "dismissed" => "Dismissed",
        _ => Status
    };

    /// <summary>
    /// Color for status indicator.
    /// </summary>
    public string StatusColor => Status switch
    {
        "open" => "#9CA3AF",       // Gray
        "in_progress" => "#F59E0B", // Amber
        "done" => "#10B981",        // Green
        "dismissed" => "#6B7280",   // Dark gray
        _ => "#9CA3AF"
    };

    /// <summary>
    /// Whether this item has an assignee.
    /// </summary>
    public bool HasAssignee => AssignedToTeamMemberId.HasValue;

    /// <summary>
    /// Whether this item has assignment info or a non-personal visibility scope.
    /// Used by UI to show visibility/assignment row.
    /// </summary>
    public bool HasAssigneeOrScope => HasAssignee || VisibilityScope != "personal";

    /// <summary>
    /// Icon to display for visibility scope.
    /// </summary>
    [Obsolete("Use VisibilityIconPath for XAML binding with PathIcon")]
    public string VisibilityIcon => VisibilityScope switch
    {
        "personal" => "🔒",
        "assigned" => "👤",
        "meeting" => "👥",
        _ => ""
    };

    /// <summary>
    /// PathIcon data for visibility scope - use with PathIcon in XAML.
    /// </summary>
    public string VisibilityIconPath => VisibilityScope switch
    {
        // Lock icon for personal
        "personal" => "M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17M18,8A2,2 0 0,1 20,10V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V10C4,8.89 4.9,8 6,8H7V6A5,5 0 0,1 12,1A5,5 0 0,1 17,6V8H18M12,3A3,3 0 0,0 9,6V8H15V6A3,3 0 0,0 12,3Z",
        // Person icon for assigned
        "assigned" => "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z",
        // People icon for meeting/team
        "meeting" => "M16,13C15.71,13 15.38,13 15.03,13.05C16.19,13.89 17,15 17,16.5V19H23V16.5C23,14.17 18.33,13 16,13M8,13C5.67,13 1,14.17 1,16.5V19H15V16.5C15,14.17 10.33,13 8,13M8,11A3,3 0 0,0 11,8A3,3 0 0,0 8,5A3,3 0 0,0 5,8A3,3 0 0,0 8,11M16,11A3,3 0 0,0 19,8A3,3 0 0,0 16,5A3,3 0 0,0 13,8A3,3 0 0,0 16,11Z",
        _ => ""
    };

    /// <summary>
    /// Display text for the assignee (e.g., "Alex Smith" or "Unassigned").
    /// </summary>
    public string AssigneeNameDisplay => HasAssignee 
        ? (!string.IsNullOrEmpty(AssignedToName) ? AssignedToName : "Assigned")
        : string.Empty;

    /// <summary>
    /// Display text showing assignment info (e.g., "Assigned to Alex" or "From Sarah").
    /// Set by the grouping logic based on context.
    /// </summary>
    public string AssignmentDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Whether to show the assignment display text.
    /// </summary>
    public bool ShowAssignmentDisplay => !string.IsNullOrEmpty(AssignmentDisplay);

    /// <summary>
    /// Whether the item is completed or dismissed.
    /// </summary>
    public bool IsComplete => Status == "done" || Status == "dismissed";

    /// <summary>
    /// Whether this item is overdue.
    /// </summary>
    public bool IsOverdue => DueAt.HasValue && DueAt.Value < DateTime.UtcNow && !IsComplete;

    #endregion

    #region Permission Helpers (set CurrentUserTeamMemberId before using)

    /// <summary>
    /// The current user's team member ID. Set by ViewModel for permission checks.
    /// </summary>
    public Guid? CurrentUserTeamMemberId { get; set; }

    /// <summary>
    /// Whether the current user is the requester (creator) of this prep item.
    /// </summary>
    public bool IsCurrentUserRequester =>
        CurrentUserTeamMemberId.HasValue &&
        RequestedByTeamMemberId == CurrentUserTeamMemberId.Value;

    /// <summary>
    /// Whether the current user is the assignee of this prep item.
    /// </summary>
    public bool IsCurrentUserAssignee =>
        CurrentUserTeamMemberId.HasValue &&
        AssignedToTeamMemberId.HasValue &&
        AssignedToTeamMemberId.Value == CurrentUserTeamMemberId.Value;

    /// <summary>
    /// Whether the current user can edit title/body (requester only).
    /// </summary>
    public bool CanEditContent => IsCurrentUserRequester;

    /// <summary>
    /// Whether the current user can edit status (requester or assignee).
    /// </summary>
    public bool CanEditStatus => IsCurrentUserRequester || IsCurrentUserAssignee;

    /// <summary>
    /// Whether the current user can edit assignee notes (assignee only).
    /// </summary>
    public bool CanEditAssigneeNotes => IsCurrentUserAssignee;

    /// <summary>
    /// Whether the current user can change assignment (requester only).
    /// </summary>
    public bool CanChangeAssignment => IsCurrentUserRequester;

    /// <summary>
    /// Whether this item is read-only for the current user.
    /// </summary>
    public bool IsReadOnly => !IsCurrentUserRequester && !IsCurrentUserAssignee;

    #endregion
}
