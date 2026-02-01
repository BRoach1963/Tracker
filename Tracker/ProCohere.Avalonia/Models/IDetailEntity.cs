using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Interface for entities that can be displayed in the EntityDetailFlyout.
/// Commands are wired up by the parent ViewModel when creating/selecting the entity.
/// This decouples the flyout from any specific ViewModel - it just displays whatever
/// implements this interface and the appropriate DataTemplate handles the rendering.
/// </summary>
public interface IDetailEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Display title for the entity (shown in flyout header).
    /// </summary>
    string Title { get; }
    
    /// <summary>
    /// Command to close the flyout. Wired up by parent ViewModel.
    /// </summary>
    ICommand? CloseCommand { get; set; }
    
    /// <summary>
    /// Command to edit the entity. Wired up by parent ViewModel.
    /// </summary>
    ICommand? EditCommand { get; set; }
    
    /// <summary>
    /// Command to delete the entity. Wired up by parent ViewModel.
    /// </summary>
    ICommand? DeleteCommand { get; set; }
}

/// <summary>
/// Represents an additional action that can be displayed in a "More Actions" menu.
/// Allows entity-specific actions without modifying the shell or interface.
/// </summary>
public record EntityAction(string Label, string IconKey, ICommand Command);

/// <summary>
/// Extended interface for entities with additional actions beyond Edit/Delete.
/// </summary>
public interface IDetailEntityWithActions : IDetailEntity
{
    /// <summary>
    /// Additional entity-specific actions for the "More Actions" menu.
    /// </summary>
    IReadOnlyList<EntityAction>? MoreActions { get; }
}
