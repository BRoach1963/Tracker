namespace ProCohere.Avalonia.Models;

/// <summary>
/// Goal scope filter - which goals to display.
/// Used in GoalsViewModel for filtering the goal list.
/// </summary>
public enum GoalScope
{
    /// <summary>
    /// Goals owned by the current user.
    /// </summary>
    MyGoals,

    /// <summary>
    /// Goals visible to the team.
    /// </summary>
    TeamGoals,

    /// <summary>
    /// Goals shared across the organization.
    /// </summary>
    SharedGoals
}

/// <summary>
/// Extension methods for GoalScope.
/// </summary>
public static class GoalScopeExtensions
{
    /// <summary>
    /// Gets the display name for a scope.
    /// </summary>
    public static string ToDisplayName(this GoalScope scope) => scope switch
    {
        GoalScope.MyGoals => "My Goals",
        GoalScope.TeamGoals => "Team Goals",
        GoalScope.SharedGoals => "Shared Goals",
        _ => scope.ToString()
    };

    /// <summary>
    /// Parses a string to GoalScope.
    /// </summary>
    public static GoalScope ParseGoalScope(string? value) => value?.ToLower().Replace(" ", "") switch
    {
        "mygoals" or "my" => GoalScope.MyGoals,
        "teamgoals" or "team" => GoalScope.TeamGoals,
        "sharedgoals" or "shared" => GoalScope.SharedGoals,
        _ => GoalScope.MyGoals // Default
    };

    /// <summary>
    /// Converts scope index to GoalScope.
    /// </summary>
    public static GoalScope FromIndex(int index) => index switch
    {
        0 => GoalScope.MyGoals,
        1 => GoalScope.TeamGoals,
        2 => GoalScope.SharedGoals,
        _ => GoalScope.MyGoals
    };

    /// <summary>
    /// Converts GoalScope to index.
    /// </summary>
    public static int ToIndex(this GoalScope scope) => scope switch
    {
        GoalScope.MyGoals => 0,
        GoalScope.TeamGoals => 1,
        GoalScope.SharedGoals => 2,
        _ => 0
    };
}
