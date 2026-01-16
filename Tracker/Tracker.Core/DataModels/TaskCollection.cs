using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Core.Interfaces;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// A named collection of tasks that can be treated as a single measurable unit.
    /// Maps to Supabase 'task_collections' table.
    /// Implements IMeasurable to provide progress to Targets.
    /// Implements IKpiSource to provide values to Metrics.
    /// 
    /// Progress is calculated as: (Completed Tasks / Total Tasks) × 100
    /// </summary>
    [Table("task_collections")]
    public class TaskCollection : IMeasurable, IKpiSource
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The organization this task collection belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Name of the task collection.
        /// Maps to: name VARCHAR(200) NOT NULL
        /// Example: "Q1 Customer Interviews", "Bug Fixes Sprint 5"
        /// </summary>
        [Column("name")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of what this collection represents.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Optional query configuration for dynamic task collections.
        /// Maps to: query_config JSONB NULL
        /// </summary>
        [Column("query_config")]
        public string? QueryConfig { get; set; }

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

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        #region Navigation Properties

        /// <summary>
        /// The tasks in this collection.
        /// </summary>
        [NotMapped]
        public List<TaskCollectionItem> Items { get; set; } = new();

        #endregion

        #region IMeasurable Implementation

        /// <summary>
        /// IMeasurable.GuidId - returns the collection Id.
        /// </summary>
        Guid IMeasurable.GuidId => Id;

        /// <summary>
        /// IMeasurable.Id - legacy int, deprecated.
        /// </summary>
        [Obsolete("Use GuidId instead")]
        int IMeasurable.Id => 0;

        /// <summary>
        /// IMeasurable.DisplayName - returns the collection name.
        /// </summary>
        [NotMapped]
        public string DisplayName => Name;

        /// <summary>
        /// IMeasurable.CurrentProgress - percentage of completed tasks.
        /// </summary>
        decimal IMeasurable.CurrentProgress => Progress;

        /// <summary>
        /// Progress - percentage of completed tasks (0-100).
        /// </summary>
        [NotMapped]
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
        /// DisplayValue - shows completed/total tasks.
        /// </summary>
        [NotMapped]
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
        /// MeasurableType - always TaskCollection.
        /// </summary>
        [NotMapped]
        public MeasurableType MeasurableType => MeasurableType.TaskCollection;

        #endregion

        #region IKpiSource Implementation

        /// <summary>
        /// IKpiSource.SourceId - legacy int, deprecated.
        /// </summary>
        [Obsolete("Use Id (Guid) instead")]
        int IKpiSource.SourceId => 0;

        /// <summary>
        /// IKpiSource.SourceDisplayName - returns the collection name.
        /// </summary>
        string IKpiSource.SourceDisplayName => Name;

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
        KpiSourceType IKpiSource.SourceType => KpiSourceType.TaskQuery;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Total number of tasks in the collection.
        /// </summary>
        [NotMapped]
        public int TotalTasks => Items?.Count ?? 0;

        /// <summary>
        /// Number of completed tasks.
        /// </summary>
        [NotMapped]
        public int CompletedTasks => Items?.Count(i => i.Task?.IsCompleted == true) ?? 0;

        /// <summary>
        /// Number of incomplete tasks.
        /// </summary>
        [NotMapped]
        public int IncompleteTasks => TotalTasks - CompletedTasks;

        #endregion
    }
}


