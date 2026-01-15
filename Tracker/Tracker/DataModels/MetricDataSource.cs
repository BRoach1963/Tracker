using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Data source configuration for a metric.
    /// Maps to Supabase 'metric_data_sources' table.
    /// Defines where metric values come from.
    /// </summary>
    [Table("metric_data_sources")]
    public class MetricDataSource
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent metric this source belongs to.
        /// Maps to: metric_id UUID NOT NULL
        /// </summary>
        [Column("metric_id")]
        public Guid MetricId { get; set; }

        /// <summary>
        /// Source type (manual, project, task_query, child_metric, api).
        /// Maps to: source_type VARCHAR(50) NOT NULL
        /// </summary>
        [Column("source_type")]
        [MaxLength(50)]
        public string SourceType { get; set; } = "manual";

        /// <summary>
        /// ID of the source entity (based on source_type).
        /// Maps to: source_id UUID NULL
        /// </summary>
        [Column("source_id")]
        public Guid? SourceId { get; set; }

        /// <summary>
        /// Additional configuration (JSON).
        /// Maps to: source_config JSONB NULL
        /// </summary>
        [Column("source_config")]
        public string? SourceConfig { get; set; }

        /// <summary>
        /// How to aggregate values from this source (stored as string).
        /// Maps to: aggregation_type VARCHAR(50) NOT NULL DEFAULT 'latest'
        /// </summary>
        [Column("aggregation_type")]
        [MaxLength(50)]
        public string AggregationTypeString { get; set; } = "latest";

        /// <summary>
        /// Aggregation type as enum.
        /// </summary>
        [NotMapped]
        public AggregationTypeEnum AggregationType
        {
            get => Enum.TryParse<AggregationTypeEnum>(AggregationTypeString, true, out var result) ? result : AggregationTypeEnum.Latest;
            set => AggregationTypeString = value.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Parent metric.
        /// </summary>
        [NotMapped]
        public Metric? Metric { get; set; }

        #endregion

        #region Runtime Properties (Not Persisted)

        /// <summary>
        /// Display name for the source (resolved at runtime).
        /// </summary>
        [NotMapped]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Current value from this source (resolved at runtime).
        /// </summary>
        [NotMapped]
        public decimal? CurrentValue { get; set; }

        #endregion
    }
}
