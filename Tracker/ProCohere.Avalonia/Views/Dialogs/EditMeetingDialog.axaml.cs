using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating and editing meetings.
/// Minimal code-behind - all business logic in EditMeetingDialogViewModel.
/// Code-behind only handles:
/// - ViewModel initialization
/// - Dialog showing (entity picker, edit dialogs) which requires Window reference
/// </summary>
public partial class EditMeetingDialog : Window
{
    private readonly EditMeetingDialogViewModel _viewModel;

    public EditMeetingDialog()
    {
        InitializeComponent();
        
        _viewModel = new EditMeetingDialogViewModel();
        DataContext = _viewModel;
        
        // Close window when ViewModel requests it
        _viewModel.CloseRequested += (_, _) => Close();
        
        // Handle dialog requests from ViewModel
        _viewModel.EntityPickerForPrepRequested += OnEntityPickerForPrepRequested;
        _viewModel.EntityPickerForAgendaRequested += OnEntityPickerForAgendaRequested;
        _viewModel.EditPrepItemRequested += OnEditPrepItemRequested;
        _viewModel.EditAgendaItemRequested += OnEditAgendaItemRequested;
    }

    #region Public Properties
    
    /// <summary>
    /// Gets the dialog result after closing.
    /// </summary>
    public EditMeetingResult? Result => _viewModel.Result;
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Initialize the dialog with team members for attendee selection.
    /// Call this before ShowDialog.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        _viewModel.Initialize(teamMembers);
    }

    /// <summary>
    /// Load an existing meeting for editing.
    /// Call this after SetTeamMembers for edit mode.
    /// </summary>
    public async Task LoadMeetingAsync(MeetingDetail meeting)
    {
        await _viewModel.LoadMeetingAsync(meeting);
        
        // Sync UI elements that can't be bound directly
        DetailsPanel.SetMeetingType(meeting.MeetingType);
        DetailsPanel.SetDuration(meeting.DurationMinutes ?? 30);
        DetailsPanel.SetDateTime(meeting.ScheduledAt);
    }

    /// <summary>
    /// Synchronous version for backwards compatibility.
    /// </summary>
    public void LoadMeeting(MeetingDetail meeting)
    {
        _ = LoadMeetingAsync(meeting);
    }
    
    #endregion

    #region Dialog Event Handlers
    
    /// <summary>
    /// Shows entity picker for prep items.
    /// This requires Window reference so it's in code-behind.
    /// </summary>
    private async void OnEntityPickerForPrepRequested(object? sender, EventArgs e)
    {
        var picker = new EntityPickerDialog();
        await picker.ShowDialog(this);
        
        if (picker.Result != null)
        {
            _viewModel.AddFromExistingPrepCommand.Execute(picker.Result);
        }
    }

    /// <summary>
    /// Shows entity picker for agenda items.
    /// </summary>
    private async void OnEntityPickerForAgendaRequested(object? sender, EventArgs e)
    {
        var picker = new EntityPickerDialog();
        await picker.ShowDialog(this);
        
        if (picker.Result != null)
        {
            _viewModel.LinkExistingAgendaItemCommand.Execute(picker.Result);
        }
    }

    /// <summary>
    /// Shows edit dialog for prep item.
    /// </summary>
    private async void OnEditPrepItemRequested(object? sender, MeetingPrepItem item)
    {
        var dialog = new EditPrepItemDialog(item);
        // Note: Not setting attendees - assignment handled separately
        await dialog.ShowDialog(this);
        
        // The dialog returns UpdatedItem if saved
        if (dialog.UpdatedItem != null)
        {
            // Copy updated properties back to the original item
            item.PrepPrompt = dialog.UpdatedItem.PrepPrompt;
            item.PrepResponse = dialog.UpdatedItem.PrepResponse;
            item.VisibilityScope = dialog.UpdatedItem.VisibilityScope;
            item.AssignedToTeamMemberId = dialog.UpdatedItem.AssignedToTeamMemberId;
            item.Status = dialog.UpdatedItem.Status;
        }
    }

    /// <summary>
    /// Shows edit dialog for agenda item.
    /// </summary>
    private async void OnEditAgendaItemRequested(object? sender, DialogAgendaItem item)
    {
        var dialog = new EditAgendaItemDialog(item);
        await dialog.ShowDialog(this);
        
        // The dialog returns Result if saved
        if (dialog.Result != null)
        {
            // Copy updated properties back to the original item
            item.Title = dialog.Result.Title;
            item.DisplayTitle = dialog.Result.DisplayTitle;
            item.SharedContext = dialog.Result.SharedContext;
            item.PrivateContext = dialog.Result.PrivateContext;
            item.VisibilityScope = dialog.Result.VisibilityScope;
            
            // Update talking points
            item.TalkingPoints.Clear();
            foreach (var tp in dialog.Result.TalkingPoints)
            {
                item.TalkingPoints.Add(tp);
            }
        }
    }
    
    #endregion
}
