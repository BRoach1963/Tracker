namespace Tracker.Common.Enums
{
    /// <summary>
    /// Status of a pulse survey.
    /// </summary>
    public enum SurveyStatus
    {
        /// <summary>Survey is being drafted, not yet sent.</summary>
        Draft,
        
        /// <summary>Survey is active and accepting responses.</summary>
        Active,
        
        /// <summary>Survey is closed, no more responses accepted.</summary>
        Closed,
        
        /// <summary>Survey is archived/historical.</summary>
        Archived
    }

    /// <summary>
    /// Types of questions in a pulse survey.
    /// </summary>
    public enum SurveyQuestionType
    {
        /// <summary>Numeric rating scale (e.g., 1-5, 1-10).</summary>
        Rating,
        
        /// <summary>Free-form text response.</summary>
        OpenEnded,
        
        /// <summary>Yes/No question.</summary>
        YesNo,
        
        /// <summary>Net Promoter Score (0-10 with categories).</summary>
        NPS
    }
}
