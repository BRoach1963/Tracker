using Tracker.Common.Enums;
using Tracker.Services.Microsoft365;

namespace Tracker.DataModels
{
    public class TeamMember : AuditableEntity
    {
        #region Public Properties

        public int Id { get; set; } = 0;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string NickName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string CellPhone { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public DateTime BirthDay { get; set; } = DateTime.MinValue;

        public DateTime HireDate { get; set; } = new DateTime(1900, 1, 1);

        public DateTime TerminationDate { get; set; } = new DateTime(1900, 1, 1);

        public bool IsActive { get; set; } = true;

        public int ManagerId { get; set; } = 0;

        public byte[] ProfileImage { get; set; } = Array.Empty<byte>();

        public string LinkedInProfile { get; set; } = string.Empty;

        public string FacebookProfile { get; set; } = string.Empty;

        public string InstagramProfile { get; set; } = string.Empty;

        public string XProfile { get; set; } = string.Empty;

        public EngineeringSpecialtyEnum Specialty { get; set; }

        public SkillLevelEnum SkillLevel { get; set; }

        public RoleEnum Role { get; set; }

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
                if (HireDate.Year < 1901) return "—";
                var years = (DateTime.Now - HireDate).Days / 365;
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
        /// Date of last 1:1 meeting (populated at runtime).
        /// </summary>
        public DateTime? LastOneOnOneDate { get; set; }

        /// <summary>
        /// Display string for last 1:1.
        /// </summary>
        public string LastOneOnOneDisplay
        {
            get
            {
                if (!LastOneOnOneDate.HasValue) return "Never";
                var days = (DateTime.Now - LastOneOnOneDate.Value).Days;
                if (days == 0) return "Today";
                if (days == 1) return "Yesterday";
                if (days < 7) return $"{days} days ago";
                if (days < 14) return "1 week ago";
                if (days < 30) return $"{days / 7} weeks ago";
                return LastOneOnOneDate.Value.ToString("MMM dd");
            }
        }

        /// <summary>
        /// Number of open tasks assigned to this team member (populated at runtime).
        /// </summary>
        public int OpenTaskCount { get; set; }

        /// <summary>
        /// Number of active goals for this team member (populated at runtime).
        /// </summary>
        public int ActiveGoalCount { get; set; }

        /// <summary>
        /// Number of upcoming (scheduled) meetings for this team member (populated at runtime).
        /// </summary>
        public int UpcomingMeetingCount { get; set; }

        /// <summary>
        /// Next scheduled 1:1 date (populated at runtime).
        /// </summary>
        public DateTime? NextOneOnOneDate { get; set; }

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
