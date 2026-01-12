using System.Text;
using Tracker.Classes;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes pulse surveys and their responses for semantic search.
    /// Enables AI to analyze survey results and provide insights.
    /// </summary>
    public class PulseSurveyIndexer : EntityIndexerBase
    {
        private static readonly Lazy<PulseSurveyIndexer> _instance =
            new(() => new PulseSurveyIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static PulseSurveyIndexer Instance => _instance.Value;

        private PulseSurveyIndexer() : base("PulseSurveyIndexer")
        {
        }

        protected override string EntityTypeName => "pulse surveys";

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

        protected override async Task<IEnumerable<object>> FetchEntitiesAsync()
        {
            var repository = CreatePulseSurveyRepository();
            if (repository == null)
            {
                return Enumerable.Empty<object>();
            }

            var surveys = await repository.GetPulseSurveysAsync();
            return surveys.Where(s => !s.IsDeleted).Cast<object>();
        }

        protected override async Task IndexSingleEntityAsync(object entity)
        {
            var survey = (PulseSurvey)entity;
            try
            {
                // Create rich text representation of the survey
                var sb = new StringBuilder();
                sb.AppendLine($"Pulse Survey: {survey.Title}");
                sb.AppendLine($"Status: {survey.Status}");
                
                if (!string.IsNullOrEmpty(survey.Description))
                    sb.AppendLine($"Description: {survey.Description}");
                
                if (survey.SentDate.HasValue)
                    sb.AppendLine($"Sent: {survey.SentDate.Value:MMMM d, yyyy}");
                
                if (survey.DueDate.HasValue)
                    sb.AppendLine($"Due: {survey.DueDate.Value:MMMM d, yyyy}");
                
                if (survey.ClosedDate.HasValue)
                    sb.AppendLine($"Closed: {survey.ClosedDate.Value:MMMM d, yyyy}");

                sb.AppendLine($"Anonymous: {(survey.IsAnonymous ? "Yes" : "No")}");

                // Add questions
                if (survey.Questions?.Any() == true)
                {
                    sb.AppendLine();
                    sb.AppendLine("Questions:");
                    foreach (var question in survey.Questions.OrderBy(q => q.SortOrder))
                    {
                        sb.AppendLine($"  Q{question.SortOrder}: {question.Text} ({question.QuestionType})");
                    }
                }

                // Add response summary
                if (survey.Responses?.Any() == true)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Responses: {survey.Responses.Count} total");
                    
                    // Analyze responses for rating questions
                    var ratingAnswers = survey.Responses
                        .SelectMany(r => r.Answers ?? Enumerable.Empty<PulseSurveyAnswer>())
                        .Where(a => a.RatingValue.HasValue)
                        .ToList();
                    
                    if (ratingAnswers.Any())
                    {
                        var avgRating = ratingAnswers.Average(a => a.RatingValue!.Value);
                        sb.AppendLine($"Average Rating: {avgRating:F1}");
                    }

                    // Include text responses (anonymized if needed)
                    var textAnswers = survey.Responses
                        .SelectMany(r => r.Answers ?? Enumerable.Empty<PulseSurveyAnswer>())
                        .Where(a => !string.IsNullOrEmpty(a.TextValue))
                        .Select(a => a.TextValue!)
                        .Take(10) // Limit to avoid token overflow
                        .ToList();

                    if (textAnswers.Any())
                    {
                        sb.AppendLine();
                        sb.AppendLine("Sample Feedback:");
                        foreach (var feedback in textAnswers)
                        {
                            sb.AppendLine($"  - \"{feedback}\"");
                        }
                    }

                    // Breakdown by question
                    if (survey.Questions?.Any() == true)
                    {
                        sb.AppendLine();
                        sb.AppendLine("Results by Question:");
                        foreach (var question in survey.Questions.OrderBy(q => q.SortOrder))
                        {
                            var questionAnswers = survey.Responses
                                .SelectMany(r => r.Answers ?? Enumerable.Empty<PulseSurveyAnswer>())
                                .Where(a => a.PulseSurveyQuestionId == question.Id)
                                .ToList();

                            if (!questionAnswers.Any()) continue;

                            sb.AppendLine($"  Q{question.SortOrder}: {question.Text}");
                            
                            // Rating summary
                            var ratings = questionAnswers.Where(a => a.RatingValue.HasValue).ToList();
                            if (ratings.Any())
                            {
                                var avg = ratings.Average(a => a.RatingValue!.Value);
                                var min = ratings.Min(a => a.RatingValue!.Value);
                                var max = ratings.Max(a => a.RatingValue!.Value);
                                sb.AppendLine($"    Rating: avg={avg:F1}, min={min}, max={max} ({ratings.Count} responses)");
                            }

                            // Yes/No summary
                            var yesNo = questionAnswers.Where(a => a.BoolValue.HasValue).ToList();
                            if (yesNo.Any())
                            {
                                var yesCount = yesNo.Count(a => a.BoolValue == true);
                                var noCount = yesNo.Count(a => a.BoolValue == false);
                                sb.AppendLine($"    Yes: {yesCount}, No: {noCount}");
                            }
                        }
                    }
                }

                var content = sb.ToString();

                // Metadata for filtering
                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "pulse_survey",
                    ["id"] = survey.Id,
                    ["title"] = survey.Title,
                    ["status"] = survey.Status.ToString(),
                    ["response_count"] = survey.Responses?.Count ?? 0,
                    ["is_anonymous"] = survey.IsAnonymous
                };

                await IndexEntityAsync($"pulse_survey_{survey.Id}", content, metadata);
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing pulse survey {0}: {1}", survey.Id, ex.Message);
            }
        }
    }
}
