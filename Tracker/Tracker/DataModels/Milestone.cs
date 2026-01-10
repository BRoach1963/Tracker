namespace Tracker.DataModels
{
    /// <summary>
    /// Key deliverable within a project.
    /// Maps to Supabase 'milestones' table.
    /// </summary>
    public class Milestone : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Project this milestone belongs to.
        /// </summary>
        public Guid ProjectId { get; set; }
        public Project? Project { get; set; }

        /// <summary>
        /// Milestone title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the milestone.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Target date for completion.
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
