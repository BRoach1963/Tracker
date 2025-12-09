using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    public class Concern : AuditableEntity
    {
        public int Id { get; set; }

        public string Description { get; set; } = string.Empty;

        public ConcernType Type { get; set; }

        public int TeamMemberId { get; set; }

        public int ProjectId { get; set; }

        public DateTime DateRaised { get; set;}

        public Severity SeverityLevel { get; set; }

        public Impact ImpactLevel { get; set; }

        public string Details { get; set; } = string.Empty;

        public bool ActionNeeded { get; set; }

        public ResolutionStatus ResolutionStatus {get; set;}

        public int? ActionItemId { get; set; }

        public ActionItem? LinkedActionItem { get; set; }

    }
}
