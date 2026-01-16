using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Core.Common.Enums;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// A survey sent to team members (pulse surveys, engagement surveys, etc.).
    /// Maps to Supabase 'surveys' table.
    /// </summary>
    [Table("surveys")]
    public class Survey
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this survey belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Survey title.
        /// Maps to: title VARCHAR(200) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional description or instructions.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Type of survey (pulse, engagement, custom, etc.).
        /// Maps to: survey_type VARCHAR(50) NOT NULL DEFAULT 'pulse'
        /// </summary>
        [Column("survey_type")]
        [MaxLength(50)]
        public string SurveyType { get; set; } = "pulse";

        /// <summary>
        /// Status (stored as string).
        /// Maps to: status survey_status (enum) NOT NULL DEFAULT 'draft'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string StatusString { get; set; } = "draft";

        /// <summary>
        /// Status as enum.
        /// </summary>
        [NotMapped]
        public SurveyStatus Status
        {
            get => Enum.TryParse<SurveyStatus>(StatusString, true, out var result) ? result : SurveyStatus.Draft;
            set => StatusString = value.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// How often the survey repeats.
        /// Maps to: frequency survey_frequency (enum) NOT NULL DEFAULT 'once'
        /// Values: once, weekly, biweekly, monthly, quarterly
        /// </summary>
        [Column("frequency")]
        [MaxLength(50)]
        public string Frequency { get; set; } = "once";

        /// <summary>
        /// Start date for the survey.
        /// Maps to: start_date DATE NULL
        /// </summary>
        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for the survey.
        /// Maps to: end_date DATE NULL
        /// </summary>
        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Next scheduled send date (for recurring surveys).
        /// Maps to: next_send_date DATE NULL
        /// </summary>
        [Column("next_send_date")]
        public DateTime? NextSendDate { get; set; }

        /// <summary>
        /// Target all employees in the organization.
        /// Maps to: target_all_employees BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("target_all_employees")]
        public bool TargetAllEmployees { get; set; } = true;

        /// <summary>
        /// Specific team IDs to target (if not all employees).
        /// Maps to: target_team_ids UUID[] NULL
        /// Stored as JSON array string for Dapper compatibility.
        /// </summary>
        [Column("target_team_ids")]
        public string? TargetTeamIds { get; set; }

        /// <summary>
        /// Specific team member IDs to target.
        /// Maps to: target_team_member_ids UUID[] NULL
        /// Stored as JSON array string for Dapper compatibility.
        /// </summary>
        [Column("target_team_member_ids")]
        public string? TargetTeamMemberIds { get; set; }

        /// <summary>
        /// Whether responses are anonymous.
        /// Maps to: is_anonymous BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_anonymous")]
        public bool IsAnonymous { get; set; } = true;

        /// <summary>
        /// Allow free-text comments on questions.
        /// Maps to: allow_comments BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("allow_comments")]
        public bool AllowComments { get; set; } = true;

        /// <summary>
        /// Send reminder before survey closes.
        /// Maps to: reminder_enabled BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("reminder_enabled")]
        public bool ReminderEnabled { get; set; } = true;

        /// <summary>
        /// Days before close to send reminder.
        /// Maps to: reminder_days_before_close INT4 NULL DEFAULT 2
        /// </summary>
        [Column("reminder_days_before_close")]
        public int? ReminderDaysBeforeClose { get; set; } = 2;

        /// <summary>
        /// Welcome message shown at start of survey.
        /// Maps to: welcome_message TEXT NULL
        /// </summary>
        [Column("welcome_message")]
        public string? WelcomeMessage { get; set; }

        /// <summary>
        /// Thank you message shown after completion.
        /// Maps to: thank_you_message TEXT NULL
        /// </summary>
        [Column("thank_you_message")]
        public string? ThankYouMessage { get; set; }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who created this survey.
        /// Maps to: created_by UUID NULL
        /// </summary>
        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// Maps to: is_deleted BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// When soft deleted.
        /// Maps to: deleted_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Questions in this survey.
        /// </summary>
        [NotMapped]
        public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();

        /// <summary>
        /// Responses to this survey.
        /// </summary>
        [NotMapped]
        public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();

        /// <summary>
        /// Instances of this survey (for recurring surveys).
        /// </summary>
        [NotMapped]
        public ICollection<SurveyInstance> Instances { get; set; } = new List<SurveyInstance>();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Number of questions in this survey.
        /// </summary>
        [NotMapped]
        public int QuestionCount => Questions?.Count ?? 0;

        /// <summary>
        /// Number of responses received.
        /// </summary>
        [NotMapped]
        public int ResponseCount => Responses?.Count ?? 0;

        /// <summary>
        /// Status display string.
        /// </summary>
        [NotMapped]
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
    /// A question within a survey.
    /// Maps to Supabase 'survey_questions' table.
    /// </summary>
    [Table("survey_questions")]
    public class SurveyQuestion
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The survey this question belongs to.
        /// Maps to: survey_id UUID NOT NULL
        /// </summary>
        [Column("survey_id")]
        public Guid SurveyId { get; set; }

        /// <summary>
        /// Question text.
        /// Maps to: question_text TEXT NOT NULL
        /// </summary>
        [Column("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        /// <summary>
        /// Help text or additional instructions.
        /// Maps to: help_text TEXT NULL
        /// </summary>
        [Column("help_text")]
        public string? HelpText { get; set; }

        /// <summary>
        /// Type of response expected (stored as string).
        /// Maps to: question_type survey_question_type (enum) NOT NULL DEFAULT 'rating'
        /// </summary>
        [Column("question_type")]
        [MaxLength(50)]
        public string QuestionTypeString { get; set; } = "rating";

        /// <summary>
        /// Question type as enum.
        /// </summary>
        [NotMapped]
        public SurveyQuestionType QuestionType
        {
            get => Enum.TryParse<SurveyQuestionType>(QuestionTypeString, true, out var result) ? result : SurveyQuestionType.Rating;
            set => QuestionTypeString = value.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// For Rating type: minimum value.
        /// Maps to: min_value INT4 NULL DEFAULT 1
        /// </summary>
        [Column("min_value")]
        public int? MinValue { get; set; } = 1;

        /// <summary>
        /// For Rating type: maximum value.
        /// Maps to: max_value INT4 NULL DEFAULT 5
        /// </summary>
        [Column("max_value")]
        public int? MaxValue { get; set; } = 5;

        /// <summary>
        /// RatingMax - alias for MaxValue for backward compatibility.
        /// </summary>
        [NotMapped]
        public int RatingMax => MaxValue ?? 5;

        /// <summary>
        /// Label for minimum rating.
        /// Maps to: min_label VARCHAR(100) NULL
        /// </summary>
        [Column("min_label")]
        [MaxLength(100)]
        public string? MinLabel { get; set; }

        /// <summary>
        /// Label for maximum rating.
        /// Maps to: max_label VARCHAR(100) NULL
        /// </summary>
        [Column("max_label")]
        [MaxLength(100)]
        public string? MaxLabel { get; set; }

        /// <summary>
        /// Options for multiple choice questions (JSON).
        /// Maps to: options JSONB NULL
        /// </summary>
        [Column("options")]
        public string? Options { get; set; }

        /// <summary>
        /// Whether this question is required.
        /// Maps to: is_required BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_required")]
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Display order within the survey.
        /// Maps to: sort_order INT4 NOT NULL DEFAULT 0
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; }

        /// <summary>
        /// Category for grouping questions.
        /// Maps to: category VARCHAR(100) NULL
        /// </summary>
        [Column("category")]
        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Parent survey.
        /// </summary>
        [NotMapped]
        public Survey? Survey { get; set; }

        #endregion
    }

    /// <summary>
    /// An instance of a survey (for recurring surveys or tracking send batches).
    /// Maps to Supabase 'survey_instances' table.
    /// </summary>
    [Table("survey_instances")]
    public class SurveyInstance
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent survey.
        /// Maps to: survey_id UUID NOT NULL
        /// </summary>
        [Column("survey_id")]
        public Guid SurveyId { get; set; }

        /// <summary>
        /// Period start date.
        /// Maps to: period_start DATE NOT NULL
        /// </summary>
        [Column("period_start")]
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Period end date.
        /// Maps to: period_end DATE NOT NULL
        /// </summary>
        [Column("period_end")]
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Status (stored as string).
        /// Maps to: status survey_status (enum) NOT NULL DEFAULT 'active'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "active";

        /// <summary>
        /// When the survey was sent.
        /// Maps to: sent_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// When the survey was closed.
        /// Maps to: closed_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// Total recipients for this instance.
        /// Maps to: total_recipients INT4 NULL DEFAULT 0
        /// </summary>
        [Column("total_recipients")]
        public int TotalRecipients { get; set; }

        /// <summary>
        /// Total responses received.
        /// Maps to: total_responses INT4 NULL DEFAULT 0
        /// </summary>
        [Column("total_responses")]
        public int TotalResponses { get; set; }

        /// <summary>
        /// Response rate percentage.
        /// Maps to: response_rate NUMERIC NULL
        /// </summary>
        [Column("response_rate")]
        public decimal? ResponseRate { get; set; }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Parent survey.
        /// </summary>
        [NotMapped]
        public Survey? Survey { get; set; }

        #endregion
    }

    /// <summary>
    /// A response to a survey from a team member.
    /// Maps to Supabase 'survey_responses' table.
    /// </summary>
    [Table("survey_responses")]
    public class SurveyResponse
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent survey.
        /// Maps to: survey_id UUID NOT NULL
        /// </summary>
        [Column("survey_id")]
        public Guid SurveyId { get; set; }

        /// <summary>
        /// Survey instance (if applicable).
        /// Maps to: instance_id UUID NULL
        /// </summary>
        [Column("instance_id")]
        public Guid? InstanceId { get; set; }

        /// <summary>
        /// Team member who responded (null if anonymous).
        /// Maps to: team_member_id UUID NULL
        /// </summary>
        [Column("team_member_id")]
        public Guid? TeamMemberId { get; set; }

        /// <summary>
        /// Anonymous token for tracking without identity.
        /// Maps to: anonymous_token UUID NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("anonymous_token")]
        public Guid? AnonymousToken { get; set; }

        /// <summary>
        /// When the response was started.
        /// Maps to: started_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("started_at")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the response was completed.
        /// Maps to: completed_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// SubmittedAt - alias for CompletedAt for backward compatibility.
        /// </summary>
        [NotMapped]
        public DateTime? SubmittedAt => CompletedAt;

        /// <summary>
        /// Whether the response is complete.
        /// Maps to: is_complete BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_complete")]
        public bool IsComplete { get; set; }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Parent survey.
        /// </summary>
        [NotMapped]
        public Survey? Survey { get; set; }

        /// <summary>
        /// Survey instance.
        /// </summary>
        [NotMapped]
        public SurveyInstance? Instance { get; set; }

        /// <summary>
        /// Team member (if not anonymous).
        /// </summary>
        [NotMapped]
        public TeamMember? TeamMember { get; set; }

        /// <summary>
        /// Individual answers.
        /// </summary>
        [NotMapped]
        public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();

        #endregion
    }

    /// <summary>
    /// An individual answer to a survey question.
    /// Maps to Supabase 'survey_answers' table.
    /// </summary>
    [Table("survey_answers")]
    public class SurveyAnswer
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent response.
        /// Maps to: response_id UUID NOT NULL
        /// </summary>
        [Column("response_id")]
        public Guid ResponseId { get; set; }

        /// <summary>
        /// Question being answered.
        /// Maps to: question_id UUID NOT NULL
        /// </summary>
        [Column("question_id")]
        public Guid QuestionId { get; set; }

        /// <summary>
        /// For Rating questions: the numeric rating.
        /// Maps to: rating_value INT4 NULL
        /// </summary>
        [Column("rating_value")]
        public int? RatingValue { get; set; }

        /// <summary>
        /// For Text questions: the text response.
        /// Maps to: text_value TEXT NULL
        /// </summary>
        [Column("text_value")]
        public string? TextValue { get; set; }

        /// <summary>
        /// For MultiSelect questions: selected options (JSON).
        /// Maps to: selected_options JSONB NULL
        /// </summary>
        [Column("selected_options")]
        public string? SelectedOptions { get; set; }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Parent response.
        /// </summary>
        [NotMapped]
        public SurveyResponse? Response { get; set; }

        /// <summary>
        /// Question being answered.
        /// </summary>
        [NotMapped]
        public SurveyQuestion? Question { get; set; }

        #endregion
    }

    #region Legacy Aliases (for backward compatibility)

    /// <summary>
    /// Alias for Survey (backward compatibility).
    /// </summary>
    [Obsolete("Use Survey instead")]
    public class PulseSurvey : Survey { }

    /// <summary>
    /// Alias for SurveyQuestion (backward compatibility).
    /// </summary>
    [Obsolete("Use SurveyQuestion instead")]
    public class PulseSurveyQuestion : SurveyQuestion { }

    /// <summary>
    /// Alias for SurveyResponse (backward compatibility).
    /// </summary>
    [Obsolete("Use SurveyResponse instead")]
    public class PulseSurveyResponse : SurveyResponse { }

    /// <summary>
    /// Alias for SurveyAnswer (backward compatibility).
    /// </summary>
    [Obsolete("Use SurveyAnswer instead")]
    public class PulseSurveyAnswer : SurveyAnswer { }

    #endregion
}
