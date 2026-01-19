namespace ProCohere.Avalonia.Models;

/// <summary>
/// Goal visibility levels - who can see this goal.
/// </summary>
public enum GoalVisibility
{
    /// <summary>
    /// Only visible to manager and individual contributor.
    /// Private development goals, sensitive matters.
    /// </summary>
    Private,

    /// <summary>
    /// Visible to team members.
    /// Team-level objectives, collaborative goals.
    /// </summary>
    Team,

    /// <summary>
    /// Visible across the organization.
    /// Org-wide initiatives, cross-team alignment.
    /// </summary>
    Organization
}

/// <summary>
/// Extension methods for GoalVisibility.
/// </summary>
public static class GoalVisibilityExtensions
{
    /// <summary>
    /// Gets the display name for a visibility level.
    /// </summary>
    public static string ToDisplayName(this GoalVisibility visibility) => visibility switch
    {
        GoalVisibility.Private => "Private",
        GoalVisibility.Team => "Team",
        GoalVisibility.Organization => "Organization",
        _ => visibility.ToString()
    };

    /// <summary>
    /// Gets the icon data for a visibility level.
    /// </summary>
    public static string ToIconData(this GoalVisibility visibility) => visibility switch
    {
        GoalVisibility.Private => "M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17M18,8A2,2 0 0,1 20,10V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V10C4,8.89 4.9,8 6,8H7V6A5,5 0 0,1 12,1A5,5 0 0,1 17,6V8H18M12,3A3,3 0 0,0 9,6V8H15V6A3,3 0 0,0 12,3Z", // Lock icon
        GoalVisibility.Team => "M16,13C15.71,13 15.38,13 15.03,13.05C16.19,13.89 17,15 17,16.5V19H23V16.5C23,14.17 18.33,13 16,13M8,13C5.67,13 1,14.17 1,16.5V19H15V16.5C15,14.17 10.33,13 8,13M8,11A3,3 0 0,0 11,8A3,3 0 0,0 8,5A3,3 0 0,0 5,8A3,3 0 0,0 8,11M16,11A3,3 0 0,0 19,8A3,3 0 0,0 16,5A3,3 0 0,0 13,8A3,3 0 0,0 16,11Z", // Team icon
        GoalVisibility.Organization => "M12,5.5A3.5,3.5 0 0,1 15.5,9A3.5,3.5 0 0,1 12,12.5A3.5,3.5 0 0,1 8.5,9A3.5,3.5 0 0,1 12,5.5M5,8C5.56,8 6.08,8.15 6.53,8.42C6.38,9.85 6.8,11.27 7.66,12.38C7.16,13.34 6.16,14 5,14A3,3 0 0,1 2,11A3,3 0 0,1 5,8M19,8A3,3 0 0,1 22,11A3,3 0 0,1 19,14C17.84,14 16.84,13.34 16.34,12.38C17.2,11.27 17.62,9.85 17.47,8.42C17.92,8.15 18.44,8 19,8M5.5,18.25C5.5,16.18 8.41,14.5 12,14.5C15.59,14.5 18.5,16.18 18.5,18.25V20H5.5V18.25M0,20V18.5C0,17.11 1.89,15.94 4.45,15.6C3.86,16.28 3.5,17.22 3.5,18.25V20H0M24,20H20.5V18.25C20.5,17.22 20.14,16.28 19.55,15.6C22.11,15.94 24,17.11 24,18.5V20Z", // Org icon
        _ => string.Empty
    };

    /// <summary>
    /// Parses a string to GoalVisibility.
    /// </summary>
    public static GoalVisibility ParseGoalVisibility(string? value) => value?.ToLower() switch
    {
        "private" => GoalVisibility.Private,
        "team" => GoalVisibility.Team,
        "organization" or "org" => GoalVisibility.Organization,
        _ => GoalVisibility.Team // Default
    };
}
