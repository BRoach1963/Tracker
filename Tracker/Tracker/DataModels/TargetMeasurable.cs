using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Links a Target to a measurable source (metric, project, task collection).
    /// Maps to Supabase 'target_measurables' table.
    /// This is a polymorphic association - MeasurableType determines which entity MeasurableId references.
    /// </summary>
    public class TargetMeasurable : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Parent target this measurable belongs to.
        /// </summary>
        public Guid TargetId { get; set; }
        public Target? Target { get; set; }

        /// <summary>
        /// Type of measurable entity (metric, project, task_collection).
        /// </summary>
        public string MeasurableType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the measurable entity.
        /// </summary>
        public Guid MeasurableId { get; set; }

        /// <summary>
        /// How to aggregate values from this source.
        /// </summary>
        public AggregationTypeEnum AggregationType { get; set; } = AggregationTypeEnum.Latest;

        #region Runtime Properties

        /// <summary>
        /// Display name for the linked measurable (resolved at runtime).
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Current progress of the linked measurable (resolved at runtime).
        /// </summary>
        public decimal? CurrentProgress { get; set; }

        #endregion
    }
}
