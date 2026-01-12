namespace Tracker.DataModels
{
    /// <summary>
    /// Links a Metric to a Meeting for tracking discussions and outcomes.
    /// Maps to Supabase 'meeting_metric_links' table.
    /// </summary>
    public class MeetingMetricLink : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Meeting this link belongs to.
        /// </summary>
        public Guid MeetingId { get; set; }
        public Meeting? Meeting { get; set; }

        /// <summary>
        /// Metric being discussed.
        /// </summary>
        public Guid MetricId { get; set; }
        public Metric? Metric { get; set; }

        /// <summary>
        /// Notes from the discussion about this metric.
        /// </summary>
        public string? DiscussionNotes { get; set; }

        /// <summary>
        /// User who created this link.
        /// </summary>
        public Guid UserId { get; set; }
    }
}
