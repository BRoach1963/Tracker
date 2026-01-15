using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Services.Data.Repositories;
using Tracker.DataModels;
using Tracker.DTOs;
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
                var cutoffDate = DateTime.Today.AddDays(-settings.FeedbackLookbackDays);

                var repository = CreateFeedbackRepository();
                if (repository == null)
                {
                    _logger.Debug("No current user context, skipping feedback data");
                    return null;
                }

                // Get feedback for this team member
                var allFeedback = await repository.GetFeedbackForTeamMemberAsync(teamMember.Id);
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
                    var feedbackType = ParseFeedbackType(feedback.FeedbackType, feedback.Sentiment);
                    var priority = GetPriorityFromFeedbackType(feedbackType);
                    var icon = GetIconFromFeedbackType(feedbackType);
                    var typeLabel = GetLabelFromFeedbackType(feedbackType);

                    var subtext = !string.IsNullOrWhiteSpace(feedback.Content) && feedback.Content.Length > 50
                        ? feedback.Content.Substring(0, 50) + "..."
                        : feedback.Content;

                    section.Items.Add(new PrepItem
                    {
                        Title = $"{typeLabel}: {feedback.Content?.Split(new[] { '.', '!' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "Feedback"}",
                        Subtext = $"{feedback.CreatedAt:MMM d} • {subtext}",
                        Description = feedback.Content,
                        Priority = priority,
                        LinkType = PrepItemLinkType.Feedback,
                        LinkId = feedback.Id.GetHashCode(), // Convert Guid to int for compatibility
                        Icon = icon
                    });
                }

                // Update section description
                var positiveFeedback = recentFeedback.Count(f => IsPositiveFeedback(f.FeedbackType, f.Sentiment));
                var constructiveFeedback = recentFeedback.Count(f => IsConstructiveFeedback(f.FeedbackType, f.Sentiment));

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

        private static FeedbackRepository? CreateFeedbackRepository()
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                return null;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            var context = contextFactory.CreateContext();
            return new FeedbackRepository(context, userId.Value, () => contextFactory.CreateContext());
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

        /// <summary>
        /// Parses the feedback type from the string FeedbackType and Sentiment properties.
        /// </summary>
        private static FeedbackType ParseFeedbackType(string feedbackType, string sentiment)
        {
            // First check FeedbackType string
            if (Enum.TryParse<FeedbackType>(feedbackType, true, out var parsed))
                return parsed;
            
            // Fall back to sentiment-based determination
            return sentiment?.ToLowerInvariant() switch
            {
                "positive" => FeedbackType.Positive,
                "constructive" => FeedbackType.Constructive,
                _ => FeedbackType.Positive // Default
            };
        }

        /// <summary>
        /// Checks if the feedback is positive based on type and sentiment.
        /// </summary>
        private static bool IsPositiveFeedback(string feedbackType, string sentiment)
        {
            var type = ParseFeedbackType(feedbackType, sentiment);
            return type == FeedbackType.Positive || type == FeedbackType.Recognition;
        }

        /// <summary>
        /// Checks if the feedback is constructive based on type and sentiment.
        /// </summary>
        private static bool IsConstructiveFeedback(string feedbackType, string sentiment)
        {
            var type = ParseFeedbackType(feedbackType, sentiment);
            return type == FeedbackType.Constructive || type == FeedbackType.Coaching;
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
