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

        /// <summary>
        /// Links to existing IndividualTasks that were discussed in this meeting.
        /// </summary>
        public List<OneOnOneLinkedTask> LinkedTasks { get; set; } = new() { };

        /// <summary>
        /// Links to existing ObjectiveKeyResults (OKRs) that were discussed in this meeting.
        /// </summary>
        public List<OneOnOneLinkedOkr> LinkedOkrs { get; set; } = new() { };

        /// <summary>
        /// Links to existing KeyPerformanceIndicators (KPIs) that were discussed in this meeting.
        /// </summary>
        public List<OneOnOneLinkedKpi> LinkedKpis { get; set; } = new() { };

        public MeetingStatusEnum Status { get; set; }

        public TeamMember TeamMember { get; set; } = new();

        public string TeamMemberName => $"{TeamMember.FirstName} {TeamMember.LastName}";

        /// <summary>
        /// Google Calendar event ID for this meeting.
        /// </summary>
        public string? GoogleCalendarEventId { get; set; }

        /// <summary>
        /// Outlook Calendar event ID for this meeting.
        /// </summary>
        public string? OutlookCalendarEventId { get; set; }

        /// <summary>
        /// Whether this meeting is synced to Google Calendar.
        /// </summary>
        public bool IsSyncedToGoogle { get; set; }

        /// <summary>
        /// Whether this meeting is synced to Outlook Calendar.
        /// </summary>
        public bool IsSyncedToOutlook { get; set; }

        #endregion
    }
}
