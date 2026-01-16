using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Team member model with computed counts - maps to the team_members table.
/// Used for dashboard display.
/// </summary>
[Table("team_members")]
public class TeamMemberDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("job_title")]
    public string? JobTitle { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("manager_user_id")]
    public Guid? ManagerUserId { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("birthday")]
    public DateTime? Birthday { get; set; }

    [Column("hire_date")]
    public DateTime? HireDate { get; set; }

    [Column("linkedin_url")]
    public string? LinkedInUrl { get; set; }

    [Column("x_profile_url")]
    public string? XProfileUrl { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    #region Computed Properties (for dashboard)

    /// <summary>
    /// Full name computed from first + last name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Initials for avatar display.
    /// </summary>
    public string Initials
    {
        get
        {
            var first = !string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString().ToUpper() : "";
            var last = !string.IsNullOrEmpty(LastName) ? LastName[0].ToString().ToUpper() : "";
            return $"{first}{last}";
        }
    }

    /// <summary>
    /// Number of open tasks assigned to this team member.
    /// Computed separately by DashboardService.
    /// </summary>
    public int OpenTaskCount { get; set; }

    /// <summary>
    /// Number of active goals owned by this team member.
    /// Computed separately by DashboardService.
    /// </summary>
    public int ActiveGoalCount { get; set; }

    /// <summary>
    /// Date of last meeting with this team member.
    /// Computed separately by DashboardService.
    /// </summary>
    public DateTime? LastMeetingDate { get; set; }

    /// <summary>
    /// Friendly text for last meeting date (e.g., "3d ago", "Never").
    /// </summary>
    public string LastMeetingText
    {
        get
        {
            if (!LastMeetingDate.HasValue)
                return "—";

            var diff = DateTime.UtcNow - LastMeetingDate.Value;
            if (diff.TotalMinutes < 60)
                return "Today";
            if (diff.TotalHours < 24)
                return "Today";
            if (diff.TotalDays < 1)
                return "Today";
            if (diff.TotalDays < 2)
                return "Yesterday";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";
            if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)}w ago";
            return LastMeetingDate.Value.ToString("MMM d");
        }
    }

    /// <summary>
    /// Status display text (Active/Inactive).
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Whether this team member needs attention (e.g., no recent meeting).
    /// </summary>
    public bool NeedsAttention => IsActive && (!LastMeetingDate.HasValue || (DateTime.UtcNow - LastMeetingDate.Value).TotalDays > 21);

    /// <summary>
    /// Next scheduled meeting date.
    /// </summary>
    public DateTime? NextMeetingDate { get; set; }

    /// <summary>
    /// Friendly text for next meeting.
    /// </summary>
    public string NextMeetingText
    {
        get
        {
            if (!NextMeetingDate.HasValue)
            {
                if (NeedsAttention)
                    return "Overdue";
                return "—";
            }

            var diff = NextMeetingDate.Value - DateTime.UtcNow;
            if (diff.TotalDays < 0)
                return "Overdue";
            if (diff.TotalHours < 24)
                return "Today";
            if (diff.TotalDays < 2)
                return "Tomorrow";
            if (diff.TotalDays < 7)
                return NextMeetingDate.Value.ToString("ddd");
            return NextMeetingDate.Value.ToString("MMM d");
        }
    }

    /// <summary>
    /// Tenure (time since CreatedAt).
    /// </summary>
    public string Tenure
    {
        get
        {
            var diff = DateTime.UtcNow - CreatedAt;
            if (diff.TotalDays < 30)
                return $"{(int)diff.TotalDays}d";
            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)}mo";
            var years = (int)(diff.TotalDays / 365);
            var months = (int)((diff.TotalDays % 365) / 30);
            if (months > 0)
                return $"{years}y {months}mo";
            return $"{years}y";
        }
    }

    /// <summary>
    /// Birthday display (month/day format).
    /// </summary>
    public string BirthdayDisplay => Birthday?.ToString("MMM d") ?? "—";

    /// <summary>
    /// Hire date display (full date).
    /// </summary>
    public string HireDateDisplay => HireDate?.ToString("MMM d, yyyy") ?? "—";

    /// <summary>
    /// Tenure computed from hire date (more accurate than CreatedAt).
    /// </summary>
    public string TenureFromHireDate
    {
        get
        {
            if (!HireDate.HasValue)
                return Tenure; // Fall back to CreatedAt-based tenure

            var diff = DateTime.UtcNow - HireDate.Value;
            if (diff.TotalDays < 30)
                return $"{(int)diff.TotalDays}d";
            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)}mo";
            var years = (int)(diff.TotalDays / 365);
            var months = (int)((diff.TotalDays % 365) / 30);
            if (months > 0)
                return $"{years}y {months}mo";
            return $"{years}y";
        }
    }

    /// <summary>
    /// Has LinkedIn profile.
    /// </summary>
    public bool HasLinkedIn => !string.IsNullOrWhiteSpace(LinkedInUrl);

    /// <summary>
    /// Has X (Twitter) profile.
    /// </summary>
    public bool HasXProfile => !string.IsNullOrWhiteSpace(XProfileUrl);

    /// <summary>
    /// Has phone number.
    /// </summary>
    public bool HasPhone => !string.IsNullOrWhiteSpace(Phone);

    #endregion
}
