using Tracker.Common.Enums;
using Tracker.Interfaces;

namespace Tracker.DataModels
{
    /// <summary>
    /// Key Performance Indicator - a measurable metric to track progress.
    /// 
    /// KPIs can be:
    /// - Standalone: Not linked to any OKR
    /// - Linked: Feeds into Key Results via IMeasurable interface
    /// - Composite: Calculated from child KPIs
    /// 
    /// KPIs implement both IMeasurable (to feed Key Results) and IKpiSource (for composite KPIs).
    /// </summary>
    public class KeyPerformanceIndicator : AuditableEntity, IMeasurable, IKpiSource
    {
        /// <summary>
        /// Primary key for the KPI.
        /// </summary>
        public int KpiId { get; set; }
        
        /// <summary>
        /// Name of the KPI.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Extended description of what this KPI measures.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Current value of the KPI.
        /// Can be manually entered or calculated from data sources.
        /// </summary>
        public double Value { get; set; }
        
        /// <summary>
        /// Target value to achieve.
        /// </summary>
        public double TargetValue { get; set; }
        
        /// <summary>
        /// Unit of measurement (%, $, count, points, hours, etc.).
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Optional category for grouping KPIs (e.g., "Customer", "Revenue", "Quality").
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Owner responsible for this KPI.
        /// </summary>
        public TeamMember Owner { get; set; } = null!;
        
        /// <summary>
        /// When the value was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Whether higher is better (GreaterOrEqual) or lower is better (LessOrEqual).
        /// </summary>
        public TargetDirectionEnum TargetDirection { get; set; } = TargetDirectionEnum.GreaterOrEqual;

        /// <summary>
        /// How often this KPI is updated/measured.
        /// </summary>
        public KpiFrequencyEnum Frequency { get; set; } = KpiFrequencyEnum.OnDemand;

        /// <summary>
        /// If true, this KPI's value is calculated from child KPIs.
        /// </summary>
        public bool IsComposite { get; set; }

        /// <summary>
        /// FK to parent KPI for composite hierarchies. Null for top-level KPIs.
        /// </summary>
        public int? ParentKpiId { get; set; }

        /// <summary>
        /// Navigation to parent KPI (for composite hierarchies).
        /// </summary>
        public KeyPerformanceIndicator? ParentKpi { get; set; }

        /// <summary>
        /// Child KPIs for composite calculations.
        /// </summary>
        public List<KeyPerformanceIndicator> ChildKpis { get; set; } = new();

        /// <summary>
        /// Data sources that feed this KPI's value.
        /// </summary>
        public List<KpiDataSource> DataSources { get; set; } = new();

        #region IMeasurable Implementation

        /// <summary>
        /// IMeasurable.MeasurableId - returns the KPI Id.
        /// </summary>
        public int MeasurableId => KpiId;

        /// <summary>
        /// IMeasurable.DisplayName - returns the KPI name.
        /// </summary>
        public string DisplayName => Name;

        /// <summary>
        /// IMeasurable.Progress - percentage towards target (0-100+).
        /// </summary>
        public decimal Progress => (decimal)PercentComplete;

        /// <summary>
        /// IMeasurable.DisplayValue - shows current value with unit.
        /// </summary>
        public string DisplayValue => string.IsNullOrEmpty(Unit) 
            ? $"{Value:N1}" 
            : $"{Value:N1} {Unit}";

        /// <summary>
        /// IMeasurable.MeasurableType - always KPI.
        /// </summary>
        public MeasurableType MeasurableType => MeasurableType.Kpi;

        #endregion

        #region IKpiSource Implementation

        /// <summary>
        /// IKpiSource.SourceId - returns the KPI Id.
        /// </summary>
        public int SourceId => KpiId;

        /// <summary>
        /// IKpiSource.SourceDisplayName - returns the KPI name.
        /// </summary>
        public string SourceDisplayName => Name;

        /// <summary>
        /// IKpiSource.GetValue - returns the current KPI value.
        /// </summary>
        public decimal GetValue() => (decimal)Value;

        /// <summary>
        /// IKpiSource.SourceType - always ChildKpi when used as a source.
        /// </summary>
        public KpiSourceType SourceType => KpiSourceType.ChildKpi;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Calculated status based on Value vs TargetValue.
        /// Green: On target or better
        /// Amber: Within 10% of target  
        /// Red: More than 10% away from target
        /// </summary>
        public KpiStatusEnum Status
        {
            get
            {
                if (TargetValue == 0) return KpiStatusEnum.OnTarget;
                
                var percentComplete = (Value / TargetValue) * 100;
                
                if (TargetDirection == TargetDirectionEnum.GreaterOrEqual)
                {
                    // Higher is better (e.g., revenue, completions)
                    if (percentComplete >= 100)
                        return KpiStatusEnum.OnTarget;      // Green: Met or exceeded
                    else if (percentComplete >= 90)
                        return KpiStatusEnum.CloseToTarget; // Amber: Within 10%
                    else
                        return KpiStatusEnum.OffTarget;     // Red: Below 90%
                }
                else
                {
                    // Lower is better (e.g., bugs, response time)
                    if (percentComplete <= 100)
                        return KpiStatusEnum.OnTarget;      // Green: At or below target
                    else if (percentComplete <= 110)
                        return KpiStatusEnum.CloseToTarget; // Amber: Within 10% over
                    else
                        return KpiStatusEnum.OffTarget;     // Red: More than 10% over
                }
            }
        }
        
        /// <summary>
        /// Percentage complete towards target (0-100+).
        /// </summary>
        public double PercentComplete => TargetValue == 0 ? 100 : Math.Round((Value / TargetValue) * 100, 1);
        
        /// <summary>
        /// Number of 1:1 meetings where this KPI was discussed (computed, not persisted).
        /// </summary>
        public int MeetingCount { get; set; }

        /// <summary>
        /// Whether this KPI has any data sources configured.
        /// </summary>
        public bool HasDataSources => DataSources?.Count > 0;

        /// <summary>
        /// Whether this KPI has child KPIs (is a composite parent).
        /// </summary>
        public bool HasChildKpis => ChildKpis?.Count > 0;

        #endregion
    }
}
