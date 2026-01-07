using Tracker.Interfaces;

namespace Tracker.DataModels
{
    /// <summary>
    /// A named collection of tasks that can be treated as a single measurable unit.
    /// Implements IMeasurable to provide progress to Key Results.
    /// Implements IKpiSource to provide values to KPIs.
    /// 
    /// Progress is calculated as: (Completed Tasks / Total Tasks) × 100
    /// </summary>
    public class TaskCollection : AuditableEntity, IMeasurable, IKpiSource
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The organization this task collection belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Name of the task collection.
        /// Example: "Q1 Customer Interviews", "Bug Fixes Sprint 5"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of what this collection represents.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The tasks in this collection.
        /// </summary>
        public List<TaskCollectionItem> Items { get; set; } = new();

        #region IMeasurable Implementation

        /// <summary>
        /// IMeasurable.MeasurableId - returns the collection Id.
        /// </summary>
        public int MeasurableId => Id;

        /// <summary>
        /// IMeasurable.DisplayName - returns the collection name.
        /// </summary>
        public string DisplayName => Name;

        /// <summary>
        /// IMeasurable.Progress - percentage of completed tasks.
        /// </summary>
        public decimal Progress
        {
            get
            {
                if (Items == null || Items.Count == 0) return 0m;
                var completed = Items.Count(i => i.Task?.IsCompleted == true);
                return Math.Round((decimal)completed / Items.Count * 100m, 1);
            }
        }

        /// <summary>
        /// IMeasurable.DisplayValue - shows completed/total tasks.
        /// </summary>
        public string DisplayValue
        {
            get
            {
                if (Items == null || Items.Count == 0) return "0/0 tasks";
                var completed = Items.Count(i => i.Task?.IsCompleted == true);
                return $"{completed}/{Items.Count} tasks";
            }
        }

        /// <summary>
        /// IMeasurable.MeasurableType - always TaskCollection.
        /// </summary>
        public MeasurableType MeasurableType => MeasurableType.TaskCollection;

        #endregion

        #region IKpiSource Implementation

        /// <summary>
        /// IKpiSource.SourceId - returns the collection Id.
        /// </summary>
        public int SourceId => Id;

        /// <summary>
        /// IKpiSource.SourceDisplayName - returns the collection name.
        /// </summary>
        public string SourceDisplayName => Name;

        /// <summary>
        /// IKpiSource.GetValue - returns the count of completed tasks.
        /// </summary>
        public decimal GetValue()
        {
            if (Items == null) return 0m;
            return Items.Count(i => i.Task?.IsCompleted == true);
        }

        /// <summary>
        /// IKpiSource.SourceType - always TaskQuery for collections.
        /// </summary>
        public KpiSourceType SourceType => KpiSourceType.TaskQuery;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Total number of tasks in the collection.
        /// </summary>
        public int TotalTasks => Items?.Count ?? 0;

        /// <summary>
        /// Number of completed tasks.
        /// </summary>
        public int CompletedTasks => Items?.Count(i => i.Task?.IsCompleted == true) ?? 0;

        /// <summary>
        /// Number of incomplete tasks.
        /// </summary>
        public int IncompleteTasks => TotalTasks - CompletedTasks;

        #endregion
    }
}


