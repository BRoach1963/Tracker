using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a survey definition.
/// Maps to Supabase surveys table.
/// </summary>
public class Survey
{
    /// <summary>
    /// Unique identifier for this survey.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this survey belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Survey title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Survey description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of survey (pulse, engagement, onboarding, exit, custom).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SurveyType { get; set; } = "pulse";

    /// <summary>
    /// Survey status.
    /// </summary>
    [Required]
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    /// <summary>
    /// Frequency for recurring surveys.
    /// </summary>
    [Required]
    public SurveyFrequency Frequency { get; set; } = SurveyFrequency.Once;

    /// <summary>
    /// Start date.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// End date.
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Next scheduled send date.
    /// </summary>
    public DateOnly? NextSendDate { get; set; }

    /// <summary>
    /// Whether to target all employees.
    /// </summary>
    public bool TargetAllEmployees { get; set; } = true;

    /// <summary>
    /// Target team IDs (JSON array).
    /// </summary>
    public string? TargetTeamIds { get; set; }

    /// <summary>
    /// Target team member IDs (JSON array).
    /// </summary>
    public string? TargetTeamMemberIds { get; set; }

    /// <summary>
    /// Whether responses are anonymous.
    /// </summary>
    public bool IsAnonymous { get; set; } = true;

    /// <summary>
    /// Whether to allow comments.
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Whether reminders are enabled.
    /// </summary>
    public bool ReminderEnabled { get; set; } = true;

    /// <summary>
    /// Days before close to send reminder.
    /// </summary>
    public int ReminderDaysBeforeClose { get; set; } = 2;

    /// <summary>
    /// Welcome message shown at start.
    /// </summary>
    public string? WelcomeMessage { get; set; }

    /// <summary>
    /// Thank you message shown at end.
    /// </summary>
    public string? ThankYouMessage { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this survey.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Whether this survey is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this survey was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    public virtual ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    public virtual ICollection<SurveyInstance> Instances { get; set; } = new List<SurveyInstance>();
    public virtual ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();

    // Computed properties

    /// <summary>
    /// Whether the survey is currently active.
    /// </summary>
    [NotMapped]
    public bool IsActive => Status == SurveyStatus.Active;

    /// <summary>
    /// Whether the survey is recurring.
    /// </summary>
    [NotMapped]
    public bool IsRecurring => Frequency != SurveyFrequency.Once;
}

/// <summary>
/// Represents a question within a survey.
/// Maps to Supabase survey_questions table.
/// </summary>
public class SurveyQuestion
{
    /// <summary>
    /// Unique identifier for this question.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Survey this question belongs to.
    /// </summary>
    [Required]
    public Guid SurveyId { get; set; }

    /// <summary>
    /// Question text.
    /// </summary>
    [Required]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Help text for the question.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Type of question.
    /// </summary>
    [Required]
    public SurveyQuestionType QuestionType { get; set; } = SurveyQuestionType.Rating;

    /// <summary>
    /// Minimum value for rating questions.
    /// </summary>
    public int MinValue { get; set; } = 1;

    /// <summary>
    /// Maximum value for rating questions.
    /// </summary>
    public int MaxValue { get; set; } = 5;

    /// <summary>
    /// Label for minimum value.
    /// </summary>
    [MaxLength(100)]
    public string? MinLabel { get; set; }

    /// <summary>
    /// Label for maximum value.
    /// </summary>
    [MaxLength(100)]
    public string? MaxLabel { get; set; }

    /// <summary>
    /// Options for choice questions (JSON array).
    /// </summary>
    public string? Options { get; set; }

    /// <summary>
    /// Whether this question is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Order of this question in the survey.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Category for grouping/reporting.
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }

    public virtual ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}

/// <summary>
/// Represents a specific send of a survey (for recurring surveys).
/// Maps to Supabase survey_instances table.
/// </summary>
public class SurveyInstance
{
    /// <summary>
    /// Unique identifier for this instance.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Survey this instance belongs to.
    /// </summary>
    [Required]
    public Guid SurveyId { get; set; }

    /// <summary>
    /// Start of the period.
    /// </summary>
    [Required]
    public DateOnly PeriodStart { get; set; }

    /// <summary>
    /// End of the period.
    /// </summary>
    [Required]
    public DateOnly PeriodEnd { get; set; }

    /// <summary>
    /// Status of this instance.
    /// </summary>
    [Required]
    public SurveyStatus Status { get; set; } = SurveyStatus.Active;

    /// <summary>
    /// When the survey was sent.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the survey was closed.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Total number of recipients.
    /// </summary>
    public int TotalRecipients { get; set; }

    /// <summary>
    /// Total number of responses.
    /// </summary>
    public int TotalResponses { get; set; }

    /// <summary>
    /// Response rate percentage.
    /// </summary>
    public decimal? ResponseRate { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }

    public virtual ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();

    // Computed properties

    /// <summary>
    /// Whether the instance is currently active.
    /// </summary>
    [NotMapped]
    public bool IsActive => Status == SurveyStatus.Active;

    /// <summary>
    /// Duration of the instance period in days.
    /// </summary>
    [NotMapped]
    public int PeriodDays => PeriodEnd.DayNumber - PeriodStart.DayNumber;
}

/// <summary>
/// Represents an individual response to a survey.
/// Maps to Supabase survey_responses table.
/// </summary>
public class SurveyResponse
{
    /// <summary>
    /// Unique identifier for this response.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Survey this response is for.
    /// </summary>
    [Required]
    public Guid SurveyId { get; set; }

    /// <summary>
    /// Instance this response is for (if applicable).
    /// </summary>
    public Guid? InstanceId { get; set; }

    /// <summary>
    /// Team member who responded (nullable if anonymous).
    /// </summary>
    public Guid? TeamMemberId { get; set; }

    /// <summary>
    /// Anonymous token for tracking completion.
    /// </summary>
    public Guid AnonymousToken { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When the response was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the response was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether the response is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }

    [ForeignKey(nameof(InstanceId))]
    public virtual SurveyInstance? Instance { get; set; }

    [ForeignKey(nameof(TeamMemberId))]
    public virtual TeamMember? TeamMember { get; set; }

    public virtual ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();

    // Computed properties

    /// <summary>
    /// Time taken to complete (if complete).
    /// </summary>
    [NotMapped]
    public TimeSpan? TimeTaken => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
}

/// <summary>
/// Represents an individual question answer.
/// Maps to Supabase survey_answers table.
/// </summary>
public class SurveyAnswer
{
    /// <summary>
    /// Unique identifier for this answer.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Response this answer belongs to.
    /// </summary>
    [Required]
    public Guid ResponseId { get; set; }

    /// <summary>
    /// Question being answered.
    /// </summary>
    [Required]
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Rating value (for rating questions).
    /// </summary>
    public int? RatingValue { get; set; }

    /// <summary>
    /// Text value (for text questions).
    /// </summary>
    public string? TextValue { get; set; }

    /// <summary>
    /// Selected options (JSON for multi-select).
    /// </summary>
    public string? SelectedOptions { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ResponseId))]
    public virtual SurveyResponse? Response { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public virtual SurveyQuestion? Question { get; set; }
}
