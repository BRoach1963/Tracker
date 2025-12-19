using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    /// <summary>
    /// A deliverable with defined scope and timeline.
    /// 
    /// Projects can be:
    /// - Standalone: Not linked to any OKR/KPI
    /// - Linked: Feeds into Key Results via IMeasurable interface
    /// - KPI Source: Provides completion % to KPIs via IKpiSource interface
    /// 
    /// Progress is calculated as: (Completed Tasks / Total Tasks) × 100
    /// </summary>
    public class Project : AuditableEntity, IMeasurable, IKpiSource
    {
        /// <summary>
        /// Primary key for the project.
        /// </summary>
        public int ID { get; set; }
        
        /// <summary>
        /// Project name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Extended description of the project.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Planned start date.
        /// </summary>
        public DateTime StartDate { get; set; }
        
        /// <summary>
        /// Planned end date.
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Current status (NotStarted, InProgress, OnHold, Completed, Cancelled).
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Team member who owns/leads the project.
        /// </summary>
        public TeamMember Owner { get; set; } = new();
        
        /// <summary>
        /// Tasks within this project.
        /// </summary>
        public List<IndividualTask> Tasks { get; set; } = new();
        
        /// <summary>
        /// Team members assigned to this project.
        /// </summary>
        public List<TeamMember> TeamMembers { get; set; } = new();
        
        /// <summary>
        /// Budget allocated for the project.
        /// </summary>
        public decimal Budget { get; set; } = decimal.MinValue;
        
        /// <summary>
        /// Project milestones.
        /// </summary>
        public List<Milestone> Milestones { get; set; } = new();
        
        /// <summary>
        /// Dependencies on other projects.
        /// </summary>
        public List<ProjectDependency> Dependencies { get; set; } = new();
        
        /// <summary>
        /// Identified risks.
        /// </summary>
        public List<Risk> Risks { get; set; } = new();

        #region IMeasurable Implementation

        /// <summary>
        /// IMeasurable.MeasurableId - returns the project ID.
        /// </summary>
        public int MeasurableId => ID;

        /// <summary>
        /// IMeasurable.DisplayName - returns the project name.
        /// </summary>
        public string DisplayName => Name;

        /// <summary>
        /// IMeasurable.Progress - percentage of completed tasks.
        /// </summary>
        public decimal Progress
        {
            get
            {
                if (Tasks == null || Tasks.Count == 0) return 0m;
                var completed = Tasks.Count(t => t.IsCompleted);
                return Math.Round((decimal)completed / Tasks.Count * 100m, 1);
            }
        }

        /// <summary>
        /// IMeasurable.DisplayValue - shows completed/total tasks.
        /// </summary>
        public string DisplayValue
        {
            get
            {
                if (Tasks == null || Tasks.Count == 0) return "0/0 tasks";
                var completed = Tasks.Count(t => t.IsCompleted);
                return $"{completed}/{Tasks.Count} tasks";
            }
        }

        /// <summary>
        /// IMeasurable.MeasurableType - always Project.
        /// </summary>
        public MeasurableType MeasurableType => MeasurableType.Project;

        #endregion

        #region IKpiSource Implementation

        /// <summary>
        /// IKpiSource.SourceId - returns the project ID.
        /// </summary>
        public int SourceId => ID;

        /// <summary>
        /// IKpiSource.SourceDisplayName - returns the project name.
        /// </summary>
        public string SourceDisplayName => Name;

        /// <summary>
        /// IKpiSource.GetValue - returns the completion percentage.
        /// </summary>
        public decimal GetValue() => Progress;

        /// <summary>
        /// IKpiSource.SourceType - always Project.
        /// </summary>
        public KpiSourceType SourceType => KpiSourceType.Project;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Total number of tasks in the project.
        /// </summary>
        public int TotalTasks => Tasks?.Count ?? 0;

        /// <summary>
        /// Number of completed tasks.
        /// </summary>
        public int CompletedTasks => Tasks?.Count(t => t.IsCompleted) ?? 0;

        /// <summary>
        /// Number of incomplete tasks.
        /// </summary>
        public int IncompleteTasks => TotalTasks - CompletedTasks;

        /// <summary>
        /// Whether the project is overdue (past end date and not completed).
        /// </summary>
        public bool IsOverdue => EndDate.HasValue && EndDate.Value < DateTime.Today && Status != "Completed";

        /// <summary>
        /// Days until the project end date (negative if overdue).
        /// </summary>
        public int? DaysRemaining => EndDate.HasValue ? (int)(EndDate.Value - DateTime.Today).TotalDays : null;

        #endregion
    }
}
