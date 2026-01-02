using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.DataModels;

namespace Tracker.Services.Analytics
{
    /// <summary>
    /// Evaluates data sufficiency and calculates confidence levels for predictions.
    /// </summary>
    public class DataSufficiencyChecker
    {
        #region Result Classes

        /// <summary>
        /// Result of data sufficiency evaluation.
        /// </summary>
        public class SufficiencyResult
        {
            /// <summary>Overall confidence level for predictions.</summary>
            public ConfidenceLevel Confidence { get; init; }

            /// <summary>Numeric confidence score (0-100).</summary>
            public double ConfidenceScore { get; init; }

            /// <summary>Number of data points available.</summary>
            public int DataPointCount { get; init; }

            /// <summary>Number of days of data coverage.</summary>
            public int DaysCoverage { get; init; }

            /// <summary>Whether minimum requirements are met.</summary>
            public bool IsSufficient { get; init; }

            /// <summary>R-squared value from trend analysis (if available).</summary>
            public double? RSquared { get; init; }

            /// <summary>Data quality score (0-100) based on consistency and gaps.</summary>
            public double DataQualityScore { get; init; }

            /// <summary>Suggestions for improving data quality.</summary>
            public List<string> Suggestions { get; init; } = new();

            /// <summary>Human-readable summary.</summary>
            public string Summary { get; init; } = string.Empty;
        }

        /// <summary>
        /// Confidence levels for predictions.
        /// </summary>
        public enum ConfidenceLevel
        {
            /// <summary>High confidence - reliable predictions.</summary>
            High,
            /// <summary>Medium confidence - reasonable predictions.</summary>
            Medium,
            /// <summary>Low confidence - use with caution.</summary>
            Low,
            /// <summary>Very low confidence - not recommended.</summary>
            VeryLow,
            /// <summary>Insufficient data for any prediction.</summary>
            Insufficient
        }

        #endregion

        #region Configuration

        /// <summary>Minimum data points for any prediction.</summary>
        public int MinimumDataPoints { get; set; } = 3;

        /// <summary>Data points needed for high confidence.</summary>
        public int HighConfidenceDataPoints { get; set; } = 14;

        /// <summary>Data points needed for medium confidence.</summary>
        public int MediumConfidenceDataPoints { get; set; } = 7;

        /// <summary>Days of coverage needed for high confidence.</summary>
        public int HighConfidenceDays { get; set; } = 21;

        /// <summary>Days of coverage needed for medium confidence.</summary>
        public int MediumConfidenceDays { get; set; } = 10;

        /// <summary>R-squared threshold for high confidence.</summary>
        public double HighConfidenceRSquared { get; set; } = 0.8;

        /// <summary>R-squared threshold for medium confidence.</summary>
        public double MediumConfidenceRSquared { get; set; } = 0.5;

        /// <summary>Maximum allowed gap between data points (days).</summary>
        public int MaxAllowedGapDays { get; set; } = 7;

        #endregion

        #region Public Methods

        /// <summary>
        /// Evaluates data sufficiency for prediction.
        /// </summary>
        /// <param name="snapshots">Progress snapshots.</param>
        /// <param name="trendResult">Optional trend analysis result.</param>
        /// <returns>Sufficiency evaluation result.</returns>
        public SufficiencyResult Evaluate(
            IEnumerable<ProgressSnapshot> snapshots,
            TrendAnalyzer.TrendResult? trendResult = null)
        {
            var snapshotList = snapshots?.OrderBy(s => s.SnapshotDate).ToList() 
                               ?? new List<ProgressSnapshot>();

            int dataPointCount = snapshotList.Count;
            int daysCoverage = CalculateDaysCoverage(snapshotList);
            double dataQuality = CalculateDataQuality(snapshotList);
            double? rSquared = trendResult?.RSquared;

            // Calculate confidence score
            double confidenceScore = CalculateConfidenceScore(
                dataPointCount, daysCoverage, dataQuality, rSquared);

            // Determine confidence level
            var confidence = DetermineConfidenceLevel(
                dataPointCount, daysCoverage, rSquared, confidenceScore);

            // Generate suggestions
            var suggestions = GenerateSuggestions(
                dataPointCount, daysCoverage, dataQuality, rSquared);

            // Generate summary
            string summary = GenerateSummary(confidence, confidenceScore, dataPointCount, daysCoverage);

            return new SufficiencyResult
            {
                Confidence = confidence,
                ConfidenceScore = confidenceScore,
                DataPointCount = dataPointCount,
                DaysCoverage = daysCoverage,
                IsSufficient = dataPointCount >= MinimumDataPoints,
                RSquared = rSquared,
                DataQualityScore = dataQuality,
                Suggestions = suggestions,
                Summary = summary
            };
        }

        /// <summary>
        /// Quick check if minimum data requirements are met.
        /// </summary>
        public bool HasMinimumData(IEnumerable<ProgressSnapshot> snapshots)
        {
            return snapshots?.Count() >= MinimumDataPoints;
        }

        /// <summary>
        /// Gets a display string for the confidence level.
        /// </summary>
        public static string GetConfidenceDisplayText(ConfidenceLevel level)
        {
            return level switch
            {
                ConfidenceLevel.High => "High Confidence",
                ConfidenceLevel.Medium => "Medium Confidence",
                ConfidenceLevel.Low => "Low Confidence",
                ConfidenceLevel.VeryLow => "Very Low Confidence",
                ConfidenceLevel.Insufficient => "Insufficient Data",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets a color hint for the confidence level (for UI display).
        /// </summary>
        public static string GetConfidenceColorHint(ConfidenceLevel level)
        {
            return level switch
            {
                ConfidenceLevel.High => "Green",
                ConfidenceLevel.Medium => "Orange",
                ConfidenceLevel.Low => "Red",
                ConfidenceLevel.VeryLow => "DarkRed",
                ConfidenceLevel.Insufficient => "Gray",
                _ => "Gray"
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates the number of days covered by snapshots.
        /// </summary>
        private int CalculateDaysCoverage(List<ProgressSnapshot> snapshots)
        {
            if (snapshots.Count < 2)
                return snapshots.Count;

            var firstDate = snapshots.First().SnapshotDate;
            var lastDate = snapshots.Last().SnapshotDate;

            return (int)(lastDate - firstDate).TotalDays + 1;
        }

        /// <summary>
        /// Calculates data quality score based on consistency and gaps.
        /// </summary>
        private double CalculateDataQuality(List<ProgressSnapshot> snapshots)
        {
            if (snapshots.Count < 2)
                return snapshots.Count > 0 ? 25 : 0;

            double qualityScore = 100;

            // Check for gaps
            var gaps = CalculateGaps(snapshots);
            var largeGaps = gaps.Count(g => g > MaxAllowedGapDays);
            var avgGap = gaps.Average();

            // Penalize for large gaps
            qualityScore -= largeGaps * 10;

            // Penalize for inconsistent intervals
            var idealGap = 1.0; // Daily snapshots ideal
            var gapDeviation = Math.Abs(avgGap - idealGap);
            qualityScore -= Math.Min(20, gapDeviation * 5);

            // Check for value consistency (no wild swings)
            var progressValues = snapshots.Select(s => (double)s.Progress).ToList();
            var volatility = CalculateVolatility(progressValues);
            qualityScore -= Math.Min(20, volatility * 2);

            return Math.Max(0, Math.Min(100, qualityScore));
        }

        /// <summary>
        /// Calculates gaps between consecutive snapshots (in days).
        /// </summary>
        private List<int> CalculateGaps(List<ProgressSnapshot> snapshots)
        {
            var gaps = new List<int>();

            for (int i = 1; i < snapshots.Count; i++)
            {
                var gap = (int)(snapshots[i].SnapshotDate - snapshots[i - 1].SnapshotDate).TotalDays;
                gaps.Add(gap);
            }

            return gaps;
        }

        /// <summary>
        /// Calculates volatility (standard deviation of day-over-day changes).
        /// </summary>
        private double CalculateVolatility(List<double> values)
        {
            if (values.Count < 2)
                return 0;

            var changes = new List<double>();
            for (int i = 1; i < values.Count; i++)
            {
                changes.Add(Math.Abs(values[i] - values[i - 1]));
            }

            if (changes.Count == 0)
                return 0;

            double avg = changes.Average();
            double sumSquaredDiff = changes.Sum(c => Math.Pow(c - avg, 2));
            return Math.Sqrt(sumSquaredDiff / changes.Count);
        }

        /// <summary>
        /// Calculates overall confidence score (0-100).
        /// </summary>
        private double CalculateConfidenceScore(
            int dataPointCount,
            int daysCoverage,
            double dataQuality,
            double? rSquared)
        {
            // Weight factors
            const double dataPointWeight = 0.25;
            const double daysCoverageWeight = 0.25;
            const double dataQualityWeight = 0.25;
            const double rSquaredWeight = 0.25;

            // Normalize data points (0-100 scale)
            double dataPointScore = Math.Min(100, (dataPointCount / (double)HighConfidenceDataPoints) * 100);

            // Normalize days coverage (0-100 scale)
            double daysScore = Math.Min(100, (daysCoverage / (double)HighConfidenceDays) * 100);

            // R-squared is already 0-1, convert to 0-100
            double rSquaredScore = (rSquared ?? 0.5) * 100;

            // Calculate weighted score
            double score = (dataPointScore * dataPointWeight) +
                          (daysScore * daysCoverageWeight) +
                          (dataQuality * dataQualityWeight) +
                          (rSquaredScore * rSquaredWeight);

            return Math.Round(score, 1);
        }

        /// <summary>
        /// Determines confidence level from metrics.
        /// </summary>
        private ConfidenceLevel DetermineConfidenceLevel(
            int dataPointCount,
            int daysCoverage,
            double? rSquared,
            double confidenceScore)
        {
            if (dataPointCount < MinimumDataPoints)
                return ConfidenceLevel.Insufficient;

            // High confidence requires meeting multiple criteria
            if (dataPointCount >= HighConfidenceDataPoints &&
                daysCoverage >= HighConfidenceDays &&
                (rSquared ?? 0) >= HighConfidenceRSquared &&
                confidenceScore >= 75)
            {
                return ConfidenceLevel.High;
            }

            // Medium confidence
            if (dataPointCount >= MediumConfidenceDataPoints &&
                daysCoverage >= MediumConfidenceDays &&
                (rSquared ?? 0) >= MediumConfidenceRSquared &&
                confidenceScore >= 50)
            {
                return ConfidenceLevel.Medium;
            }

            // Low confidence
            if (confidenceScore >= 30)
            {
                return ConfidenceLevel.Low;
            }

            return ConfidenceLevel.VeryLow;
        }

        /// <summary>
        /// Generates suggestions for improving data quality.
        /// </summary>
        private List<string> GenerateSuggestions(
            int dataPointCount,
            int daysCoverage,
            double dataQuality,
            double? rSquared)
        {
            var suggestions = new List<string>();

            if (dataPointCount < HighConfidenceDataPoints)
            {
                int needed = HighConfidenceDataPoints - dataPointCount;
                suggestions.Add($"Continue tracking for {needed} more days to improve prediction accuracy");
            }

            if (daysCoverage < HighConfidenceDays && dataPointCount >= MinimumDataPoints)
            {
                suggestions.Add("More historical data will improve long-term trend analysis");
            }

            if (dataQuality < 70)
            {
                suggestions.Add("Try to update progress more consistently to improve data quality");
            }

            if (rSquared.HasValue && rSquared.Value < MediumConfidenceRSquared)
            {
                suggestions.Add("Progress has been irregular; predictions may be less reliable");
            }

            if (suggestions.Count == 0 && dataPointCount >= HighConfidenceDataPoints)
            {
                suggestions.Add("Data quality is good for reliable predictions");
            }

            return suggestions;
        }

        /// <summary>
        /// Generates human-readable summary.
        /// </summary>
        private string GenerateSummary(
            ConfidenceLevel confidence,
            double confidenceScore,
            int dataPointCount,
            int daysCoverage)
        {
            return confidence switch
            {
                ConfidenceLevel.High => 
                    $"High confidence ({confidenceScore:F0}%) based on {dataPointCount} data points over {daysCoverage} days",
                ConfidenceLevel.Medium => 
                    $"Medium confidence ({confidenceScore:F0}%) - predictions are reasonably reliable",
                ConfidenceLevel.Low => 
                    $"Low confidence ({confidenceScore:F0}%) - use predictions with caution",
                ConfidenceLevel.VeryLow => 
                    $"Very low confidence ({confidenceScore:F0}%) - more data needed for reliable predictions",
                ConfidenceLevel.Insufficient => 
                    $"Only {dataPointCount} data points available - need at least {MinimumDataPoints} for predictions",
                _ => "Unable to evaluate data sufficiency"
            };
        }

        #endregion
    }
}
