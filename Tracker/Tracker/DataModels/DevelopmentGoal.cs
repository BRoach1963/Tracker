using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Personal development goal for a team member.
    /// Maps to Supabase 'development_goals' table.
    /// Tracks career growth, skill development, certifications, etc.
    /// </summary>
    public class DevelopmentGoal : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this goal belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member this goal belongs to.
        /// </summary>
        public Guid TeamMemberId { get; set; }
        public TeamMember? TeamMember { get; set; }

        /// <summary>
        /// Goal title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Category of the goal.
        /// </summary>
        public DevelopmentGoalCategory Category { get; set; } = DevelopmentGoalCategory.SkillDevelopment;

        /// <summary>
        /// Target completion date.
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// When work started on this goal.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the goal was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Current status of the goal.
        /// </summary>
        public DevelopmentGoalStatus Status { get; set; } = DevelopmentGoalStatus.Draft;

        /// <summary>
        /// Progress percentage (0-100).
        /// </summary>
        public int ProgressPercent { get; set; }

        /// <summary>
        /// Why this goal is important to the person.
        /// </summary>
        public string? WhyImportant { get; set; }

        /// <summary>
        /// How to know when the goal is achieved.
        /// </summary>
        public string? SuccessCriteria { get; set; }

        /// <summary>
        /// What support/help is needed.
        /// </summary>
        public string? SupportNeeded { get; set; }

        /// <summary>
        /// Resources (links, books, courses, etc.).
        /// </summary>
        public string? Resources { get; set; }

        /// <summary>
        /// Is this goal private (only visible to self and manager)?
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Is this goal shared with manager?
        /// </summary>
        public bool SharedWithManager { get; set; } = true;

        /// <summary>
        /// Optional linked performance review.
        /// </summary>
        public Guid? ReviewId { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Milestones to track progress.
        /// </summary>
        public List<DevelopmentGoalMilestone> Milestones { get; set; } = new();

        /// <summary>
        /// Comments and check-ins on this goal.
        /// </summary>
        public List<DevelopmentGoalComment> Comments { get; set; } = new();

        #region Computed Properties

        /// <summary>
        /// Is the goal overdue?
        /// </summary>
        public bool IsOverdue => TargetDate.HasValue && 
            TargetDate.Value < DateTime.Today && 
            Status != DevelopmentGoalStatus.Completed &&
            Status != DevelopmentGoalStatus.Cancelled;

        /// <summary>
        /// Days until target date (negative if overdue).
        /// </summary>
        public int? DaysRemaining => TargetDate.HasValue 
            ? (int)(TargetDate.Value - DateTime.Today).TotalDays 
            : null;

        /// <summary>
        /// Is the goal active?
        /// </summary>
        public bool IsActive => Status == DevelopmentGoalStatus.Active;

        #endregion
    }
}
