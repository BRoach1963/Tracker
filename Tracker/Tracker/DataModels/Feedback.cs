using System;
using System.Collections.Generic;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents feedback given from one team member to another.
    /// Tracks feedback history for performance reviews and engagement tracking.
    /// Maps to Supabase 'feedback' table.
    /// </summary>
    public class Feedback : AuditableEntity
    {
        /// <summary>
        /// Unique identifier (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The organization this feedback belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        /// <summary>
        /// The team member who gave this feedback.
        /// </summary>
        public Guid FromTeamMemberId { get; set; }
        public TeamMember? FromTeamMember { get; set; }

        /// <summary>
        /// The team member who received this feedback.
        /// </summary>
        public Guid ToTeamMemberId { get; set; }
        public TeamMember? ToTeamMember { get; set; }

        /// <summary>
        /// Type of feedback: general, specific_skill, behavioral, performance.
        /// </summary>
        public string FeedbackType { get; set; } = "general";

        /// <summary>
        /// Sentiment of the feedback: positive, neutral, constructive.
        /// </summary>
        public string Sentiment { get; set; } = "neutral";

        /// <summary>
        /// The actual feedback content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Type of context this feedback relates to: project, meeting, task, general.
        /// </summary>
        public string? ContextType { get; set; }

        /// <summary>
        /// ID of the related entity (project, meeting, task, etc.).
        /// </summary>
        public Guid? ContextId { get; set; }

        /// <summary>
        /// Whether this feedback is private (only visible to giver/receiver).
        /// </summary>
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// Whether this feedback was given in response to a feedback request.
        /// </summary>
        public bool IsRequested { get; set; } = false;

        /// <summary>
        /// If this was requested, the ID of the feedback request.
        /// </summary>
        public Guid? RequestId { get; set; }

        /// <summary>
        /// AI-generated summary of the feedback (optional).
        /// </summary>
        public string? AiSummary { get; set; }

        /// <summary>
        /// AI-generated tags for categorization (JSONB).
        /// </summary>
        public Dictionary<string, object>? AiTags { get; set; }

        /// <summary>
        /// Whether the recipient has acknowledged reading this feedback.
        /// </summary>
        public bool IsAcknowledged { get; set; } = false;

        /// <summary>
        /// When the recipient acknowledged this feedback.
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>
        /// Whether this feedback is deleted (soft delete).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// When this feedback was deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Who deleted this feedback.
        /// </summary>
        public Guid? DeletedBy { get; set; }
    }
}

