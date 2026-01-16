using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Core.Common.Enums;
using Tracker.Core.Interfaces;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// Metric (formerly KPI) - a quantitative measure of performance.
    /// Maps to Supabase 'metrics' table.
    /// Implements IMeasurable to feed into Targets.
    /// </summary>
    [Table("metrics")]
    public class Metric : AuditableEntity, IMeasurable
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this metric belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member who owns this metric.
        /// Maps to: owner_team_member_id UUID NULL
        /// </summary>
        [Column("owner_team_member_id")]
        public Guid? OwnerTeamMemberId { get; set; }

        /// <summary>
        /// User who created this metric.
        /// Maps to: created_by_user_id UUID NOT NULL
        /// </summary>
        [Column("created_by_user_id")]
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Name of the metric.
        /// Maps to: name VARCHAR(200) NOT NULL
        /// </summary>
        [Column("name")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this metric measures.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Category for grouping (Sales, Engineering, Customer Success, etc.).
        /// Maps to: category VARCHAR(100) NULL
        /// </summary>
        [Column("category")]
        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Current value of the metric.
        /// Maps to: current_value NUMERIC NULL
        /// </summary>
        [Column("current_value")]
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Target value to achieve.
        /// Maps to: target_value NUMERIC NULL
        /// </summary>
        [Column("target_value")]
        public decimal? TargetValue { get; set; }

        /// <summary>
        /// Baseline value for comparison.
        /// Maps to: baseline_value NUMERIC NULL
        /// </summary>
        [Column("baseline_value")]
        public decimal? BaselineValue { get; set; }

        /// <summary>
        /// Unit of measurement (%, $, count, hours, etc.).
        /// Maps to: unit VARCHAR(50) NULL
        /// </summary>
        [Column("unit")]
        [MaxLength(50)]
        public string? Unit { get; set; }

        /// <summary>
        /// Target direction (stored as string).
        /// Maps to: target_direction VARCHAR(50) NOT NULL DEFAULT 'higher_is_better'
        /// </summary>
        [Column("target_direction")]
        [MaxLength(50)]
        public string TargetDirectionString { get; set; } = "higher_is_better";

        /// <summary>
        /// Target direction as enum.
        /// </summary>
        [NotMapped]
        public MetricTargetDirection TargetDirection
        {
            get => TargetDirectionString switch
            {
                "lower_is_better" => MetricTargetDirection.LowerIsBetter,
                "target_value" => MetricTargetDirection.TargetValue,
                _ => MetricTargetDirection.HigherIsBetter
            };
            set => TargetDirectionString = value switch
            {
                MetricTargetDirection.LowerIsBetter => "lower_is_better",
                MetricTargetDirection.TargetValue => "target_value",
                _ => "higher_is_better"
            };
        }

        /// <summary>
        /// Update frequency (stored as string).
        /// Maps to: frequency VARCHAR(50) NOT NULL DEFAULT 'monthly'
        /// </summary>
        [Column("frequency")]
        [MaxLength(50)]
        public string FrequencyString { get; set; } = "monthly";

        /// <summary>
        /// Frequency as enum.
        /// </summary>
        [NotMapped]
        public MetricFrequency Frequency
        {
            get => Enum.TryParse<MetricFrequency>(FrequencyString, true, out var result) ? result : MetricFrequency.Monthly;
            set => FrequencyString = value.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// When the value was last updated.
        /// Maps to: last_updated_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("last_updated_at")]
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// Is this a composite metric (calculated from children)?
        /// Maps to: is_composite BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_composite")]
        public bool IsComposite { get; set; }

        /// <summary>
        /// Parent metric for composite hierarchies.
        /// Maps to: parent_metric_id UUID NULL
        /// </summary>
        [Column("parent_metric_id")]
        public Guid? ParentMetricId { get; set; }

        /// <summary>
        /// Is this metric visible to the team?
        /// Maps to: is_team_visible BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_team_visible")]
        public bool IsTeamVisible { get; set; } = true;

        /// <summary>
        /// Is this metric visible to the entire organization?
        /// Maps to: is_org_visible BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_org_visible")]
        public bool IsOrgVisible { get; set; }

        /// <summary>
        /// Warning threshold - below this = at risk.
        /// Maps to: warning_threshold NUMERIC NULL
        /// </summary>
        [Column("warning_threshold")]
        public decimal? WarningThreshold { get; set; }

        /// <summary>
        /// Critical threshold - below this = off track.
        /// Maps to: critical_threshold NUMERIC NULL
        /// </summary>
        [Column("critical_threshold")]
        public decimal? CriticalThreshold { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Owner team member.
        /// </summary>
        [NotMapped]
        public TeamMember? Owner { get; set; }

        /// <summary>
        /// Parent metric for hierarchies.
        /// </summary>
        [NotMapped]
        public Metric? ParentMetric { get; set; }

        /// <summary>
        /// Child metrics for composite calculations.
        /// </summary>
        [NotMapped]
        public List<Metric> ChildMetrics { get; set; } = new();

        /// <summary>
        /// Data sources for this metric.
        /// </summary>
        [NotMapped]
        public List<MetricDataSource> DataSources { get; set; } = new();

        /// <summary>
        /// Historical values for trending.
        /// </summary>
        [NotMapped]
        public List<MetricHistory> History { get; set; } = new();

        #endregion

        #region IMeasurable Implementation

        int IMeasurable.Id => 0; // Deprecated - use Guid Id
        Guid IMeasurable.GuidId => Id;
        string IMeasurable.DisplayName => Name;
        decimal IMeasurable.CurrentProgress => Progress;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Progress percentage towards target.
        /// </summary>
        [NotMapped]
        public decimal Progress
        {
            get
            {
                if (!TargetValue.HasValue || TargetValue.Value == 0) return 0;

                var progress = (CurrentValue / TargetValue.Value) * 100m;
                
                if (TargetDirection == MetricTargetDirection.LowerIsBetter)
                {
                    progress = Math.Max(0, (2m - (CurrentValue / TargetValue.Value)) * 100m);
                }

                return Math.Max(0, Math.Min(progress, 100m));
            }
        }

        /// <summary>
        /// Status based on thresholds and progress.
        /// </summary>
        [NotMapped]
        public GoalStatus Status
        {
            get
            {
                if (!TargetValue.HasValue) return GoalStatus.NotStarted;
                if (Progress >= 100) return GoalStatus.Completed;
                if (CriticalThreshold.HasValue && Progress < (decimal)CriticalThreshold.Value) return GoalStatus.OffTrack;
                if (WarningThreshold.HasValue && Progress < (decimal)WarningThreshold.Value) return GoalStatus.AtRisk;
                return GoalStatus.OnTrack;
            }
        }

        /// <summary>
        /// Is metric on track?
        /// </summary>
        [NotMapped]
        public bool IsOnTrack => Status == GoalStatus.OnTrack || Status == GoalStatus.Completed;

        #endregion
    }
}
