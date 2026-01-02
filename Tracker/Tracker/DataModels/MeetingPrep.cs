using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Auto-generated meeting preparation package for a 1:1 meeting.
    /// Contains all relevant information gathered from various sources
    /// to help managers prepare for and conduct effective 1:1s.
    /// </summary>
    public class MeetingPrep
    {
        #region Core Properties

        /// <summary>
        /// The ID of the OneOnOne meeting this prep is for.
        /// </summary>
        public int MeetingId { get; set; }

        /// <summary>
        /// The team member this meeting is with.
        /// </summary>
        public TeamMember TeamMember { get; set; } = new();

        /// <summary>
        /// The scheduled date of the meeting.
        /// </summary>
        public DateTime MeetingDate { get; set; }

        /// <summary>
        /// When this prep was generated.
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        #endregion

        #region Prep Content

        /// <summary>
        /// All sections of the prep package.
        /// </summary>
        public List<PrepSection> Sections { get; set; } = new();

        /// <summary>
        /// AI-generated agenda suggestions (optional, if AI is enabled).
        /// </summary>
        public string AiSuggestedAgenda { get; set; } = string.Empty;

        #endregion

        #region Statistics

        /// <summary>
        /// Total count of overdue tasks.
        /// </summary>
        public int OverdueTaskCount { get; set; }

        /// <summary>
        /// Total count of open action items from previous meetings.
        /// </summary>
        public int OpenActionItemCount { get; set; }

        /// <summary>
        /// Count of OKRs at risk or behind schedule.
        /// </summary>
        public int OkrsAtRiskCount { get; set; }

        /// <summary>
        /// Days since last 1:1 meeting.
        /// </summary>
        public int DaysSinceLastMeeting { get; set; }

        #endregion

        #region Display Properties

        /// <summary>
        /// Meeting title for display.
        /// </summary>
        public string MeetingTitle => $"1:1 with {TeamMember.FullName}";

        /// <summary>
        /// Scheduled date formatted for display.
        /// </summary>
        public string ScheduledDateDisplay => MeetingDate.ToString("dddd, MMMM d, yyyy");

        /// <summary>
        /// Summary of key statistics for display.
        /// </summary>
        public string StatsSummary
        {
            get
            {
                var parts = new List<string>();
                
                if (OverdueTaskCount > 0)
                    parts.Add($"{OverdueTaskCount} overdue");
                if (OpenActionItemCount > 0)
                    parts.Add($"{OpenActionItemCount} action items");
                if (OkrsAtRiskCount > 0)
                    parts.Add($"{OkrsAtRiskCount} OKRs at risk");
                
                return parts.Any() ? string.Join(" • ", parts) : "Looking good!";
            }
        }

        /// <summary>
        /// Gets sections sorted by priority.
        /// </summary>
        public IEnumerable<PrepSection> SectionsByPriority =>
            Sections.OrderBy(s => s.SortOrder);

        /// <summary>
        /// Gets sections that have items (non-empty sections).
        /// </summary>
        public IEnumerable<PrepSection> NonEmptySections =>
            Sections.Where(s => s.HasItems).OrderBy(s => s.SortOrder);

        /// <summary>
        /// Total count of all items across all sections.
        /// </summary>
        public int TotalItemCount => Sections.Sum(s => s.ItemCount);

        /// <summary>
        /// Total count of critical priority items.
        /// </summary>
        public int CriticalItemCount =>
            Sections.Sum(s => s.Items.Count(i => i.Priority == PrepItemPriority.Critical));

        /// <summary>
        /// Whether there are any urgent items requiring attention.
        /// </summary>
        public bool HasUrgentItems =>
            Sections.Any(s => s.Type == PrepSectionType.Urgent && s.HasItems);

        /// <summary>
        /// Whether AI suggestions are available.
        /// </summary>
        public bool HasAiSuggestions => !string.IsNullOrWhiteSpace(AiSuggestedAgenda);

        #endregion

        #region Methods

        /// <summary>
        /// Gets a section by type, creating it if it doesn't exist.
        /// </summary>
        public PrepSection GetOrCreateSection(PrepSectionType type)
        {
            var section = Sections.FirstOrDefault(s => s.Type == type);
            if (section == null)
            {
                section = PrepSection.Create(type);
                Sections.Add(section);
            }
            return section;
        }

        /// <summary>
        /// Adds an item to a section, creating the section if needed.
        /// </summary>
        public void AddItem(PrepSectionType sectionType, PrepItem item)
        {
            var section = GetOrCreateSection(sectionType);
            section.Items.Add(item);
        }

        /// <summary>
        /// Removes empty sections from the prep.
        /// </summary>
        public void PruneEmptySections()
        {
            Sections.RemoveAll(s => !s.HasItems);
        }

        /// <summary>
        /// Sorts all items within each section by priority.
        /// </summary>
        public void SortAllItems()
        {
            foreach (var section in Sections)
            {
                section.Items = section.Items
                    .OrderByDescending(i => i.SortOrder)
                    .ToList();
            }
        }

        /// <summary>
        /// Limits items per section to a maximum count.
        /// </summary>
        public void LimitItemsPerSection(int maxItems)
        {
            foreach (var section in Sections)
            {
                if (section.Items.Count > maxItems)
                {
                    // Keep highest priority items
                    section.Items = section.Items
                        .OrderByDescending(i => i.SortOrder)
                        .Take(maxItems)
                        .ToList();
                }
            }
        }

        #endregion
    }
}
