using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    /// <summary>
    /// A task that comes out of a 1:1 meeting.
    /// Unified model that replaces both ActionItem and FollowUpItem.
    /// </summary>
    public class MeetingTask : AuditableEntity, ITask
    {
        public int Id { get; set; }

        /// <summary>
        /// The organization this task belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Description of what needs to be done.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When this task is due.
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Whether the task has been completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Additional notes about the task.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Team member responsible for the task.
        /// </summary>
        public TeamMember Owner { get; set; } = new();

        /// <summary>
        /// FK to the 1:1 meeting this task came from.
        /// </summary>
        public int OneOnOneId { get; set; }

        // ITask interface implementations
        public string Status => IsCompleted ? "Completed" : "Pending";
        public string OwnerName => $"{Owner.FirstName} {Owner.LastName}";
        public TaskTypeEnum Type => TaskTypeEnum.ActionItem; // For interface compatibility
    }
}

