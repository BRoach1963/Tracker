using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    public class IndividualTask : AuditableEntity, ITask
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime DueDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Status => IsCompleted ? "Completed" : "Incomplete";
        public TeamMember Owner { get; set; } = new();
        public string OwnerName => $"{Owner.FirstName} {Owner.LastName}";
        public TaskTypeEnum Type => TaskTypeEnum.Individual;
        
        /// <summary>
        /// Number of 1:1 meetings where this task was discussed (non-persisted, computed property).
        /// </summary>
        public int MeetingCount { get; set; }
    }
}
