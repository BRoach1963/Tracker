using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ProCohere.Avalonia.Views.Dialogs.AlertDialog;
using static ProCohere.Avalonia.Views.Dialogs.ConfirmationDialog;

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
    public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        var dialog = new ConfirmationDialog(title, message, confirmText, cancelText, ConfirmationType.Default);
        await dialog.ShowDialog(_parentWindow);
        return dialog.IsConfirmed;
    }
    
    /// <inheritdoc />
    public async Task<bool> ShowDestructiveConfirmationAsync(string title, string message, string confirmText = "Delete", string cancelText = "Cancel")
    {
        var dialog = new ConfirmationDialog(title, message, confirmText, cancelText, ConfirmationType.Destructive);
        await dialog.ShowDialog(_parentWindow);
        return dialog.IsConfirmed;
    }
    
    /// <inheritdoc />
    public async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new AlertDialog(title, message, AlertType.Error);
        await dialog.ShowDialog(_parentWindow);
    }
    
    /// <inheritdoc />
    public async Task ShowInfoAsync(string title, string message)
    {
        var dialog = new AlertDialog(title, message, AlertType.Information);
        await dialog.ShowDialog(_parentWindow);
    }
    
    /// <inheritdoc />
    public async Task ShowWarningAsync(string title, string message)
    {
        var dialog = new AlertDialog(title, message, AlertType.Warning);
        await dialog.ShowDialog(_parentWindow);
    }
}
