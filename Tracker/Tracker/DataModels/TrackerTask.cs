using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Individual work item - the atomic unit of work in Tracker.
    /// Maps to Supabase 'tasks' table.
    /// Can be standalone, linked to a project, goal, or meeting.
    /// 
    /// Type is determined by which FK is populated:
    /// - MeetingId only → MeetingActionItem
    /// - ProjectId only → ProjectTask
    /// - GoalId only → GoalTask
    /// - None → Standalone
    /// </summary>
    public class TrackerTask : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this task belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member assigned to this task.
        /// </summary>
        public Guid? OwnerTeamMemberId { get; set; }
        public TeamMember? Owner { get; set; }

        /// <summary>
        /// User who created this task.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Parent task (for subtasks). UUID FK to tasks.
        /// </summary>
        public Guid? ParentTaskId { get; set; }
        public TrackerTask? ParentTask { get; set; }

        /// <summary>
        /// Project this task belongs to. UUID FK to projects. Nullable.
        /// </summary>
        public Guid? ProjectId { get; set; }
        public Project? Project { get; set; }

        /// <summary>
        /// Goal this task is linked to. UUID FK to goals. Nullable.
        /// </summary>
        public Guid? GoalId { get; set; }
        public Goal? Goal { get; set; }

        /// <summary>
        /// Meeting this task came from (action item). UUID. Nullable.
        /// </summary>
        public Guid? MeetingId { get; set; }

        /// <summary>
        /// Source agenda item that initiated this task. UUID FK to meeting_agenda_items. Nullable.
        /// </summary>
        public Guid? SourceAgendaItemId { get; set; }

        /// <summary>
        /// Source meeting from which this task originated. UUID FK to meetings. Nullable.
        /// </summary>
        public Guid? SourceMeetingId { get; set; }

        /// <summary>
        /// Task title. VARCHAR(300) NOT NULL
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Task description/details. TEXT. Nullable.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Additional notes about the task. TEXT. Nullable.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Current status - maps to task_status enum.
        /// </summary>
        public WorkItemStatus Status { get; set; } = WorkItemStatus.NotStarted;

        /// <summary>
        /// Priority level - maps to task_priority enum.
        /// </summary>
        public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;

        /// <summary>
        /// When the task is due. TIMESTAMPTZ. Nullable.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// When the task was completed. TIMESTAMPTZ. Nullable.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Sort order for display. INTEGER NOT NULL DEFAULT 0
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Subtasks of this task.
        /// </summary>
        public List<TrackerTask> Subtasks { get; set; } = new();

        #region Computed Properties

        /// <summary>
        /// Number of meetings where this task was discussed.
        /// Not mapped to the database; populated by reporting/analytics queries.
        /// </summary>
        [NotMapped]
        public int MeetingCount { get; set; }

        /// <summary>
        /// Is the task completed?
        /// </summary>
        public bool IsCompleted => Status == WorkItemStatus.Completed;

        /// <summary>
        /// Is the task overdue?
        /// </summary>
        public bool IsOverdue => DueDate.HasValue && 
            DueDate.Value < DateTime.Today && 
            Status != WorkItemStatus.Completed &&
            Status != WorkItemStatus.Cancelled;

        /// <summary>
        /// Days until due date (negative if overdue).
        /// </summary>
        public int? DaysRemaining => DueDate.HasValue 
            ? (int)(DueDate.Value - DateTime.Today).TotalDays 
            : null;

        /// <summary>
        /// Derived task type based on populated FK.
        /// NOT stored in database - computed from which FK is set.
        /// </summary>
        public TaskType DerivedType => 
            MeetingId.HasValue ? TaskType.MeetingActionItem :
            ProjectId.HasValue ? TaskType.ProjectTask :
            GoalId.HasValue ? TaskType.GoalTask :
            TaskType.Standalone;

        #endregion
    }

    /// <summary>
    /// Task type determined by which FK is populated in the Task entity.
    /// This is NOT stored in the database - it's computed from context.
    /// </summary>
    public enum TaskType
    {
        /// <summary>
        /// Standalone task - no FK relationships.
        /// </summary>
        Standalone = 0,

        /// <summary>
        /// Task belongs to a project (ProjectId is set).
        /// </summary>
        ProjectTask = 1,

        /// <summary>
        /// Task is linked to a goal (GoalId is set).
        /// </summary>
        GoalTask = 2,

        /// <summary>
        /// Action item from a meeting (MeetingId is set).
        /// </summary>
        MeetingActionItem = 3
    }
}
