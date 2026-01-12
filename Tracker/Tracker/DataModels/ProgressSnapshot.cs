namespace Tracker.DataModels
{
    /// <summary>
    /// Supported entity types for progress snapshots.
    /// </summary>
    public enum SnapshotEntityType
    {
        Goal = 0,
        Target = 1,
        Project = 2,
        Task = 3
    }

    /// <summary>
    /// Represents a point-in-time snapshot of progress for a unified entity
    /// (Goal, Target, Project, or Task) in the new schema.
    /// Used for trajectory analysis and predictive analytics.
    /// 
    /// Snapshots are captured periodically and stored for historical trend analysis.
    /// This data enables:
    /// - Velocity calculations (progress per day)
    /// - Trajectory projections (will we hit the target?)
    /// - Confidence intervals (how reliable is the prediction?)
    /// - Trend visualization (charts showing progress over time)
    /// </summary>
    public class ProgressSnapshot
    {
        /// <summary>
        /// Primary key for the snapshot.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The type of entity being tracked: Goal, Target, Project, or Task.
        /// </summary>
        public SnapshotEntityType EntityType { get; set; } = SnapshotEntityType.Goal;

        /// <summary>
        /// The unique identifier of the entity (Goal, Target, Project, or Task).
        /// Uses Guid to align with unified schema.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// The date of the snapshot (date only, no time component).
        /// </summary>
        public DateTime SnapshotDate { get; set; }

        /// <summary>
        /// The current value at the time of snapshot.
        /// For goals/targets/projects this is typically the completion percentage
        /// or current progress value. For metrics-style entities this is the
        /// current measured value.
        /// </summary>
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// The target value at the time of snapshot.
        /// For goals/projects this is often 100 (representing 100% complete).
        /// For metrics-style entities this is the numeric target value.
        /// </summary>
        public decimal TargetValue { get; set; }

        /// <summary>
        /// The progress percentage (0-100+) at the time of snapshot.
        /// Calculated as (CurrentValue / TargetValue) * 100 for most entities,
        /// or directly stored for OKRs/Projects.
        /// </summary>
        public decimal Progress { get; set; }

        /// <summary>
        /// The user who owns this snapshot data.
        /// Enables multi-user support in shared databases.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// When this snapshot was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
