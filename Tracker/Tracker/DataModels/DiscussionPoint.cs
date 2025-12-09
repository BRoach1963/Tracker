using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    public class DiscussionPoint : AuditableEntity
    {
        public int Id { get; set; }

        public DiscussionType Type { get; set; }

        public string Description { get; set; } = string.Empty;

        public int TeamMemberId { get; set; }

        public int ProjectId { get; set; }

        public DateTime DateRaised { get; set; }

        public Severity PriorityLevel { get; set; }

        public bool FollowUpRequired { get; set; }

        public string Details { get; set; } = string.Empty;

        public ResolutionStatus FollowUpStatus { get; set; }

        public int? ActionItemId { get; set; }

        public ActionItem? LinkedActionItem { get; set; }
    }
}
