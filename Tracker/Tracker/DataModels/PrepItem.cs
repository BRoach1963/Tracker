using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// An individual item within a prep section.
    /// Represents a specific topic, task, or point to discuss.
    /// </summary>
    public class PrepItem
    {
        /// <summary>
        /// Main title/headline for this item.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Additional context or details (e.g., "Due 3 days ago", "Rated 2/5").
        /// </summary>
        public string Subtext { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description or notes about this item.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Priority level determining display order and visual treatment.
        /// </summary>
        public PrepItemPriority Priority { get; set; } = PrepItemPriority.Normal;

        /// <summary>
        /// Type of linked entity, if this item references something specific.
        /// </summary>
        public PrepItemLinkType? LinkType { get; set; }

        /// <summary>
        /// ID of the linked entity (task ID, OKR ID, etc.).
        /// </summary>
        public int? LinkId { get; set; }

        /// <summary>
        /// Whether this item has been added to the meeting agenda.
        /// </summary>
        public bool IsAddedToAgenda { get; set; }

        /// <summary>
        /// Optional icon name for display (e.g., "Warning", "Clock", "Gift").
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Sort key for ordering within a section.
        /// Higher priority items have higher sort values.
        /// </summary>
        public int SortOrder => (int)Priority * 100 + (string.IsNullOrEmpty(Subtext) ? 0 : 10);

        /// <summary>
        /// Creates a formatted string for adding to a meeting agenda.
        /// </summary>
        public string ToAgendaText()
        {
            if (!string.IsNullOrEmpty(Subtext))
            {
                return $"{Title} ({Subtext})";
            }
            return Title;
        }
    }

    /// <summary>
    /// Types of entities that a prep item can link to.
    /// </summary>
    public enum PrepItemLinkType
    {
        /// <summary>
        /// Links to an IndividualTask.
        /// </summary>
        Task,

        /// <summary>
        /// Links to an OKR (Objective).
        /// </summary>
        Okr,

        /// <summary>
        /// Links to a KPI.
        /// </summary>
        Kpi,

        /// <summary>
        /// Links to a MeetingTask (action item from a previous meeting).
        /// </summary>
        MeetingTask,

        /// <summary>
        /// Links to a previous OneOnOne meeting.
        /// </summary>
        Meeting,

        /// <summary>
        /// Links to a PulseSurvey response.
        /// </summary>
        Survey,

        /// <summary>
        /// Links to Feedback given to the team member.
        /// </summary>
        Feedback
    }
}
