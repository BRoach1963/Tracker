using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result from the entity picker dialog.
/// </summary>
public class EntityPickerResult
{
    /// <summary>
    /// The ID of the selected entity.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// The type of the selected entity (task, goal, metric, project).
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The title/name of the selected entity.
    /// </summary>
    public string EntityTitle { get; set; } = string.Empty;
}
