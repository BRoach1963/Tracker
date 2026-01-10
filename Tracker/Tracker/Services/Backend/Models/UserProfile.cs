using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Services.Backend.Models
{
    /// <summary>
    /// User profile model - matches the users table in Supabase.
    /// Used by SupabaseService for profile operations.
    /// </summary>
    [Table("users")]
    public class UserProfile : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Links to Supabase auth.users.id
        /// </summary>
        [Column("supabase_auth_id")]
        public Guid? SupabaseAuthId { get; set; }

        /// <summary>
        /// Organization this user belongs to.
        /// </summary>
        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        /// <summary>
        /// User's job title.
        /// </summary>
        [Column("job_title")]
        public string? JobTitle { get; set; }

        /// <summary>
        /// User's company name.
        /// </summary>
        [Column("company")]
        public string? Company { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("timezone")]
        public string Timezone { get; set; } = "UTC";

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// If this user is also tracked as a team member.
        /// </summary>
        [Column("linked_team_member_id")]
        public Guid? LinkedTeamMemberId { get; set; }

        /// <summary>
        /// JSON blob of user preferences.
        /// </summary>
        [Column("preferences")]
        public string? Preferences { get; set; }

        /// <summary>
        /// JSON blob of notification settings.
        /// </summary>
        [Column("notification_settings")]
        public string? NotificationSettings { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_email_verified")]
        public bool IsEmailVerified { get; set; }

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [Column("deleted_by")]
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Gets the user's initials for avatar display.
        /// </summary>
        public string Initials
        {
            get
            {
                if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                    return $"{FirstName[0]}{LastName[0]}".ToUpper();

                if (!string.IsNullOrEmpty(DisplayName))
                {
                    var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                        return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
                    if (parts.Length == 1 && parts[0].Length >= 2)
                        return parts[0][..2].ToUpper();
                }

                if (!string.IsNullOrEmpty(Email))
                    return Email[..Math.Min(2, Email.Length)].ToUpper();

                return "??";
            }
        }

        /// <summary>
        /// Gets the full avatar URL from Supabase storage.
        /// </summary>
        public string? FullAvatarUrl
        {
            get
            {
                if (string.IsNullOrEmpty(AvatarUrl))
                    return null;

                return $"{SupabaseConfig.ProjectUrl}/storage/v1/object/public/{SupabaseConfig.AvatarBucket}/{AvatarUrl}";
            }
        }

        /// <summary>
        /// Gets the user's full name.
        /// </summary>
        public string FullName =>
            !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName)
                ? $"{FirstName} {LastName}"
                : DisplayName;

        /// <summary>
        /// Whether this user has admin privileges.
        /// This is determined by checking user_roles after profile load.
        /// Not stored in the users table - set by SupabaseService.
        /// </summary>
        public bool IsAdmin { get; set; }
    }
}
