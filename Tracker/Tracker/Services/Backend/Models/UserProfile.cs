using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Services.Backend.Models
{
    /// <summary>
    /// User profile model - matches the profiles table in Supabase.
    /// </summary>
    [Table("profiles")]
    public class UserProfile : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("display_name")]
        public string? DisplayName { get; set; }

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        [Column("job_title")]
        public string? JobTitle { get; set; }

        [Column("company")]
        public string? Company { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("timezone")]
        public string Timezone { get; set; } = "UTC";

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("preferred_theme")]
        public string PreferredTheme { get; set; } = "tracker";

        [Column("locale")]
        public string Locale { get; set; } = "en-US";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("deactivated_at")]
        public DateTime? DeactivatedAt { get; set; }

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
                    return Email[..2].ToUpper();

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
    }
}

