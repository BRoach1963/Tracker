namespace ProCohere.Avalonia.Models;

/// <summary>
/// Goal types based on their strategic purpose.
/// Philosophy: Different goals serve different purposes.
/// </summary>
public enum GoalType
{
    /// <summary>
    /// Personal development and capability building.
    /// Focus: Learning, skills, growth mindset.
    /// </summary>
    Growth,

    /// <summary>
    /// Concrete outcomes and delivery focus.
    /// Focus: Measurable deliverables, milestones.
    /// </summary>
    Execution,

    /// <summary>
    /// Stability and sustainability (health).
    /// Focus: Maintaining standards, process health.
    /// </summary>
    Operational,

    /// <summary>
    /// Learning and assessment (exploratory).
    /// Focus: Discovery, experimentation, validation.
    /// </summary>
    Directional
}

/// <summary>
/// Extension methods for GoalType.
/// </summary>
public static class GoalTypeExtensions
{
    /// <summary>
    /// Gets the display name for a goal type.
    /// </summary>
    public static string ToDisplayName(this GoalType goalType) => goalType switch
    {
        GoalType.Growth => "Growth",
        GoalType.Execution => "Execution",
        GoalType.Operational => "Operational",
        GoalType.Directional => "Directional",
        _ => goalType.ToString()
    };

    /// <summary>
    /// Gets the description for a goal type.
    /// </summary>
    public static string ToDescription(this GoalType goalType) => goalType switch
    {
        GoalType.Growth => "Personal development and capability building",
        GoalType.Execution => "Concrete outcomes and delivery focus",
        GoalType.Operational => "Stability and sustainability",
        GoalType.Directional => "Learning and assessment",
        _ => string.Empty
    };

    /// <summary>
    /// Parses a string to GoalType.
    /// </summary>
    public static GoalType ParseGoalType(string? value) => value?.ToLower() switch
    {
        "growth" => GoalType.Growth,
        "execution" => GoalType.Execution,
        "operational" => GoalType.Operational,
        "directional" => GoalType.Directional,
        _ => GoalType.Execution // Default
    };
}
