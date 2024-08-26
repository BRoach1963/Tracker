using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    public class OneOnOne
    {

        #region Public Properties

        public int Id { get; set; }

        public DateTime Date { get; set; }


        public TimeSpan StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string Agenda { get; set; }

        public string Notes { get; set; }

        public List<ActionItem> ActionItems { get; set; }

        public bool IsRecurring { get; set; }

        public List<string> DiscussionPoints { get; set; }

        public string Feedback { get; set; }

        public List<string> Concerns { get; set; }

        public List<ObjectiveKeyResult> ObjectiveKeyResults { get; set; }

        public List<FollowUpItem> FollowUpItems { get; set; }

        public MeetingStatusEnum Status { get; set; }

        public TeamMember TeamMember { get; set; }

        public string TeamMemberName => $"{TeamMember.FirstName} {TeamMember.LastName}";

        #endregion
    }
}
