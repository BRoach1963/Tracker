namespace ProCohere.Avalonia.Models;

/// <summary>
/// Goal health status - reflects current state without judgment.
/// Philosophy: Health describes state, NOT value judgment.
/// NO red/yellow/green, NO progress bars, NO percentages.
/// </summary>
public enum GoalHealth
{
    /// <summary>
    /// Goal is progressing well.
    /// The team feels aligned and making progress.
    /// </summary>
    OnTrack,

    /// <summary>
    /// Some concerns worth discussing.
    /// Minor blockers or shifts that warrant attention.
    /// </summary>
    NeedsAttention,

    /// <summary>
    /// Significant challenges present.
    /// Substantial blockers requiring intervention.
    /// </summary>
    AtRisk,

    /// <summary>
    /// Goal intent may need reconsideration.
    /// The original framing may no longer serve its purpose.
    /// </summary>
    ReframingNeeded
}

/// <summary>
/// Extension methods for GoalHealth.
/// </summary>
public static class GoalHealthExtensions
{
    /// <summary>
    /// Gets the display name for a goal health status.
    /// Uses neutral language - NO value judgments.
    /// </summary>
    public static string ToDisplayName(this GoalHealth health) => health switch
    {
        GoalHealth.OnTrack => "On Track",
        GoalHealth.NeedsAttention => "Needs Attention",
        GoalHealth.AtRisk => "At Risk",
        GoalHealth.ReframingNeeded => "Reframing Needed",
        _ => health.ToString()
    };

    /// <summary>
    /// Gets a neutral badge style class for the health status.
    /// Uses blue/navy tones only - NO red/yellow/green.
    /// </summary>
    public static string ToBadgeClass(this GoalHealth health) => health switch
    {
        GoalHealth.OnTrack => "health-on-track",
        GoalHealth.NeedsAttention => "health-needs-attention",
        GoalHealth.AtRisk => "health-at-risk",
        GoalHealth.ReframingNeeded => "health-reframing",
        _ => "health-default"
    };

    /// <summary>
    /// Gets a reflection prompt when changing health.
    /// </summary>
    public static string GetReflectionPrompt(this GoalHealth health) => health switch
    {
        GoalHealth.OnTrack => "What's going well that supports this assessment?",
        GoalHealth.NeedsAttention => "What concerns are emerging that need discussion?",
        GoalHealth.AtRisk => "What significant challenges are you observing?",
        GoalHealth.ReframingNeeded => "How has the situation changed that affects this goal's relevance?",
        _ => "What has changed?"
    };

    /// <summary>
    /// Parses a string to GoalHealth.
    /// </summary>
    public static GoalHealth ParseGoalHealth(string? value) => value?.ToLower().Replace("_", "") switch
    {
        "ontrack" => GoalHealth.OnTrack,
        "needsattention" => GoalHealth.NeedsAttention,
        "atrisk" => GoalHealth.AtRisk,
        "reframingneeded" => GoalHealth.ReframingNeeded,
        _ => GoalHealth.OnTrack // Default
    };
}
