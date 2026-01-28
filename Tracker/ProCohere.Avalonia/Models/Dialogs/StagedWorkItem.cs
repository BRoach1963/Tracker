namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Represents a staged task to be created with a project.
/// Title-only bootstrapping - no assignees, dates, or priorities.
/// </summary>
public class StagedTask
{
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Represents a staged goal to be created with a project.
/// Title-only bootstrapping.
/// </summary>
public class StagedGoal
{
    public string Title { get; set; } = string.Empty;
}
