using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Views.Dialogs; // For EntityPickerResult - TODO: move to Models
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for showing dialogs from ViewModels without violating MVVM.
/// ViewModels depend on this interface; the View provides the implementation
/// with the necessary Window reference.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows the entity picker dialog and returns the selected entity, or null if cancelled.
    /// </summary>
    Task<EntityPickerResult?> ShowEntityPickerAsync();
    
    /// <summary>
    /// Shows the edit prep item dialog for an existing item.
    /// Returns the updated item if saved, or null if cancelled.
    /// </summary>
    /// <param name="item">The prep item to edit</param>
    /// <param name="attendees">Available attendees for assignment</param>
    /// <param name="currentUserTeamMemberId">Current user's team member ID (excluded from assignee list)</param>
    Task<PrepItemDialogResult?> ShowEditPrepItemDialogAsync(
        MeetingPrepItem item,
        IEnumerable<MeetingAttendee>? attendees = null,
        Guid? currentUserTeamMemberId = null);
    
    /// <summary>
    /// Shows the edit agenda item dialog for an existing item.
    /// Returns the updated item if saved, or null if cancelled.
    /// </summary>
    Task<AgendaItemDialogResult?> ShowEditAgendaItemDialogAsync(DialogAgendaItem item);
    
    /// <summary>
    /// Shows a confirmation dialog.
    /// Returns true if confirmed, false if cancelled.
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message);
    
    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    Task ShowErrorAsync(string title, string message);
}

/// <summary>
/// Result from editing a prep item.
/// </summary>
public class PrepItemDialogResult
{
    public required string Title { get; init; }
    public string? Body { get; init; }
    public string? PrepPrompt { get; init; }
    public string? PrepResponse { get; init; }
    public string? AssigneeNotes { get; init; }
    public string? VisibilityScope { get; init; }
    public Guid? AssignedToTeamMemberId { get; init; }
    public string? AssignedToName { get; init; }
    public string? Status { get; init; }
    public DateTime? PreparedAt { get; init; }
    
    /// <summary>
    /// Creates a result from a MeetingPrepItem.
    /// </summary>
    public static PrepItemDialogResult FromPrepItem(MeetingPrepItem item) => new()
    {
        Title = item.Title,
        Body = item.Body,
        PrepPrompt = item.PrepPrompt,
        PrepResponse = item.PrepResponse,
        AssigneeNotes = item.AssigneeNotes,
        VisibilityScope = item.VisibilityScope,
        AssignedToTeamMemberId = item.AssignedToTeamMemberId,
        AssignedToName = item.AssignedToName,
        Status = item.Status,
        PreparedAt = item.PreparedAt
    };
}

/// <summary>
/// Result from editing an agenda item.
/// </summary>
public class AgendaItemDialogResult
{
    public required string Title { get; init; }
    public string? DisplayTitle { get; init; }
    public string? SharedContext { get; init; }
    public string? PrivateContext { get; init; }
    public string? VisibilityScope { get; init; }
    public List<TalkingPoint> TalkingPoints { get; init; } = new();
    
    /// <summary>
    /// Creates a result from a DialogAgendaItem.
    /// </summary>
    public static AgendaItemDialogResult FromAgendaItem(DialogAgendaItem item) => new()
    {
        Title = item.Title,
        DisplayTitle = item.DisplayTitle,
        SharedContext = item.SharedContext,
        PrivateContext = item.PrivateContext,
        VisibilityScope = item.VisibilityScope,
        TalkingPoints = new List<TalkingPoint>(item.TalkingPoints)
    };
}
