using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for editing prep item details including prep prompt, response, scope, assignee, and status.
/// </summary>
public partial class EditPrepItemDialog : Window
{
    private readonly EditPrepItemDialogViewModel _viewModel;
    
    /// <summary>
    /// The updated item after saving (null if cancelled).
    /// </summary>
    public MeetingPrepItem? UpdatedItem => _viewModel.UpdatedItem;
    
    public EditPrepItemDialog()
    {
        InitializeComponent();
        _viewModel = new EditPrepItemDialogViewModel();
        DataContext = _viewModel;
        _viewModel.CloseRequested += result => Close(result);
    }
    
    public EditPrepItemDialog(MeetingPrepItem item) : this()
    {
        _viewModel = new EditPrepItemDialogViewModel(item);
        DataContext = _viewModel;
        _viewModel.CloseRequested += result => Close(result);
    }
    
    /// <summary>
    /// Sets the meeting attendees for the assignee picker.
    /// </summary>
    public void SetAttendees(IEnumerable<MeetingAttendee> attendees, Guid currentUserTeamMemberId)
    {
        _viewModel.SetAttendees(attendees, currentUserTeamMemberId);
    }
}
