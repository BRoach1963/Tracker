namespace Tracker.DataModels
{
    /// <summary>
    /// Junction entity linking existing IndividualTasks to OneOnOne meetings.
    /// Allows tracking which tasks were discussed in which meetings.
    /// </summary>
    public class OneOnOneLinkedTask : AuditableEntity
    {
        /// <summary>
        /// Unique identifier for this link.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The OneOnOne meeting where this task was discussed.
        /// </summary>
        public int OneOnOneId { get; set; }
        public OneOnOne OneOnOne { get; set; } = null!;

        /// <summary>
        /// The IndividualTask that was discussed.
        /// </summary>
        public int TaskId { get; set; }
        public IndividualTask Task { get; set; } = null!;

        /// <summary>
        /// Optional notes about the discussion of this task in this meeting.
        /// </summary>
        public string DiscussionNotes { get; set; } = string.Empty;
    }
}

