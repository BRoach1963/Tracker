namespace Tracker.DataModels
{
    /// <summary>
    /// Represents an organization (company, firm, team) that owns all data in the system.
    /// 
    /// Organizations provide multi-tenancy:
    /// - All data is scoped to an organization via OrganizationId
    /// - Users belong to exactly one organization (no crossover)
    /// - Each organization is a separate billable entity
    /// 
    /// Row-Level Security (PostgreSQL):
    /// RLS policies use OrganizationId to ensure users can only access their org's data.
    /// 
    /// SQL Server:
    /// Application-level filtering handles data isolation.
    /// </summary>
    public class Organization : AuditableEntity
    {
        /// <summary>
        /// Primary key - GUID for PostgreSQL compatibility and distributed systems.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display name of the organization (e.g., "Acme Corporation").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly identifier (e.g., "acme-corp").
        /// Used for URLs and API access. Must be unique.
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        /// Whether this organization is active.
        /// Inactive orgs are suspended but data is preserved.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Subscription tier for billing purposes.
        /// Examples: "free", "pro", "enterprise"
        /// </summary>
        public string SubscriptionTier { get; set; } = "free";

        /// <summary>
        /// Maximum number of users allowed in this organization.
        /// Null means unlimited (enterprise tier).
        /// </summary>
        public int? MaxUsers { get; set; } = 5;

        /// <summary>
        /// Maximum number of team members that can be tracked.
        /// Null means unlimited.
        /// </summary>
        public int? MaxTeamMembers { get; set; } = 25;

        #region Navigation Properties

        /// <summary>
        /// Users who belong to this organization.
        /// </summary>
        public ICollection<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// Team members being tracked within this organization.
        /// </summary>
        public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the slug or generates one from the name.
        /// </summary>
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
