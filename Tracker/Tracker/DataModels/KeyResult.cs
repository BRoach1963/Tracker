using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Key Result - a measurable outcome that belongs to an OKR.
    /// Key Results are NOT standalone - they only exist within an OKR.
    /// 
    /// Progress is calculated as: (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
    /// 
    /// CurrentValue can be:
    /// - Manually entered
    /// - Auto-calculated from linked Measurables (KPI, Project, TaskCollection)
    /// </summary>
    public class KeyResult : AuditableEntity
    {
        /// <summary>
        /// Primary key for the Key Result.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// FK to the parent OKR. Required - Key Results cannot exist without an OKR.
        /// </summary>
        public int OkrId { get; set; }

        /// <summary>
        /// What we're measuring - the key result statement.
        /// Example: "Increase NPS from 45 to 60"
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional extended description of the key result.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The target value to achieve.
        /// </summary>
        public decimal TargetValue { get; set; }

        /// <summary>
        /// The current value (manually entered or calculated from Measurables).
        /// </summary>
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// The starting baseline value (default 0).
        /// Used for progress calculation when starting from non-zero.
        /// </summary>
        public decimal StartingValue { get; set; }

        /// <summary>
        /// Unit of measurement (%, points, hours, count, $, etc.).
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Weight for weighted average calculations in the parent OKR.
        /// Default is 1.0 (equal weighting).
        /// </summary>
        public decimal Weight { get; set; } = 1.0m;

        /// <summary>
        /// Sort order for display within the OKR.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Whether higher is better (GreaterOrEqual) or lower is better (LessOrEqual).
        /// </summary>
        public TargetDirectionEnum TargetDirection { get; set; } = TargetDirectionEnum.GreaterOrEqual;

        /// <summary>
        /// Navigation property to the parent OKR.
        /// </summary>
        public ObjectiveKeyResult? Okr { get; set; }

        /// <summary>
        /// Collection of measurable sources that feed this Key Result.
        /// </summary>
        public List<KeyResultMeasurable> Measurables { get; set; } = new();

        #region Computed Properties

        /// <summary>
        /// Progress percentage (0-100+) towards the target.
        /// Calculated as: (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
        /// </summary>
        public decimal Progress
        {
            get
            {
                var range = TargetValue - StartingValue;
                if (range == 0) return CurrentValue >= TargetValue ? 100m : 0m;

                var progress = ((CurrentValue - StartingValue) / range) * 100m;
                return Math.Round(progress, 1);
            }
        }

        /// <summary>
        /// Status based on progress percentage.
        /// </summary>
        public KpiStatusEnum Status
        {
            get
            {
                var progress = Progress;
                if (TargetDirection == TargetDirectionEnum.GreaterOrEqual)
                {
                    if (progress >= 100) return KpiStatusEnum.OnTarget;
                    if (progress >= 70) return KpiStatusEnum.CloseToTarget;
                    return KpiStatusEnum.OffTarget;
                }
                else
                {
                    // Lower is better - invert the logic
                    if (progress <= 100) return KpiStatusEnum.OnTarget;
                    if (progress <= 110) return KpiStatusEnum.CloseToTarget;
                    return KpiStatusEnum.OffTarget;
                }
            }
        }

        /// <summary>
        /// Display string for the current value with unit.
        /// Example: "53/60 points" or "75%"
        /// </summary>
        public string DisplayValue => $"{CurrentValue}/{TargetValue} {Unit}".Trim();

        /// <summary>
        /// Whether this Key Result has any linked measurables.
        /// </summary>
        public bool HasMeasurables => Measurables?.Count > 0;

        #endregion
    }
}


