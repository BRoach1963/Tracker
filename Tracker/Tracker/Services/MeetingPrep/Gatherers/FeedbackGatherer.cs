using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.MeetingPrep.Gatherers
{
    /// <summary>
    /// Gathers recent feedback given to the team member.
    /// </summary>
    public class FeedbackGatherer : IMeetingPrepGatherer
    {
        private readonly ILogger _logger;

        public string Name => "Feedback Gatherer";
        public PrepSectionType SectionType => PrepSectionType.RecentFeedback;
        public bool IsEnabled { get; set; } = true;

        public FeedbackGatherer()
        {
            _logger = LoggingManager.GetComponentLogger("FeedbackGatherer");
        }

        public async Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var section = PrepSection.Create(PrepSectionType.RecentFeedback);
            var settings = GetSettings();

            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping feedback data");
                    return null;
                }

                var cutoffDate = DateTime.Today.AddDays(-settings.FeedbackLookbackDays);

                // Get feedback for this team member
                var allFeedback = await dbManager.GetFeedbackForTeamMemberAsync(teamMember.Id);
                if (allFeedback == null || allFeedback.Count == 0)
                {
                    return null;
                }

                // Filter to recent feedback
                var recentFeedback = allFeedback
                    .Where(f => f.CreatedAt.Date >= cutoffDate)
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(settings.MaxItemsPerSection)
                    .ToList();

                if (!recentFeedback.Any())
                {
                    return null;
                }

                foreach (var feedback in recentFeedback)
                {
                    var priority = GetPriorityFromFeedbackType(feedback.Type);
                    var icon = GetIconFromFeedbackType(feedback.Type);
                    var typeLabel = GetLabelFromFeedbackType(feedback.Type);

                    var subtext = !string.IsNullOrWhiteSpace(feedback.Content) && feedback.Content.Length > 50
                        ? feedback.Content.Substring(0, 50) + "..."
                        : feedback.Content;

                    section.Items.Add(new PrepItem
                    {
                        Title = $"{typeLabel}: {feedback.Title}",
                        Subtext = $"{feedback.CreatedAt:MMM d} • {subtext}",
                        Description = feedback.Content,
                        Priority = priority,
                        LinkType = PrepItemLinkType.Feedback,
                        LinkId = feedback.Id,
                        Icon = icon
                    });
                }

                // Update section description
                var positiveFeedback = recentFeedback.Count(f => f.Type == FeedbackType.Positive || f.Type == FeedbackType.Recognition);
                var constructiveFeedback = recentFeedback.Count(f => f.Type == FeedbackType.Constructive || f.Type == FeedbackType.Coaching);

                if (positiveFeedback > 0 && constructiveFeedback > 0)
                {
                    section.Description = $"{positiveFeedback} positive, {constructiveFeedback} constructive";
                }
                else if (positiveFeedback > 0)
                {
                    section.Description = $"{positiveFeedback} positive feedback item{(positiveFeedback != 1 ? "s" : "")}";
                }
                else if (constructiveFeedback > 0)
                {
                    section.Description = $"{constructiveFeedback} constructive feedback item{(constructiveFeedback != 1 ? "s" : "")}";
                }
                else
                {
                    section.Description = $"{recentFeedback.Count} recent feedback item{(recentFeedback.Count != 1 ? "s" : "")}";
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error gathering feedback data: {0}", ex.Message);
            }

            return section.HasItems ? section : null;
        }

        private static PrepItemPriority GetPriorityFromFeedbackType(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.Positive => PrepItemPriority.Low,
                FeedbackType.Recognition => PrepItemPriority.Low,
                FeedbackType.Constructive => PrepItemPriority.Normal,
                FeedbackType.Coaching => PrepItemPriority.Normal,
                FeedbackType.PerformanceReview => PrepItemPriority.Normal,
                _ => PrepItemPriority.Normal
            };
        }

        private static string GetIconFromFeedbackType(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.Positive => "ThumbsUp",
                FeedbackType.Recognition => "Star",
                FeedbackType.Constructive => "LightBulb",
                FeedbackType.Coaching => "Person",
                FeedbackType.PerformanceReview => "Document",
                _ => "Comment"
            };
        }

        private static string GetLabelFromFeedbackType(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.Positive => "Positive",
                FeedbackType.Recognition => "Recognition",
                FeedbackType.Constructive => "Constructive",
                FeedbackType.Coaching => "Coaching",
                FeedbackType.PerformanceReview => "Review",
                _ => "Feedback"
            };
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
