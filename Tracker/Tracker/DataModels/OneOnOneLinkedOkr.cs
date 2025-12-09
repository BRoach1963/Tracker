namespace Tracker.DataModels
{
    /// <summary>
    /// Junction entity linking existing ObjectiveKeyResults to OneOnOne meetings.
    /// Allows tracking which OKRs were discussed in which meetings.
    /// </summary>
    public class OneOnOneLinkedOkr : AuditableEntity
    {
        /// <summary>
        /// Unique identifier for this link.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The OneOnOne meeting where this OKR was discussed.
        /// </summary>
        public int OneOnOneId { get; set; }
        public OneOnOne OneOnOne { get; set; } = null!;

        /// <summary>
        /// The ObjectiveKeyResult that was discussed.
        /// </summary>
        public int OkrId { get; set; }
        public ObjectiveKeyResult Okr { get; set; } = null!;

        /// <summary>
        /// Optional notes about the discussion of this OKR in this meeting.
        /// </summary>
        public string DiscussionNotes { get; set; } = string.Empty;
    }
}

