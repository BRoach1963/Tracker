using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Objective and Key Results (OKR) - a goal-setting framework entity.
    /// The Objective is the qualitative goal ("what" we want to achieve).
    /// Key Results are the measurable outcomes ("how" we measure success).
    /// 
    /// Key Rules:
    /// - An Objective should have 1+ Key Results to be measurable
    /// - Key Results exist ONLY within OKRs (not standalone)
    /// - Progress is automatically calculated from Key Results
    /// </summary>
    public class ObjectiveKeyResult : AuditableEntity
    {
        /// <summary>
        /// Primary key for the OKR.
        /// </summary>
        public int ObjectiveId { get; set; }
        
        /// <summary>
        /// The objective statement - what we want to achieve.
        /// Example: "Improve Customer Satisfaction"
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Extended description of the objective.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Team member who owns this OKR.
        /// </summary>
        public TeamMember Owner { get; set; } = null!;
        
        /// <summary>
        /// Period start date.
        /// </summary>
        public DateTime StartDate { get; set; }
        
        /// <summary>
        /// Period end date.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Time period for this OKR (Q1-Q4, Annual, Custom).
        /// </summary>
        public TimePeriodEnum TimePeriod { get; set; } = TimePeriodEnum.Custom;

        /// <summary>
        /// Year for the time period (e.g., 2025).
        /// </summary>
        public int Year { get; set; } = DateTime.Now.Year;

        /// <summary>
        /// Optional FK to a Project. OKRs don't require a project - 
        /// they connect to projects through Key Results' Measurables.
        /// Kept for backwards compatibility.
        /// </summary>
        public int? ProjectId { get; set; }

        /// <summary>
        /// Manual status override. If null, status is auto-calculated from Key Results.
        /// </summary>
        public ObjectiveStatusEnum? StatusOverride { get; set; }

        /// <summary>
        /// Key Results that measure progress towards this objective.
        /// </summary>
        public List<KeyResult> KeyResults { get; set; } = new();

        #region Computed Properties

        /// <summary>
        /// Calculated status based on Key Result progress, or manual override.
        /// OnTrack: Progress ≥ 70%
        /// AtRisk: Progress 40-69%
        /// OffTrack: Progress < 40%
        /// </summary>
        public ObjectiveStatusEnum Status
        {
            get
            {
                // Allow manual override
                if (StatusOverride.HasValue)
                    return StatusOverride.Value;

                // Calculate from Key Results
                if (KeyResults == null || KeyResults.Count == 0)
                    return ObjectiveStatusEnum.OffTrack;

                // If any KR is off target, the OKR is off track
                if (KeyResults.Any(kr => kr.Status == KpiStatusEnum.OffTarget))
                    return ObjectiveStatusEnum.OffTrack;

                // If any KR is close to target, the OKR is at risk
                if (KeyResults.Any(kr => kr.Status == KpiStatusEnum.CloseToTarget))
                    return ObjectiveStatusEnum.AtRisk;

                // All KRs on target
                return ObjectiveStatusEnum.OnTrack;
            }
        }

        /// <summary>
        /// Overall completion percentage, calculated as weighted average of Key Results.
        /// </summary>
        public double CompletionPercentage
        {
            get
            {
                if (KeyResults == null || KeyResults.Count == 0) 
                    return 0;

                // Calculate weighted average
                var totalWeight = KeyResults.Sum(kr => kr.Weight);
                if (totalWeight == 0) 
                    return 0;

                var weightedSum = KeyResults.Sum(kr => kr.Progress * kr.Weight);
                return (double)Math.Round(weightedSum / totalWeight, 1);
            }
        }
        
        /// <summary>
        /// Number of 1:1 meetings where this OKR was discussed (non-persisted, computed property).
        /// </summary>
        public int MeetingCount { get; set; }

        /// <summary>
        /// Display string for the time period.
        /// Example: "Q1 2025" or "2025"
        /// </summary>
        public string TimePeriodDisplay
        {
            get
            {
                return TimePeriod switch
                {
                    TimePeriodEnum.Q1 => $"Q1 {Year}",
                    TimePeriodEnum.Q2 => $"Q2 {Year}",
                    TimePeriodEnum.Q3 => $"Q3 {Year}",
                    TimePeriodEnum.Q4 => $"Q4 {Year}",
                    TimePeriodEnum.Annual => $"{Year}",
                    TimePeriodEnum.Custom => $"{StartDate:MMM d} - {EndDate:MMM d, yyyy}",
                    _ => $"{StartDate:MMM d} - {EndDate:MMM d, yyyy}"
                };
            }
        }

        /// <summary>
        /// Number of Key Results in this OKR.
        /// </summary>
        public int KeyResultCount => KeyResults?.Count ?? 0;

        /// <summary>
        /// Whether this OKR has any Key Results defined.
        /// </summary>
        public bool HasKeyResults => KeyResultCount > 0;

        /// <summary>
        /// Count of linked KPIs across all Key Results.
        /// </summary>
        public int LinkedKpiCount => KeyResults?
            .SelectMany(kr => kr.Measurables ?? Enumerable.Empty<KeyResultMeasurable>())
            .Count(m => m.MeasurableType == Interfaces.MeasurableType.Kpi) ?? 0;

        /// <summary>
        /// Count of linked Projects across all Key Results.
        /// </summary>
        public int LinkedProjectCount => KeyResults?
            .SelectMany(kr => kr.Measurables ?? Enumerable.Empty<KeyResultMeasurable>())
            .Count(m => m.MeasurableType == Interfaces.MeasurableType.Project) ?? 0;

        /// <summary>
        /// Count of linked TaskCollections across all Key Results.
        /// </summary>
        public int LinkedTaskCollectionCount => KeyResults?
            .SelectMany(kr => kr.Measurables ?? Enumerable.Empty<KeyResultMeasurable>())
            .Count(m => m.MeasurableType == Interfaces.MeasurableType.TaskCollection) ?? 0;

        /// <summary>
        /// Whether this OKR is currently active (between start and end dates).
        /// </summary>
        public bool IsActive => DateTime.Today >= StartDate.Date && DateTime.Today <= EndDate.Date;

        /// <summary>
        /// Days remaining until the end date (negative if past).
        /// </summary>
        public int DaysRemaining => (int)(EndDate.Date - DateTime.Today).TotalDays;

        #endregion
    }
}
