namespace ProCohere.Avalonia.Models;

/// <summary>
/// Metric lifecycle state.
/// Philosophy: Metrics evolve like goals. A retired metric isn't a failure - 
/// it simply means it's no longer meaningful to track.
/// </summary>
public enum MetricLifecycle
{
    /// <summary>
    /// Meaningful and relevant right now.
    /// Being actively monitored and discussed.
    /// </summary>
    Active,

    /// <summary>
    /// Exists but not being actively monitored.
    /// May be temporarily less relevant or on hold.
    /// </summary>
    Dormant,

    /// <summary>
    /// No longer meaningful (terminal state).
    /// Kept for historical context but not monitored.
    /// </summary>
    Retired
}

/// <summary>
/// Extension methods for MetricLifecycle.
/// </summary>
public static class MetricLifecycleExtensions
{
    /// <summary>
    /// Gets the display name for a metric lifecycle.
    /// </summary>
    public static string ToDisplayName(this MetricLifecycle lifecycle) => lifecycle switch
    {
        MetricLifecycle.Active => "Active",
        MetricLifecycle.Dormant => "Dormant",
        MetricLifecycle.Retired => "Retired",
        _ => lifecycle.ToString()
    };

    /// <summary>
    /// Gets a description of what this lifecycle state means.
    /// </summary>
    public static string GetDescription(this MetricLifecycle lifecycle) => lifecycle switch
    {
        MetricLifecycle.Active => "Being actively monitored and discussed",
        MetricLifecycle.Dormant => "Exists but not currently prioritized",
        MetricLifecycle.Retired => "No longer relevant, kept for history",
        _ => "Unknown state"
    };

    /// <summary>
    /// Gets a reflection prompt for lifecycle changes.
    /// </summary>
    public static string GetReflectionPrompt(this MetricLifecycle lifecycle) => lifecycle switch
    {
        MetricLifecycle.Active => "Why is this metric important to monitor now?",
        MetricLifecycle.Dormant => "Why are we deprioritizing this metric?",
        MetricLifecycle.Retired => "What has changed that makes this metric no longer meaningful?",
        _ => "What has changed?"
    };

    /// <summary>
    /// Whether this lifecycle state is terminal (goal is done).
    /// </summary>
    public static bool IsTerminal(this MetricLifecycle lifecycle) => lifecycle switch
    {
        MetricLifecycle.Retired => true,
        _ => false
    };

    /// <summary>
    /// Whether this lifecycle state is actionable (metric should appear in active views).
    /// </summary>
    public static bool IsActionable(this MetricLifecycle lifecycle) => lifecycle switch
    {
        MetricLifecycle.Active => true,
        MetricLifecycle.Dormant => false,
        MetricLifecycle.Retired => false,
        _ => false
    };

    /// <summary>
    /// Parses a string to MetricLifecycle.
    /// </summary>
    public static MetricLifecycle ParseMetricLifecycle(string? value) => value?.ToLower() switch
    {
        "active" => MetricLifecycle.Active,
        "dormant" => MetricLifecycle.Dormant,
        "retired" => MetricLifecycle.Retired,
        _ => MetricLifecycle.Active // Default
    };
}
