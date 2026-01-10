using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents feedback given to a team member.
    /// Tracks feedback history for performance reviews.
    /// </summary>
    public class Feedback : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The organization this feedback belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// The team member this feedback is for.
        /// </summary>
        public Guid TeamMemberId { get; set; }
        public TeamMember TeamMember { get; set; } = null!;

        /// <summary>
        /// When the feedback was given.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Type of feedback (Positive, Constructive, Recognition, etc.)
        /// </summary>
        public FeedbackType Type { get; set; }

        /// <summary>
        /// Brief title/summary of the feedback.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed feedback content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Context where feedback was given (project name, meeting, etc.)
        /// </summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Optional link to 1:1 meeting where feedback was given.
        /// </summary>
        public int? OneOnOneId { get; set; }
    }
}

