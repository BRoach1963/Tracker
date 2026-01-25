using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Team member model - maps to the team_members table in Supabase procohere schema.
/// Represents a person in an organization (may or may not have a linked user account).
/// </summary>
[Table("team_members")]
public class TeamMember : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// FK to public.users if this team member has a user account.
    /// Null for placeholder/external team members.
    /// </summary>
    [Column("linked_user_id")]
    public Guid? LinkedUserId { get; set; }

    /// <summary>
    /// FK to roles table for permission assignment.
    /// </summary>
    [Column("role_id")]
    public Guid RoleId { get; set; }

    #endregion

    #region Personal Info

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Preferred display name (used if set, otherwise FullName).
    /// </summary>
    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("job_title")]
    public string? JobTitle { get; set; }

    #endregion

    #region Hierarchy

    /// <summary>
    /// FK to this person's manager (self-referential).
    /// </summary>
    [Column("manager_team_member_id")]
    public Guid? ManagerTeamMemberId { get; set; }

    #endregion

    #region Status

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

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
    /// The role assigned to this team member.
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// This person's manager (populated by service).
    /// </summary>
    public TeamMember? Manager { get; set; }

    /// <summary>
    /// Direct reports (populated by service).
    /// </summary>
    public List<TeamMember> DirectReports { get; set; } = new();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Full name from first + last name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Name to display (uses DisplayName if set, otherwise FullName).
    /// </summary>
    public string NameDisplay => !string.IsNullOrEmpty(DisplayName) ? DisplayName : FullName;

    /// <summary>
    /// Initials for avatar fallback.
    /// </summary>
    public string Initials
    {
        get
        {
            var first = FirstName?.Length > 0 ? FirstName[0].ToString().ToUpper() : "";
            var last = LastName?.Length > 0 ? LastName[0].ToString().ToUpper() : "";
            return $"{first}{last}";
        }
    }

    /// <summary>
    /// Whether this team member has a linked user account.
    /// </summary>
    public bool HasUserAccount => LinkedUserId.HasValue;

    /// <summary>
    /// Whether this person has a manager.
    /// </summary>
    public bool HasManager => ManagerTeamMemberId.HasValue;

    #endregion
}

/// <summary>
/// Team model - maps to the teams table in Supabase procohere schema.
/// Represents a group/department within an organization.
/// Supports hierarchical structure via parent_team_id.
/// </summary>
[Table("teams")]
public class Team : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// FK to parent team for hierarchy (null if top-level).
    /// </summary>
    [Column("parent_team_id")]
    public Guid? ParentTeamId { get; set; }

    #endregion

    #region Content

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// FK to team_members - the team lead.
    /// </summary>
    [Column("lead_team_member_id")]
    public Guid? LeadTeamMemberId { get; set; }

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
    /// Parent team (populated by service).
    /// </summary>
    public Team? ParentTeam { get; set; }

    /// <summary>
    /// Child teams (populated by service).
    /// </summary>
    public List<Team> ChildTeams { get; set; } = new();

    /// <summary>
    /// Team lead (populated by service).
    /// </summary>
    public TeamMember? Lead { get; set; }

    /// <summary>
    /// Members of this team (populated by service via team_team_members join).
    /// </summary>
    public List<TeamMember> Members { get; set; } = new();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this is a top-level team.
    /// </summary>
    public bool IsTopLevel => !ParentTeamId.HasValue;

    /// <summary>
    /// Whether this team has a designated lead.
    /// </summary>
    public bool HasLead => LeadTeamMemberId.HasValue;

    /// <summary>
    /// Number of members.
    /// </summary>
    public int MemberCount => Members.Count;

    #endregion
}

/// <summary>
/// Team membership model - maps to the team_memberships table in Supabase procohere schema.
/// Represents a many-to-many relationship between teams and team members.
/// A team member can belong to multiple teams, and a team can have multiple members.
/// </summary>
[Table("team_memberships")]
public class TeamMembership : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Membership

    /// <summary>
    /// FK to the team this membership is for.
    /// </summary>
    [Column("team_id")]
    public Guid TeamId { get; set; }

    /// <summary>
    /// FK to the team member in this membership.
    /// </summary>
    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// Role in the team: 'member', 'lead', or 'viewer'.
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

    #region Navigation (not mapped)

    /// <summary>
    /// The team (populated by service).
    /// </summary>
    public Team? Team { get; set; }

    /// <summary>
    /// The team member (populated by service).
    /// </summary>
    public TeamMember? Member { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this is a lead membership.
    /// </summary>
    public bool IsLead => Role == "lead";

    /// <summary>
    /// Whether this is a viewer membership.
    /// </summary>
    public bool IsViewer => Role == "viewer";

    /// <summary>
    /// Whether this is a regular member.
    /// </summary>
    public bool IsMember => Role == "member";

    #endregion

    #region Constants

    public const string RoleMember = "member";
    public const string RoleLead = "lead";
    public const string RoleViewer = "viewer";

    #endregion
}
