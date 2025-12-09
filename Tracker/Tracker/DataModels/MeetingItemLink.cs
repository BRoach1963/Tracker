using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a link between a 1:1 meeting and an existing item (Task, OKR, KPI, Project).
    /// This enables tracking which items were discussed in which meetings.
    /// </summary>
    public class MeetingItemLink : AuditableEntity
    {
        /// <summary>
        /// Unique identifier for the link.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the OneOnOne meeting this link belongs to.
        /// </summary>
        public int OneOnOneId { get; set; }

        /// <summary>
        /// Navigation property to the parent meeting.
        /// </summary>
        public OneOnOne? OneOnOne { get; set; }

        /// <summary>
        /// The type of item being linked (Task, OKR, KPI, Project).
        /// </summary>
        public LinkedItemType ItemType { get; set; }

        /// <summary>
        /// The ID of the linked item in its respective table.
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Optional display name for quick reference without loading the full item.
        /// </summary>
        public string ItemDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Optional notes specific to discussing this item in this meeting.
        /// </summary>
        public string DiscussionNotes { get; set; } = string.Empty;

        /// <summary>
        /// Status of this item at the time of the meeting (snapshot).
        /// </summary>
        public string StatusAtMeeting { get; set; } = string.Empty;

        /// <summary>
        /// Whether this item was marked as resolved/completed during this meeting.
        /// </summary>
        public bool WasResolved { get; set; }

        /// <summary>
        /// Order in which this item appears in the meeting agenda.
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}

