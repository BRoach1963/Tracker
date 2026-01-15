using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Individual work item - the atomic unit of work in Tracker.
    /// Maps to Supabase 'tasks' table (27 columns after ALTER).
    /// Can be standalone, linked to a project, goal, or meeting.
    /// 
    /// Type is determined by which FK is populated:
    /// - MeetingId only → MeetingActionItem
    /// - ProjectId only → ProjectTask
    /// - GoalId only → GoalTask
    /// - None → Standalone
    /// </summary>
    [Table("tasks")]
    public class TrackerTask : AuditableEntity
    {
        #region Primary Key & Foreign Keys

        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this task belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member assigned to this task.
        /// Maps to: owner_team_member_id UUID NULL
        /// </summary>
        [Column("owner_team_member_id")]
        public Guid? OwnerTeamMemberId { get; set; }

        /// <summary>
        /// User who created this task.
        /// Maps to: created_by_user_id UUID NOT NULL
        /// </summary>
        [Column("created_by_user_id")]
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Parent task (for subtasks).
        /// Maps to: parent_task_id UUID NULL
        /// </summary>
        [Column("parent_task_id")]
        public Guid? ParentTaskId { get; set; }

        /// <summary>
        /// Project this task belongs to.
        /// Maps to: project_id UUID NULL
        /// </summary>
        [Column("project_id")]
        public Guid? ProjectId { get; set; }

        /// <summary>
        /// Goal this task is linked to.
        /// Maps to: goal_id UUID NULL
        /// </summary>
        [Column("goal_id")]
        public Guid? GoalId { get; set; }

        /// <summary>
        /// Meeting this task came from (action item).
        /// Maps to: meeting_id UUID NULL
        /// </summary>
        [Column("meeting_id")]
        public Guid? MeetingId { get; set; }

        /// <summary>
        /// Source agenda item that initiated this task.
        /// Maps to: source_agenda_item_id UUID NULL (ADDED)
        /// </summary>
        [Column("source_agenda_item_id")]
        public Guid? SourceAgendaItemId { get; set; }

        /// <summary>
        /// Source meeting from which this task originated.
        /// Maps to: source_meeting_id UUID NULL (ADDED)
        /// </summary>
        [Column("source_meeting_id")]
        public Guid? SourceMeetingId { get; set; }

        #endregion

        #region Content

        /// <summary>
        /// Task title.
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Task description/details.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Additional notes about the task.
        /// Maps to: notes TEXT NULL (ADDED)
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }

        #endregion

        #region Status & Priority

        /// <summary>
        /// Current status (stored as string, enum via computed property).
        /// Maps to: status task_status (enum) NOT NULL DEFAULT 'not_started'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string StatusString { get; set; } = "not_started";

        /// <summary>
        /// Current status as enum.
        /// </summary>
        [NotMapped]
        public WorkItemStatus Status
        {
            get => StatusString switch
            {
                "not_started" => WorkItemStatus.NotStarted,
                "in_progress" => WorkItemStatus.InProgress,
                "completed" => WorkItemStatus.Completed,
                "blocked" => WorkItemStatus.Blocked,
                "cancelled" => WorkItemStatus.Cancelled,
                _ => WorkItemStatus.NotStarted
            };
            set => StatusString = value switch
            {
                WorkItemStatus.NotStarted => "not_started",
                WorkItemStatus.InProgress => "in_progress",
                WorkItemStatus.Completed => "completed",
                WorkItemStatus.Blocked => "blocked",
                WorkItemStatus.Cancelled => "cancelled",
                _ => "not_started"
            };
        }

        /// <summary>
        /// Priority level (stored as string, enum via computed property).
        /// Maps to: priority task_priority (enum) NOT NULL DEFAULT 'medium'
        /// </summary>
        [Column("priority")]
        [MaxLength(50)]
        public string PriorityString { get; set; } = "medium";

        /// <summary>
        /// Priority level as enum.
        /// </summary>
        [NotMapped]
        public WorkItemPriority Priority
        {
            get => PriorityString switch
            {
                "low" => WorkItemPriority.Low,
                "medium" => WorkItemPriority.Medium,
                "high" => WorkItemPriority.High,
                "critical" => WorkItemPriority.Critical,
                _ => WorkItemPriority.Medium
            };
            set => PriorityString = value switch
            {
                WorkItemPriority.Low => "low",
                WorkItemPriority.Medium => "medium",
                WorkItemPriority.High => "high",
                WorkItemPriority.Critical => "critical",
                _ => "medium"
            };
        }

        #endregion

        #region Dates

        /// <summary>
        /// When the task is due.
        /// Maps to: due_date TIMESTAMPTZ NULL
        /// </summary>
        [Column("due_date")]
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// When the task was completed.
        /// Maps to: completed_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        #endregion

        #region Display

        /// <summary>
        /// Sort order for display.
        /// Maps to: sort_order INT4 NOT NULL DEFAULT 0
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; } = 0;

        #endregion

        #region Offline Sync

        /// <summary>
        /// Unique ID for offline sync.
        /// Maps to: sync_id UUID NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("sync_id")]
        public Guid? SyncId { get; set; }

        /// <summary>
        /// Version number for conflict resolution.
        /// Maps to: sync_version INT4 NULL DEFAULT 1
        /// </summary>
        [Column("sync_version")]
        public int? SyncVersion { get; set; } = 1;

        /// <summary>
        /// Last sync modification time.
        /// Maps to: sync_modified_at TIMESTAMPTZ NULL DEFAULT now()
        /// </summary>
        [Column("sync_modified_at")]
        public DateTime? SyncModifiedAt { get; set; }

        /// <summary>
        /// Sync status: synced, pending, conflict.
        /// Maps to: sync_status sync_status (enum) NULL DEFAULT 'synced'
        /// </summary>
        [Column("sync_status")]
        [MaxLength(50)]
        public string? SyncStatus { get; set; } = "synced";

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Owner team member.
        /// </summary>
        public TeamMember? Owner { get; set; }

        /// <summary>
        /// Parent task (for subtasks).
        /// </summary>
        public TrackerTask? ParentTask { get; set; }

        /// <summary>
        /// Project this task belongs to.
        /// </summary>
        public Project? Project { get; set; }

        /// <summary>
        /// Goal this task is linked to.
        /// </summary>
        public Goal? Goal { get; set; }

        /// <summary>
        /// Subtasks of this task.
        /// </summary>
        public List<TrackerTask> Subtasks { get; set; } = new();

        #endregion

        #region Computed Properties (Not Mapped)

        /// <summary>
        /// Number of meetings where this task was discussed.
        /// Not mapped to the database; populated by reporting/analytics queries.
        /// </summary>
        [NotMapped]
        public int MeetingCount { get; set; }

        /// <summary>
        /// Is the task completed?
        /// </summary>
        [NotMapped]
        public bool IsCompleted => Status == WorkItemStatus.Completed;

        /// <summary>
        /// Is the task overdue?
        /// </summary>
        [NotMapped]
        public bool IsOverdue => DueDate.HasValue && 
            DueDate.Value < DateTime.Today && 
            Status != WorkItemStatus.Completed &&
            Status != WorkItemStatus.Cancelled;

        /// <summary>
        /// Days until due date (negative if overdue).
        /// </summary>
        [NotMapped]
        public int? DaysRemaining => DueDate.HasValue 
            ? (int)(DueDate.Value - DateTime.Today).TotalDays 
            : null;

        /// <summary>
        /// Derived task type based on populated FK.
        /// NOT stored in database - computed from which FK is set.
        /// </summary>
        [NotMapped]
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
