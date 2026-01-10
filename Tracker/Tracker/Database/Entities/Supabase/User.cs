using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Database.Entities.Supabase;

/// <summary>
/// User entity - application users who log in.
/// Maps to the 'users' table in Supabase.
/// Links to Supabase auth.users via supabase_auth_id.
/// </summary>
[Table("users")]
public class User : BaseModel
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

    /// <summary>
    /// User's email address.
    /// </summary>
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown in the UI.
    /// </summary>
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    [Column("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    [Column("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// URL to user's avatar image.
    /// </summary>
    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// User's phone number.
    /// </summary>
    [Column("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// User's timezone (IANA format).
    /// </summary>
    [Column("timezone")]
    public string Timezone { get; set; } = "UTC";

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

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether email has been verified.
    /// </summary>
    [Column("is_email_verified")]
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// Last login timestamp.
    /// </summary>
    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// When the user was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user was last updated.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Who created this user record.
    /// </summary>
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When the user was deleted.
    /// </summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted this user.
    /// </summary>
    [Column("deleted_by")]
    public string? DeletedBy { get; set; }

    #region Computed Properties

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string FullName =>
        !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName)
            ? $"{FirstName} {LastName}"
            : DisplayName;

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

    #endregion
}
