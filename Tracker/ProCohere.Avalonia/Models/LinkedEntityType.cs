namespace ProCohere.Avalonia.Models;

/// <summary>
/// Types of entities that can be linked to a note.
/// </summary>
public enum LinkedEntityType
{
    /// <summary>No entity linked.</summary>
    None = 0,

    /// <summary>Link to a team member.</summary>
    TeamMember,

    /// <summary>Link to a meeting.</summary>
    Meeting,

    /// <summary>Link to a project.</summary>
    Project,

    /// <summary>Link to a goal.</summary>
    Goal,

    /// <summary>Link to a task.</summary>
    Task,

    /// <summary>Link to a metric.</summary>
    Metric,

    /// <summary>Link to a target (key result).</summary>
    Target
}

/// <summary>
/// Extension methods for LinkedEntityType.
/// </summary>
public static class LinkedEntityTypeExtensions
{
    /// <summary>
    /// Gets the database column name for the entity type in the notes table.
    /// Note: Metric and Target do NOT have corresponding columns in the notes table.
    /// </summary>
    public static string GetColumnName(this LinkedEntityType entityType) => entityType switch
    {
        LinkedEntityType.TeamMember => "linked_team_member_id",
        LinkedEntityType.Meeting => "linked_meeting_id",
        LinkedEntityType.Project => "linked_project_id",
        LinkedEntityType.Goal => "linked_goal_id",
        LinkedEntityType.Task => "linked_task_id",
        // Metric and Target are not supported in the notes table
        LinkedEntityType.Metric => throw new System.NotSupportedException("Metric links are not supported in notes - column does not exist"),
        LinkedEntityType.Target => throw new System.NotSupportedException("Target links are not supported in notes - column does not exist"),
        _ => throw new System.ArgumentException($"Invalid entity type: {entityType}")
    };

    /// <summary>
    /// Gets a display-friendly name for the entity type.
    /// </summary>
    public static string GetDisplayName(this LinkedEntityType entityType) => entityType switch
    {
        LinkedEntityType.None => "None",
        LinkedEntityType.TeamMember => "Team Member",
        LinkedEntityType.Meeting => "Meeting",
        LinkedEntityType.Project => "Project",
        LinkedEntityType.Goal => "Goal",
        LinkedEntityType.Task => "Task",
        LinkedEntityType.Metric => "Metric",
        LinkedEntityType.Target => "Target",
        _ => entityType.ToString()
    };

    /// <summary>
    /// Gets the icon path data for the entity type.
    /// Using Material Design icon paths.
    /// </summary>
    public static string GetIconPath(this LinkedEntityType entityType) => entityType switch
    {
        // Person icon
        LinkedEntityType.TeamMember => "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z",
        // Calendar icon
        LinkedEntityType.Meeting => "M19,19H5V8H19M16,1V3H8V1H6V3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3H18V1M17,12H12V17H17V12Z",
        // Folder icon
        LinkedEntityType.Project => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z",
        // Target/bullseye icon
        LinkedEntityType.Goal => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8M12,10A2,2 0 0,0 10,12A2,2 0 0,0 12,14A2,2 0 0,0 14,12A2,2 0 0,0 12,10Z",
        // Checkbox icon
        LinkedEntityType.Task => "M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M10,17L5,12L6.41,10.58L10,14.17L17.59,6.58L19,8L10,17Z",
        // Chart icon
        LinkedEntityType.Metric => "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H22V21H2V3H4V17.54L9.5,8L16,11.78Z",
        // Flag/milestone icon
        LinkedEntityType.Target => "M14.4,6L14,4H5V21H7V14H12.6L13,16H20V6H14.4Z",
        _ => string.Empty
    };
}
