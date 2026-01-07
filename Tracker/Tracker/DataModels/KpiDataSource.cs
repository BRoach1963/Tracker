using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    /// <summary>
    /// Links a KPI to a data source (Project, TaskQuery, ChildKpi, or Manual).
    /// This is a polymorphic association - SourceType determines which table SourceId points to.
    /// 
    /// For Manual sources, SourceId is null and the KPI value is entered directly.
    /// For TaskQuery sources, the QueryCriteria field defines what tasks to count.
    /// </summary>
    public class KpiDataSource : AuditableEntity
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The organization this data source belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// FK to the parent KPI.
        /// </summary>
        public int KpiId { get; set; }

        /// <summary>
        /// The type of data source (Project, TaskQuery, ChildKpi, Manual).
        /// </summary>
        public KpiSourceType SourceType { get; set; }

        /// <summary>
        /// FK to the source entity (Project, TaskCollection, or KPI based on SourceType).
        /// Null for Manual source type.
        /// </summary>
        public int? SourceId { get; set; }

        /// <summary>
        /// How to aggregate this source's value with others.
        /// </summary>
        public AggregationTypeEnum AggregationType { get; set; } = AggregationTypeEnum.Latest;

        /// <summary>
        /// Optional weight for weighted aggregation (default 1.0).
        /// </summary>
        public decimal Weight { get; set; } = 1.0m;

        /// <summary>
        /// For TaskQuery sources: JSON criteria for task selection.
        /// Example: {"status": "Completed", "projectId": 5}
        /// </summary>
        public string? QueryCriteria { get; set; }

        /// <summary>
        /// Sort order for display.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Navigation property to the parent KPI.
        /// </summary>
        public KeyPerformanceIndicator? Kpi { get; set; }

        #region Computed Properties

        /// <summary>
        /// Display name for the linked source (resolved at runtime).
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Current value from the source (resolved at runtime).
        /// </summary>
        public decimal? CurrentValue { get; set; }

        #endregion
    }
}


