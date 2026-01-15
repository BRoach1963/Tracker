using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Services.Backend.Models
{
    /// <summary>
    /// Represents a survey stored in Supabase.
    /// Maps to the 'surveys' table.
    /// </summary>
    [Table("surveys")]
    public class SupabaseSurvey : BaseModel
    {
        [PrimaryKey("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Column("tracker_id")]
        [JsonPropertyName("tracker_id")]
        public string? TrackerId { get; set; }

        [Column("owner_id")]
        [JsonPropertyName("owner_id")]
        public string OwnerId { get; set; } = string.Empty;

        [Column("title")]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [Column("is_anonymous")]
        [JsonPropertyName("is_anonymous")]
        public bool IsAnonymous { get; set; }

        [Column("status")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = "draft";

        [Column("due_date")]
        [JsonPropertyName("due_date")]
        public DateTime? DueDate { get; set; }

        [Column("created_at")]
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents a survey question stored in Supabase.
    /// Maps to the 'survey_questions' table.
    /// </summary>
    [Table("survey_questions")]
    public class SupabaseSurveyQuestion : BaseModel
    {
        [PrimaryKey("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Column("survey_id")]
        [JsonPropertyName("survey_id")]
        public string SurveyId { get; set; } = string.Empty;

        [Column("tracker_id")]
        [JsonPropertyName("tracker_id")]
        public string? TrackerId { get; set; }

        [Column("question_text")]
        [JsonPropertyName("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [Column("question_type")]
        [JsonPropertyName("question_type")]
        public string QuestionType { get; set; } = "rating";

        [Column("options")]
        [JsonPropertyName("options")]
        public string? Options { get; set; }

        [Column("is_required")]
        [JsonPropertyName("is_required")]
        public bool IsRequired { get; set; } = true;

        [Column("sort_order")]
        [JsonPropertyName("sort_order")]
        public int SortOrder { get; set; }

        [Column("created_at")]
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents a survey token for external survey links.
    /// Maps to the 'survey_tokens' table.
    /// </summary>
    [Table("survey_tokens")]
    public class SupabaseSurveyToken : BaseModel
    {
        [PrimaryKey("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Column("survey_id")]
        [JsonPropertyName("survey_id")]
        public string SurveyId { get; set; } = string.Empty;

        [Column("token")]
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [Column("team_member_name")]
        [JsonPropertyName("team_member_name")]
        public string? TeamMemberName { get; set; }

        [Column("team_member_id")]
        [JsonPropertyName("team_member_id")]
        public Guid? TeamMemberId { get; set; }

        [Column("expires_at")]
        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("used_at")]
        [JsonPropertyName("used_at")]
        public DateTime? UsedAt { get; set; }

        [Column("created_at")]
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents a survey response stored in Supabase.
    /// Maps to the 'survey_responses' table.
    /// </summary>
    [Table("survey_responses")]
    public class SupabaseSurveyResponse : BaseModel
    {
        [PrimaryKey("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Column("survey_id")]
        [JsonPropertyName("survey_id")]
        public string SurveyId { get; set; } = string.Empty;

        [Column("token_id")]
        [JsonPropertyName("token_id")]
        public string? TokenId { get; set; }

        [Column("submitted_at")]
        [JsonPropertyName("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        [Column("respondent_name")]
        [JsonPropertyName("respondent_name")]
        public string? RespondentName { get; set; }

        [Column("synced_to_tracker")]
        [JsonPropertyName("synced_to_tracker")]
        public bool SyncedToTracker { get; set; }

        [Column("synced_at")]
        [JsonPropertyName("synced_at")]
        public DateTime? SyncedAt { get; set; }
    }

    /// <summary>
    /// Represents a survey answer stored in Supabase.
    /// Maps to the 'survey_answers' table.
    /// </summary>
    [Table("survey_answers")]
    public class SupabaseSurveyAnswer : BaseModel
    {
        [PrimaryKey("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Column("response_id")]
        [JsonPropertyName("response_id")]
        public string ResponseId { get; set; } = string.Empty;

        [Column("question_id")]
        [JsonPropertyName("question_id")]
        public string QuestionId { get; set; } = string.Empty;

        [Column("answer_text")]
        [JsonPropertyName("answer_text")]
        public string? AnswerText { get; set; }

        [Column("answer_rating")]
        [JsonPropertyName("answer_rating")]
        public int? AnswerRating { get; set; }

        [Column("answer_boolean")]
        [JsonPropertyName("answer_boolean")]
        public bool? AnswerBoolean { get; set; }
    }
}
