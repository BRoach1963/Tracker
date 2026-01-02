using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.DataModels;

namespace Tracker.Services.Analytics
{
    /// <summary>
    /// Predicts trajectory and completion dates based on historical progress data.
    /// </summary>
    public class TrajectoryPredictor
    {
        #region Result Classes

        /// <summary>
        /// Result of trajectory prediction.
        /// </summary>
        public class TrajectoryResult
        {
            /// <summary>Whether the prediction is valid (has enough data).</summary>
            public bool IsValid { get; init; }

            /// <summary>Predicted completion date based on current trajectory.</summary>
            public DateTime? PredictedCompletionDate { get; init; }

            /// <summary>Original target/due date for comparison.</summary>
            public DateTime? TargetDate { get; init; }

            /// <summary>Days ahead (+) or behind (-) schedule.</summary>
            public int? DaysFromTarget { get; init; }

            /// <summary>Current progress percentage (0-100).</summary>
            public double CurrentProgress { get; init; }

            /// <summary>Expected progress at this point if on linear track to target.</summary>
            public double ExpectedProgress { get; init; }

            /// <summary>Progress gap (current - expected). Positive = ahead, negative = behind.</summary>
            public double ProgressGap { get; init; }

            /// <summary>Whether currently on track to meet target date.</summary>
            public bool IsOnTrack { get; init; }

            /// <summary>Risk level based on trajectory.</summary>
            public RiskLevel Risk { get; init; }

            /// <summary>The trend analysis used for prediction.</summary>
            public TrendAnalyzer.TrendResult? TrendAnalysis { get; init; }

            /// <summary>Human-readable status summary.</summary>
            public string Summary { get; init; } = string.Empty;

            /// <summary>
            /// Creates an invalid result due to insufficient data.
            /// </summary>
            public static TrajectoryResult InsufficientData() => new()
            {
                IsValid = false,
                Risk = RiskLevel.Unknown,
                Summary = "Not enough data to predict trajectory"
            };
        }

        /// <summary>
        /// Risk level for trajectory prediction.
        /// </summary>
        public enum RiskLevel
        {
            /// <summary>Well ahead of schedule.</summary>
            OnTrack,
            /// <summary>Slightly behind but recoverable.</summary>
            AtRisk,
            /// <summary>Significantly behind, likely to miss target.</summary>
            Critical,
            /// <summary>Cannot determine risk level.</summary>
            Unknown
        }

        /// <summary>
        /// A single point on a trajectory projection.
        /// </summary>
        public class TrajectoryPoint
        {
            public DateTime Date { get; init; }
            public double ProjectedProgress { get; init; }
            public double ExpectedProgress { get; init; }
            public bool IsHistorical { get; init; }
        }

        #endregion

        #region Dependencies

        private readonly TrendAnalyzer _trendAnalyzer;

        #endregion

        #region Configuration

        /// <summary>Threshold for "at risk" (days behind).</summary>
        public int AtRiskThresholdDays { get; set; } = 7;

        /// <summary>Threshold for "critical" (days behind).</summary>
        public int CriticalThresholdDays { get; set; } = 14;

        /// <summary>Progress gap threshold for "at risk" (percentage points).</summary>
        public double AtRiskProgressGap { get; set; } = -10;

        /// <summary>Progress gap threshold for "critical" (percentage points).</summary>
        public double CriticalProgressGap { get; set; } = -25;

        #endregion

        #region Constructor

        public TrajectoryPredictor(TrendAnalyzer? trendAnalyzer = null)
        {
            _trendAnalyzer = trendAnalyzer ?? new TrendAnalyzer();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Predicts trajectory based on historical snapshots.
        /// </summary>
        /// <param name="snapshots">Historical progress snapshots.</param>
        /// <param name="startDate">Start date of the tracked item.</param>
        /// <param name="targetDate">Target completion date.</param>
        /// <param name="targetValue">Target progress value (default 100).</param>
        /// <returns>Trajectory prediction result.</returns>
        public TrajectoryResult Predict(
            IEnumerable<ProgressSnapshot> snapshots,
            DateTime startDate,
            DateTime? targetDate,
            double targetValue = 100)
        {
            var snapshotList = snapshots?.ToList() ?? new List<ProgressSnapshot>();

            if (snapshotList.Count < 2)
            {
                return TrajectoryResult.InsufficientData();
            }

            // Analyze trend
            var trend = _trendAnalyzer.Analyze(snapshotList);

            if (trend.Direction == TrendAnalyzer.TrendDirection.Insufficient)
            {
                return TrajectoryResult.InsufficientData();
            }

            // Get current state
            var latestSnapshot = snapshotList.OrderByDescending(s => s.SnapshotDate).First();
            var currentProgress = (double)latestSnapshot.Progress;
            var today = DateTime.Today;

            // Calculate expected progress (linear from start to target)
            double expectedProgress = CalculateExpectedProgress(startDate, targetDate, today, targetValue);
            double progressGap = currentProgress - expectedProgress;

            // Predict completion date
            DateTime? predictedCompletion = _trendAnalyzer.ProjectCompletionDate(
                trend, today, currentProgress, targetValue);

            // Calculate days from target
            int? daysFromTarget = null;
            if (predictedCompletion.HasValue && targetDate.HasValue)
            {
                daysFromTarget = (targetDate.Value - predictedCompletion.Value).Days;
            }

            // Determine if on track
            bool isOnTrack = DetermineOnTrack(progressGap, daysFromTarget);

            // Assess risk
            var risk = AssessRisk(progressGap, daysFromTarget, targetDate, predictedCompletion);

            // Generate summary
            string summary = GenerateSummary(
                currentProgress, expectedProgress, progressGap,
                predictedCompletion, targetDate, daysFromTarget, risk);

            return new TrajectoryResult
            {
                IsValid = true,
                PredictedCompletionDate = predictedCompletion,
                TargetDate = targetDate,
                DaysFromTarget = daysFromTarget,
                CurrentProgress = currentProgress,
                ExpectedProgress = expectedProgress,
                ProgressGap = progressGap,
                IsOnTrack = isOnTrack,
                Risk = risk,
                TrendAnalysis = trend,
                Summary = summary
            };
        }

        /// <summary>
        /// Generates trajectory points for charting.
        /// </summary>
        /// <param name="snapshots">Historical snapshots.</param>
        /// <param name="startDate">Start date.</param>
        /// <param name="targetDate">Target date.</param>
        /// <param name="trend">Trend analysis result.</param>
        /// <param name="projectionDays">Days to project into future.</param>
        /// <returns>List of trajectory points.</returns>
        public List<TrajectoryPoint> GenerateTrajectoryPoints(
            IEnumerable<ProgressSnapshot> snapshots,
            DateTime startDate,
            DateTime? targetDate,
            TrendAnalyzer.TrendResult trend,
            int projectionDays = 30)
        {
            var points = new List<TrajectoryPoint>();
            var snapshotList = snapshots.OrderBy(s => s.SnapshotDate).ToList();

            if (snapshotList.Count == 0)
                return points;

            var today = DateTime.Today;
            var endDate = targetDate ?? today.AddDays(projectionDays);

            // Historical points
            foreach (var snapshot in snapshotList)
            {
                double expected = CalculateExpectedProgress(startDate, targetDate, snapshot.SnapshotDate, 100);
                points.Add(new TrajectoryPoint
                {
                    Date = snapshot.SnapshotDate,
                    ProjectedProgress = (double)snapshot.Progress,
                    ExpectedProgress = expected,
                    IsHistorical = true
                });
            }

            // Future projections (if we have trend data)
            if (trend.Direction != TrendAnalyzer.TrendDirection.Insufficient)
            {
                var lastSnapshot = snapshotList.Last();
                var projectionStart = lastSnapshot.SnapshotDate.AddDays(1);
                var projectionEnd = endDate > today ? endDate : today.AddDays(projectionDays);

                for (var date = projectionStart; date <= projectionEnd; date = date.AddDays(1))
                {
                    var daysFromLast = (date - lastSnapshot.SnapshotDate).TotalDays;
                    var projectedValue = (double)lastSnapshot.Progress + (trend.Slope * daysFromLast);
                    projectedValue = Math.Min(100, Math.Max(0, projectedValue)); // Clamp 0-100

                    double expected = CalculateExpectedProgress(startDate, targetDate, date, 100);

                    points.Add(new TrajectoryPoint
                    {
                        Date = date,
                        ProjectedProgress = projectedValue,
                        ExpectedProgress = expected,
                        IsHistorical = false
                    });

                    // Stop if we've reached 100%
                    if (projectedValue >= 100)
                        break;
                }
            }

            return points;
        }

        /// <summary>
        /// Calculates required daily progress to meet target.
        /// </summary>
        /// <param name="currentProgress">Current progress percentage.</param>
        /// <param name="targetDate">Target completion date.</param>
        /// <param name="targetValue">Target value (default 100).</param>
        /// <returns>Required daily progress, or null if target date is past.</returns>
        public double? CalculateRequiredDailyProgress(
            double currentProgress,
            DateTime targetDate,
            double targetValue = 100)
        {
            var daysRemaining = (targetDate - DateTime.Today).TotalDays;

            if (daysRemaining <= 0)
                return null;

            var remainingProgress = targetValue - currentProgress;
            if (remainingProgress <= 0)
                return 0; // Already complete

            return remainingProgress / daysRemaining;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates expected progress at a given date assuming linear progress.
        /// </summary>
        private double CalculateExpectedProgress(
            DateTime startDate,
            DateTime? targetDate,
            DateTime currentDate,
            double targetValue)
        {
            if (!targetDate.HasValue)
                return 0; // Can't calculate without target

            var totalDuration = (targetDate.Value - startDate).TotalDays;
            if (totalDuration <= 0)
                return targetValue;

            var elapsed = (currentDate - startDate).TotalDays;
            var progress = (elapsed / totalDuration) * targetValue;

            return Math.Min(targetValue, Math.Max(0, progress));
        }

        /// <summary>
        /// Determines if progress is on track.
        /// </summary>
        private bool DetermineOnTrack(double progressGap, int? daysFromTarget)
        {
            // On track if progress gap is not significantly negative
            // and predicted completion is before or close to target
            if (progressGap >= AtRiskProgressGap)
            {
                if (!daysFromTarget.HasValue || daysFromTarget.Value >= -AtRiskThresholdDays)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Assesses risk level based on trajectory.
        /// </summary>
        private RiskLevel AssessRisk(
            double progressGap,
            int? daysFromTarget,
            DateTime? targetDate,
            DateTime? predictedCompletion)
        {
            // If no target date, can't assess risk
            if (!targetDate.HasValue)
                return RiskLevel.Unknown;

            // Critical conditions
            if (progressGap <= CriticalProgressGap)
                return RiskLevel.Critical;

            if (daysFromTarget.HasValue && daysFromTarget.Value <= -CriticalThresholdDays)
                return RiskLevel.Critical;

            if (!predictedCompletion.HasValue && progressGap < 0)
                return RiskLevel.Critical; // Can't reach target with current trend

            // At risk conditions
            if (progressGap <= AtRiskProgressGap)
                return RiskLevel.AtRisk;

            if (daysFromTarget.HasValue && daysFromTarget.Value <= -AtRiskThresholdDays)
                return RiskLevel.AtRisk;

            return RiskLevel.OnTrack;
        }

        /// <summary>
        /// Generates a human-readable summary.
        /// </summary>
        private string GenerateSummary(
            double currentProgress,
            double expectedProgress,
            double progressGap,
            DateTime? predictedCompletion,
            DateTime? targetDate,
            int? daysFromTarget,
            RiskLevel risk)
        {
            var parts = new List<string>();

            // Progress status
            if (Math.Abs(progressGap) < 5)
            {
                parts.Add($"On schedule at {currentProgress:F0}%");
            }
            else if (progressGap > 0)
            {
                parts.Add($"Ahead by {progressGap:F0} points ({currentProgress:F0}% vs {expectedProgress:F0}% expected)");
            }
            else
            {
                parts.Add($"Behind by {Math.Abs(progressGap):F0} points ({currentProgress:F0}% vs {expectedProgress:F0}% expected)");
            }

            // Completion prediction
            if (predictedCompletion.HasValue)
            {
                if (targetDate.HasValue)
                {
                    if (daysFromTarget > 0)
                    {
                        parts.Add($"Expected to complete {daysFromTarget} days early on {predictedCompletion.Value:MMM d}");
                    }
                    else if (daysFromTarget < 0)
                    {
                        parts.Add($"Expected to complete {Math.Abs(daysFromTarget.Value)} days late on {predictedCompletion.Value:MMM d}");
                    }
                    else
                    {
                        parts.Add($"On track to complete on target date {predictedCompletion.Value:MMM d}");
                    }
                }
                else
                {
                    parts.Add($"Expected completion: {predictedCompletion.Value:MMM d, yyyy}");
                }
            }
            else if (risk == RiskLevel.Critical)
            {
                parts.Add("Current trajectory will not reach target");
            }

            return string.Join(". ", parts);
        }

        #endregion
    }
}
