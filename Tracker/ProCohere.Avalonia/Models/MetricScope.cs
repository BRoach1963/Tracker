namespace ProCohere.Avalonia.Models;

/// <summary>
/// Scope of what a metric measures.
/// Philosophy: Scope helps users understand the level of aggregation and who it affects.
/// </summary>
public enum MetricScope
{
    /// <summary>
    /// Personal/individual metric.
    /// Belongs to one person, sensitive to display publicly.
    /// </summary>
    Individual,

    /// <summary>
    /// Team-level metric.
    /// Aggregated across a team, visible to team members.
    /// </summary>
    Team,

    /// <summary>
    /// Organization-wide metric.
    /// Company or org-level, typically visible to all.
    /// </summary>
    Organization
}

/// <summary>
/// Extension methods for MetricScope.
/// </summary>
public static class MetricScopeExtensions
{
    /// <summary>
    /// Gets the display name for a metric scope.
    /// </summary>
    public static string ToDisplayName(this MetricScope scope) => scope switch
    {
        MetricScope.Individual => "Individual",
        MetricScope.Team => "Team",
        MetricScope.Organization => "Organization",
        _ => scope.ToString()
    };

    /// <summary>
    /// Gets a short label for badges.
    /// </summary>
    public static string ToShortLabel(this MetricScope scope) => scope switch
    {
        MetricScope.Individual => "Ind",
        MetricScope.Team => "Team",
        MetricScope.Organization => "Org",
        _ => "?"
    };

    /// <summary>
    /// Gets a description of what this scope means.
    /// </summary>
    public static string GetDescription(this MetricScope scope) => scope switch
    {
        MetricScope.Individual => "Personal metric for one person",
        MetricScope.Team => "Aggregated across the team",
        MetricScope.Organization => "Company or organization-wide",
        _ => "Unknown scope"
    };

    /// <summary>
    /// Whether this scope typically requires sensitivity handling.
    /// Individual metrics are more sensitive than org metrics.
    /// </summary>
    public static bool IsSensitiveByDefault(this MetricScope scope) => scope switch
    {
        MetricScope.Individual => true,
        MetricScope.Team => false,
        MetricScope.Organization => false,
        _ => false
    };

    /// <summary>
    /// Parses a string to MetricScope.
    /// </summary>
    public static MetricScope ParseMetricScope(string? value) => value?.ToLower() switch
    {
        "individual" => MetricScope.Individual,
        "team" => MetricScope.Team,
        "organization" or "org" => MetricScope.Organization,
        _ => MetricScope.Individual // Default
    };
}
