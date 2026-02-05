using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for analytics of a single survey question.
/// Aggregates response data for display in analytics UI.
/// </summary>
public partial class QuestionAnalyticsViewModel : ObservableObject
{
    private readonly SurveyQuestion _question;
    private readonly List<SurveyAnswer> _answers;

    #region Question Info

    public Guid QuestionId => _question.Id;
    public string QuestionText => _question.QuestionText;
    public string QuestionType => _question.QuestionType;
    public string QuestionTypeDisplay => _question.QuestionTypeDisplay;
    public int SortOrder => _question.SortOrder;
    public bool IsRequired => _question.IsRequired;

    #endregion

    #region Response Stats

    public int TotalResponses => _answers.Count;
    public int ValidResponses => _answers.Count(a => !string.IsNullOrWhiteSpace(a.AnswerText) || a.AnswerNumeric.HasValue);
    public int SkippedResponses => TotalResponses - ValidResponses;
    public decimal ResponseRate => TotalResponses > 0 ? (decimal)ValidResponses / TotalResponses * 100 : 0;
    public string ResponseRateText => $"{ResponseRate:F1}%";
    public string ResponseSummaryText => $"{ValidResponses} responses • {ResponseRateText} response rate";

    #endregion

    #region Rating Question Analytics

    public bool IsRatingQuestion => _question.IsRatingQuestion;
    public int? MinValue => _question.MinValue;
    public int? MaxValue => _question.MaxValue;

    /// <summary>
    /// Average rating (for rating questions).
    /// </summary>
    public decimal? AverageRating
    {
        get
        {
            if (!IsRatingQuestion) return null;
            var numericAnswers = _answers.Where(a => a.AnswerNumeric.HasValue).Select(a => a.AnswerNumeric!.Value).ToList();
            return numericAnswers.Any() ? numericAnswers.Average() : null;
        }
    }

    public string AverageRatingText => AverageRating.HasValue ? $"{AverageRating.Value:F2}" : "N/A";

    /// <summary>
    /// Distribution of ratings (for chart display).
    /// Key = rating value, Value = count of responses.
    /// </summary>
    public Dictionary<int, int> RatingDistribution
    {
        get
        {
            if (!IsRatingQuestion || !MinValue.HasValue || !MaxValue.HasValue)
                return new Dictionary<int, int>();

            // Initialize all possible ratings with 0
            var distribution = new Dictionary<int, int>();
            for (int i = MinValue.Value; i <= MaxValue.Value; i++)
            {
                distribution[i] = 0;
            }

            // Count actual responses
            foreach (var answer in _answers.Where(a => a.AnswerNumeric.HasValue))
            {
                var rating = (int)answer.AnswerNumeric!.Value;
                if (distribution.ContainsKey(rating))
                    distribution[rating]++;
            }

            return distribution;
        }
    }

    /// <summary>
    /// For display in UI - formatted as "Rating: Count" pairs.
    /// </summary>
    public List<RatingCount> RatingCounts
    {
        get
        {
            var total = RatingDistribution.Sum(kvp => kvp.Value);
            return RatingDistribution
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new RatingCount
                {
                    Rating = kvp.Key,
                    Count = kvp.Value,
                    Percentage = total > 0 ? (decimal)kvp.Value / total * 100 : 0
                })
                .ToList();
        }
    }

    #endregion

    #region Text Question Analytics

    public bool IsTextQuestion => _question.IsTextQuestion;

    /// <summary>
    /// All text responses (for text questions).
    /// </summary>
    public List<string> TextResponses
    {
        get
        {
            if (!IsTextQuestion) return new List<string>();
            return _answers
                .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                .Select(a => a.AnswerText!)
                .ToList();
        }
    }

    public string TextResponsesCountText => $"{TextResponses.Count} text responses";

    #endregion

    #region Choice Question Analytics

    public bool IsChoiceQuestion => _question.IsChoiceQuestion;

    /// <summary>
    /// Distribution of choices (for choice/multi-choice questions).
    /// Key = choice text, Value = count of selections.
    /// </summary>
    public Dictionary<string, int> ChoiceDistribution
    {
        get
        {
            if (!IsChoiceQuestion) return new Dictionary<string, int>();

            var distribution = new Dictionary<string, int>();

            foreach (var answer in _answers.Where(a => !string.IsNullOrWhiteSpace(a.AnswerText)))
            {
                var choice = answer.AnswerText!;
                if (!distribution.ContainsKey(choice))
                    distribution[choice] = 0;
                distribution[choice]++;
            }

            return distribution;
        }
    }

    /// <summary>
    /// For display in UI - formatted as choice with count and percentage.
    /// </summary>
    public List<ChoiceCount> ChoiceCounts
    {
        get
        {
            var total = ChoiceDistribution.Sum(kvp => kvp.Value);
            return ChoiceDistribution
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => new ChoiceCount
                {
                    Choice = kvp.Key,
                    Count = kvp.Value,
                    Percentage = total > 0 ? (decimal)kvp.Value / total * 100 : 0
                })
                .ToList();
        }
    }

    #endregion

    public QuestionAnalyticsViewModel(SurveyQuestion question, List<SurveyAnswer> answers)
    {
        _question = question ?? throw new ArgumentNullException(nameof(question));
        _answers = answers ?? new List<SurveyAnswer>();
    }
}

/// <summary>
/// Helper class for rating distribution display.
/// </summary>
public class RatingCount
{
    public int Rating { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Helper class for choice distribution display.
/// </summary>
public class ChoiceCount
{
    public string Choice { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public string PercentageText => $"{Percentage:F1}%";
}
