using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// Historical value entry for a metric.
    /// Maps to Supabase 'metric_history' table.
    /// Used for trending and historical analysis.
    /// </summary>
    [Table("metric_history")]
    public class MetricHistory
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent metric this history entry belongs to.
        /// Maps to: metric_id UUID NOT NULL
        /// </summary>
        [Column("metric_id")]
        public Guid MetricId { get; set; }

        /// <summary>
        /// The recorded value.
        /// Maps to: value NUMERIC NOT NULL
        /// </summary>
        [Column("value")]
        public decimal Value { get; set; }

        /// <summary>
        /// When this value was recorded.
        /// Maps to: recorded_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("recorded_at")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who recorded this value.
        /// Maps to: recorded_by_user_id UUID NULL
        /// </summary>
        [Column("recorded_by_user_id")]
        public Guid? RecordedByUserId { get; set; }

        /// <summary>
        /// Source of the recording (manual, api, calculated).
        /// Maps to: source VARCHAR(50) NULL DEFAULT 'manual'
        /// </summary>
        [Column("source")]
        [MaxLength(50)]
        public string? Source { get; set; } = "manual";

        /// <summary>
        /// Optional notes about this measurement.
        /// Maps to: notes TEXT NULL
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Parent metric.
        /// </summary>
        [NotMapped]
        public Metric? Metric { get; set; }

        #endregion
    }
}
