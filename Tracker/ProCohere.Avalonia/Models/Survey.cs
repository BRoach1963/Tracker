using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Survey model - maps to the surveys table in Supabase procohere schema.
/// Represents a survey that can be sent to team members.
/// </summary>
[Table("surveys")]
public class Survey : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Team member who created the survey.
    /// </summary>
    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    #endregion

    #region Content

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    #endregion

    #region Status & Settings

    /// <summary>
    /// Survey status: 'draft', 'active', 'closed', 'archived'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "draft";

    /// <summary>
    /// Whether responses are anonymous.
    /// </summary>
    [Column("is_anonymous")]
    public bool IsAnonymous { get; set; }

    /// <summary>
    /// When the survey opens for responses.
    /// </summary>
    [Column("starts_at")]
    public DateTime? StartsAt { get; set; }

    /// <summary>
    /// When the survey closes.
    /// </summary>
    [Column("ends_at")]
    public DateTime? EndsAt { get; set; }

    #endregion

    #region Survey Type & Scheduling

    /// <summary>
    /// Survey type: 'pulse', 'engagement', 'custom', etc.
    /// </summary>
    [Column("survey_type")]
    public string SurveyType { get; set; } = "custom";

    /// <summary>
    /// Frequency: 'one_time', 'weekly', 'biweekly', 'monthly', 'quarterly'.
    /// </summary>
    [Column("frequency")]
    public string Frequency { get; set; } = "one_time";

    /// <summary>
    /// When to send the next instance (for recurring surveys).
    /// </summary>
    [Column("next_send_date")]
    public DateTime? NextSendDate { get; set; }

    #endregion

    #region Targeting

    /// <summary>
    /// Whether to send to all employees in the organization.
    /// </summary>
    [Column("target_all_employees")]
    public bool TargetAllEmployees { get; set; } = true;

    /// <summary>
    /// Specific team IDs to target (if not targeting all).
    /// </summary>
    [Column("target_team_ids")]
    public Guid[]? TargetTeamIds { get; set; }

    /// <summary>
    /// Specific team member IDs to target (if not targeting all).
    /// </summary>
    [Column("target_team_member_ids")]
    public Guid[]? TargetTeamMemberIds { get; set; }

    #endregion

    #region UX Settings

    /// <summary>
    /// Whether respondents can add freeform comments.
    /// </summary>
    [Column("allow_comments")]
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Whether to send reminder notifications.
    /// </summary>
    [Column("reminder_enabled")]
    public bool ReminderEnabled { get; set; }

    /// <summary>
    /// Days before close to send reminder.
    /// </summary>
    [Column("reminder_days_before_close")]
    public int? ReminderDaysBeforeClose { get; set; }

    /// <summary>
    /// Message shown to respondents before starting the survey.
    /// </summary>
    [Column("welcome_message")]
    public string? WelcomeMessage { get; set; }

    /// <summary>
    /// Message shown after completing the survey.
    /// </summary>
    [Column("thank_you_message")]
    public string? ThankYouMessage { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Navigation (not mapped)

    /// <summary>
    /// Questions in this survey (populated by service).
    /// </summary>
    public List<SurveyQuestion> Questions { get; set; } = new();

    /// <summary>
    /// Responses to this survey (populated by service).
    /// </summary>
    public List<SurveyResponse> Responses { get; set; } = new();

    #endregion

    #region Computed Properties

    public bool IsDraft => Status == "draft";
    public bool IsActive => Status == "active";
    public bool IsClosed => Status == "closed";

    public int QuestionCount => Questions.Count;
    public int ResponseCount => Responses.Count;

    public bool IsRecurring => Frequency != "one_time";
    public bool IsPulseSurvey => SurveyType == "pulse";

    /// <summary>
    /// Whether the survey is currently accepting responses.
    /// </summary>
    public bool IsOpen
    {
        get
        {
            if (Status != "active") return false;
            var now = DateTime.UtcNow;
            if (StartsAt.HasValue && now < StartsAt.Value) return false;
            if (EndsAt.HasValue && now > EndsAt.Value) return false;
            return true;
        }
    }

    public string StatusDisplay => Status switch
    {
        "draft" => "Draft",
        "active" => "Active",
        "closed" => "Closed",
        "archived" => "Archived",
        _ => Status
    };

    #endregion
}

/// <summary>
/// Survey question model - maps to survey_questions table.
/// </summary>
[Table("survey_questions")]
public class SurveyQuestion : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("survey_id")]
    public Guid SurveyId { get; set; }

    #endregion

    #region Question Content

    [Column("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Question type: 'text', 'rating', 'choice', 'multi_choice', etc.
    /// </summary>
    [Column("question_type")]
    public string QuestionType { get; set; } = "text";

    /// <summary>
    /// Options for choice questions (JSON array).
    /// </summary>
    [Column("options")]
    public string? Options { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    /// <summary>
    /// For rating questions: minimum value.
    /// </summary>
    [Column("min_value")]
    public int? MinValue { get; set; }

    /// <summary>
    /// For rating questions: maximum value.
    /// </summary>
    [Column("max_value")]
    public int? MaxValue { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    public bool IsTextQuestion => QuestionType == "text";
    public bool IsRatingQuestion => QuestionType == "rating";
    public bool IsChoiceQuestion => QuestionType == "choice" || QuestionType == "multi_choice";

    public string QuestionTypeDisplay => QuestionType switch
    {
        "text" => "Text",
        "rating" => "Rating",
        "choice" => "Single Choice",
        "multi_choice" => "Multiple Choice",
        _ => QuestionType
    };

    #endregion
}

/// <summary>
/// Survey response model - maps to survey_responses table.
/// Represents a respondent's submission (header record).
/// </summary>
[Table("survey_responses")]
public class SurveyResponse : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("survey_id")]
    public Guid SurveyId { get; set; }

    /// <summary>
    /// Team member who responded (null if anonymous).
    /// </summary>
    [Column("respondent_id")]
    public Guid? RespondentId { get; set; }

    #endregion

    #region Response Status

    /// <summary>
    /// When the response was submitted.
    /// </summary>
    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Whether all required questions were answered.
    /// </summary>
    [Column("is_complete")]
    public bool IsComplete { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Navigation (not mapped)

    /// <summary>
    /// Individual answers in this response (populated by service).
    /// </summary>
    public List<SurveyAnswer> Answers { get; set; } = new();

    #endregion

    #region Computed Properties

    public bool IsSubmitted => SubmittedAt.HasValue;
    public bool IsAnonymous => !RespondentId.HasValue;

    #endregion
}

/// <summary>
/// Survey answer model - maps to survey_answers table.
/// Represents an answer to a specific question.
/// </summary>
[Table("survey_answers")]
public class SurveyAnswer : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("response_id")]
    public Guid ResponseId { get; set; }

    [Column("question_id")]
    public Guid QuestionId { get; set; }

    #endregion

    #region Answer Values

    /// <summary>
    /// Text answer (for text questions or comments).
    /// </summary>
    [Column("answer_text")]
    public string? AnswerText { get; set; }

    /// <summary>
    /// Numeric answer (for rating questions).
    /// </summary>
    [Column("answer_numeric")]
    public decimal? AnswerNumeric { get; set; }

    /// <summary>
    /// JSON answer (for multi-choice or complex answers).
    /// </summary>
    [Column("answer_json")]
    public string? AnswerJson { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    public bool HasTextAnswer => !string.IsNullOrEmpty(AnswerText);
    public bool HasNumericAnswer => AnswerNumeric.HasValue;
    public bool HasJsonAnswer => !string.IsNullOrEmpty(AnswerJson);

    /// <summary>
    /// Display-friendly answer value.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            if (HasNumericAnswer) return AnswerNumeric!.Value.ToString();
            if (HasTextAnswer) return AnswerText!;
            if (HasJsonAnswer) return "[Multiple values]";
            return string.Empty;
        }
    }

    #endregion
}

/// <summary>
/// Survey instance model - maps to survey_instances table.
/// Represents a single "send" of a recurring survey.
/// </summary>
[Table("survey_instances")]
public class SurveyInstance : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("survey_id")]
    public Guid SurveyId { get; set; }

    #endregion

    #region Instance Info

    /// <summary>
    /// Sequential instance number (1, 2, 3...).
    /// </summary>
    [Column("instance_number")]
    public int InstanceNumber { get; set; } = 1;

    /// <summary>
    /// Instance status: 'pending', 'sent', 'active', 'closed'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// When the instance was sent out.
    /// </summary>
    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When this instance closes for responses.
    /// </summary>
    [Column("closes_at")]
    public DateTime? ClosesAt { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    public bool IsPending => Status == "pending";
    public bool IsSent => Status == "sent";
    public bool IsActive => Status == "active";
    public bool IsClosed => Status == "closed";

    public bool IsOpen
    {
        get
        {
            if (Status != "active") return false;
            if (ClosesAt.HasValue && DateTime.UtcNow > ClosesAt.Value) return false;
            return true;
        }
    }

    #endregion
}
