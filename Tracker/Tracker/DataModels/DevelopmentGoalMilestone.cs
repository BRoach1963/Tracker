using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A milestone within a development goal.
    /// Maps to Supabase 'development_goal_milestones' table.
    /// </summary>
    public class DevelopmentGoalMilestone : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Parent development goal.
        /// </summary>
        public Guid GoalId { get; set; }
        public DevelopmentGoal? Goal { get; set; }

        /// <summary>
        /// Milestone title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Target date for this milestone.
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// When the milestone was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Current status.
        /// </summary>
        public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;

        /// <summary>
        /// Sort order for display.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }
    }
}
