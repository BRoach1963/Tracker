using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a kudos/recognition sent to a team member.
    /// Kudos are composed in Tracker and delivered externally via Teams, Slack, or Email.
    /// </summary>
    public class Kudos : AuditableEntity
    {
        #region Properties

        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The organization this kudos belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Foreign key to the User (manager) who sent this kudos.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Foreign key to the team member receiving this kudos.
        /// </summary>
        public int TeamMemberId { get; set; }

        /// <summary>
        /// Navigation property to the team member.
        /// </summary>
        public virtual TeamMember? TeamMember { get; set; }

        #endregion

        #region Content

        /// <summary>
        /// Optional headline/title for the kudos.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The kudos message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Category of recognition.
        /// </summary>
        public KudosCategory Category { get; set; } = KudosCategory.Other;

        #endregion

        #region Linked Items (Optional)

        /// <summary>
        /// Optional link to a task that prompted this kudos.
        /// </summary>
        public int? LinkedTaskId { get; set; }

        /// <summary>
        /// Optional link to an OKR that prompted this kudos.
        /// </summary>
        public int? LinkedOkrId { get; set; }

        /// <summary>
        /// Optional link to a meeting where this kudos was mentioned.
        /// </summary>
        public int? LinkedMeetingId { get; set; }

        #endregion

        #region Delivery

        /// <summary>
        /// The channel through which this kudos should be/was delivered.
        /// </summary>
        public DeliveryChannel DeliveryChannel { get; set; } = DeliveryChannel.InternalOnly;

        /// <summary>
        /// Current delivery status.
        /// </summary>
        public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Draft;

        /// <summary>
        /// When the kudos was successfully delivered (UTC).
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// Error message if delivery failed.
        /// </summary>
        public string? DeliveryError { get; set; }

        /// <summary>
        /// Optional scheduled delivery time (UTC). If set, kudos will be sent at this time.
        /// </summary>
        public DateTime? ScheduledFor { get; set; }

        #endregion

        #region Visibility Options

        /// <summary>
        /// If true, kudos is also posted to a team channel (not just DM).
        /// </summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// If true, this kudos should appear in meeting prep materials.
        /// </summary>
        public bool MentionInMeetingPrep { get; set; } = true;

        #endregion

        #region Display Helpers

        /// <summary>
        /// Gets a friendly display name for the category.
        /// </summary>
        public string CategoryDisplayName => Category switch
        {
            KudosCategory.TeamWork => "🤝 Team Work",
            KudosCategory.Innovation => "💡 Innovation",
            KudosCategory.Leadership => "👑 Leadership",
            KudosCategory.CustomerFocus => "🎯 Customer Focus",
            KudosCategory.GoingAboveBeyond => "🚀 Above & Beyond",
            KudosCategory.ProblemSolving => "🔧 Problem Solving",
            KudosCategory.LearningGrowth => "📚 Learning & Growth",
            KudosCategory.Reliability => "⏰ Reliability",
            KudosCategory.Communication => "💬 Communication",
            _ => "⭐ Recognition"
        };

        /// <summary>
        /// Gets a friendly display name for the delivery channel.
        /// </summary>
        public string ChannelDisplayName => DeliveryChannel switch
        {
            DeliveryChannel.MicrosoftTeams => "Microsoft Teams",
            DeliveryChannel.Slack => "Slack",
            DeliveryChannel.Email => "Email",
            _ => "Internal Only"
        };

        /// <summary>
        /// Gets a friendly display name for the delivery status.
        /// </summary>
        public string StatusDisplayName => DeliveryStatus switch
        {
            DeliveryStatus.Draft => "Draft",
            DeliveryStatus.Scheduled => "Scheduled",
            DeliveryStatus.Sending => "Sending...",
            DeliveryStatus.Delivered => "✅ Delivered",
            DeliveryStatus.Failed => "❌ Failed",
            _ => "Unknown"
        };

        #endregion
    }
}
