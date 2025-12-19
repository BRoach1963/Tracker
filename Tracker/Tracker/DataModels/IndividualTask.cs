using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    /// <summary>
    /// An individual task - the atomic unit of work.
    /// Tasks can exist standalone or belong to a Project.
    /// </summary>
    public class IndividualTask : AuditableEntity, ITask
    {
        /// <summary>
        /// Primary key for the task.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Task title/description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether the task is completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// When the task is due.
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Additional notes about the task.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Team member who owns/is assigned this task.
        /// </summary>
        public TeamMember Owner { get; set; } = new();

        /// <summary>
        /// Optional FK to a parent Project. Null for standalone tasks.
        /// </summary>
        public int? ProjectId { get; set; }

        /// <summary>
        /// Navigation property to the parent project.
        /// </summary>
        public Project? Project { get; set; }

        /// <summary>
        /// Optional FK to a parent task (for subtasks).
        /// </summary>
        public int? ParentTaskId { get; set; }

        /// <summary>
        /// Navigation property to the parent task.
        /// </summary>
        public IndividualTask? ParentTask { get; set; }

        /// <summary>
        /// Subtasks of this task.
        /// </summary>
        public List<IndividualTask> Subtasks { get; set; } = new();

        #region ITask Implementation

        /// <summary>
        /// ITask.Status - returns "Completed" or "Incomplete".
        /// </summary>
        public string Status => IsCompleted ? "Completed" : "Incomplete";

        /// <summary>
        /// ITask.OwnerName - full name of the task owner.
        /// </summary>
        public string OwnerName => $"{Owner.FirstName} {Owner.LastName}";

        /// <summary>
        /// ITask.Type - always Individual for this entity.
        /// </summary>
        public TaskTypeEnum Type => TaskTypeEnum.Individual;

        #endregion

        #region Computed Properties
        
        /// <summary>
        /// Number of 1:1 meetings where this task was discussed (non-persisted, computed property).
        /// </summary>
        public int MeetingCount { get; set; }

        /// <summary>
        /// Whether this task is overdue (past due date and not completed).
        /// </summary>
        public bool IsOverdue => !IsCompleted && DueDate.Date < DateTime.Today;

        /// <summary>
        /// Days until the task is due (negative if overdue).
        /// </summary>
        public int DaysUntilDue => (int)(DueDate.Date - DateTime.Today).TotalDays;

        /// <summary>
        /// Whether this task has subtasks.
        /// </summary>
        public bool HasSubtasks => Subtasks?.Count > 0;

        /// <summary>
        /// Progress percentage for tasks with subtasks.
        /// </summary>
        public decimal SubtaskProgress
        {
            get
            {
                if (Subtasks == null || Subtasks.Count == 0) 
                    return IsCompleted ? 100m : 0m;
                var completed = Subtasks.Count(s => s.IsCompleted);
                return Math.Round((decimal)completed / Subtasks.Count * 100m, 1);
            }
        }

        #endregion
    }
}
