using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a user who can log in and access the system.
    /// Maps to Supabase 'users' table (29 columns after ALTER).
    /// 
    /// Users belong to an organization and can manage team members.
    /// Authentication can be via Supabase Auth (supabase_auth_id) or local password.
    /// </summary>
    [Table("users")]
    public class User : AuditableEntity
    {
        #region Primary Key & Foreign Keys

        /// <summary>
        /// Primary key for the User entity.
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// The Supabase Auth user ID.
        /// Links to the auth.users table in Supabase for SSO/OAuth.
        /// Maps to: supabase_auth_id UUID NULL
        /// </summary>
        [Column("supabase_auth_id")]
        public Guid? SupabaseAuthId { get; set; }

        /// <summary>
        /// The organization this user belongs to.
        /// Maps to: organization_id UUID NULL
        /// </summary>
        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// The firm/license this user belongs to.
        /// Used for multi-tenant licensing scenarios.
        /// Maps to: firm_id UUID NULL
        /// </summary>
        [Column("firm_id")]
        public Guid? FirmId { get; set; }

        /// <summary>
        /// Links this user to their team member record (if they are also a team member).
        /// Maps to: linked_team_member_id UUID NULL
        /// </summary>
        [Column("linked_team_member_id")]
        public Guid? LinkedTeamMemberId { get; set; }

        #endregion

        #region Identity & Authentication

        /// <summary>
        /// Login identifier (Windows username, SSO identifier, or email).
        /// Maps to: username VARCHAR(200) NULL
        /// </summary>
        [Column("username")]
        [MaxLength(200)]
        public string? Username { get; set; }

        /// <summary>
        /// Email address of the user.
        /// Maps to: email VARCHAR(255) NOT NULL
        /// </summary>
        [Column("email")]
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user's email has been verified.
        /// Maps to: is_email_verified BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_email_verified")]
        public bool IsEmailVerified { get; set; } = false;

        /// <summary>
        /// BCrypt-hashed password for local authentication.
        /// NULL when using Supabase Auth.
        /// Maps to: password_hash TEXT NULL
        /// </summary>
        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        /// <summary>
        /// When the user last logged in.
        /// Maps to: last_login_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        #endregion

        #region Profile Information

        /// <summary>
        /// Display name for the user (e.g., "John Doe").
        /// Maps to: display_name VARCHAR(200) NOT NULL
        /// </summary>
        [Column("display_name")]
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// User's first name.
        /// Maps to: first_name VARCHAR(100) NULL
        /// </summary>
        [Column("first_name")]
        [MaxLength(100)]
        public string? FirstName { get; set; }

        /// <summary>
        /// User's last name.
        /// Maps to: last_name VARCHAR(100) NULL
        /// </summary>
        [Column("last_name")]
        [MaxLength(100)]
        public string? LastName { get; set; }

        /// <summary>
        /// URL to user's avatar image (stored in Supabase Storage).
        /// Maps to: avatar_url TEXT NULL
        /// </summary>
        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// User's phone number.
        /// Maps to: phone VARCHAR(50) NULL
        /// </summary>
        [Column("phone")]
        [MaxLength(50)]
        public string? Phone { get; set; }

        /// <summary>
        /// User's timezone (e.g., "America/New_York").
        /// Maps to: timezone VARCHAR(100) NULL DEFAULT 'UTC'
        /// </summary>
        [Column("timezone")]
        [MaxLength(100)]
        public string? Timezone { get; set; } = "UTC";

        /// <summary>
        /// User's job title.
        /// Maps to: job_title VARCHAR(200) NULL
        /// </summary>
        [Column("job_title")]
        [MaxLength(200)]
        public string? JobTitle { get; set; }

        /// <summary>
        /// User's company name.
        /// Maps to: company VARCHAR(200) NULL
        /// </summary>
        [Column("company")]
        [MaxLength(200)]
        public string? Company { get; set; }

        #endregion

        #region Role & Permissions

        /// <summary>
        /// Whether this user account is active.
        /// Inactive users cannot log in but their data is preserved.
        /// Maps to: is_active BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this user has administrator privileges.
        /// Admins can access admin tools for database management, user cleanup, etc.
        /// Maps to: is_admin BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_admin")]
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Primary role: admin, hr_admin, manager, viewer.
        /// For fine-grained permissions, see user_roles table.
        /// Maps to: role VARCHAR(50) NOT NULL DEFAULT 'manager'
        /// </summary>
        [Column("role")]
        [MaxLength(50)]
        public string Role { get; set; } = "manager";

        #endregion

        #region Settings (JSONB)

        /// <summary>
        /// User preferences as JSON.
        /// Maps to: preferences JSONB NULL DEFAULT '{}'
        /// </summary>
        [Column("preferences")]
        public string? PreferencesJson { get; set; }

        /// <summary>
        /// Notification settings as JSON.
        /// Maps to: notification_settings JSONB NULL
        /// </summary>
        [Column("notification_settings")]
        public string? NotificationSettingsJson { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// The organization this user belongs to.
        /// </summary>
        public Organization? Organization { get; set; }

        /// <summary>
        /// The team member record linked to this user (if any).
        /// </summary>
        public TeamMember? LinkedTeamMember { get; set; }

        /// <summary>
        /// Team members currently managed by this user.
        /// </summary>
        public ICollection<TeamMember> ManagedTeamMembers { get; set; } = new List<TeamMember>();

        /// <summary>
        /// History of manager assignments for this user.
        /// </summary>
        public ICollection<ManagerHistory> ManagerHistories { get; set; } = new List<ManagerHistory>();

        #endregion

        #region Local Cache (Not Persisted)

        /// <summary>
        /// Cached avatar image bytes for local display.
        /// Loaded from AvatarUrl, not stored in database.
        /// </summary>
        [NotMapped]
        public byte[]? AvatarImageCache { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Full name (FirstName + LastName) or DisplayName if names not set.
        /// </summary>
        [NotMapped]
        public string FullName => 
            !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
                ? $"{FirstName} {LastName}".Trim()
                : DisplayName;

        #endregion
    }
}

