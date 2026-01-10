using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;
using Tracker.Services.Microsoft365;

namespace Tracker.DataModels
{
    [Table("team_members")]
    public class TeamMember : AuditableEntity
    {
        #region Primary Keys and Foreign Keys

        /// <summary>
        /// Primary key - UUID for PostgreSQL compatibility.
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Legacy integer ID for SQLite/SQL Server backwards compatibility.
        /// Will be deprecated after full PostgreSQL migration.
        /// </summary>
        [NotMapped]
        public int LegacyId { get; set; } = 0;

        /// <summary>
        /// The organization this team member belongs to.
        /// Required for RLS filtering in PostgreSQL.
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// The current manager (user) for this team member.
        /// This is the primary manager relationship - can change over time.
        /// Historical manager assignments are tracked in ManagerHistory.
        /// </summary>
        [Column("manager_user_id")]
        public Guid? ManagerUserId { get; set; }

        /// <summary>
        /// If this team member also has a user account (login), this links them.
        /// Enables team members to access the system themselves.
        /// </summary>
        [Column("linked_user_id")]
        public Guid? LinkedUserId { get; set; }

        #endregion

        #region Personal Information

        [Column("first_name")]
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Column("nickname")]
        [MaxLength(50)]
        public string? Nickname { get; set; }

        [Column("email")]
        [MaxLength(255)]
        public string? Email { get; set; }

        [Column("phone")]
        [MaxLength(50)]
        public string? Phone { get; set; }

        [Column("birthday")]
        public DateTime? Birthday { get; set; }

        [Column("location")]
        [MaxLength(200)]
        public string? Location { get; set; }

        [Column("bio")]
        public string? Bio { get; set; }

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        #endregion

        #region Work Information

        [Column("job_title")]
        [MaxLength(200)]
        public string? JobTitle { get; set; }

        [Column("department")]
        [MaxLength(200)]
        public string? Department { get; set; }

        [Column("hire_date")]
        public DateTime? HireDate { get; set; }

        [Column("termination_date")]
        public DateTime? TerminationDate { get; set; }

        [Column("employment_status")]
        public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        #endregion

        #region Social Links

        [Column("linkedin_url")]
        [MaxLength(500)]
        public string? LinkedInUrl { get; set; }

        // Additional social profiles (not in Supabase schema, but kept for desktop app)
        [NotMapped]
        public string? FacebookProfile { get; set; }

        [NotMapped]
        public string? InstagramProfile { get; set; }

        [NotMapped]
        public string? XProfile { get; set; }

        #endregion

        #region Cached Counts (for performance)

        [Column("active_goal_count")]
        public int ActiveGoalCount { get; set; } = 0;

        [Column("open_task_count")]
        public int OpenTaskCount { get; set; } = 0;

        #endregion

        #region Meeting Tracking

        [Column("last_meeting_date")]
        public DateTime? LastMeetingDate { get; set; }

        [Column("next_meeting_date")]
        public DateTime? NextMeetingDate { get; set; }

        #endregion

        #region Sync Metadata (for offline support)

        [Column("sync_id")]
        public Guid SyncId { get; set; } = Guid.NewGuid();

        [Column("sync_version")]
        public int SyncVersion { get; set; } = 1;

        [Column("sync_modified_at")]
        public DateTime SyncModifiedAt { get; set; } = DateTime.UtcNow;

        [Column("sync_status")]
        public Common.Enums.SyncStatus SyncStatus { get; set; } = Common.Enums.SyncStatus.Synced;

        #endregion

        #region Legacy Fields (for backwards compatibility - not mapped to PostgreSQL)

        /// <summary>
        /// Legacy manager ID - kept for backwards compatibility with SQLite/SQL Server.
        /// Use ManagerUserId for PostgreSQL.
        /// </summary>
        [NotMapped]
        public int LegacyManagerId { get; set; } = 0;

        /// <summary>
        /// Legacy profile image stored as bytes (SQLite/SQL Server).
        /// Use AvatarUrl for PostgreSQL.
        /// </summary>
        [NotMapped]
        public byte[] ProfileImage { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Engineering specialty (desktop app specific, not in PostgreSQL).
        /// </summary>
        [NotMapped]
        public EngineeringSpecialtyEnum Specialty { get; set; }

        /// <summary>
        /// Skill level (desktop app specific, not in PostgreSQL).
        /// </summary>
        [NotMapped]
        public SkillLevelEnum SkillLevel { get; set; }

        /// <summary>
        /// Role (desktop app specific, not in PostgreSQL - use user_roles table instead).
        /// </summary>
        [NotMapped]
        public RoleEnum Role { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// The organization this team member belongs to.
        /// </summary>
        public Organization? Organization { get; set; }

        /// <summary>
        /// The manager (user) for this team member.
        /// </summary>
        public User? Manager { get; set; }

        /// <summary>
        /// If this team member has a user account, this links to it.
        /// </summary>
        public User? LinkedUser { get; set; }

        /// <summary>
        /// History of manager assignments for this team member.
        /// </summary>
        public ICollection<ManagerHistory> ManagerHistories { get; set; } = new List<ManagerHistory>();

        /// <summary>
        /// Team memberships (which teams this member belongs to).
        /// </summary>
        public ICollection<TeamMembership> TeamMemberships { get; set; } = new List<TeamMembership>();

        #endregion

        #region Computed Display Properties

        /// <summary>
        /// Full name for display.
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        /// <summary>
        /// Alias for FullName for backwards compatibility.
        /// </summary>
        public string Name => FullName;

        /// <summary>
        /// Initials for avatar display.
        /// </summary>
        public string Initials => $"{(FirstName.Length > 0 ? FirstName[0] : ' ')}{(LastName.Length > 0 ? LastName[0] : ' ')}".ToUpper();

        /// <summary>
        /// Years of tenure (from hire date).
        /// </summary>
        public string Tenure
        {
            get
            {
                if (!HireDate.HasValue || HireDate.Value.Year < 1901) return "—";
                var years = (DateTime.Now - HireDate.Value).Days / 365;
                if (years < 1) return "< 1 yr";
                return years == 1 ? "1 yr" : $"{years} yrs";
            }
        }

        /// <summary>
        /// Status display (Active/Inactive).
        /// </summary>
        public string StatusDisplay => IsActive ? "Active" : "Inactive";

        #endregion

        #region Runtime Properties (populated by queries)

        /// <summary>
        /// Display string for last 1:1.
        /// </summary>
        public string LastOneOnOneDisplay
        {
            get
            {
                if (!LastMeetingDate.HasValue) return "Never";
                var days = (DateTime.Now - LastMeetingDate.Value).Days;
                if (days == 0) return "Today";
                if (days == 1) return "Yesterday";
                if (days < 7) return $"{days} days ago";
                if (days < 14) return "1 week ago";
                if (days < 30) return $"{days / 7} weeks ago";
                return LastMeetingDate.Value.ToString("MMM dd");
            }
        }

        /// <summary>
        /// Number of upcoming (scheduled) meetings for this team member (populated at runtime).
        /// </summary>
        [NotMapped]
        public int UpcomingMeetingCount { get; set; }

        /// <summary>
        /// Next scheduled 1:1 date (populated at runtime - uses NextMeetingDate from database).
        /// </summary>
        [NotMapped]
        public DateTime? NextOneOnOneDate
        {
            get => NextMeetingDate;
            set => NextMeetingDate = value;
        }

        /// <summary>
        /// Last 1:1 date - alias to LastMeetingDate for compatibility.
        /// </summary>
        [NotMapped]
        public DateTime? LastOneOnOneDate
        {
            get => LastMeetingDate;
            set => LastMeetingDate = value;
        }

        /// <summary>
        /// Display string for next 1:1.
        /// </summary>
        public string NextOneOnOneDisplay
        {
            get
            {
                if (!NextOneOnOneDate.HasValue) return "—";
                var days = (NextOneOnOneDate.Value - DateTime.Now).Days;
                if (days < 0) return "Overdue";
                if (days == 0) return "Today";
                if (days == 1) return "Tomorrow";
                if (days < 7) return $"In {days} days";
                return NextOneOnOneDate.Value.ToString("MMM dd");
            }
        }

        /// <summary>
        /// Days since last 1:1 meeting (for sorting and display).
        /// </summary>
        public int DaysSinceLastMeeting
        {
            get
            {
                if (!LastOneOnOneDate.HasValue) return 999; // Never had a meeting
                return (DateTime.Now - LastOneOnOneDate.Value).Days;
            }
        }

        #endregion

        #region Microsoft 365 Integration (Runtime only, not persisted)

        /// <summary>
        /// Current presence/availability status from Microsoft 365.
        /// Populated at runtime via Microsoft Graph API.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Services.Microsoft365.PresenceStatus Presence { get; set; } = Services.Microsoft365.PresenceStatus.Unknown;

        /// <summary>
        /// Presence status emoji for display.
        /// </summary>
        public string PresenceEmoji => Presence.ToEmoji();

        /// <summary>
        /// Presence status display text.
        /// </summary>
        public string PresenceDisplay => Presence.ToDisplayString();

        /// <summary>
        /// Profile photo from Azure AD (runtime only).
        /// Falls back to local ProfileImage if not available.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public System.Windows.Media.ImageSource? AzureAdPhoto { get; set; }

        #endregion

        #region Slack Integration (Runtime only, not persisted)

        /// <summary>
        /// Slack user ID (populated at runtime by matching email).
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? SlackUserId { get; set; }

        /// <summary>
        /// Slack presence status (Active/Away).
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Services.Slack.SlackPresence SlackPresence { get; set; } = Services.Slack.SlackPresence.Unknown;

        /// <summary>
        /// Slack presence emoji for display.
        /// </summary>
        public string SlackPresenceEmoji => SlackPresence switch
        {
            Services.Slack.SlackPresence.Active => "🟢",
            Services.Slack.SlackPresence.Away => "🔘",
            _ => "⚪"
        };

        /// <summary>
        /// Slack presence display text.
        /// </summary>
        public string SlackPresenceDisplay => SlackPresence switch
        {
            Services.Slack.SlackPresence.Active => "Active",
            Services.Slack.SlackPresence.Away => "Away",
            _ => "Unknown"
        };

        /// <summary>
        /// Profile photo URL from Slack (runtime only).
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? SlackPhotoUrl { get; set; }

        /// <summary>
        /// Profile photo from Slack as ImageSource (runtime only).
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public System.Windows.Media.ImageSource? SlackPhoto { get; set; }

        #endregion

        #region Combined Presence (Best Available)

        /// <summary>
        /// Combined presence emoji from best available source (M365 > Slack > Unknown).
        /// </summary>
        public string CombinedPresenceEmoji
        {
            get
            {
                // Prefer Microsoft 365 if available
                if (Presence != Services.Microsoft365.PresenceStatus.Unknown)
                    return PresenceEmoji;
                // Fall back to Slack
                if (SlackPresence != Services.Slack.SlackPresence.Unknown)
                    return SlackPresenceEmoji;
                // Unknown
                return "⚪";
            }
        }

        /// <summary>
        /// Combined presence display text from best available source.
        /// </summary>
        public string CombinedPresenceDisplay
        {
            get
            {
                if (Presence != Services.Microsoft365.PresenceStatus.Unknown)
                    return PresenceDisplay;
                if (SlackPresence != Services.Slack.SlackPresence.Unknown)
                    return SlackPresenceDisplay;
                return "Unknown";
            }
        }

        /// <summary>
        /// Best available profile photo (Azure AD > Slack > Local).
        /// </summary>
        public System.Windows.Media.ImageSource? BestProfilePhoto
        {
            get
            {
                // Prefer Azure AD photo
                if (AzureAdPhoto != null)
                    return AzureAdPhoto;
                // Fall back to Slack photo
                if (SlackPhoto != null)
                    return SlackPhoto;
                // Fall back to local profile image (would need converter to ImageSource)
                return null;
            }
        }

        #endregion
    }
}
