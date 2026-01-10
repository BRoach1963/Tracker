using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    /// <summary>
    /// Metric (formerly KPI) - a quantitative measure of performance.
    /// Maps to Supabase 'metrics' table.
    /// Implements IMeasurable to feed into Targets.
    /// </summary>
    public class Metric : AuditableEntity, IMeasurable
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this metric belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member who owns this metric.
        /// </summary>
        public Guid? OwnerTeamMemberId { get; set; }
        public TeamMember? Owner { get; set; }

        /// <summary>
        /// User who created this metric.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Name of the metric.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this metric measures.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Category for grouping (Sales, Engineering, Customer Success, etc.).
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Current value of the metric.
        /// </summary>
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Target value to achieve.
        /// </summary>
        public decimal? TargetValue { get; set; }

        /// <summary>
        /// Baseline value for comparison.
        /// </summary>
        public decimal? BaselineValue { get; set; }

        /// <summary>
        /// Unit of measurement (%, $, count, hours, etc.).
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Target direction (higher_is_better, lower_is_better, target_value).
        /// </summary>
        public MetricTargetDirection TargetDirection { get; set; } = MetricTargetDirection.HigherIsBetter;

        /// <summary>
        /// Update frequency (daily, weekly, monthly, quarterly, annually).
        /// </summary>
        public MetricFrequency Frequency { get; set; } = MetricFrequency.Monthly;

        /// <summary>
        /// When the value was last updated.
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// Is this a composite metric (calculated from children)?
        /// </summary>
        public bool IsComposite { get; set; }

        /// <summary>
        /// Parent metric for composite hierarchies.
        /// </summary>
        public Guid? ParentMetricId { get; set; }
        public Metric? ParentMetric { get; set; }

        /// <summary>
        /// Is this metric visible to the team?
        /// </summary>
        public bool IsTeamVisible { get; set; } = true;

        /// <summary>
        /// Is this metric visible to the entire organization?
        /// </summary>
        public bool IsOrgVisible { get; set; }

        /// <summary>
        /// Warning threshold - below this = at risk.
        /// </summary>
        public decimal? WarningThreshold { get; set; }

        /// <summary>
        /// Critical threshold - below this = off track.
        /// </summary>
        public decimal? CriticalThreshold { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Child metrics for composite calculations.
        /// </summary>
        public List<Metric> ChildMetrics { get; set; } = new();

        /// <summary>
        /// Data sources for this metric.
        /// </summary>
        public List<MetricDataSource> DataSources { get; set; } = new();

        /// <summary>
        /// Historical values for trending.
        /// </summary>
        public List<MetricHistory> History { get; set; } = new();

        #region IMeasurable Implementation

        /// <summary>
        /// Measurable ID (same as Id for metrics).
        /// </summary>
        int IMeasurable.Id => 0; // Deprecated - use Guid Id
        
        Guid IMeasurable.GuidId => Id;
        
        string IMeasurable.DisplayName => Name;
        
        decimal IMeasurable.CurrentProgress => Progress;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Progress percentage towards target.
        /// </summary>
        public decimal Progress
        {
            get
            {
                if (!TargetValue.HasValue || TargetValue.Value == 0) return 0;

                var progress = (CurrentValue / TargetValue.Value) * 100m;
                
                if (TargetDirection == MetricTargetDirection.LowerIsBetter)
                {
                    // Inverted - lower current value = higher progress
                    progress = Math.Max(0, (2m - (CurrentValue / TargetValue.Value)) * 100m);
                }

                return Math.Max(0, Math.Min(progress, 100m));
            }
        }

        /// <summary>
        /// Status based on thresholds and progress.
        /// </summary>
        public OkrStatus Status
        {
            get
            {
                if (!TargetValue.HasValue) return OkrStatus.NotStarted;
                if (Progress >= 100) return OkrStatus.Completed;
                if (CriticalThreshold.HasValue && Progress < (decimal)CriticalThreshold.Value) return OkrStatus.OffTrack;
                if (WarningThreshold.HasValue && Progress < (decimal)WarningThreshold.Value) return OkrStatus.AtRisk;
                return OkrStatus.OnTrack;
            }
        }

        /// <summary>
        /// Is metric on track?
        /// </summary>
        public bool IsOnTrack => Status == OkrStatus.OnTrack || Status == OkrStatus.Completed;

        #endregion
    }
}
