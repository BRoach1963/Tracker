namespace Tracker.Models;

/// <summary>
/// Status of a review cycle.
/// Maps to Supabase review_cycle_status enum.
/// </summary>
public enum ReviewCycleStatus
{
    Draft,
    Active,
    Completed,
    Cancelled
}

/// <summary>
/// Status of an individual review.
/// Maps to Supabase review_status enum.
/// </summary>
public enum ReviewStatus
{
    NotStarted,
    InProgress,
    Submitted,
    Acknowledged,
    Completed
}

/// <summary>
/// Type of review question.
/// Maps to Supabase review_question_type enum.
/// </summary>
public enum ReviewQuestionType
{
    Rating,
    Text,
    YesNo,
    MultipleChoice,
    Competency
}

/// <summary>
/// Status of a survey.
/// Maps to Supabase survey_status enum.
/// </summary>
public enum SurveyStatus
{
    Draft,
    Scheduled,
    Active,
    Closed,
    Cancelled
}

/// <summary>
/// Frequency of a recurring survey.
/// Maps to Supabase survey_frequency enum.
/// </summary>
public enum SurveyFrequency
{
    Once,
    Weekly,
    Biweekly,
    Monthly,
    Quarterly
}

/// <summary>
/// Type of survey question.
/// Maps to Supabase survey_question_type enum.
/// </summary>
public enum SurveyQuestionType
{
    Rating,
    Nps,
    Text,
    YesNo,
    MultipleChoice,
    MultiSelect,
    Emoji
}
