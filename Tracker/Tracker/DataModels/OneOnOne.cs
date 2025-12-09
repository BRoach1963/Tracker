using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    public class OneOnOne : AuditableEntity
    {
        #region Public Properties

        public int Id { get; set; } = 0;

        public string Description { get; set; } = string.Empty;

        public DateTime Date { get; set; }


        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string Agenda { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public List<ActionItem> ActionItems { get; set; } = new() { };

        public bool IsRecurring { get; set; }

        public List<DiscussionPoint> DiscussionPoints { get; set; } = new() { };

        public string Feedback { get; set; } = string.Empty;

        public List<Concern> Concerns { get; set; } = new() { };

        public List<ObjectiveKeyResult> ObjectiveKeyResults { get; set; } = new() { };

        public List<FollowUpItem> FollowUpItems { get; set; } = new() { };

        public MeetingStatusEnum Status { get; set; }

        public TeamMember TeamMember { get; set; } = new();

        public string TeamMemberName => $"{TeamMember.FirstName} {TeamMember.LastName}";

        #endregion
    }
}
