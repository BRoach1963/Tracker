namespace Tracker.DataModels
{
    /// <summary>
    /// A milestone within a Goal.
    /// Maps to Supabase 'goal_milestones' table.
    /// </summary>
    public class GoalMilestone : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Parent goal this milestone belongs to.
        /// </summary>
        public Guid GoalId { get; set; }
        public Goal? Goal { get; set; }

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
        public DateTime TargetDate { get; set; }

        /// <summary>
        /// When the milestone was completed.
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Whether the milestone is completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Sort order for display.
        /// </summary>
        public int SortOrder { get; set; }
    }
}

