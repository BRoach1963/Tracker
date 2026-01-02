namespace Tracker.Common.Enums
{
    /// <summary>
    /// Types of performance reviews.
    /// </summary>
    public enum ReviewType
    {
        /// <summary>Annual performance review.</summary>
        Annual,
        
        /// <summary>Semi-annual (twice per year) review.</summary>
        SemiAnnual,
        
        /// <summary>Quarterly check-in.</summary>
        Quarterly,
        
        /// <summary>Probation period review.</summary>
        Probation,
        
        /// <summary>Project-end review.</summary>
        Project,
        
        /// <summary>360-degree feedback review.</summary>
        ThreeSixty,
        
        /// <summary>Custom/ad-hoc review.</summary>
        Custom
    }

    /// <summary>
    /// Types of questions in a performance review.
    /// </summary>
    public enum ReviewQuestionType
    {
        /// <summary>Long-form text response.</summary>
        LongText,
        
        /// <summary>Short text response.</summary>
        ShortText,
        
        /// <summary>Numeric rating scale.</summary>
        Rating,
        
        /// <summary>Yes/No question.</summary>
        YesNo,
        
        /// <summary>Competency assessment with specific scale.</summary>
        Competency
    }

    /// <summary>
    /// Status of a review cycle.
    /// </summary>
    public enum ReviewCycleStatus
    {
        /// <summary>Cycle is being set up.</summary>
        Draft,
        
        /// <summary>Self-assessments are in progress.</summary>
        SelfReviewInProgress,
        
        /// <summary>Manager reviews are in progress.</summary>
        ManagerReviewInProgress,
        
        /// <summary>Calibration discussions happening.</summary>
        Calibration,
        
        /// <summary>All reviews completed.</summary>
        Completed,
        
        /// <summary>Cycle is archived.</summary>
        Archived
    }

    /// <summary>
    /// Status of an individual performance review.
    /// </summary>
    public enum ReviewStatus
    {
        /// <summary>Review not started.</summary>
        NotStarted,
        
        /// <summary>Employee is working on self-assessment.</summary>
        SelfReviewInProgress,
        
        /// <summary>Self-assessment submitted.</summary>
        SelfReviewComplete,
        
        /// <summary>Manager is working on their review.</summary>
        ManagerReviewInProgress,
        
        /// <summary>Manager review completed, pending share.</summary>
        ManagerReviewComplete,
        
        /// <summary>Review shared with employee.</summary>
        Shared,
        
        /// <summary>Review discussed in 1:1.</summary>
        Discussed
    }
}
