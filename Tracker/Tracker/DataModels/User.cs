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
        public int Id { get; set; } = 0;

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
    }
}

