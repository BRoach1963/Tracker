using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Services
{
    /// <summary>
    /// Service for calculating OKR and Key Result progress, and determining status.
    /// This is the primary service for OKR progress management.
    /// </summary>
    public interface IOkrProgressService
    {
        /// <summary>
        /// Calculates the progress percentage for a Key Result.
        /// Progress = (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
        /// </summary>
        /// <param name="keyResultId">The Key Result ID.</param>
        /// <returns>Progress percentage (0-100+, can exceed 100 if target exceeded).</returns>
        Task<decimal> CalculateKeyResultProgressAsync(int keyResultId);

        /// <summary>
        /// Calculates the overall progress for an OKR based on its Key Results.
        /// Uses weighted average if weights are specified, simple average otherwise.
        /// </summary>
        /// <param name="okrId">The OKR ID.</param>
        /// <returns>Overall progress percentage.</returns>
        Task<decimal> CalculateOkrProgressAsync(int okrId);

        /// <summary>
        /// Determines the status of an OKR based on Key Result progress.
        /// - OnTrack: All KRs on target or progress >= 70%
        /// - AtRisk: Any KR close to target or progress 40-69%
        /// - OffTrack: Any KR off target or progress < 40%
        /// </summary>
        /// <param name="okrId">The OKR ID.</param>
        /// <returns>The calculated status.</returns>
        Task<ObjectiveStatusEnum> DetermineOkrStatusAsync(int okrId);

        /// <summary>
        /// Refreshes progress calculations for all OKRs and their Key Results.
        /// Updates Key Result CurrentValues from linked Measurables.
        /// </summary>
        /// <returns>Number of OKRs with updated progress.</returns>
        Task<int> RefreshAllOkrProgressAsync();

        /// <summary>
        /// Refreshes progress for a single OKR and all its Key Results.
        /// </summary>
        /// <param name="okrId">The OKR ID to refresh.</param>
        /// <returns>True if any values changed, false otherwise.</returns>
        Task<bool> RefreshOkrProgressAsync(int okrId);

        /// <summary>
        /// Updates a Key Result's CurrentValue from its linked Measurables.
        /// Only updates if the Key Result has linked Measurables.
        /// </summary>
        /// <param name="keyResultId">The Key Result ID.</param>
        /// <returns>True if the value changed, false otherwise.</returns>
        Task<bool> RefreshKeyResultValueAsync(int keyResultId);

        /// <summary>
        /// Gets a summary of OKR progress including all Key Results with their progress.
        /// </summary>
        /// <param name="okrId">The OKR ID.</param>
        /// <returns>Progress summary with OKR and KR details.</returns>
        Task<OkrProgressSummary> GetOkrProgressSummaryAsync(int okrId);

        /// <summary>
        /// Gets all OKRs for a time period with their current progress.
        /// </summary>
        /// <param name="timePeriod">The time period filter.</param>
        /// <param name="year">The year filter.</param>
        /// <returns>List of OKRs with progress summaries.</returns>
        Task<List<OkrProgressSummary>> GetOkrsWithProgressAsync(TimePeriodEnum? timePeriod = null, int? year = null);
    }

    /// <summary>
    /// Summary of OKR progress including all Key Results.
    /// </summary>
    public class OkrProgressSummary
    {
        /// <summary>The OKR entity.</summary>
        public ObjectiveKeyResult Okr { get; set; } = null!;

        /// <summary>Calculated overall progress percentage.</summary>
        public decimal Progress { get; set; }

        /// <summary>Calculated or overridden status.</summary>
        public ObjectiveStatusEnum Status { get; set; }

        /// <summary>Progress details for each Key Result.</summary>
        public List<KeyResultProgressSummary> KeyResultSummaries { get; set; } = new();

        /// <summary>Total count of linked KPIs across all Key Results.</summary>
        public int LinkedKpiCount { get; set; }

        /// <summary>Total count of linked Projects across all Key Results.</summary>
        public int LinkedProjectCount { get; set; }

        /// <summary>Total count of linked Task Collections across all Key Results.</summary>
        public int LinkedTaskCollectionCount { get; set; }
    }

    /// <summary>
    /// Summary of Key Result progress.
    /// </summary>
    public class KeyResultProgressSummary
    {
        /// <summary>The Key Result entity.</summary>
        public KeyResult KeyResult { get; set; } = null!;

        /// <summary>Calculated progress percentage.</summary>
        public decimal Progress { get; set; }

        /// <summary>Calculated status based on progress.</summary>
        public KpiStatusEnum Status { get; set; }

        /// <summary>Count of linked Measurables.</summary>
        public int MeasurableCount { get; set; }

        /// <summary>Whether the CurrentValue was auto-calculated from Measurables.</summary>
        public bool IsAutoCalculated { get; set; }
    }
}

