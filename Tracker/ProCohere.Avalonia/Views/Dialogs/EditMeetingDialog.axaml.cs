using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating and editing meetings.
/// 
/// MINIMAL CODE-BEHIND - following MVVM strictly:
/// - Creates ViewModel
/// - Provides DialogService (View's only responsibility - it has the Window reference)
/// - Wires up CloseRequested event
/// - Exposes public methods for caller convenience (delegates to ViewModel)
/// 
/// ALL business logic is in EditMeetingDialogViewModel.
/// </summary>
public partial class EditMeetingDialog : Window
{
    private readonly EditMeetingDialogViewModel _viewModel;
    private bool _forceClose;

    public EditMeetingDialog()
    {
        InitializeComponent();
        
        _viewModel = new EditMeetingDialogViewModel();
        DataContext = _viewModel;
        
        // Provide dialog service to ViewModel (View's responsibility - it has the Window)
        _viewModel.SetDialogService(new DialogService(this));
        
        // Close window when ViewModel requests it (this is the "approved" close path)
        _viewModel.CloseRequested += (_, _) =>
        {
            _forceClose = true;
            Close();
        };
    }
    
    /// <summary>
    /// Handle window closing to show confirmation if there are unsaved changes.
    /// </summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        // If already approved to close (via Cancel command), let it close
        if (_forceClose)
        {
            base.OnClosing(e);
            return;
        }
        
        // Check if there are unsaved changes
        if (_viewModel.HasUnsavedChanges)
        {
            e.Cancel = true;
            
            // Execute the Cancel command which will show confirmation
            if (_viewModel.CancelCommand.CanExecute(null))
            {
                await _viewModel.CancelCommand.ExecuteAsync(null);
            }
        }
        else
        {
            base.OnClosing(e);
        }
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
        
        // Sync UI elements that can't be bound directly (View-specific, not business logic)
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
    
    /// <summary>
    /// Pre-select an attendee for the meeting (useful for "Schedule Meeting with [Person]").
    /// Call this after SetTeamMembers and before ShowDialog.
    /// Sets meeting type to 1:1 automatically.
    /// </summary>
    public void PreSelectAttendee(TeamMemberDetail attendee)
    {
        _viewModel.PreSelectAttendee(attendee);
        DetailsPanel.SetMeetingType("one_on_one");
    }
    
    #endregion
}
