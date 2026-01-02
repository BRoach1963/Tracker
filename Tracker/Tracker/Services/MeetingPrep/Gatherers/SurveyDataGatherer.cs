using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.MeetingPrep.Gatherers
{
    /// <summary>
    /// Gathers recent survey responses for the team member.
    /// Respects anonymity settings - only shows non-anonymous responses.
    /// </summary>
    public class SurveyDataGatherer : IMeetingPrepGatherer
    {
        private readonly ILogger _logger;

        public string Name => "Survey Data Gatherer";
        public PrepSectionType SectionType => PrepSectionType.SurveyFeedback;
        public bool IsEnabled { get; set; } = true;

        public SurveyDataGatherer()
        {
            _logger = LoggingManager.GetComponentLogger("SurveyDataGatherer");
        }

        public async Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var section = PrepSection.Create(PrepSectionType.SurveyFeedback);
            var settings = GetSettings();

            if (!settings.IncludeSurveyResponses)
            {
                return null;
            }

            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping survey data");
                    return null;
                }

                var cutoffDate = DateTime.Today.AddDays(-settings.SurveyLookbackDays);

                // Get all surveys
                var surveys = await dbManager.GetPulseSurveysAsync();
                if (surveys == null || surveys.Count == 0)
                {
                    return null;
                }

                var recentResponses = new List<(PulseSurvey Survey, PulseSurveyResponse Response)>();

                foreach (var survey in surveys)
                {
                    // CRITICAL: Skip anonymous surveys to protect team member privacy
                    if (survey.IsAnonymous)
                    {
                        continue;
                    }

                    // Find responses from this team member within the lookback period
                    var memberResponses = survey.Responses?
                        .Where(r => r.TeamMemberId == teamMember.Id && 
                                   r.SubmittedAt.Date >= cutoffDate)
                        .ToList();

                    if (memberResponses != null && memberResponses.Any())
                    {
                        foreach (var response in memberResponses)
                        {
                            recentResponses.Add((survey, response));
                        }
                    }
                }

                if (!recentResponses.Any())
                {
                    section.Items.Add(new PrepItem
                    {
                        Title = "No recent survey responses",
                        Subtext = "Or responses are from anonymous surveys",
                        Priority = PrepItemPriority.Low
                    });
                    return section;
                }

                // Process responses to find notable answers
                foreach (var (survey, response) in recentResponses.OrderByDescending(r => r.Response.SubmittedAt)
                    .Take(settings.MaxItemsPerSection))
                {
                    // Find low ratings (potential concerns)
                    var lowRatings = response.Answers?
                        .Where(a => a.RatingValue.HasValue && a.RatingValue.Value <= 2)
                        .ToList() ?? new List<PulseSurveyAnswer>();

                    // Find high ratings (positive notes)
                    var highRatings = response.Answers?
                        .Where(a => a.RatingValue.HasValue && a.RatingValue.Value >= 4)
                        .ToList() ?? new List<PulseSurveyAnswer>();

                    // Add low ratings as concerns (higher priority)
                    foreach (var answer in lowRatings.Take(2))
                    {
                        var question = answer.PulseSurveyQuestion;
                        section.Items.Add(new PrepItem
                        {
                            Title = question?.Text ?? "Survey Response",
                            Subtext = $"Rated {answer.RatingValue}/5 on {response.SubmittedAt:MMM d}",
                            Description = "Consider checking in about this area",
                            Priority = answer.RatingValue <= 1 ? PrepItemPriority.Critical : PrepItemPriority.High,
                            LinkType = PrepItemLinkType.Survey,
                            LinkId = survey.Id,
                            Icon = "Warning"
                        });
                    }

                    // If we have room, add high ratings as positives
                    if (section.Items.Count < settings.MaxItemsPerSection && highRatings.Any())
                    {
                        var topRating = highRatings.OrderByDescending(a => a.RatingValue).First();
                        var question = topRating.PulseSurveyQuestion;
                        section.Items.Add(new PrepItem
                        {
                            Title = question?.Text ?? "Survey Response",
                            Subtext = $"Rated {topRating.RatingValue}/5 on {response.SubmittedAt:MMM d}",
                            Description = "Positive feedback area",
                            Priority = PrepItemPriority.Low,
                            LinkType = PrepItemLinkType.Survey,
                            LinkId = survey.Id,
                            Icon = "Star"
                        });
                    }

                    // Add open-ended responses if any
                    var textAnswers = response.Answers?
                        .Where(a => !string.IsNullOrWhiteSpace(a.TextValue))
                        .Take(2)
                        .ToList();

                    if (textAnswers != null && section.Items.Count < settings.MaxItemsPerSection)
                    {
                        foreach (var answer in textAnswers)
                        {
                            var question = answer.PulseSurveyQuestion;
                            var textPreview = answer.TextValue!.Length > 80
                                ? answer.TextValue.Substring(0, 80) + "..."
                                : answer.TextValue;

                            section.Items.Add(new PrepItem
                            {
                                Title = question?.Text ?? "Open Response",
                                Subtext = textPreview,
                                Priority = PrepItemPriority.Normal,
                                LinkType = PrepItemLinkType.Survey,
                                LinkId = survey.Id,
                                Icon = "Comment"
                            });
                        }
                    }
                }

                // Update section description
                var concernCount = section.Items.Count(i => i.Priority >= PrepItemPriority.High);
                if (concernCount > 0)
                {
                    section.Description = $"{concernCount} area{(concernCount != 1 ? "s" : "")} may need attention";
                    section.IsExpanded = true;
                }
                else
                {
                    section.Description = $"From {recentResponses.Count} recent response{(recentResponses.Count != 1 ? "s" : "")}";
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error gathering survey data: {0}", ex.Message);
            }

            return section.HasItems ? section : null;
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
