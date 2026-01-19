using System;
using System.Text.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// User profile model - maps to the users table in Supabase.
/// Used for profile CRUD operations via Postgrest.
/// </summary>
[Table("users")]
public class UserProfile : BaseModel
{
    /// <summary>
    /// Primary key - same as auth.users.id (no separate supabase_auth_id in new schema).
    /// </summary>
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

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

    /// <summary>
    /// User's birthday (month and day).
    /// </summary>
    [Column("birthday")]
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// User's hire date at their company.
    /// </summary>
    [Column("hire_date")]
    public DateTime? HireDate { get; set; }

    [Column("timezone")]
    public string Timezone { get; set; } = "UTC";

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// JSON blob of user preferences. Stored as JsonElement to handle raw JSON from Postgrest.
    /// </summary>
    [Column("preferences")]
    public JsonElement? Preferences { get; set; }

    /// <summary>
    /// JSON blob of notification settings. Stored as JsonElement to handle raw JSON from Postgrest.
    /// </summary>
    [Column("notification_settings")]
    public JsonElement? NotificationSettings { get; set; }

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

    #region Computed Properties

    /// <summary>
    /// Gets the user's initials from first/last name or display name.
    /// </summary>
    public string Initials
    {
        get
        {
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            {
                return $"{FirstName[0]}{LastName[0]}".ToUpper();
            }
            if (!string.IsNullOrEmpty(DisplayName))
            {
                var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                if (parts.Length == 1 && parts[0].Length >= 1)
                    return parts[0][0].ToString().ToUpper();
            }
            if (!string.IsNullOrEmpty(Email))
            {
                return Email[0].ToString().ToUpper();
            }
            return "?";
        }
    }

    /// <summary>
    /// Gets the full name from first/last name or falls back to display name.
    /// </summary>
    public string FullName
    {
        get
        {
            if (!string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName))
            {
                return $"{FirstName} {LastName}".Trim();
            }
            return DisplayName;
        }
    }

    #endregion
}
