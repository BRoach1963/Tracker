using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Project status enum values matching procohere.project_status.
/// </summary>
public static class ProjectStatus
{
    public const string Active = "active";
    public const string Paused = "paused";
    public const string Completed = "completed";
}

/// <summary>
/// Project model - maps to the procohere.projects table in Supabase.
/// Projects group related work items and team members together.
/// </summary>
[Table("projects")]
public class Project : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Team member who owns this project.
    /// </summary>
    [Column("owner_team_member_id")]
    public Guid OwnerTeamMemberId { get; set; }

    #endregion

    #region Content

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    #endregion

    #region Status & Dates

    /// <summary>
    /// Project status: 'active', 'paused', 'completed'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = ProjectStatus.Active;

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

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
    /// Members of this project (populated by service).
    /// </summary>
    public List<ProjectMember> Members { get; set; } = new();

    /// <summary>
    /// Linked entities (populated by service).
    /// </summary>
    public List<ProjectLink> Links { get; set; } = new();

    #endregion

    #region Computed Properties

    public bool IsActive => Status == ProjectStatus.Active;
    public bool IsPaused => Status == ProjectStatus.Paused;
    public bool IsCompleted => Status == ProjectStatus.Completed;

    public string StatusDisplay => Status switch
    {
        ProjectStatus.Active => "Active",
        ProjectStatus.Paused => "Paused",
        ProjectStatus.Completed => "Completed",
        _ => Status
    };

    public int MemberCount => Members.Count;
    public int LinkCount => Links.Count;

    /// <summary>
    /// True if project is past due date and not completed.
    /// </summary>
    public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today && !IsCompleted;

    #endregion

    #region Owner Info (populated by service)

    /// <summary>
    /// Display name of the owner (populated by service layer).
    /// </summary>
    public string? OwnerDisplayName { get; set; }
    
    /// <summary>
    /// Owner's initials for avatar display (populated by service layer).
    /// </summary>
    public string? OwnerInitials { get; set; }
    
    /// <summary>
    /// True if the owner is inactive/deleted (orphaned project).
    /// </summary>
    public bool IsOrphaned { get; set; }
    
    /// <summary>
    /// Whether this project has a valid owner with a display name.
    /// </summary>
    public bool HasOwner => !string.IsNullOrEmpty(OwnerDisplayName) && !IsOrphaned;

    #endregion

    #region Signal Counts (populated by batch RPC)

    /// <summary>
    /// Count of overdue tasks linked to this project.
    /// Populated by GetProjectSignalsBatchAsync.
    /// </summary>
    public int OverdueTaskCount { get; set; }

    /// <summary>
    /// Count of goals needing attention (at_risk, needs_attention, blocked).
    /// Populated by GetProjectSignalsBatchAsync.
    /// </summary>
    public int GoalsNeedingAttention { get; set; }

    /// <summary>
    /// Whether this project has any signals that need attention.
    /// </summary>
    public bool HasSignals => OverdueTaskCount > 0 || GoalsNeedingAttention > 0;

    /// <summary>
    /// Total count of signals for badge display.
    /// </summary>
    public int TotalSignalCount => OverdueTaskCount + GoalsNeedingAttention;

    #endregion
}

/// <summary>
/// Project member role values matching procohere.project_member_role.
/// </summary>
public static class ProjectMemberRole
{
    public const string Member = "member";
    public const string Lead = "lead";
    public const string Viewer = "viewer";
}

/// <summary>
/// Project member model - maps to procohere.project_members table.
/// Associates team members with projects.
/// </summary>
[Table("project_members")]
public class ProjectMember : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    #endregion

    #region Role

    /// <summary>
    /// Role on project: 'member', 'lead', 'viewer'.
    /// </summary>
    [Column("role")]
    public string Role { get; set; } = ProjectMemberRole.Member;

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
    /// The team member details (populated by service).
    /// </summary>
    public TeamMemberDetail? TeamMember { get; set; }

    #endregion

    #region Computed Properties

    public bool IsLead => Role == ProjectMemberRole.Lead;
    public bool IsMember => Role == ProjectMemberRole.Member;
    public bool IsViewer => Role == ProjectMemberRole.Viewer;

    public string RoleDisplay => Role switch
    {
        ProjectMemberRole.Lead => "Lead",
        ProjectMemberRole.Member => "Member",
        ProjectMemberRole.Viewer => "Viewer",
        _ => Role
    };

    #endregion
}

/// <summary>
/// Project link entity type values matching procohere.project_link_entity_type.
/// </summary>
public static class ProjectLinkEntityType
{
    public const string ChronicleNote = "chronicle_note";
    public const string Goal = "goal";
    public const string Metric = "metric";
    public const string Meeting = "meeting";
    public const string Contact = "contact";
    public const string Company = "company";
    public const string Reminder = "reminder";
    public const string Task = "task";
}

/// <summary>
/// Project link model - maps to procohere.project_links table.
/// Links projects to related entities (goals, tasks, metrics, meetings, etc.).
/// </summary>
[Table("project_links")]
public class ProjectLink : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("project_id")]
    public Guid ProjectId { get; set; }

    #endregion

    #region Linked Entity

    /// <summary>
    /// Entity type: 'chronicle_note', 'goal', 'metric', 'meeting', 'contact', 'company', 'reminder', 'task'.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    [Column("entity_id")]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Cached title of the linked entity at link time.
    /// </summary>
    [Column("entity_title_snapshot")]
    public string? EntityTitleSnapshot { get; set; }

    #endregion

    #region Creator

    [Column("created_by_team_member_id")]
    public Guid CreatedByTeamMemberId { get; set; }

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

    #region Computed Properties

    public bool IsChronicleNoteLink => EntityType == ProjectLinkEntityType.ChronicleNote;
    public bool IsGoalLink => EntityType == ProjectLinkEntityType.Goal;
    public bool IsMetricLink => EntityType == ProjectLinkEntityType.Metric;
    public bool IsMeetingLink => EntityType == ProjectLinkEntityType.Meeting;
    public bool IsContactLink => EntityType == ProjectLinkEntityType.Contact;
    public bool IsCompanyLink => EntityType == ProjectLinkEntityType.Company;
    public bool IsReminderLink => EntityType == ProjectLinkEntityType.Reminder;
    public bool IsTaskLink => EntityType == ProjectLinkEntityType.Task;

    /// <summary>
    /// Display-friendly title for the linked entity.
    /// Returns the snapshot if available, otherwise a fallback based on entity type.
    /// </summary>
    public string DisplayTitle => !string.IsNullOrWhiteSpace(EntityTitleSnapshot)
        ? EntityTitleSnapshot
        : EntityType switch
        {
            ProjectLinkEntityType.Goal => "(Untitled Goal)",
            ProjectLinkEntityType.Task => "(Untitled Task)",
            ProjectLinkEntityType.Meeting => "(Untitled Meeting)",
            ProjectLinkEntityType.ChronicleNote => "(Untitled Note)",
            ProjectLinkEntityType.Metric => "(Untitled Metric)",
            _ => $"({EntityTypeDisplay})"
        };

    public string EntityTypeDisplay => EntityType switch
    {
        ProjectLinkEntityType.ChronicleNote => "Chronicle Note",
        ProjectLinkEntityType.Goal => "Goal",
        ProjectLinkEntityType.Metric => "Metric",
        ProjectLinkEntityType.Meeting => "Meeting",
        ProjectLinkEntityType.Contact => "Contact",
        ProjectLinkEntityType.Company => "Company",
        ProjectLinkEntityType.Reminder => "Reminder",
        ProjectLinkEntityType.Task => "Task",
        _ => EntityType
    };

    /// <summary>
    /// Icon for the entity type (using Segoe Fluent Icons).
    /// </summary>
    public string EntityTypeIcon => EntityType switch
    {
        ProjectLinkEntityType.ChronicleNote => "\uE70B",  // Edit
        ProjectLinkEntityType.Goal => "\uE8FB",           // Target
        ProjectLinkEntityType.Metric => "\uE9D9",         // Chart
        ProjectLinkEntityType.Meeting => "\uE787",        // Calendar
        ProjectLinkEntityType.Contact => "\uE77B",        // Contact
        ProjectLinkEntityType.Company => "\uE731",        // Building
        ProjectLinkEntityType.Reminder => "\uEA8F",       // Alarm
        ProjectLinkEntityType.Task => "\uE73A",           // Checkbox
        _ => "\uE71B"                                      // Link
    };

    #endregion
}
