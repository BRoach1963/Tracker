namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a point-in-time snapshot of progress for an entity (OKR, KPI, Project, KeyResult).
    /// Used for trajectory analysis and predictive analytics.
    /// 
    /// Snapshots are captured daily on app startup and stored for historical trend analysis.
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
        public int Id { get; set; }

        /// <summary>
        /// The type of entity being tracked: "OKR", "KPI", "KeyResult", "Project".
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the entity (ObjectiveId, KpiId, KeyResultId, or ProjectId).
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// The date of the snapshot (date only, no time component).
        /// </summary>
        public DateTime SnapshotDate { get; set; }

        /// <summary>
        /// The current value at the time of snapshot.
        /// For OKRs/Projects this is the completion percentage.
        /// For KPIs this is the actual metric value.
        /// For KeyResults this is the CurrentValue.
        /// </summary>
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// The target value at the time of snapshot.
        /// For OKRs/Projects this is always 100.
        /// For KPIs this is the TargetValue.
        /// For KeyResults this is the TargetValue.
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

    /// <summary>
    /// Supported entity types for progress snapshots.
    /// </summary>
    public static class SnapshotEntityType
    {
        public const string OKR = "OKR";
        public const string KPI = "KPI";
        public const string KeyResult = "KeyResult";
        public const string Project = "Project";
    }
}
