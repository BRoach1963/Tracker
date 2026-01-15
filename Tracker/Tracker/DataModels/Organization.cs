using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents an organization (company, firm, team) that owns all data in the system.
    /// Maps to Supabase 'organizations' table.
    /// 
    /// Organizations provide multi-tenancy:
    /// - All data is scoped to an organization via OrganizationId
    /// - Users belong to exactly one organization (no crossover)
    /// - Each organization is a separate billable entity
    /// 
    /// Row-Level Security (PostgreSQL):
    /// RLS policies use OrganizationId to ensure users can only access their org's data.
    /// </summary>
    [Table("organizations")]
    public class Organization
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display name of the organization (e.g., "Acme Corporation").
        /// Maps to: name VARCHAR(200) NOT NULL
        /// </summary>
        [Column("name")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly identifier (e.g., "acme-corp").
        /// Maps to: slug VARCHAR(100) NULL
        /// </summary>
        [Column("slug")]
        [MaxLength(100)]
        public string? Slug { get; set; }

        /// <summary>
        /// Subscription tier for billing purposes.
        /// Maps to: subscription_tier VARCHAR(50) NOT NULL DEFAULT 'free'
        /// </summary>
        [Column("subscription_tier")]
        [MaxLength(50)]
        public string SubscriptionTier { get; set; } = "free";

        /// <summary>
        /// Maximum number of users allowed in this organization.
        /// Maps to: max_users INT4 NULL DEFAULT 5
        /// </summary>
        [Column("max_users")]
        public int? MaxUsers { get; set; } = 5;

        /// <summary>
        /// Maximum number of team members that can be tracked.
        /// Maps to: max_team_members INT4 NULL DEFAULT 25
        /// </summary>
        [Column("max_team_members")]
        public int? MaxTeamMembers { get; set; } = 25;

        /// <summary>
        /// Organization-level settings (JSON).
        /// Maps to: settings JSONB NULL DEFAULT '{}'
        /// </summary>
        [Column("settings")]
        public string? Settings { get; set; } = "{}";

        /// <summary>
        /// Whether this organization is active.
        /// Maps to: is_active BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created this organization.
        /// Maps to: created_by VARCHAR(100) NULL
        /// </summary>
        [Column("created_by")]
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Users who belong to this organization.
        /// </summary>
        [NotMapped]
        public ICollection<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// Team members being tracked within this organization.
        /// </summary>
        [NotMapped]
        public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the slug or generates one from the name.
        /// </summary>
        [NotMapped]
        public string EffectiveSlug => Slug ?? GenerateSlug(Name);

        /// <summary>
        /// Generates a URL-friendly slug from a name.
        /// </summary>
        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            
            return name.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("&", "and")
                .Replace("'", "")
                .Replace("\"", "");
        }

        #endregion
    }
}
