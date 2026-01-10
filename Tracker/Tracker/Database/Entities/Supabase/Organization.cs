using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Database.Entities.Supabase;

/// <summary>
/// Organization entity - the top-level tenant for all data.
/// Maps to the 'organizations' table in Supabase.
/// </summary>
[Table("organizations")]
public class Organization : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization name.
    /// </summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier.
    /// </summary>
    [Column("slug")]
    public string? Slug { get; set; }

    /// <summary>
    /// Subscription tier: free, professional, enterprise.
    /// </summary>
    [Column("subscription_tier")]
    public string SubscriptionTier { get; set; } = "free";

    /// <summary>
    /// Maximum number of app users allowed.
    /// </summary>
    [Column("max_users")]
    public int MaxUsers { get; set; } = 5;

    /// <summary>
    /// Maximum number of team members that can be tracked.
    /// </summary>
    [Column("max_team_members")]
    public int MaxTeamMembers { get; set; } = 25;

    /// <summary>
    /// JSON settings blob.
    /// </summary>
    [Column("settings")]
    public string? Settings { get; set; }

    /// <summary>
    /// Whether the organization is active.
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the organization was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the organization was last updated.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Who created this organization.
    /// </summary>
    [Column("created_by")]
    public string? CreatedBy { get; set; }
}
