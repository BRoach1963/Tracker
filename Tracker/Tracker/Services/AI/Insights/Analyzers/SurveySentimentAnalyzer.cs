using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI.Insights.Analyzers
{
    /// <summary>
    /// Analyzes pulse survey responses and generates insights when team members
    /// submit low ratings that may indicate issues requiring attention.
    /// </summary>
    public class SurveySentimentAnalyzer : IInsightAnalyzer
    {
        private readonly ILogger _logger;

        public string Name => "Survey Sentiment Analyzer";

        public IEnumerable<InsightType> SupportedInsightTypes => new[] { InsightType.SurveyAlert };

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Rating at or below this threshold triggers an alert (on a 5-point scale).
        /// </summary>
        public int LowRatingThreshold { get; set; } = 2;

        /// <summary>
        /// Only analyze responses from the last X days.
        /// </summary>
        public int LookBackDays { get; set; } = 14;

        /// <summary>
        /// NPS score at or below this value is considered a detractor (0-10 scale).
        /// </summary>
        public int NpsDetractorThreshold { get; set; } = 6;

        public SurveySentimentAnalyzer()
        {
            _logger = LoggingManager.GetComponentLogger("SurveySentimentAnalyzer");
        }

        public async Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                var surveyRepository = CreatePulseSurveyRepository();
                if (surveyRepository == null)
                {
                    _logger.Debug("No current user or database context available, skipping survey sentiment analysis");
                    return insights;
                }

                // Get recent surveys with responses
                var surveys = await surveyRepository.GetPulseSurveysAsync();
                if (surveys == null || surveys.Count == 0)
                {
                    _logger.Debug("No surveys found");
                    return insights;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-LookBackDays);

                foreach (var survey in surveys)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Only analyze active or recently closed surveys
                    if (survey.Status == SurveyStatus.Draft || survey.Status == SurveyStatus.Archived)
                        continue;

                    // Analyze responses
                    var surveyInsights = AnalyzeSurveyResponses(survey, cutoffDate);
                    insights.AddRange(surveyInsights);
                }

                _logger.Info("Survey sentiment analysis complete: {0} insights generated", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing survey sentiment: {0}", ex.Message);
            }

            return insights;
        }

        private static PulseSurveyRepository? CreatePulseSurveyRepository()
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                return null;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            var context = contextFactory.CreateContext();
            return new PulseSurveyRepository(context, userId.Value, () => contextFactory.CreateContext());
        }

        /// <summary>
        /// Analyzes responses for a single survey.
        /// </summary>
        private List<Insight> AnalyzeSurveyResponses(PulseSurvey survey, DateTime cutoffDate)
        {
            var insights = new List<Insight>();

            if (survey.Responses == null || survey.Responses.Count == 0)
                return insights;

            // Get recent responses
            var recentResponses = survey.Responses
                .Where(r => r.SubmittedAt >= cutoffDate)
                .ToList();

            if (recentResponses.Count == 0)
                return insights;

            // Track low ratings by question for aggregate insights
            var lowRatingsByQuestion = new Dictionary<int, List<(PulseSurveyResponse Response, PulseSurveyAnswer Answer)>>();

            foreach (var response in recentResponses)
            {
                if (response.Answers == null)
                    continue;

                foreach (var answer in response.Answers)
                {
                    if (answer.PulseSurveyQuestion == null)
                        continue;

                    var isLowRating = IsLowRating(answer);
                    if (isLowRating)
                    {
                        if (!lowRatingsByQuestion.ContainsKey(answer.PulseSurveyQuestionId))
                        {
                            lowRatingsByQuestion[answer.PulseSurveyQuestionId] = new();
                        }
                        lowRatingsByQuestion[answer.PulseSurveyQuestionId].Add((response, answer));
                    }
                }
            }

            // Generate insights for questions with concerning responses
            foreach (var kvp in lowRatingsByQuestion)
            {
                var questionId = kvp.Key;
                var lowResponses = kvp.Value;
                
                if (lowResponses.Count == 0)
                    continue;

                // Get the question details
                var question = lowResponses.First().Answer.PulseSurveyQuestion;
                if (question == null)
                {
                    question = survey.Questions.FirstOrDefault(q => q.Id == questionId);
                }

                var insight = CreateSurveyInsight(survey, question, lowResponses);
                if (insight != null)
                {
                    insights.Add(insight);
                }
            }

            return insights;
        }

        /// <summary>
        /// Determines if an answer represents a low/concerning rating.
        /// </summary>
        private bool IsLowRating(PulseSurveyAnswer answer)
        {
            if (answer.PulseSurveyQuestion == null)
                return false;

            var questionType = answer.PulseSurveyQuestion.QuestionType;

            switch (questionType)
            {
                case SurveyQuestionType.Rating:
                    if (!answer.RatingValue.HasValue)
                        return false;
                    
                    // Normalize the rating to compare against threshold
                    // Most ratings are 1-5, so we use that as baseline
                    var maxRating = answer.PulseSurveyQuestion.RatingMax;
                    var normalizedThreshold = (LowRatingThreshold / 5.0) * maxRating;
                    return answer.RatingValue.Value <= normalizedThreshold;

                case SurveyQuestionType.NPS:
                    if (!answer.RatingValue.HasValue)
                        return false;
                    return answer.RatingValue.Value <= NpsDetractorThreshold;

                case SurveyQuestionType.YesNo:
                    // Can't really determine "low" for yes/no without context
                    return false;

                case SurveyQuestionType.OpenEnded:
                    // Text responses would need NLP analysis - skip for now
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates an insight for a survey question with low ratings.
        /// </summary>
        private Insight? CreateSurveyInsight(
            PulseSurvey survey, 
            PulseSurveyQuestion? question, 
            List<(PulseSurveyResponse Response, PulseSurveyAnswer Answer)> lowResponses)
        {
            if (question == null || lowResponses.Count == 0)
                return null;

            var responseCount = lowResponses.Count;
            var isAnonymous = survey.IsAnonymous;
            
            // Calculate average rating of the low responses
            var avgRating = lowResponses
                .Where(r => r.Answer.RatingValue.HasValue)
                .Select(r => r.Answer.RatingValue!.Value)
                .DefaultIfEmpty(0)
                .Average();

            // Determine severity based on number of low responses
            var severity = responseCount >= 3 ? InsightSeverity.Critical 
                         : responseCount >= 2 ? InsightSeverity.Warning 
                         : InsightSeverity.Info;

            var categoryText = string.IsNullOrEmpty(question.Category) 
                ? "" 
                : $" ({question.Category})";

            var respondentInfo = isAnonymous 
                ? $"{responseCount} anonymous response(s)" 
                : $"{responseCount} team member(s)";

            // Don't reveal specific team members for anonymous surveys
            var teamMemberNames = "";
            if (!isAnonymous && responseCount <= 3)
            {
                var names = lowResponses
                    .Where(r => r.Response.TeamMember != null)
                    .Select(r => r.Response.TeamMember!.FirstName)
                    .Distinct()
                    .ToList();
                if (names.Count > 0)
                {
                    teamMemberNames = $" from {string.Join(", ", names)}";
                }
            }

            return new Insight
            {
                UniqueKey = $"survey_alert_{survey.Id}_{question.Id}_{DateTime.Now:yyyy-MM-dd}",
                Type = InsightType.SurveyAlert,
                Severity = severity,
                Title = $"Low survey ratings{categoryText}",
                Description = $"The question \"{TruncateTitle(question.Text, 60)}\" received low ratings " +
                              $"(avg: {avgRating:F1}) from {respondentInfo}{teamMemberNames} " +
                              $"in the \"{survey.Title}\" survey.",
                ActionSuggestion = isAnonymous 
                    ? "Review survey results and consider addressing the underlying concerns in a team meeting."
                    : "Follow up with the respondent(s) to understand their concerns and how you can help.",
                EntityType = "Survey",
                // EntityId not set - survey.Id is Guid, EntityId is int?
                GeneratedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Truncates a title to a maximum length with ellipsis.
        /// </summary>
        private static string TruncateTitle(string title, int maxLength)
        {
            if (string.IsNullOrEmpty(title) || title.Length <= maxLength)
                return title;
            return title.Substring(0, maxLength - 3) + "...";
        }
    }
}
