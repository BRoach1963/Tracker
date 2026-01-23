using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Project model - maps to the projects table in Supabase procohere schema.
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

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    #endregion

    #region Status & Dates

    /// <summary>
    /// Project status: 'planning', 'active', 'on_hold', 'completed', 'cancelled'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "planning";

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("target_date")]
    public DateTime? TargetDate { get; set; }

    #endregion

    #region Archive

    [Column("is_archived")]
    public bool IsArchived { get; set; }

    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }

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

    public bool IsPlanning => Status == "planning";
    public bool IsActive => Status == "active";
    public bool IsOnHold => Status == "on_hold";
    public bool IsCompleted => Status == "completed";
    public bool IsCancelled => Status == "cancelled";

    public string StatusDisplay => Status switch
    {
        "planning" => "Planning",
        "active" => "Active",
        "on_hold" => "On Hold",
        "completed" => "Completed",
        "cancelled" => "Cancelled",
        _ => Status
    };

    public int MemberCount => Members.Count;
    public int LinkCount => Links.Count;

    #endregion
}

/// <summary>
/// Project member model - maps to project_members table.
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
    /// Role on project: 'owner', 'lead', 'member', 'contributor', 'viewer'.
    /// </summary>
    [Column("role")]
    public string Role { get; set; } = "member";

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

    #endregion

    #region Computed Properties

    public bool IsOwner => Role == "owner";
    public bool IsLead => Role == "lead";
    public bool IsMember => Role == "member";
    public bool IsContributor => Role == "contributor";
    public bool IsViewer => Role == "viewer";

    public string RoleDisplay => Role switch
    {
        "owner" => "Owner",
        "lead" => "Lead",
        "member" => "Member",
        "contributor" => "Contributor",
        "viewer" => "Viewer",
        _ => Role
    };

    #endregion
}

/// <summary>
/// Project link model - maps to project_links table.
/// Links projects to related entities (goals, tasks, metrics, meetings).
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
    /// Entity type: 'goal', 'task', 'metric', 'meeting'.
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

    #endregion

    #region Computed Properties

    public bool IsGoalLink => EntityType == "goal";
    public bool IsTaskLink => EntityType == "task";
    public bool IsMetricLink => EntityType == "metric";
    public bool IsMeetingLink => EntityType == "meeting";

    public string EntityTypeDisplay => EntityType switch
    {
        "goal" => "Goal",
        "task" => "Task",
        "metric" => "Metric",
        "meeting" => "Meeting",
        _ => EntityType
    };

    #endregion
}
