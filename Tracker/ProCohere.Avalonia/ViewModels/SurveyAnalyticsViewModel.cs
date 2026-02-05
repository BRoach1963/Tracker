using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for survey analytics dialog.
/// Shows response statistics and question-level analytics.
/// </summary>
public partial class SurveyAnalyticsViewModel : ObservableObject
{
    private readonly Guid _surveyId;

    #region Survey Info

    [ObservableProperty]
    private Survey? _survey;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _surveyType = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private bool _isAnonymous;

    public string SurveyTypeDisplay => SurveyType switch
    {
        "pulse" => "Pulse Survey",
        "engagement" => "Engagement Survey",
        "custom" => "Custom Survey",
        _ => SurveyType
    };

    public string DatesDisplay
    {
        get
        {
            if (Survey?.StartsAt == null && Survey?.EndsAt == null)
                return "No date range";

            if (Survey?.StartsAt != null && Survey?.EndsAt != null)
                return $"{Survey.StartsAt.Value:MMM d} - {Survey.EndsAt.Value:MMM d, yyyy}";

            if (Survey?.StartsAt != null)
                return $"Starts {Survey.StartsAt.Value:MMM d, yyyy}";

            if (Survey?.EndsAt != null)
                return $"Ends {Survey.EndsAt.Value:MMM d, yyyy}";

            return "No dates";
        }
    }

    #endregion

    #region Response Statistics

    [ObservableProperty]
    private int _totalResponses;

    [ObservableProperty]
    private int _completedResponses;

    [ObservableProperty]
    private int _pendingResponses;

    [ObservableProperty]
    private decimal _completionRate;

    public string CompletionRateText => $"{CompletionRate:F1}%";
    public string ResponseSummary => $"{CompletedResponses} of {TotalResponses} responses";

    #endregion

    #region Question Analytics

    [ObservableProperty]
    private ObservableCollection<QuestionAnalyticsViewModel> _questionAnalytics = new();

    public int TotalQuestions => QuestionAnalytics.Count;
    public bool HasQuestions => QuestionAnalytics.Any();
    public bool NoQuestions => !HasQuestions;

    #endregion

    #region UI State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    #endregion

    public SurveyAnalyticsViewModel(Guid surveyId)
    {
        _surveyId = surveyId;
    }

    /// <summary>
    /// Loads survey data and analytics.
    /// </summary>
    [RelayCommand]
    private async Task LoadAnalyticsAsync(CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            System.Diagnostics.Debug.WriteLine($"[SurveyAnalyticsViewModel] Loading analytics for survey {_surveyId}");

            // Get survey with all questions and responses
            var analyticsData = await SurveyService.Instance.GetSurveyAnalyticsAsync(_surveyId, ct);

            if (analyticsData == null)
            {
                ErrorMessage = "Failed to load survey analytics";
                return;
            }

            // Update survey info
            Survey = analyticsData.Survey;
            Title = analyticsData.Survey.Title;
            Description = analyticsData.Survey.Description;
            SurveyType = analyticsData.Survey.SurveyType;
            Status = analyticsData.Survey.Status;
            IsAnonymous = analyticsData.Survey.IsAnonymous;

            // Update response stats
            TotalResponses = analyticsData.TotalResponses;
            CompletedResponses = analyticsData.CompletedResponses;
            PendingResponses = TotalResponses - CompletedResponses;
            CompletionRate = TotalResponses > 0 ? (decimal)CompletedResponses / TotalResponses * 100 : 0;

            // Update question analytics
            QuestionAnalytics.Clear();
            foreach (var question in analyticsData.Questions.OrderBy(q => q.SortOrder))
            {
                // Get answers for this question
                var questionAnswers = analyticsData.Answers
                    .Where(a => a.QuestionId == question.Id)
                    .ToList();

                var questionAnalytics = new QuestionAnalyticsViewModel(question, questionAnswers);
                QuestionAnalytics.Add(questionAnalytics);
            }

            System.Diagnostics.Debug.WriteLine($"[SurveyAnalyticsViewModel] Loaded {TotalQuestions} questions, {CompletedResponses}/{TotalResponses} responses");

            OnPropertyChanged(nameof(SurveyTypeDisplay));
            OnPropertyChanged(nameof(DatesDisplay));
            OnPropertyChanged(nameof(CompletionRateText));
            OnPropertyChanged(nameof(ResponseSummary));
            OnPropertyChanged(nameof(TotalQuestions));
            OnPropertyChanged(nameof(HasQuestions));
            OnPropertyChanged(nameof(NoQuestions));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load analytics: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[SurveyAnalyticsViewModel] ERROR: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
