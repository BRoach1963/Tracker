using System;
using System.Windows.Input;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents information about an entity linked to a note.
/// Used for display in note list items and detail views.
/// </summary>
public class LinkedEntityInfo
{
    /// <summary>
    /// The type of entity.
    /// </summary>
    public LinkedEntityType EntityType { get; set; }

    /// <summary>
    /// The ID of the linked entity.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Display name of the entity (e.g., meeting title, person name, goal title).
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// SVG path data for the entity type icon.
    /// </summary>
    public string IconPath => EntityType.GetIconPath();

    /// <summary>
    /// Display label for the entity type.
    /// </summary>
    public string TypeLabel => EntityType.GetDisplayName();

    /// <summary>
    /// Command to remove this link (used in editor).
    /// </summary>
    public ICommand? RemoveLinkCommand { get; set; }

    /// <summary>
    /// Command to navigate to this entity (used in detail view).
    /// </summary>
    public ICommand? NavigateCommand { get; set; }

    /// <summary>
    /// Creates a LinkedEntityInfo for a team member.
    /// </summary>
    public static LinkedEntityInfo ForTeamMember(Guid id, string name) => new()
    {
        EntityType = LinkedEntityType.TeamMember,
        EntityId = id,
        DisplayName = name
    };

    /// <summary>
    /// Creates a LinkedEntityInfo for a meeting.
    /// </summary>
    public static LinkedEntityInfo ForMeeting(Guid id, string title) => new()
    {
        EntityType = LinkedEntityType.Meeting,
        EntityId = id,
        DisplayName = title
    };

    /// <summary>
    /// Creates a LinkedEntityInfo for a project.
    /// </summary>
    public static LinkedEntityInfo ForProject(Guid id, string name) => new()
    {
        EntityType = LinkedEntityType.Project,
        EntityId = id,
        DisplayName = name
    };

    /// <summary>
    /// Creates a LinkedEntityInfo for a goal.
    /// </summary>
    public static LinkedEntityInfo ForGoal(Guid id, string title) => new()
    {
        EntityType = LinkedEntityType.Goal,
        EntityId = id,
        DisplayName = title
    };

    /// <summary>
    /// Creates a LinkedEntityInfo for a task.
    /// </summary>
    public static LinkedEntityInfo ForTask(Guid id, string title) => new()
    {
        EntityType = LinkedEntityType.Task,
        EntityId = id,
        DisplayName = title
    };

    /// <summary>
    /// Creates a LinkedEntityInfo for a metric.
    /// </summary>
    public static LinkedEntityInfo ForMetric(Guid id, string name) => new()
    {
        EntityType = LinkedEntityType.Metric,
        EntityId = id,
        DisplayName = name
    };

    /// <summary>
    /// Creates a LinkedEntityInfo for a target.
    /// </summary>
    public static LinkedEntityInfo ForTarget(Guid id, string name) => new()
    {
        EntityType = LinkedEntityType.Target,
        EntityId = id,
        DisplayName = name
    };
}
