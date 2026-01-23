using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Role model - maps to the roles table in Supabase procohere schema.
/// Defines organizational roles with JSONB-based permissions.
/// </summary>
[Table("roles")]
public class Role : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Content

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Permissions stored as JSONB. Structure depends on permission system design.
    /// Example: {"meetings": {"create": true, "delete": false}, "goals": {"manage": true}}
    /// </summary>
    [Column("permissions")]
    public string Permissions { get; set; } = "{}";

    /// <summary>
    /// Whether this is a built-in role (admin, member, viewer) that cannot be deleted.
    /// </summary>
    [Column("is_system_role")]
    public bool IsSystemRole { get; set; }

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
    /// Team members with this role (populated by service).
    /// </summary>
    public List<TeamMember> TeamMembers { get; set; } = new();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this role can be edited (non-system roles only).
    /// </summary>
    public bool CanEdit => !IsSystemRole;

    /// <summary>
    /// Whether this role can be deleted (non-system roles only).
    /// </summary>
    public bool CanDelete => !IsSystemRole;

    #endregion
}

/// <summary>
/// Well-known system role names.
/// </summary>
public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Member = "Member";
    public const string Viewer = "Viewer";
}
