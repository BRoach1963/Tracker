using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A reusable template for 1:1 meetings with pre-defined agenda items.
    /// </summary>
    public class MeetingTemplate : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Name of the template (e.g., "Weekly Check-in", "Performance Review").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of when to use this template.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Suggested duration in minutes.
        /// </summary>
        public int SuggestedDurationMinutes { get; set; } = 30;

        /// <summary>
        /// Pre-defined agenda items for this template.
        /// </summary>
        public List<MeetingTemplateItem> Items { get; set; } = new();

        /// <summary>
        /// Whether this is a system-provided template or user-created.
        /// </summary>
        public bool IsSystemTemplate { get; set; }

        /// <summary>
        /// Sort order for display.
        /// </summary>
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// An agenda item within a meeting template.
    /// </summary>
    public class MeetingTemplateItem : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The template this item belongs to.
        /// </summary>
        public int MeetingTemplateId { get; set; }

        /// <summary>
        /// The description/topic of the agenda item.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category of the agenda item.
        /// </summary>
        public AgendaItemCategory Category { get; set; } = AgendaItemCategory.Topic;

        /// <summary>
        /// Priority of the agenda item.
        /// </summary>
        public Severity Priority { get; set; } = Severity.Medium;

        /// <summary>
        /// Sort order within the template.
        /// </summary>
        public int SortOrder { get; set; }
    }
}

