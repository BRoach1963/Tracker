using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Interfaces
{
    public interface ITask
    {
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
