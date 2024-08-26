using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    public class ActionItem : ITask
    {
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }

        public string Notes { get; set; }

        public string Status => IsCompleted ? "Completed" : "Incomplete";

        public TeamMember Owner { get; set; }

        public string OwnerName => $"{Owner.FirstName} {Owner.LastName}";

        public TaskTypeEnum Type => TaskTypeEnum.ActionItem;
    }
}
