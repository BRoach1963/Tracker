using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Implementation of IDialogService that uses Avalonia dialogs.
/// The View provides the parent Window reference.
/// </summary>
public class DialogService : IDialogService
{
    private readonly Window _parentWindow;
    
    public DialogService(Window parentWindow)
    {
        _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
    }
    
    /// <inheritdoc />
    public async Task<EntityPickerResult?> ShowEntityPickerAsync()
    {
        var dialog = new EntityPickerDialog();
        await dialog.ShowDialog(_parentWindow);
        return dialog.Result;
    }
    
    /// <inheritdoc />
    public async Task<PrepItemDialogResult?> ShowEditPrepItemDialogAsync(
        MeetingPrepItem item,
        IEnumerable<MeetingAttendee>? attendees = null,
        Guid? currentUserTeamMemberId = null)
    {
        var dialog = new EditPrepItemDialog(item);
        
        if (attendees != null && currentUserTeamMemberId.HasValue)
        {
            dialog.SetAttendees(attendees, currentUserTeamMemberId.Value);
        }
        
        await dialog.ShowDialog(_parentWindow);
        
        if (dialog.UpdatedItem == null)
            return null;
            
        return PrepItemDialogResult.FromPrepItem(dialog.UpdatedItem);
    }
    
    /// <inheritdoc />
    public async Task<AgendaItemDialogResult?> ShowEditAgendaItemDialogAsync(DialogAgendaItem item)
    {
        var dialog = new EditAgendaItemDialog(item);
        await dialog.ShowDialog(_parentWindow);
        
        if (dialog.Result == null)
            return null;
            
        return new AgendaItemDialogResult
        {
            Title = dialog.Result.Title,
            DisplayTitle = dialog.Result.DisplayTitle,
            SharedContext = dialog.Result.SharedContext,
            PrivateContext = dialog.Result.PrivateContext,
            VisibilityScope = dialog.Result.VisibilityScope,
            TalkingPoints = dialog.Result.TalkingPoints
        };
    }
    
    /// <inheritdoc />
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        // TODO: Implement proper confirmation dialog
        // For now, return true (confirm)
        await Task.CompletedTask;
        return true;
    }
    
    /// <inheritdoc />
    public async Task ShowErrorAsync(string title, string message)
    {
        // TODO: Implement proper error dialog
        await Task.CompletedTask;
    }
}
