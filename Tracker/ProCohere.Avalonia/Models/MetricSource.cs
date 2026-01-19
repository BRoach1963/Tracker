namespace ProCohere.Avalonia.Models;

/// <summary>
/// Source of metric data.
/// Philosophy: Knowing the source helps users understand reliability and context.
/// </summary>
public enum MetricSource
{
    /// <summary>
    /// Automated data from integrated systems (CRM, analytics, etc.).
    /// Most reliable and objective, but may lack nuance.
    /// </summary>
    System,

    /// <summary>
    /// Data collected from surveys, forms, or feedback tools.
    /// Captures human sentiment but may have response bias.
    /// </summary>
    Survey,

    /// <summary>
    /// Human-curated data entered manually.
    /// Most flexible but requires discipline to maintain.
    /// </summary>
    Manual
}

/// <summary>
/// Extension methods for MetricSource.
/// </summary>
public static class MetricSourceExtensions
{
    /// <summary>
    /// Gets the display name for a metric source.
    /// </summary>
    public static string ToDisplayName(this MetricSource source) => source switch
    {
        MetricSource.System => "System",
        MetricSource.Survey => "Survey",
        MetricSource.Manual => "Manual",
        _ => source.ToString()
    };

    /// <summary>
    /// Gets a description of what this source means.
    /// </summary>
    public static string GetDescription(this MetricSource source) => source switch
    {
        MetricSource.System => "Automated from integrated systems",
        MetricSource.Survey => "Collected from surveys or forms",
        MetricSource.Manual => "Manually entered by team members",
        _ => "Unknown source"
    };

    /// <summary>
    /// Gets an icon hint for UI display.
    /// </summary>
    public static string GetIcon(this MetricSource source) => source switch
    {
        MetricSource.System => "⚙️",
        MetricSource.Survey => "📋",
        MetricSource.Manual => "✍️",
        _ => "❓"
    };

    /// <summary>
    /// Parses a string to MetricSource.
    /// </summary>
    public static MetricSource ParseMetricSource(string? value) => value?.ToLower() switch
    {
        "system" => MetricSource.System,
        "survey" => MetricSource.Survey,
        "manual" => MetricSource.Manual,
        _ => MetricSource.Manual // Default to manual
    };
}
