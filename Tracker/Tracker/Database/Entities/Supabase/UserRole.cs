using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Database.Entities.Supabase;

/// <summary>
/// UserRole entity - maps users to roles within an organization.
/// Maps to the 'user_roles' table in Supabase.
/// </summary>
[Table("user_roles")]
public class UserRole : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    /// <summary>
    /// The user being assigned the role.
    /// </summary>
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The organization context for this role assignment.
    /// </summary>
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The role being assigned.
    /// </summary>
    [Column("role_id")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Optional team-specific role assignment.
    /// </summary>
    [Column("team_id")]
    public Guid? TeamId { get; set; }

    /// <summary>
    /// When this role was assigned.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who assigned this role.
    /// </summary>
    [Column("assigned_by")]
    public Guid? AssignedBy { get; set; }
}
