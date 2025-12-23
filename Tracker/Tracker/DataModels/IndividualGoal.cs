using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Personal development goal for a team member.
    /// Tracks career growth, skill development, certifications, etc.
    /// </summary>
    public class IndividualGoal : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The team member this goal belongs to.
        /// </summary>
        public int TeamMemberId { get; set; }
        public TeamMember TeamMember { get; set; } = null!;

        /// <summary>
        /// Goal title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the goal.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category of the goal.
        /// </summary>
        public GoalCategory Category { get; set; }

        /// <summary>
        /// Current status of the goal.
        /// </summary>
        public GoalStatus Status { get; set; } = GoalStatus.NotStarted;

        /// <summary>
        /// Target completion date.
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// Progress percentage (0-100).
        /// </summary>
        public int ProgressPercent { get; set; }

        /// <summary>
        /// Additional notes about progress.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Milestones to track progress.
        /// </summary>
        public List<GoalMilestone> Milestones { get; set; } = new();

        /// <summary>
        /// Computed: Is the goal overdue?
        /// </summary>
        public bool IsOverdue => TargetDate.HasValue && TargetDate.Value < DateTime.Today && Status != GoalStatus.Completed;

        /// <summary>
        /// Computed: Days until target date (negative if overdue).
        /// </summary>
        public int? DaysRemaining => TargetDate.HasValue ? (int)(TargetDate.Value - DateTime.Today).TotalDays : null;
    }
}

