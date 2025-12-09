using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    public class ActionItem : AuditableEntity, ITask
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; }

        public string Notes { get; set; } = string.Empty;

        public string Status => IsCompleted ? "Completed" : "Incomplete";

        public TeamMember Owner { get; set; } = new();

        public string OwnerName => $"{Owner.FirstName} {Owner.LastName}";

        public TaskTypeEnum Type => TaskTypeEnum.ActionItem;
    }
}
