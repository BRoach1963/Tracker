using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a reusable template for performance reviews.
/// Maps to Supabase review_templates table.
/// </summary>
public class ReviewTemplate
{
    /// <summary>
    /// Unique identifier for this template.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this template belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Template name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is the default template for the organization.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this template is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Type of review (annual, quarterly, probation, project).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ReviewType { get; set; } = "annual";

    /// <summary>
    /// Whether to include self-review.
    /// </summary>
    public bool IncludeSelfReview { get; set; } = true;

    /// <summary>
    /// Whether to include peer review.
    /// </summary>
    public bool IncludePeerReview { get; set; }

    /// <summary>
    /// Whether to include upward review.
    /// </summary>
    public bool IncludeUpwardReview { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this template.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    public virtual ICollection<ReviewTemplateSection> Sections { get; set; } = new List<ReviewTemplateSection>();
    public virtual ICollection<ReviewCycle> Cycles { get; set; } = new List<ReviewCycle>();
}

/// <summary>
/// Represents a section within a review template.
/// Maps to Supabase review_template_sections table.
/// </summary>
public class ReviewTemplateSection
{
    /// <summary>
    /// Unique identifier for this section.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Template this section belongs to.
    /// </summary>
    [Required]
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Section title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Section description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Order of this section in the template.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this section is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Weight for weighted scoring.
    /// </summary>
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(TemplateId))]
    public virtual ReviewTemplate? Template { get; set; }

    public virtual ICollection<ReviewTemplateQuestion> Questions { get; set; } = new List<ReviewTemplateQuestion>();
}

/// <summary>
/// Represents a question within a review template section.
/// Maps to Supabase review_template_questions table.
/// </summary>
public class ReviewTemplateQuestion
{
    /// <summary>
    /// Unique identifier for this question.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Section this question belongs to.
    /// </summary>
    [Required]
    public Guid SectionId { get; set; }

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
    public ReviewQuestionType QuestionType { get; set; } = ReviewQuestionType.Rating;

    /// <summary>
    /// Options for multiple choice or competency questions (JSON).
    /// </summary>
    public string? Options { get; set; }

    /// <summary>
    /// Whether this question is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Order of this question in the section.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Weight for weighted scoring.
    /// </summary>
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// Minimum rating value.
    /// </summary>
    public int MinRating { get; set; } = 1;

    /// <summary>
    /// Maximum rating value.
    /// </summary>
    public int MaxRating { get; set; } = 5;

    /// <summary>
    /// Rating labels (JSON: {"1": "Needs Improvement", "5": "Exceptional"}).
    /// </summary>
    public string? RatingLabels { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SectionId))]
    public virtual ReviewTemplateSection? Section { get; set; }

    public virtual ICollection<ReviewResponse> Responses { get; set; } = new List<ReviewResponse>();
}
