using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Core.Common.Enums;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// Personal development goal for a team member.
    /// Tracks career growth, skill development, certifications, etc.
    /// Maps to: development_goals (22 columns)
    /// </summary>
    [Table("development_goals")]
    public class DevelopmentGoal : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this goal belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member this goal belongs to.
        /// Maps to: team_member_id UUID NOT NULL
        /// </summary>
        [Column("team_member_id")]
        public Guid TeamMemberId { get; set; }

        /// <summary>
        /// Goal title.
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Category (stored as string for PostgreSQL enum).
        /// Maps to: category dev_goal_category (enum) NOT NULL DEFAULT 'skill_development'
        /// </summary>
        [Column("category")]
        [MaxLength(50)]
        public string CategoryString { get; set; } = "skill_development";

        /// <summary>
        /// Category as enum.
        /// </summary>
        [NotMapped]
        public DevelopmentGoalCategory Category
        {
            get => CategoryString switch
            {
                "skill_development" => DevelopmentGoalCategory.SkillDevelopment,
                "certification" => DevelopmentGoalCategory.Certification,
                "leadership" => DevelopmentGoalCategory.Leadership,
                "career_growth" => DevelopmentGoalCategory.CareerGrowth,
                "education" => DevelopmentGoalCategory.Education,
                "networking" => DevelopmentGoalCategory.Networking,
                "wellness" => DevelopmentGoalCategory.Wellness,
                "other" => DevelopmentGoalCategory.Other,
                _ => DevelopmentGoalCategory.SkillDevelopment
            };
            set => CategoryString = value switch
            {
                DevelopmentGoalCategory.SkillDevelopment => "skill_development",
                DevelopmentGoalCategory.Certification => "certification",
                DevelopmentGoalCategory.Leadership => "leadership",
                DevelopmentGoalCategory.CareerGrowth => "career_growth",
                DevelopmentGoalCategory.Education => "education",
                DevelopmentGoalCategory.Networking => "networking",
                DevelopmentGoalCategory.Wellness => "wellness",
                DevelopmentGoalCategory.Other => "other",
                _ => "skill_development"
            };
        }

        /// <summary>
        /// Target completion date.
        /// Maps to: target_date DATE NULL
        /// </summary>
        [Column("target_date")]
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// When work started on this goal.
        /// Maps to: started_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the goal was completed.
        /// Maps to: completed_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Status (stored as string for PostgreSQL enum).
        /// Maps to: status dev_goal_status (enum) NOT NULL DEFAULT 'draft'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string StatusString { get; set; } = "draft";

        /// <summary>
        /// Status as enum.
        /// </summary>
        [NotMapped]
        public DevelopmentGoalStatus Status
        {
            get => StatusString switch
            {
                "draft" => DevelopmentGoalStatus.Draft,
                "active" => DevelopmentGoalStatus.Active,
                "completed" => DevelopmentGoalStatus.Completed,
                "on_hold" => DevelopmentGoalStatus.OnHold,
                "cancelled" => DevelopmentGoalStatus.Cancelled,
                _ => DevelopmentGoalStatus.Draft
            };
            set => StatusString = value switch
            {
                DevelopmentGoalStatus.Draft => "draft",
                DevelopmentGoalStatus.Active => "active",
                DevelopmentGoalStatus.Completed => "completed",
                DevelopmentGoalStatus.OnHold => "on_hold",
                DevelopmentGoalStatus.Cancelled => "cancelled",
                _ => "draft"
            };
        }

        /// <summary>
        /// Progress percentage (0-100).
        /// Maps to: progress_percent INT4 NULL DEFAULT 0
        /// </summary>
        [Column("progress_percent")]
        public int? ProgressPercent { get; set; } = 0;

        /// <summary>
        /// Why this goal is important to the person.
        /// Maps to: why_important TEXT NULL
        /// </summary>
        [Column("why_important")]
        public string? WhyImportant { get; set; }

        /// <summary>
        /// How to know when the goal is achieved.
        /// Maps to: success_criteria TEXT NULL
        /// </summary>
        [Column("success_criteria")]
        public string? SuccessCriteria { get; set; }

        /// <summary>
        /// What support/help is needed.
        /// Maps to: support_needed TEXT NULL
        /// </summary>
        [Column("support_needed")]
        public string? SupportNeeded { get; set; }

        /// <summary>
        /// Resources (links, books, courses, etc.).
        /// Maps to: resources TEXT NULL
        /// </summary>
        [Column("resources")]
        public string? Resources { get; set; }

        /// <summary>
        /// Is this goal private (only visible to self and manager)?
        /// Maps to: is_private BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_private")]
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// Is this goal shared with manager?
        /// Maps to: shared_with_manager BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("shared_with_manager")]
        public bool SharedWithManager { get; set; } = true;

        /// <summary>
        /// Optional linked performance review.
        /// Maps to: review_id UUID NULL
        /// </summary>
        [Column("review_id")]
        public Guid? ReviewId { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Navigation to the team member.
        /// </summary>
        [NotMapped]
        public TeamMember? TeamMember { get; set; }

        /// <summary>
        /// Navigation to the organization.
        /// </summary>
        [NotMapped]
        public Organization? Organization { get; set; }

        /// <summary>
        /// Milestones to track progress.
        /// </summary>
        [NotMapped]
        public List<DevelopmentGoalMilestone> Milestones { get; set; } = new();

        /// <summary>
        /// Comments and check-ins on this goal.
        /// </summary>
        [NotMapped]
        public List<DevelopmentGoalComment> Comments { get; set; } = new();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Is the goal overdue?
        /// </summary>
        [NotMapped]
        public bool IsOverdue => TargetDate.HasValue && 
            TargetDate.Value < DateTime.Today && 
            Status != DevelopmentGoalStatus.Completed &&
            Status != DevelopmentGoalStatus.Cancelled;

        /// <summary>
        /// Days until target date (negative if overdue).
        /// </summary>
        [NotMapped]
        public int? DaysRemaining => TargetDate.HasValue 
            ? (int)(TargetDate.Value - DateTime.Today).TotalDays 
            : null;

        /// <summary>
        /// Is the goal active?
        /// </summary>
        [NotMapped]
        public bool IsActive => Status == DevelopmentGoalStatus.Active;

        #endregion
    }
}
