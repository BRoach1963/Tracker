namespace Tracker.Models;

/// <summary>
/// Type of feedback.
/// Maps to Supabase feedback_type enum.
/// </summary>
public enum FeedbackType
{
    Praise,
    Coaching,
    Collaboration,
    General
}

/// <summary>
/// Sentiment/tone of feedback.
/// Maps to Supabase feedback_sentiment enum.
/// </summary>
public enum FeedbackSentiment
{
    Positive,
    Neutral,
    Constructive
}

/// <summary>
/// Status of a feedback request.
/// </summary>
public enum FeedbackRequestStatus
{
    Pending,
    Completed,
    Declined,
    Expired
}

/// <summary>
/// Type of performance review.
/// </summary>
public enum PerformanceReviewType
{
    Annual,
    MidYear,
    Quarterly,
    Probation
}

/// <summary>
/// Status of a performance review.
/// </summary>
public enum PerformanceReviewStatus
{
    Draft,
    SelfReview,
    ManagerReview,
    Calibration,
    Complete
}
