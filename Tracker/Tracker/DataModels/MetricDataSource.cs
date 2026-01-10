using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Data source for a metric.
    /// Maps to Supabase 'metric_data_sources' table.
    /// Defines where metric values come from.
    /// </summary>
    public class MetricDataSource : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Parent metric this source belongs to.
        /// </summary>
        public Guid MetricId { get; set; }
        public Metric? Metric { get; set; }

        /// <summary>
        /// Source type (manual, project, task_query, child_metric, api).
        /// </summary>
        public string SourceType { get; set; } = "manual";

        /// <summary>
        /// ID of the source entity (based on source_type).
        /// </summary>
        public Guid? SourceId { get; set; }

        /// <summary>
        /// Additional configuration (JSON).
        /// </summary>
        public string? SourceConfig { get; set; }

        /// <summary>
        /// How to aggregate values from this source.
        /// </summary>
        public AggregationTypeEnum AggregationType { get; set; } = AggregationTypeEnum.Latest;

        #region Runtime Properties

        /// <summary>
        /// Display name for the source (resolved at runtime).
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Current value from this source (resolved at runtime).
        /// </summary>
        public decimal? CurrentValue { get; set; }

        #endregion
    }
}
