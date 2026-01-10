using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a pulse survey sent to team members.
    /// Pulse surveys are quick, regular check-ins to measure team health and engagement.
    /// </summary>
    public class PulseSurvey : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The organization this survey belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Survey title (e.g., "Weekly Check-In", "Q4 Engagement Pulse").
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional description or instructions for respondents.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The status of this survey.
        /// </summary>
        public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

        /// <summary>
        /// When the survey was sent out.
        /// </summary>
        public DateTime? SentDate { get; set; }

        /// <summary>
        /// Deadline for responses.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// When the survey was closed.
        /// </summary>
        public DateTime? ClosedDate { get; set; }

        /// <summary>
        /// Whether responses are anonymous.
        /// </summary>
        public bool IsAnonymous { get; set; } = true;

        /// <summary>
        /// Questions in this survey.
        /// </summary>
        public ICollection<PulseSurveyQuestion> Questions { get; set; } = new List<PulseSurveyQuestion>();

        /// <summary>
        /// Responses to this survey.
        /// </summary>
        public ICollection<PulseSurveyResponse> Responses { get; set; } = new List<PulseSurveyResponse>();

        #region Computed Properties

        /// <summary>
        /// Number of questions in this survey.
        /// </summary>
        public int QuestionCount => Questions?.Count ?? 0;

        /// <summary>
        /// Number of responses received.
        /// </summary>
        public int ResponseCount => Responses?.Count ?? 0;

        /// <summary>
        /// Status display string.
        /// </summary>
        public string StatusDisplay => Status switch
        {
            SurveyStatus.Draft => "Draft",
            SurveyStatus.Active => "Active",
            SurveyStatus.Closed => "Closed",
            SurveyStatus.Archived => "Archived",
            _ => "Unknown"
        };

        #endregion
    }

    /// <summary>
    /// A question within a pulse survey.
    /// </summary>
    public class PulseSurveyQuestion : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The survey this question belongs to.
        /// </summary>
        public int PulseSurveyId { get; set; }
        public PulseSurvey PulseSurvey { get; set; } = null!;

        /// <summary>
        /// Question text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Type of response expected.
        /// </summary>
        public SurveyQuestionType QuestionType { get; set; } = SurveyQuestionType.Rating;

        /// <summary>
        /// Display order within the survey.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// For Rating type: minimum value (default 1).
        /// </summary>
        public int RatingMin { get; set; } = 1;

        /// <summary>
        /// For Rating type: maximum value (default 5).
        /// </summary>
        public int RatingMax { get; set; } = 5;

        /// <summary>
        /// Label for minimum rating (e.g., "Strongly Disagree").
        /// </summary>
        public string RatingMinLabel { get; set; } = "Strongly Disagree";

        /// <summary>
        /// Label for maximum rating (e.g., "Strongly Agree").
        /// </summary>
        public string RatingMaxLabel { get; set; } = "Strongly Agree";

        /// <summary>
        /// Category for grouping questions (e.g., "Engagement", "Management", "Culture").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Whether this question is required.
        /// </summary>
        public bool IsRequired { get; set; } = true;
    }

    /// <summary>
    /// A response to a pulse survey from a team member.
    /// </summary>
    public class PulseSurveyResponse : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The survey this response is for.
        /// </summary>
        public int PulseSurveyId { get; set; }
        public PulseSurvey PulseSurvey { get; set; } = null!;

        /// <summary>
        /// The team member who responded (null if anonymous).
        /// </summary>
        public Guid? TeamMemberId { get; set; }
        public TeamMember? TeamMember { get; set; }

        /// <summary>
        /// When the response was submitted.
        /// </summary>
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Individual answers within this response.
        /// </summary>
        public ICollection<PulseSurveyAnswer> Answers { get; set; } = new List<PulseSurveyAnswer>();
    }

    /// <summary>
    /// An individual answer to a survey question.
    /// </summary>
    public class PulseSurveyAnswer
    {
        public int Id { get; set; }

        /// <summary>
        /// The response this answer belongs to.
        /// </summary>
        public int PulseSurveyResponseId { get; set; }
        public PulseSurveyResponse PulseSurveyResponse { get; set; } = null!;

        /// <summary>
        /// The question being answered.
        /// </summary>
        public int PulseSurveyQuestionId { get; set; }
        public PulseSurveyQuestion PulseSurveyQuestion { get; set; } = null!;

        /// <summary>
        /// For Rating questions: the numeric rating given.
        /// </summary>
        public int? RatingValue { get; set; }

        /// <summary>
        /// For Text/OpenEnded questions: the text response.
        /// </summary>
        public string? TextValue { get; set; }

        /// <summary>
        /// For YesNo questions: the boolean response.
        /// </summary>
        public bool? BoolValue { get; set; }
    }
}
