using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    /// <summary>
    /// Links a Key Result to a measurable source (KPI, Project, or TaskCollection).
    /// This is a polymorphic association - MeasurableType determines which table MeasurableId points to.
    /// </summary>
    public class KeyResultMeasurable : AuditableEntity
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The organization this measurable belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// FK to the parent Key Result.
        /// </summary>
        public int KeyResultId { get; set; }

        /// <summary>
        /// The type of measurable entity (KPI, Project, TaskCollection).
        /// Determines which table MeasurableId references.
        /// </summary>
        public MeasurableType MeasurableType { get; set; }

        /// <summary>
        /// FK to the measurable entity (KPI, Project, or TaskCollection based on MeasurableType).
        /// </summary>
        public int MeasurableId { get; set; }

        /// <summary>
        /// How to aggregate this source's value with others.
        /// </summary>
        public AggregationTypeEnum AggregationType { get; set; } = AggregationTypeEnum.Latest;

        /// <summary>
        /// Optional weight for weighted aggregation (default 1.0).
        /// </summary>
        public decimal Weight { get; set; } = 1.0m;

        /// <summary>
        /// Sort order for display.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Navigation property to the parent Key Result.
        /// </summary>
        public KeyResult? KeyResult { get; set; }

        #region Computed Properties

        /// <summary>
        /// Display name for the linked measurable (resolved at runtime).
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Current progress of the linked measurable (resolved at runtime).
        /// </summary>
        public decimal? CurrentProgress { get; set; }

        /// <summary>
        /// Display value of the linked measurable (resolved at runtime).
        /// </summary>
        public string? CurrentDisplayValue { get; set; }

        #endregion
    }
}


