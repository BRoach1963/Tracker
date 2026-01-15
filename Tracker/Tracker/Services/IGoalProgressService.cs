using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Services
{
    /// <summary>
    /// Service for calculating Goal and Target progress, and determining status.
    /// Goals represent organizational objectives, and Targets represent measurable key results.
    /// This is the primary service for Goal progress management.
    /// </summary>
    public interface IGoalProgressService
    {
        /// <summary>
        /// Calculates the progress percentage for a Target.
        /// Progress = (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
        /// </summary>
        /// <param name="targetId">The Target ID.</param>
        /// <returns>Progress percentage (0-100+, can exceed 100 if target exceeded).</returns>
        Task<decimal> CalculateTargetProgressAsync(Guid targetId);

        /// <summary>
        /// Calculates the overall progress for a Goal based on its Targets.
        /// Uses weighted average if weights are specified, simple average otherwise.
        /// </summary>
        /// <param name="goalId">The Goal ID.</param>
        /// <returns>Overall progress percentage.</returns>
        Task<decimal> CalculateGoalProgressAsync(Guid goalId);

        /// <summary>
        /// Determines the status of a Goal based on Target progress.
        /// - OnTrack: All Targets on target or progress >= 70%
        /// - AtRisk: Any Target close to target or progress 40-69%
        /// - OffTrack: Any Target off target or progress < 40%
        /// </summary>
        /// <param name="goalId">The Goal ID.</param>
        /// <returns>The calculated status.</returns>
        Task<GoalStatus> DetermineGoalStatusAsync(Guid goalId);

        /// <summary>
        /// Refreshes progress calculations for all Goals and their Targets.
        /// Updates Target CurrentValues from linked Measurables.
        /// </summary>
        /// <returns>Number of Goals with updated progress.</returns>
        Task<int> RefreshAllGoalProgressAsync();

        /// <summary>
        /// Refreshes progress for a single Goal and all its Targets.
        /// </summary>
        /// <param name="goalId">The Goal ID to refresh.</param>
        /// <returns>True if any values changed, false otherwise.</returns>
        Task<bool> RefreshGoalProgressAsync(Guid goalId);

        /// <summary>
        /// Updates a Target's CurrentValue from its linked Measurables.
        /// Only updates if the Target has linked Measurables.
        /// </summary>
        /// <param name="targetId">The Target ID.</param>
        /// <returns>True if the value changed, false otherwise.</returns>
        Task<bool> RefreshTargetValueAsync(Guid targetId);

        /// <summary>
        /// Gets a summary of Goal progress including all Targets with their progress.
        /// </summary>
        /// <param name="goalId">The Goal ID.</param>
        /// <returns>Progress summary with Goal and Target details.</returns>
        Task<GoalProgressSummary> GetGoalProgressSummaryAsync(Guid goalId);

        /// <summary>
        /// Gets all Goals for a time period with their current progress.
        /// </summary>
        /// <param name="timePeriod">The time period filter.</param>
        /// <param name="year">The year filter.</param>
        /// <returns>List of Goals with progress summaries.</returns>
        Task<List<GoalProgressSummary>> GetGoalsWithProgressAsync(TimePeriodEnum? timePeriod = null, int? year = null);
    }

    /// <summary>
    /// Summary of Goal progress including all Targets.
    /// </summary>
    public class GoalProgressSummary
    {
        /// <summary>The Goal entity.</summary>
        public Goal Goal { get; set; } = null!;

        /// <summary>Calculated overall progress percentage.</summary>
        public decimal Progress { get; set; }

        /// <summary>Calculated or overridden status.</summary>
        public GoalStatus Status { get; set; }

        /// <summary>Progress details for each Target.</summary>
        public List<TargetProgressSummary> TargetSummaries { get; set; } = new();

        /// <summary>Total count of linked Metrics across all Targets.</summary>
        public int LinkedMetricCount { get; set; }

        /// <summary>Total count of linked Projects across all Targets.</summary>
        public int LinkedProjectCount { get; set; }

        /// <summary>Total count of linked Task Collections across all Targets.</summary>
        public int LinkedTaskCollectionCount { get; set; }
    }

    /// <summary>
    /// Summary of Target progress.
    /// </summary>
    public class TargetProgressSummary
    {
        /// <summary>The Target entity.</summary>
        public Target Target { get; set; } = null!;

        /// <summary>Calculated progress percentage.</summary>
        public decimal Progress { get; set; }

        /// <summary>Calculated status based on progress.</summary>
        public GoalStatus Status { get; set; }

        /// <summary>Count of linked Measurables.</summary>
        public int MeasurableCount { get; set; }

        /// <summary>Whether the CurrentValue was auto-calculated from Measurables.</summary>
        public bool IsAutoCalculated { get; set; }
    }
}

