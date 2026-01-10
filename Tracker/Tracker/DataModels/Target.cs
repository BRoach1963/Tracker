using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Target (formerly Key Result) - a measurable outcome for a Goal.
    /// Maps to Supabase 'targets' table.
    /// Progress is calculated as: (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
    /// </summary>
    public class Target : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Parent goal this target belongs to.
        /// </summary>
        public Guid GoalId { get; set; }
        public Goal? Goal { get; set; }

        /// <summary>
        /// Target title - what we're measuring.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Extended description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Target value to achieve.
        /// </summary>
        public decimal TargetValue { get; set; }

        /// <summary>
        /// Current value (manual or calculated from measurables).
        /// </summary>
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Starting baseline value.
        /// </summary>
        public decimal StartingValue { get; set; }

        /// <summary>
        /// Unit of measurement (%, points, hours, $, etc.).
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Weight for weighted progress calculation.
        /// </summary>
        public decimal Weight { get; set; } = 1.0m;

        /// <summary>
        /// Current status.
        /// </summary>
        public OkrStatus Status { get; set; } = OkrStatus.NotStarted;

        /// <summary>
        /// Sort order within the goal.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Linked measurable sources that feed this target.
        /// </summary>
        public List<TargetMeasurable> Measurables { get; set; } = new();

        #region Computed Properties

        /// <summary>
        /// Progress percentage (0-100+).
        /// </summary>
        public decimal Progress
        {
            get
            {
                var range = TargetValue - StartingValue;
                if (range == 0) return TargetValue == CurrentValue ? 100m : 0m;
                
                var progress = (CurrentValue - StartingValue) / range * 100m;
                return Math.Max(0, Math.Min(progress, 100m));
            }
        }

        /// <summary>
        /// Is target complete (progress >= 100%)?
        /// </summary>
        public bool IsComplete => Progress >= 100m;

        /// <summary>
        /// Remaining value to achieve target.
        /// </summary>
        public decimal Remaining => TargetValue - CurrentValue;

        #endregion
    }
}
