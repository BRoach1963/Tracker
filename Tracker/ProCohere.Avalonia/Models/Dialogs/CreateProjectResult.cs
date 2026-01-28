using System;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result data from the CreateProjectDialog.
/// Supports creation-time staging of tasks and goals.
/// </summary>
public class CreateProjectResult
{
    /// <summary>
    /// The name of the project.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Optional description of the project.
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Optional target due date.
    /// </summary>
    public DateTime? DueDate { get; init; }
    
    #region Staged Work (creation-time only)
    
    /// <summary>
    /// Titles of new tasks to create and link to this project.
    /// </summary>
    public List<string> NewTaskTitles { get; init; } = new();
    
    /// <summary>
    /// IDs of existing tasks to link to this project.
    /// </summary>
    public List<Guid> ExistingTaskIds { get; init; } = new();
    
    /// <summary>
    /// Titles of new goals to create and link to this project.
    /// </summary>
    public List<string> NewGoalTitles { get; init; } = new();
    
    /// <summary>
    /// IDs of existing goals to link to this project.
    /// </summary>
    public List<Guid> ExistingGoalIds { get; init; } = new();
    
    /// <summary>
    /// IDs of team members to add to this project.
    /// </summary>
    public List<Guid> MemberIds { get; init; } = new();
    
    #endregion
}
