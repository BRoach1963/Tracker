namespace Tracker.DataModels
{
    /// <summary>
    /// Historical value entry for a metric.
    /// Maps to Supabase 'metric_history' table.
    /// Used for trending and historical analysis.
    /// </summary>
    public class MetricHistory : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Parent metric this history entry belongs to.
        /// </summary>
        public Guid MetricId { get; set; }
        public Metric? Metric { get; set; }

        /// <summary>
        /// The recorded value.
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// When this value was recorded.
        /// </summary>
        public DateTime RecordedAt { get; set; }

        /// <summary>
        /// User who recorded this value.
        /// </summary>
        public Guid? RecordedByUserId { get; set; }

        /// <summary>
        /// Source of the recording (manual, api, calculated).
        /// </summary>
        public string Source { get; set; } = "manual";

        /// <summary>
        /// Optional notes about this measurement.
        /// </summary>
        public string? Notes { get; set; }
    }
}
