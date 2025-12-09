namespace Tracker.DataModels
{
    /// <summary>
    /// Junction entity linking existing KeyPerformanceIndicators to OneOnOne meetings.
    /// Allows tracking which KPIs were discussed in which meetings.
    /// </summary>
    public class OneOnOneLinkedKpi : AuditableEntity
    {
        /// <summary>
        /// Unique identifier for this link.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The OneOnOne meeting where this KPI was discussed.
        /// </summary>
        public int OneOnOneId { get; set; }
        public OneOnOne OneOnOne { get; set; } = null!;

        /// <summary>
        /// The KeyPerformanceIndicator that was discussed.
        /// </summary>
        public int KpiId { get; set; }
        public KeyPerformanceIndicator Kpi { get; set; } = null!;

        /// <summary>
        /// Optional notes about the discussion of this KPI in this meeting.
        /// </summary>
        public string DiscussionNotes { get; set; } = string.Empty;
    }
}

