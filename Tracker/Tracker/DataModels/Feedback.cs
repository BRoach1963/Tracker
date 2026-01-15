using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents feedback given from one team member to another.
    /// Tracks feedback history for performance reviews and engagement tracking.
    /// Maps to Supabase 'feedback' table (25 columns).
    /// </summary>
    [Table("feedback")]
    public class Feedback : AuditableEntity
    {
        #region Primary Key & Foreign Keys

        /// <summary>
        /// Unique identifier (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// The organization this feedback belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// The team member who gave this feedback.
        /// Maps to: from_team_member_id UUID NOT NULL
        /// </summary>
        [Column("from_team_member_id")]
        public Guid FromTeamMemberId { get; set; }

        /// <summary>
        /// The team member who received this feedback.
        /// Maps to: to_team_member_id UUID NOT NULL
        /// </summary>
        [Column("to_team_member_id")]
        public Guid ToTeamMemberId { get; set; }

        /// <summary>
        /// If this was requested, the ID of the feedback request.
        /// Maps to: request_id UUID NULL
        /// </summary>
        [Column("request_id")]
        public Guid? RequestId { get; set; }

        #endregion

        #region Content

        /// <summary>
        /// Type of feedback (stored as string for enum).
        /// Maps to: feedback_type feedback_type (enum) NOT NULL DEFAULT 'general'
        /// </summary>
        [Column("feedback_type")]
        [MaxLength(50)]
        public string FeedbackType { get; set; } = "general";

        /// <summary>
        /// Sentiment of the feedback (stored as string for enum).
        /// Maps to: sentiment feedback_sentiment (enum) NOT NULL DEFAULT 'neutral'
        /// </summary>
        [Column("sentiment")]
        [MaxLength(50)]
        public string Sentiment { get; set; } = "neutral";

        /// <summary>
        /// The actual feedback content.
        /// Maps to: content TEXT NOT NULL
        /// </summary>
        [Column("content")]
        [Required]
        public string Content { get; set; } = string.Empty;

        #endregion

        #region Context

        /// <summary>
        /// Type of context this feedback relates to: project, meeting, task, general.
        /// Maps to: context_type VARCHAR(50) NULL
        /// </summary>
        [Column("context_type")]
        [MaxLength(50)]
        public string? ContextType { get; set; }

        /// <summary>
        /// ID of the related entity (project, meeting, task, etc.).
        /// Maps to: context_id UUID NULL
        /// </summary>
        [Column("context_id")]
        public Guid? ContextId { get; set; }

        #endregion

        #region Flags

        /// <summary>
        /// Whether this feedback is private (only visible to giver/receiver).
        /// Maps to: is_private BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_private")]
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// Whether this feedback was given in response to a feedback request.
        /// Maps to: is_requested BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_requested")]
        public bool IsRequested { get; set; } = false;

        /// <summary>
        /// Whether the recipient has acknowledged reading this feedback.
        /// Maps to: is_acknowledged BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_acknowledged")]
        public bool IsAcknowledged { get; set; } = false;

        /// <summary>
        /// When the recipient acknowledged this feedback.
        /// Maps to: acknowledged_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("acknowledged_at")]
        public DateTime? AcknowledgedAt { get; set; }

        #endregion

        #region AI Features

        /// <summary>
        /// AI-generated summary of the feedback.
        /// Maps to: ai_summary TEXT NULL
        /// </summary>
        [Column("ai_summary")]
        public string? AiSummary { get; set; }

        /// <summary>
        /// AI-generated tags for categorization (JSONB).
        /// Maps to: ai_tags JSONB NULL
        /// </summary>
        [Column("ai_tags")]
        public string? AiTagsJson { get; set; }

        #endregion

        #region Offline Sync

        /// <summary>
        /// Unique ID for offline sync.
        /// Maps to: sync_id UUID NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("sync_id")]
        public Guid? SyncId { get; set; }

        /// <summary>
        /// Version number for conflict resolution.
        /// Maps to: sync_version INT4 NULL DEFAULT 1
        /// </summary>
        [Column("sync_version")]
        public int? SyncVersion { get; set; } = 1;

        /// <summary>
        /// Last sync modification time.
        /// Maps to: sync_modified_at TIMESTAMPTZ NULL DEFAULT now()
        /// </summary>
        [Column("sync_modified_at")]
        public DateTime? SyncModifiedAt { get; set; }

        /// <summary>
        /// Sync status: synced, pending, conflict.
        /// Maps to: sync_status sync_status (enum) NULL DEFAULT 'synced'
        /// </summary>
        [Column("sync_status")]
        [MaxLength(50)]
        public string? SyncStatus { get; set; } = "synced";

        #endregion

        #region Navigation Properties

        /// <summary>
        /// The organization this feedback belongs to.
        /// </summary>
        public Organization? Organization { get; set; }

        /// <summary>
        /// The team member who gave this feedback.
        /// </summary>
        public TeamMember? FromTeamMember { get; set; }

        /// <summary>
        /// The team member who received this feedback.
        /// </summary>
        public TeamMember? ToTeamMember { get; set; }

        #endregion
    }
}

