using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A template for performance reviews.
    /// Templates define the structure and questions used in review cycles.
    /// </summary>
    public class ReviewTemplate : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The organization this template belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Template name (e.g., "Annual Performance Review", "Quarterly Check-In").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of when/how to use this template.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Type of review this template is for.
        /// </summary>
        public ReviewType ReviewType { get; set; } = ReviewType.Annual;

        /// <summary>
        /// Whether this is the default template for its type.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Whether this template is active and can be used.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Sections within this template.
        /// </summary>
        public ICollection<ReviewTemplateSection> Sections { get; set; } = new List<ReviewTemplateSection>();
    }

    /// <summary>
    /// A section within a review template (e.g., "Goals & Achievements", "Areas for Growth").
    /// </summary>
    public class ReviewTemplateSection : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The template this section belongs to.
        /// </summary>
        public int ReviewTemplateId { get; set; }
        public ReviewTemplate ReviewTemplate { get; set; } = null!;

        /// <summary>
        /// Section title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Instructions or description for this section.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Display order within the template.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Questions/prompts within this section.
        /// </summary>
        public ICollection<ReviewTemplateQuestion> Questions { get; set; } = new List<ReviewTemplateQuestion>();
    }

    /// <summary>
    /// A question or prompt within a review template section.
    /// </summary>
    public class ReviewTemplateQuestion : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The section this question belongs to.
        /// </summary>
        public int ReviewTemplateSectionId { get; set; }
        public ReviewTemplateSection ReviewTemplateSection { get; set; } = null!;

        /// <summary>
        /// Question text or prompt.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Type of response expected.
        /// </summary>
        public ReviewQuestionType QuestionType { get; set; } = ReviewQuestionType.LongText;

        /// <summary>
        /// Display order within the section.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Whether this question is required.
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// For Rating type: minimum value (default 1).
        /// </summary>
        public int RatingMin { get; set; } = 1;

        /// <summary>
        /// For Rating type: maximum value (default 5).
        /// </summary>
        public int RatingMax { get; set; } = 5;

        /// <summary>
        /// Labels for rating scale (JSON array, e.g., ["Poor", "Needs Improvement", "Meets Expectations", "Exceeds", "Outstanding"]).
        /// </summary>
        public string RatingLabels { get; set; } = string.Empty;
    }

    /// <summary>
    /// A performance review cycle (e.g., "2024 Annual Review").
    /// </summary>
    public class PerformanceReviewCycle : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The organization this review cycle belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Cycle name (e.g., "Q4 2024 Reviews", "2024 Annual Performance Review").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description or goals for this review cycle.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The template used for this cycle.
        /// </summary>
        public int ReviewTemplateId { get; set; }
        public ReviewTemplate ReviewTemplate { get; set; } = null!;

        /// <summary>
        /// Status of this review cycle.
        /// </summary>
        public ReviewCycleStatus Status { get; set; } = ReviewCycleStatus.Draft;

        /// <summary>
        /// Start date for self-assessments.
        /// </summary>
        public DateTime? SelfReviewStartDate { get; set; }

        /// <summary>
        /// Deadline for self-assessments.
        /// </summary>
        public DateTime? SelfReviewDueDate { get; set; }

        /// <summary>
        /// Start date for manager reviews.
        /// </summary>
        public DateTime? ManagerReviewStartDate { get; set; }

        /// <summary>
        /// Deadline for manager reviews.
        /// </summary>
        public DateTime? ManagerReviewDueDate { get; set; }

        /// <summary>
        /// When calibration discussions should occur.
        /// </summary>
        public DateTime? CalibrationDate { get; set; }

        /// <summary>
        /// When reviews should be shared with employees.
        /// </summary>
        public DateTime? ShareDate { get; set; }

        /// <summary>
        /// Individual reviews in this cycle.
        /// </summary>
        public ICollection<PerformanceReview> Reviews { get; set; } = new List<PerformanceReview>();

        #region Computed Properties

        public string StatusDisplay => Status switch
        {
            ReviewCycleStatus.Draft => "Draft",
            ReviewCycleStatus.SelfReviewInProgress => "Self-Review In Progress",
            ReviewCycleStatus.ManagerReviewInProgress => "Manager Review In Progress",
            ReviewCycleStatus.Calibration => "Calibration",
            ReviewCycleStatus.Completed => "Completed",
            ReviewCycleStatus.Archived => "Archived",
            _ => "Unknown"
        };

        #endregion
    }

    /// <summary>
    /// An individual performance review for a team member.
    /// </summary>
    public class PerformanceReview : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The organization this review belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// The review cycle this review belongs to.
        /// </summary>
        public int PerformanceReviewCycleId { get; set; }
        public PerformanceReviewCycle PerformanceReviewCycle { get; set; } = null!;

        /// <summary>
        /// The team member being reviewed.
        /// </summary>
        public int TeamMemberId { get; set; }
        public TeamMember TeamMember { get; set; } = null!;

        /// <summary>
        /// Status of this individual review.
        /// </summary>
        public ReviewStatus Status { get; set; } = ReviewStatus.NotStarted;

        /// <summary>
        /// Overall rating given (1-5 scale typically).
        /// </summary>
        public int? OverallRating { get; set; }

        /// <summary>
        /// Manager's overall summary/comments.
        /// </summary>
        public string ManagerSummary { get; set; } = string.Empty;

        /// <summary>
        /// Employee's self-assessment summary.
        /// </summary>
        public string SelfAssessmentSummary { get; set; } = string.Empty;

        /// <summary>
        /// When the self-assessment was submitted.
        /// </summary>
        public DateTime? SelfReviewSubmittedAt { get; set; }

        /// <summary>
        /// When the manager review was submitted.
        /// </summary>
        public DateTime? ManagerReviewSubmittedAt { get; set; }

        /// <summary>
        /// When the review was shared with the employee.
        /// </summary>
        public DateTime? SharedAt { get; set; }

        /// <summary>
        /// When the 1:1 discussion occurred (if any).
        /// </summary>
        public DateTime? DiscussionDate { get; set; }

        /// <summary>
        /// Link to the 1:1 where the review was discussed.
        /// </summary>
        public int? OneOnOneId { get; set; }
        public OneOnOne? OneOnOne { get; set; }

        /// <summary>
        /// Individual section responses.
        /// </summary>
        public ICollection<PerformanceReviewSection> Sections { get; set; } = new List<PerformanceReviewSection>();

        #region Computed Properties

        public string StatusDisplay => Status switch
        {
            ReviewStatus.NotStarted => "Not Started",
            ReviewStatus.SelfReviewInProgress => "Self-Review In Progress",
            ReviewStatus.SelfReviewComplete => "Self-Review Complete",
            ReviewStatus.ManagerReviewInProgress => "Manager Review In Progress",
            ReviewStatus.ManagerReviewComplete => "Pending Share",
            ReviewStatus.Shared => "Shared",
            ReviewStatus.Discussed => "Discussed",
            _ => "Unknown"
        };

        public string RatingDisplay => OverallRating switch
        {
            1 => "1 - Needs Improvement",
            2 => "2 - Developing",
            3 => "3 - Meets Expectations",
            4 => "4 - Exceeds Expectations",
            5 => "5 - Outstanding",
            _ => "—"
        };

        #endregion
    }

    /// <summary>
    /// A section within a performance review (corresponds to template section).
    /// </summary>
    public class PerformanceReviewSection
    {
        public int Id { get; set; }

        /// <summary>
        /// The review this section belongs to.
        /// </summary>
        public int PerformanceReviewId { get; set; }
        public PerformanceReview PerformanceReview { get; set; } = null!;

        /// <summary>
        /// The template section this corresponds to.
        /// </summary>
        public int ReviewTemplateSectionId { get; set; }
        public ReviewTemplateSection ReviewTemplateSection { get; set; } = null!;

        /// <summary>
        /// Individual question responses.
        /// </summary>
        public ICollection<PerformanceReviewAnswer> Answers { get; set; } = new List<PerformanceReviewAnswer>();
    }

    /// <summary>
    /// An answer to a review question.
    /// </summary>
    public class PerformanceReviewAnswer
    {
        public int Id { get; set; }

        /// <summary>
        /// The section this answer belongs to.
        /// </summary>
        public int PerformanceReviewSectionId { get; set; }
        public PerformanceReviewSection PerformanceReviewSection { get; set; } = null!;

        /// <summary>
        /// The template question being answered.
        /// </summary>
        public int ReviewTemplateQuestionId { get; set; }
        public ReviewTemplateQuestion ReviewTemplateQuestion { get; set; } = null!;

        /// <summary>
        /// Whether this is a self-assessment or manager response.
        /// </summary>
        public bool IsSelfAssessment { get; set; }

        /// <summary>
        /// For text questions: the response text.
        /// </summary>
        public string? TextValue { get; set; }

        /// <summary>
        /// For rating questions: the numeric rating.
        /// </summary>
        public int? RatingValue { get; set; }
    }
}
