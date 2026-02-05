using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for a survey card display with response statistics.
/// Used in PulseView to show survey list.
/// </summary>
public partial class SurveyCardViewModel : ObservableObject
{
    private readonly Survey _survey;

    public SurveyCardViewModel(Survey survey, int totalResponses, int completedResponses)
    {
        _survey = survey;
        TotalResponses = totalResponses;
        CompletedResponses = completedResponses;
    }

    #region Survey Properties

    public Guid Id => _survey.Id;
    public string Title => _survey.Title;
    public string? Description => _survey.Description;
    public string Status => _survey.Status;
    public string StatusDisplay => _survey.StatusDisplay;
    public bool IsDraft => _survey.IsDraft;
    public bool IsActive => _survey.IsActive;
    public bool IsClosed => _survey.IsClosed;
    public DateTime? StartsAt => _survey.StartsAt;
    public DateTime? EndsAt => _survey.EndsAt;
    public string SurveyType => _survey.SurveyType;
    public string Frequency => _survey.Frequency;
    public bool IsAnonymous => _survey.IsAnonymous;

    #endregion

    #region Response Statistics

    [ObservableProperty]
    private int _totalResponses;

    [ObservableProperty]
    private int _completedResponses;

    public int PendingResponses => TotalResponses - CompletedResponses;

    public double CompletionRate => TotalResponses > 0 ? (double)CompletedResponses / TotalResponses * 100 : 0;

    public string CompletionRateText => $"{CompletionRate:F0}%";

    public string ResponseProgressText => $"{CompletedResponses} of {TotalResponses}";

    public bool HasResponses => TotalResponses > 0;
    public bool IsDistributed => TotalResponses > 0;

    #endregion

    #region Display Properties

    public string TypeIcon => SurveyType switch
    {
        "pulse" => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M12,9A3,3 0 0,1 15,12A3,3 0 0,1 12,15A3,3 0 0,1 9,12A3,3 0 0,1 12,9Z",
        "engagement" => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M11,16.5L6.5,12L7.91,10.59L11,13.67L16.59,8.09L18,9.5L11,16.5Z",
        _ => "M19,3H14.82C14.25,1.44 12.53,0.64 11,1.2C10.14,1.5 9.5,2.16 9.18,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M12,3A1,1 0 0,1 13,4A1,1 0 0,1 12,5A1,1 0 0,1 11,4A1,1 0 0,1 12,3Z"
    };

    public string StatusColor => Status switch
    {
        "draft" => "#9E9E9E", // Gray
        "active" => "#4CAF50", // Green
        "closed" => "#F44336", // Red
        "archived" => "#757575", // Dark gray
        _ => "#9E9E9E"
    };

    public string TypeDisplay => SurveyType switch
    {
        "pulse" => "Pulse",
        "engagement" => "Engagement",
        "custom" => "Custom",
        _ => SurveyType
    };

    public string FrequencyDisplay => Frequency switch
    {
        "one_time" => "One Time",
        "weekly" => "Weekly",
        "biweekly" => "Bi-weekly",
        "monthly" => "Monthly",
        "quarterly" => "Quarterly",
        _ => Frequency
    };

    public string DatesDisplay
    {
        get
        {
            if (StartsAt.HasValue && EndsAt.HasValue)
                return $"{StartsAt.Value:MMM d} - {EndsAt.Value:MMM d, yyyy}";
            if (StartsAt.HasValue)
                return $"Starts {StartsAt.Value:MMM d, yyyy}";
            if (EndsAt.HasValue)
                return $"Ends {EndsAt.Value:MMM d, yyyy}";
            return "No dates set";
        }
    }

    #endregion

    #region Helper Methods

    partial void OnCompletedResponsesChanged(int value)
    {
        OnPropertyChanged(nameof(PendingResponses));
        OnPropertyChanged(nameof(CompletionRate));
        OnPropertyChanged(nameof(CompletionRateText));
        OnPropertyChanged(nameof(ResponseProgressText));
    }

    partial void OnTotalResponsesChanged(int value)
    {
        OnPropertyChanged(nameof(PendingResponses));
        OnPropertyChanged(nameof(CompletionRate));
        OnPropertyChanged(nameof(CompletionRateText));
        OnPropertyChanged(nameof(ResponseProgressText));
        OnPropertyChanged(nameof(HasResponses));
        OnPropertyChanged(nameof(IsDistributed));
    }

    #endregion
}
