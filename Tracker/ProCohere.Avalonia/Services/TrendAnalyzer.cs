using System;
using System.Collections.Generic;
using System.Linq;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Analyzes metric history to determine trends using linear regression.
/// Ported from Tracker with adaptations for ProCohere's MetricHistoryEntry and MetricTrend enum.
/// </summary>
public class TrendAnalyzer
{
    #region Configuration

    /// <summary>Minimum data points required for trend analysis.</summary>
    public int MinimumDataPoints { get; set; } = 3;

    /// <summary>
    /// Slope threshold below which trend is considered stable.
    /// Default: 0.5 units per day.
    /// </summary>
    public double StableThreshold { get; set; } = 0.5;

    #endregion

    /// <summary>
    /// Analyzes metric history to determine the trend.
    /// </summary>
    /// <param name="history">Metric history entries ordered by date.</param>
    /// <returns>Analysis result with trend direction and statistics.</returns>
    public TrendResult Analyze(IEnumerable<MetricHistoryEntry> history)
    {
        var dataPoints = history?.Where(h => !h.IsDeleted).ToList() ?? new List<MetricHistoryEntry>();

        if (dataPoints.Count < MinimumDataPoints)
        {
            return TrendResult.InsufficientData();
        }

        // Convert dates to numeric values (days from first date)
        var firstDate = dataPoints.Min(h => h.RecordedAt);
        var points = dataPoints
            .Select(h => (
                X: (h.RecordedAt - firstDate).TotalDays,
                Y: (double)h.Value
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
            Description = description,
            FirstValue = points.First().Y,
            LatestValue = points.Last().Y,
            FirstDate = firstDate,
            LatestDate = dataPoints.Max(h => h.RecordedAt)
        };
    }

    /// <summary>
    /// Projects the value at a future date based on trend.
    /// </summary>
    public double ProjectValue(TrendResult result, DateTime targetDate)
    {
        if (result.Direction == MetricTrend.Unknown)
            return result.LatestValue;

        var daysFromStart = (targetDate - result.FirstDate).TotalDays;
        return result.Intercept + (result.Slope * daysFromStart);
    }

    /// <summary>
    /// Calculates when a target value will be reached based on current trend.
    /// </summary>
    public DateTime? ProjectTargetDate(TrendResult result, double targetValue)
    {
        if (result.Direction == MetricTrend.Unknown || Math.Abs(result.Slope) < 0.001)
            return null;

        // Solve: targetValue = Intercept + Slope * days
        var daysFromStart = (targetValue - result.Intercept) / result.Slope;

        if (daysFromStart < 0)
            return null; // Target in the past or unreachable

        return result.FirstDate.AddDays(daysFromStart);
    }

    #region Private Methods

    /// <summary>
    /// Performs ordinary least squares linear regression.
    /// Returns (slope, intercept, R-squared).
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
    /// Calculates the average daily change in metric value.
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
    /// Determines trend direction from slope and maps to MetricTrend enum.
    /// </summary>
    private MetricTrend DetermineTrendDirection(double slope)
    {
        if (Math.Abs(slope) <= StableThreshold)
            return MetricTrend.Stable;

        return slope > 0 ? MetricTrend.TrendingUp : MetricTrend.TrendingDown;
    }

    /// <summary>
    /// Generates a human-readable trend description.
    /// </summary>
    private string GenerateDescription(
        MetricTrend direction,
        double slope,
        double avgDailyChange,
        int dataPointCount)
    {
        var slopeDesc = Math.Abs(slope).ToString("F2");
        var timeframe = dataPointCount > 7 ? $"{dataPointCount} days" : $"{dataPointCount} data points";

        return direction switch
        {
            MetricTrend.TrendingUp => $"Trending up at {slopeDesc} per day over {timeframe}",
            MetricTrend.TrendingDown => $"Trending down at {slopeDesc} per day over {timeframe}",
            MetricTrend.Stable => $"Stable over {timeframe}",
            _ => "Insufficient data for trend analysis"
        };
    }

    #endregion
}

/// <summary>
/// Result of trend analysis for a metric.
/// </summary>
public class TrendResult
{
    /// <summary>The calculated trend direction.</summary>
    public MetricTrend Direction { get; init; }

    /// <summary>Slope of the linear regression line (units per day).</summary>
    public double Slope { get; init; }

    /// <summary>Y-intercept of the regression line.</summary>
    public double Intercept { get; init; }

    /// <summary>R-squared value indicating fit quality (0-1). Higher = more confident.</summary>
    public double RSquared { get; init; }

    /// <summary>Average daily change in metric value.</summary>
    public double AverageDailyChange { get; init; }

    /// <summary>Number of data points used in analysis.</summary>
    public int DataPointCount { get; init; }

    /// <summary>Human-readable description of the trend.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>First value in the dataset.</summary>
    public double FirstValue { get; init; }

    /// <summary>Latest value in the dataset.</summary>
    public double LatestValue { get; init; }

    /// <summary>Date of first data point.</summary>
    public DateTime FirstDate { get; init; }

    /// <summary>Date of latest data point.</summary>
    public DateTime LatestDate { get; init; }

    /// <summary>Confidence level based on R-squared.</summary>
    public string ConfidenceLevel
    {
        get
        {
            if (Direction == MetricTrend.Unknown) return "None";
            if (RSquared >= 0.8) return "High";
            if (RSquared >= 0.5) return "Medium";
            return "Low";
        }
    }

    /// <summary>Arrow icon for the trend.</summary>
    public string TrendArrow => Direction.GetArrow();

    /// <summary>Color for the trend.</summary>
    public string TrendColor => Direction.GetColor();

    /// <summary>Creates a result for insufficient data.</summary>
    public static TrendResult InsufficientData() => new()
    {
        Direction = MetricTrend.Unknown,
        Slope = 0,
        Intercept = 0,
        RSquared = 0,
        AverageDailyChange = 0,
        DataPointCount = 0,
        Description = "Not enough data to analyze trend",
        FirstValue = 0,
        LatestValue = 0,
        FirstDate = DateTime.MinValue,
        LatestDate = DateTime.MinValue
    };
}
