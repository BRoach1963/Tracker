namespace Tracker.DataModels
{
    /// <summary>
    /// A milestone within an individual goal.
    /// Helps track incremental progress toward the goal.
    /// </summary>
    public class GoalMilestone : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The goal this milestone belongs to.
        /// </summary>
        public int GoalId { get; set; }

        /// <summary>
        /// Description of the milestone.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether the milestone is completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// When the milestone was completed.
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Sort order for display.
        /// </summary>
        public int SortOrder { get; set; }
    }
}

