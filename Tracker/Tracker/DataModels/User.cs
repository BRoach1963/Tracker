namespace Tracker.DataModels
{
    /// <summary>
    /// Represents the logged-in manager/user who owns all data in the system.
    /// 
    /// In a local database, this is typically the Windows user.
    /// In an enterprise database, this enables multi-user data isolation.
    /// 
    /// All entities (TeamMembers, Projects, Tasks, OKRs, KPIs, 1:1s, etc.) are owned by a User.
    /// </summary>
    public class User : AuditableEntity
    {
        /// <summary>
        /// Primary key for the User entity.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The firm this user belongs to (from Supabase firms table).
        /// Links local user to their licensed firm.
        /// </summary>
        public Guid? FirmId { get; set; }

        /// <summary>
        /// The organization this user belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// The Supabase/PostgreSQL user ID (for PostgreSQL databases).
        /// Links to the auth.users table in Supabase.
        /// Null for SQLite/SQL Server local databases.
        /// </summary>
        public Guid? SupabaseUserId { get; set; }

        /// <summary>
        /// Windows username or login identifier (e.g., "DOMAIN\username" or "username").
        /// Used to identify the user during login.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the user (e.g., "John Doe").
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Whether this user account is active.
        /// Inactive users cannot log in but their data is preserved.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this user has administrator privileges.
        /// Admins can access admin tools for database management, user cleanup, etc.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Role within the organization.
        /// Values: "admin", "hr_admin", "manager", "viewer"
        /// </summary>
        public string Role { get; set; } = "manager";

        /// <summary>
        /// BCrypt-hashed password for local authentication.
        /// Used when authenticating against the local PostgreSQL database.
        /// </summary>
        public string? PasswordHash { get; set; }

        #region Navigation Properties

        /// <summary>
        /// The organization this user belongs to.
        /// </summary>
        public Organization? Organization { get; set; }

        /// <summary>
        /// Team members currently managed by this user.
        /// </summary>
        public ICollection<TeamMember> ManagedTeamMembers { get; set; } = new List<TeamMember>();

        /// <summary>
        /// History of manager assignments for this user.
        /// </summary>
        public ICollection<ManagerHistory> ManagerHistories { get; set; } = new List<ManagerHistory>();

        #endregion
    }
}

