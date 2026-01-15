using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.DataModels;

namespace Tracker.Services.Analytics
{
    /// <summary>
    /// Analyzes progress data to determine trends using linear regression.
    /// </summary>
    public class TrendAnalyzer
    {
        #region Trend Direction Enum

        /// <summary>
        /// Represents the direction of a trend.
        /// </summary>
        public enum TrendDirection
        {
            /// <summary>Progress is increasing.</summary>
            Improving,
            /// <summary>Progress is relatively stable.</summary>
            Stable,
            /// <summary>Progress is decreasing.</summary>
            Declining,
            /// <summary>Not enough data to determine trend.</summary>
            Insufficient
        }

        #endregion

        #region Result Classes

        /// <summary>
        /// Result of trend analysis.
        /// </summary>
        public class TrendResult
        {
            /// <summary>The calculated trend direction.</summary>
            public TrendDirection Direction { get; init; }

            /// <summary>Slope of the linear regression line (progress units per day).</summary>
            public double Slope { get; init; }

            /// <summary>Y-intercept of the regression line.</summary>
            public double Intercept { get; init; }

            /// <summary>R-squared value indicating fit quality (0-1).</summary>
            public double RSquared { get; init; }

            /// <summary>Average daily change in progress.</summary>
            public double AverageDailyChange { get; init; }

            /// <summary>Number of data points used in analysis.</summary>
            public int DataPointCount { get; init; }

            /// <summary>Description of the trend for display.</summary>
            public string Description { get; init; } = string.Empty;

            /// <summary>
            /// Creates a result for insufficient data.
            /// </summary>
            public static TrendResult InsufficientData() => new()
            {
                Direction = TrendDirection.Insufficient,
                Slope = 0,
                Intercept = 0,
                RSquared = 0,
                AverageDailyChange = 0,
                DataPointCount = 0,
                Description = "Not enough data to analyze trend"
            };
        }

        #endregion

        #region Configuration

        /// <summary>Minimum data points required for trend analysis.</summary>
        public int MinimumDataPoints { get; set; } = 3;

        /// <summary>
        /// Slope threshold below which trend is considered stable.
        /// Default: 0.5% progress per day.
        /// </summary>
        public double StableThreshold { get; set; } = 0.5;

        #endregion

        #region Public Methods

        /// <summary>
        /// Analyzes snapshots to determine the trend.
        /// </summary>
        /// <param name="snapshots">Progress snapshots ordered by date.</param>
        /// <returns>Analysis result with trend direction and statistics.</returns>
        public TrendResult Analyze(IEnumerable<ProgressSnapshot> snapshots)
        {
            var dataPoints = snapshots?.ToList() ?? new List<ProgressSnapshot>();

            if (dataPoints.Count < MinimumDataPoints)
            {
                return TrendResult.InsufficientData();
            }

            // Convert dates to numeric values (days from first date)
            var firstDate = dataPoints.Min(s => s.SnapshotDate);
            var points = dataPoints
                .Select(s => (
                    X: (s.SnapshotDate - firstDate).TotalDays,
                    Y: (double)(s.OverallScore ?? 0)
                ))
                .OrderBy(p => p.X)
                .ToList();

            // Perform linear regression
            var (slope, intercept, rSquared) = CalculateLinearRegression(points);

            // Calculate average daily change
            var avgDailyChange = CalculateAverageDailyChange(points);

            // Determine trend direction
            var direction = DetermineTrendDirection(slope);

            // Generate description
            var description = GenerateDescription(direction, slope, avgDailyChange, dataPoints.Count);

            return new TrendResult
            {
                Direction = direction,
                Slope = slope,
                Intercept = intercept,
                RSquared = rSquared,
                AverageDailyChange = avgDailyChange,
                DataPointCount = dataPoints.Count,
                Description = description
            };
        }

        /// <summary>
        /// Analyzes raw progress values with dates.
        /// </summary>
        /// <param name="dataPoints">Tuples of (date, progress value).</param>
        /// <returns>Analysis result.</returns>
        public TrendResult Analyze(IEnumerable<(DateTime Date, double Progress)> dataPoints)
        {
            var points = dataPoints?.ToList() ?? new List<(DateTime, double)>();

            if (points.Count < MinimumDataPoints)
            {
                return TrendResult.InsufficientData();
            }

            var firstDate = points.Min(p => p.Date);
            var convertedPoints = points
                .Select(p => (
                    X: (p.Date - firstDate).TotalDays,
                    Y: p.Progress
                ))
                .OrderBy(p => p.X)
                .ToList();

            var (slope, intercept, rSquared) = CalculateLinearRegression(convertedPoints);
            var avgDailyChange = CalculateAverageDailyChange(convertedPoints);
            var direction = DetermineTrendDirection(slope);
            var description = GenerateDescription(direction, slope, avgDailyChange, points.Count);

            return new TrendResult
            {
                Direction = direction,
                Slope = slope,
                Intercept = intercept,
                RSquared = rSquared,
                AverageDailyChange = avgDailyChange,
                DataPointCount = points.Count,
                Description = description
            };
        }

        /// <summary>
        /// Projects the value at a future date based on trend.
        /// </summary>
        /// <param name="result">The trend analysis result.</param>
        /// <param name="startDate">The starting date for projection.</param>
        /// <param name="targetDate">The date to project to.</param>
        /// <returns>Projected progress value.</returns>
        public double ProjectValue(TrendResult result, DateTime startDate, DateTime targetDate)
        {
            if (result.Direction == TrendDirection.Insufficient)
                return 0;

            var daysFromStart = (targetDate - startDate).TotalDays;
            return result.Intercept + (result.Slope * daysFromStart);
        }

        /// <summary>
        /// Calculates when a target value will be reached based on current trend.
        /// </summary>
        /// <param name="result">The trend analysis result.</param>
        /// <param name="currentDate">Current date.</param>
        /// <param name="currentValue">Current progress value.</param>
        /// <param name="targetValue">Target value to reach (default 100).</param>
        /// <returns>Projected date, or null if unreachable with current trend.</returns>
        public DateTime? ProjectCompletionDate(
            TrendResult result, 
            DateTime currentDate, 
            double currentValue, 
            double targetValue = 100)
        {
            if (result.Direction == TrendDirection.Insufficient)
                return null;

            // If already at or past target
            if (currentValue >= targetValue)
                return currentDate;

            // If slope is zero or negative, can't reach target
            if (result.Slope <= 0)
                return null;

            // Calculate days needed: (target - current) / slope
            var daysNeeded = (targetValue - currentValue) / result.Slope;

            // Sanity check - cap at 5 years
            if (daysNeeded > 365 * 5)
                return null;

            return currentDate.AddDays(daysNeeded);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Performs ordinary least squares linear regression.
        /// </summary>
        private (double Slope, double Intercept, double RSquared) CalculateLinearRegression(
            List<(double X, double Y)> points)
        {
            int n = points.Count;
            if (n == 0) return (0, 0, 0);

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

            foreach (var (x, y) in points)
            {
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
                sumY2 += y * y;
            }

            double meanX = sumX / n;
            double meanY = sumY / n;

            // Calculate slope: Σ((xi - x̄)(yi - ȳ)) / Σ((xi - x̄)²)
            double numerator = sumXY - (n * meanX * meanY);
            double denominator = sumX2 - (n * meanX * meanX);

            double slope = denominator != 0 ? numerator / denominator : 0;
            double intercept = meanY - (slope * meanX);

            // Calculate R-squared
            double ssTotal = sumY2 - (n * meanY * meanY);
            double ssResidual = 0;

            foreach (var (x, y) in points)
            {
                double predicted = intercept + (slope * x);
                ssResidual += Math.Pow(y - predicted, 2);
            }

            double rSquared = ssTotal != 0 ? 1 - (ssResidual / ssTotal) : 0;
            rSquared = Math.Max(0, Math.Min(1, rSquared)); // Clamp to [0, 1]

            return (slope, intercept, rSquared);
        }

        /// <summary>
        /// Calculates the average daily change in progress.
        /// </summary>
        private double CalculateAverageDailyChange(List<(double X, double Y)> points)
        {
            if (points.Count < 2) return 0;

            var ordered = points.OrderBy(p => p.X).ToList();
            double totalDays = ordered.Last().X - ordered.First().X;
            double totalChange = ordered.Last().Y - ordered.First().Y;

            return totalDays > 0 ? totalChange / totalDays : 0;
        }

        /// <summary>
        /// Determines trend direction from slope.
        /// </summary>
        private TrendDirection DetermineTrendDirection(double slope)
        {
            if (Math.Abs(slope) <= StableThreshold)
                return TrendDirection.Stable;

            return slope > 0 ? TrendDirection.Improving : TrendDirection.Declining;
        }

        /// <summary>
        /// Generates a human-readable trend description.
        /// </summary>
        private string GenerateDescription(
            TrendDirection direction, 
            double slope, 
            double avgDailyChange, 
            int dataPointCount)
        {
            var slopeDesc = Math.Abs(slope).ToString("F1");
            var timeframe = dataPointCount > 7 ? $"{dataPointCount} days" : $"{dataPointCount} data points";

            return direction switch
            {
                TrendDirection.Improving => $"Improving at {slopeDesc}% per day over {timeframe}",
                TrendDirection.Declining => $"Declining at {slopeDesc}% per day over {timeframe}",
                TrendDirection.Stable => $"Stable progress over {timeframe}",
                _ => "Insufficient data for trend analysis"
            };
        }

        #endregion
    }
}
