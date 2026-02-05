namespace ProCohere.Avalonia.Models;

/// <summary>
/// Directional indicator for metric trends.
/// Philosophy: "Metrics are signals that tell a story, NOT targets to chase."
/// We show DIRECTION only (↗ → ↘), not numeric values by default.
/// </summary>
public enum MetricTrend
{
    /// <summary>
    /// Trend is improving (context-dependent: could be increasing or decreasing).
    /// Shown as ↗ (upward arrow).
    /// </summary>
    TrendingUp,

    /// <summary>
    /// Trend is steady, no significant change.
    /// Shown as → (horizontal arrow).
    /// </summary>
    Stable,

    /// <summary>
    /// Trend is declining (context-dependent: could be increasing or decreasing).
    /// Shown as ↘ (downward arrow).
    /// </summary>
    TrendingDown,

    /// <summary>
    /// Trend shows more variability than usual.
    /// Shown as ↕ (up-down arrow).
    /// </summary>
    MoreVariable,

    /// <summary>
    /// Not enough data to determine trend.
    /// Shown as ? or no arrow.
    /// </summary>
    Unknown
}

/// <summary>
/// Extension methods for MetricTrend.
/// </summary>
public static class MetricTrendExtensions
{
    /// <summary>
    /// Gets the arrow icon for a trend direction.
    /// Philosophy: Visual, not numeric - tells the story at a glance.
    /// </summary>
    public static string GetArrow(this MetricTrend trend) => trend switch
    {
        MetricTrend.TrendingUp => "↗",
        MetricTrend.Stable => "→",
        MetricTrend.TrendingDown => "↘",
        MetricTrend.MoreVariable => "↕",
        MetricTrend.Unknown => "?",
        _ => "?"
    };

    /// <summary>
    /// Gets the display name for a trend.
    /// </summary>
    public static string ToDisplayName(this MetricTrend trend) => trend switch
    {
        MetricTrend.TrendingUp => "Improving",
        MetricTrend.Stable => "Steady",
        MetricTrend.TrendingDown => "Declining",
        MetricTrend.MoreVariable => "Variable",
        MetricTrend.Unknown => "Insufficient Data",
        _ => trend.ToString()
    };

    /// <summary>
    /// Gets a description of what this trend means.
    /// </summary>
    public static string GetDescription(this MetricTrend trend) => trend switch
    {
        MetricTrend.TrendingUp => "This metric is showing improvement over the recent period",
        MetricTrend.Stable => "This metric has been steady with no significant change",
        MetricTrend.TrendingDown => "This metric is showing decline over the recent period",
        MetricTrend.MoreVariable => "This metric is showing unusual variability",
        MetricTrend.Unknown => "Not enough data points to determine trend direction",
        _ => "Unknown trend"
    };

    /// <summary>
    /// Whether this trend warrants attention (not necessarily bad, just notable).
    /// </summary>
    public static bool IsNotable(this MetricTrend trend) => trend switch
    {
        MetricTrend.TrendingDown => true,
        MetricTrend.MoreVariable => true,
        _ => false
    };

    /// <summary>
    /// Gets a color hint for the trend (for UI styling).
    /// Note: Actual color application is context-dependent.
    /// </summary>
    public static string GetColorHint(this MetricTrend trend) => trend switch
    {
        MetricTrend.TrendingUp => "Green",
        MetricTrend.Stable => "Gray",
        MetricTrend.TrendingDown => "Orange",
        MetricTrend.MoreVariable => "Yellow",
        MetricTrend.Unknown => "Gray",
        _ => "Gray"
    };

    /// <summary>
    /// Alias for GetColorHint() for consistency with TrendResult API.
    /// </summary>
    public static string GetColor(this MetricTrend trend) => GetColorHint(trend);

    /// <summary>
    /// Parses a string to MetricTrend.
    /// </summary>
    public static MetricTrend ParseMetricTrend(string? value) => value?.ToLower() switch
    {
        "trendingup" or "trending_up" or "up" or "improving" => MetricTrend.TrendingUp,
        "stable" or "steady" => MetricTrend.Stable,
        "trendingdown" or "trending_down" or "down" or "declining" => MetricTrend.TrendingDown,
        "morevariable" or "more_variable" or "variable" => MetricTrend.MoreVariable,
        "unknown" or "insufficient" => MetricTrend.Unknown,
        _ => MetricTrend.Unknown // Default
    };
}
