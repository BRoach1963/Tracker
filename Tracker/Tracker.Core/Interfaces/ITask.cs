using Tracker.Core.Common.Enums;
using Tracker.Core.DataModels;

namespace Tracker.Core.Interfaces
{
    /// <summary>
    /// Common interface for all task types (Individual, ActionItem, FollowUp).
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Unique identifier for the task.
        /// </summary>
        int Id { get; set; }

        string Description { get; set; }

        bool IsCompleted { get; set; }

        DateTime DueDate { get; set; }

        string Notes { get; set; }

        string Status { get; }

        TeamMember Owner { get; set; }

        string OwnerName { get; }

        TaskTypeEnum Type { get; }
    }
}
