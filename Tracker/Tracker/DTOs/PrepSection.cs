using Tracker.Common.Enums;

namespace Tracker.DTOs
{
    /// <summary>
    /// A section within the meeting prep, grouping related items.
    /// Sections are displayed in priority order with visual grouping.
    /// NOTE: This is a DTO for meeting prep display, NOT a database entity.
    /// </summary>
    public class PrepSection
    {
        /// <summary>
        /// The type of this section, determining its category and default behavior.
        /// </summary>
        public PrepSectionType Type { get; set; }

        /// <summary>
        /// Display title for the section (e.g., "⚠️ Urgent Items", "📋 Task Status").
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Icon name for the section header (e.g., "Warning", "CheckList").
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Description or subtitle for the section.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The items within this section.
        /// </summary>
        public List<PrepItem> Items { get; set; } = new();

        /// <summary>
        /// Whether this section is expanded by default.
        /// Critical/urgent sections default to expanded.
        /// </summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>
        /// Sort order for the section (lower = higher priority, shown first).
        /// </summary>
        public int SortOrder => Type switch
        {
            PrepSectionType.Urgent => 0,
            PrepSectionType.FollowUp => 1,
            PrepSectionType.TaskStatus => 2,
            PrepSectionType.GoalProgress => 3,
            PrepSectionType.SurveyFeedback => 4,
            PrepSectionType.RecentFeedback => 5,
            PrepSectionType.Recognition => 6,
            PrepSectionType.Suggested => 7,
            _ => 99
        };

        /// <summary>
        /// Gets the count of items in this section.
        /// </summary>
        public int ItemCount => Items.Count;

        /// <summary>
        /// Gets whether this section has any items.
        /// </summary>
        public bool HasItems => Items.Count > 0;

        /// <summary>
        /// Gets the highest priority item in this section.
        /// </summary>
        public PrepItemPriority HighestPriority =>
            Items.Any() ? Items.Max(i => i.Priority) : PrepItemPriority.Low;

        /// <summary>
        /// Gets the count of critical items in this section.
        /// </summary>
        public int CriticalItemCount =>
            Items.Count(i => i.Priority == PrepItemPriority.Critical);

        /// <summary>
        /// Gets items sorted by priority (highest first).
        /// </summary>
        public IEnumerable<PrepItem> ItemsByPriority =>
            Items.OrderByDescending(i => i.SortOrder);

        /// <summary>
        /// Creates a standard section with title and icon based on type.
        /// </summary>
        public static PrepSection Create(PrepSectionType type)
        {
            return type switch
            {
                PrepSectionType.Urgent => new PrepSection
                {
                    Type = type,
                    Title = "⚠️ Urgent Items",
                    Icon = "Warning",
                    Description = "Items requiring immediate attention",
                    IsExpanded = true
                },
                PrepSectionType.FollowUp => new PrepSection
                {
                    Type = type,
                    Title = "📋 Follow-ups from Last Meeting",
                    Icon = "History",
                    Description = "Action items from previous 1:1",
                    IsExpanded = true
                },
                PrepSectionType.TaskStatus => new PrepSection
                {
                    Type = type,
                    Title = "✅ Task Status",
                    Icon = "CheckList",
                    Description = "Current task progress and blockers",
                    IsExpanded = true
                },
                PrepSectionType.GoalProgress => new PrepSection
                {
                    Type = type,
                    Title = "🎯 Goal Progress",
                    Icon = "Target",
                    Description = "OKR and KPI status",
                    IsExpanded = false
                },
                PrepSectionType.SurveyFeedback => new PrepSection
                {
                    Type = type,
                    Title = "📊 Survey Feedback",
                    Icon = "Poll",
                    Description = "Recent pulse survey responses",
                    IsExpanded = false
                },
                PrepSectionType.Recognition => new PrepSection
                {
                    Type = type,
                    Title = "🎉 Recognition Opportunities",
                    Icon = "Gift",
                    Description = "Birthdays, anniversaries, achievements",
                    IsExpanded = false
                },
                PrepSectionType.RecentFeedback => new PrepSection
                {
                    Type = type,
                    Title = "💬 Recent Feedback",
                    Icon = "Comment",
                    Description = "Feedback given in the last 30 days",
                    IsExpanded = false
                },
                PrepSectionType.Suggested => new PrepSection
                {
                    Type = type,
                    Title = "💡 AI Suggested Topics",
                    Icon = "Lightbulb",
                    Description = "AI-generated discussion suggestions",
                    IsExpanded = false
                },
                _ => new PrepSection { Type = type }
            };
        }
    }
}
