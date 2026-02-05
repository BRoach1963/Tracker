using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

public partial class CreateSurveyDialogViewModel : ObservableObject
{
    #region Observable Properties - Basic Info

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _surveyTypeIndex = 0; // 0=pulse, 1=engagement, 2=custom

    #endregion

    #region Observable Properties - Questions

    [ObservableProperty]
    private ObservableCollection<SurveyQuestionViewModel> _questions = new();

    [ObservableProperty]
    private SurveyQuestionViewModel? _selectedQuestion;

    [ObservableProperty]
    private bool _isAddingQuestion;

    [ObservableProperty]
    private string _newQuestionText = string.Empty;

    [ObservableProperty]
    private int _newQuestionTypeIndex = 0; // 0=text, 1=rating, 2=choice

    [ObservableProperty]
    private string _newQuestionOptions = string.Empty; // Comma-separated for choice questions

    [ObservableProperty]
    private bool _newQuestionRequired = true;

    [ObservableProperty]
    private int _newQuestionMinValue = 1;

    [ObservableProperty]
    private int _newQuestionMaxValue = 5;

    #endregion

    #region Observable Properties - Schedule

    [ObservableProperty]
    private int _frequencyIndex = 0; // 0=one_time, 1=weekly, 2=biweekly, 3=monthly

    [ObservableProperty]
    private DateTime? _startsAt = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private DateTime? _endsAt;

    #endregion

    #region Observable Properties - Targeting

    [ObservableProperty]
    private bool _targetAllEmployees = true;

    #endregion

    #region Observable Properties - Settings

    [ObservableProperty]
    private bool _isAnonymous = false;

    [ObservableProperty]
    private bool _allowComments = true;

    [ObservableProperty]
    private bool _reminderEnabled = true;

    [ObservableProperty]
    private int _reminderDaysBeforeClose = 2;

    [ObservableProperty]
    private string _welcomeMessage = string.Empty;

    [ObservableProperty]
    private string _thankYouMessage = "Thank you for your feedback!";

    #endregion

    #region Observable Properties - UI State

    [ObservableProperty]
    private int _currentStep = 0; // 0=Basic, 1=Questions, 2=Schedule, 3=Targeting, 4=Settings

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Computed Properties

    public bool IsBasicStep => CurrentStep == 0;
    public bool IsQuestionsStep => CurrentStep == 1;
    public bool IsScheduleStep => CurrentStep == 2;
    public bool IsTargetingStep => CurrentStep == 3;
    public bool IsSettingsStep => CurrentStep == 4;

    public bool CanGoNext => CurrentStep < 4;
    public bool CanGoPrevious => CurrentStep > 0;
    public bool CanSave => CurrentStep == 4 && !string.IsNullOrWhiteSpace(Title) && Questions.Count > 0;

    public bool IsRatingQuestion => NewQuestionTypeIndex == 1;
    public bool IsChoiceQuestion => NewQuestionTypeIndex == 2;

    public string StepTitle => CurrentStep switch
    {
        0 => "Basic Information",
        1 => "Questions",
        2 => "Schedule",
        3 => "Target Audience",
        4 => "Settings",
        _ => "Survey Setup"
    };

    public string SurveyType => SurveyTypeIndex switch
    {
        0 => "pulse",
        1 => "engagement",
        2 => "custom",
        _ => "custom"
    };

    public string Frequency => FrequencyIndex switch
    {
        0 => "one_time",
        1 => "weekly",
        2 => "biweekly",
        3 => "monthly",
        _ => "one_time"
    };

    public string QuestionType => NewQuestionTypeIndex switch
    {
        0 => "text",
        1 => "rating",
        2 => "choice",
        _ => "text"
    };

    #endregion

    #region Result

    public Survey? Result { get; private set; }

    #endregion

    #region Commands

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < 4)
        {
            CurrentStep++;
            OnPropertyChanged(nameof(IsBasicStep));
            OnPropertyChanged(nameof(IsQuestionsStep));
            OnPropertyChanged(nameof(IsScheduleStep));
            OnPropertyChanged(nameof(IsTargetingStep));
            OnPropertyChanged(nameof(IsSettingsStep));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(StepTitle));
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
            OnPropertyChanged(nameof(IsBasicStep));
            OnPropertyChanged(nameof(IsQuestionsStep));
            OnPropertyChanged(nameof(IsScheduleStep));
            OnPropertyChanged(nameof(IsTargetingStep));
            OnPropertyChanged(nameof(IsSettingsStep));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(StepTitle));
        }
    }

    [RelayCommand]
    private void StartAddQuestion()
    {
        IsAddingQuestion = true;
        NewQuestionText = string.Empty;
        NewQuestionTypeIndex = 0;
        NewQuestionOptions = string.Empty;
        NewQuestionRequired = true;
        NewQuestionMinValue = 1;
        NewQuestionMaxValue = 5;
    }

    [RelayCommand]
    private void SaveNewQuestion()
    {
        if (string.IsNullOrWhiteSpace(NewQuestionText))
        {
            SetError("Question text is required");
            return;
        }

        var question = new SurveyQuestionViewModel
        {
            QuestionText = NewQuestionText.Trim(),
            QuestionType = QuestionType,
            IsRequired = NewQuestionRequired,
            SortOrder = Questions.Count + 1
        };

        if (IsRatingQuestion)
        {
            question.MinValue = NewQuestionMinValue;
            question.MaxValue = NewQuestionMaxValue;
        }
        else if (IsChoiceQuestion)
        {
            question.Options = NewQuestionOptions.Trim();
        }

        Questions.Add(question);
        IsAddingQuestion = false;
        ClearError();
    }

    [RelayCommand]
    private void CancelAddQuestion()
    {
        IsAddingQuestion = false;
        ClearError();
    }

    [RelayCommand]
    private void RemoveQuestion(SurveyQuestionViewModel question)
    {
        Questions.Remove(question);
        // Update sort order
        for (int i = 0; i < Questions.Count; i++)
        {
            Questions[i].SortOrder = i + 1;
        }
    }

    [RelayCommand]
    private void MoveQuestionUp(SurveyQuestionViewModel question)
    {
        var index = Questions.IndexOf(question);
        if (index > 0)
        {
            Questions.Move(index, index - 1);
            // Update sort order
            for (int i = 0; i < Questions.Count; i++)
            {
                Questions[i].SortOrder = i + 1;
            }
        }
    }

    [RelayCommand]
    private void MoveQuestionDown(SurveyQuestionViewModel question)
    {
        var index = Questions.IndexOf(question);
        if (index < Questions.Count - 1)
        {
            Questions.Move(index, index + 1);
            // Update sort order
            for (int i = 0; i < Questions.Count; i++)
            {
                Questions[i].SortOrder = i + 1;
            }
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!ValidateSurvey())
            return;

        IsSaving = true;
        ClearError();

        try
        {
            Result = new Survey
            {
                Id = Guid.NewGuid(),
                Title = Title.Trim(),
                Description = Description?.Trim(),
                SurveyType = SurveyType,
                Frequency = Frequency,
                StartsAt = StartsAt,
                EndsAt = EndsAt,
                TargetAllEmployees = TargetAllEmployees,
                IsAnonymous = IsAnonymous,
                AllowComments = AllowComments,
                ReminderEnabled = ReminderEnabled,
                ReminderDaysBeforeClose = ReminderEnabled ? ReminderDaysBeforeClose : null,
                WelcomeMessage = WelcomeMessage?.Trim(),
                ThankYouMessage = ThankYouMessage?.Trim(),
                Status = "draft"
            };

            // Convert ViewModels to Survey Questions
            Result.Questions = Questions.Select(q => new SurveyQuestion
            {
                Id = Guid.NewGuid(),
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Options = q.Options,
                IsRequired = q.IsRequired,
                SortOrder = q.SortOrder,
                MinValue = q.MinValue,
                MaxValue = q.MaxValue
            }).ToList();

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            SetError($"Failed to create survey: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Validation

    private bool ValidateSurvey()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            SetError("Survey title is required");
            return false;
        }

        if (Questions.Count == 0)
        {
            SetError("At least one question is required");
            return false;
        }

        if (StartsAt.HasValue && EndsAt.HasValue && EndsAt.Value <= StartsAt.Value)
        {
            SetError("End date must be after start date");
            return false;
        }

        return true;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }

    #endregion

    #region Events

    public event EventHandler? CloseRequested;

    #endregion

    partial void OnNewQuestionTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsRatingQuestion));
        OnPropertyChanged(nameof(IsChoiceQuestion));
    }

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(StepTitle));
    }
}

/// <summary>
/// ViewModel for individual survey questions in the builder.
/// </summary>
public partial class SurveyQuestionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _questionText = string.Empty;

    [ObservableProperty]
    private string _questionType = "text";

    [ObservableProperty]
    private string? _options;

    [ObservableProperty]
    private bool _isRequired = true;

    [ObservableProperty]
    private int _sortOrder;

    [ObservableProperty]
    private int? _minValue;

    [ObservableProperty]
    private int? _maxValue;

    public string QuestionTypeDisplay => QuestionType switch
    {
        "text" => "Text Response",
        "rating" => $"Rating ({MinValue}-{MaxValue})",
        "choice" => "Single Choice",
        "multi_choice" => "Multiple Choice",
        _ => QuestionType
    };

    public string RequiredDisplay => IsRequired ? "Required" : "Optional";
}
