using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels.Controls;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Panel that displays pending carry-forward agenda items for a specific team member.
/// Used in meeting prep to suggest items that should be discussed.
/// </summary>
public partial class CarryForwardSuggestionsPanel : UserControl
{
    private readonly CarryForwardSuggestionsPanelViewModel _viewModel;

    /// <summary>
    /// Event fired when user wants to add an item to the current meeting.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? AddToMeetingRequested;

    /// <summary>
    /// Event fired when user skips an item (it stays pending for next time).
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? ItemSkipped;

    /// <summary>
    /// Event fired when user marks an item as resolved.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? ItemResolved;

    public CarryForwardSuggestionsPanel()
    {
        InitializeComponent();
        
        _viewModel = new CarryForwardSuggestionsPanelViewModel();
        DataContext = _viewModel;
        
        // Wire ViewModel events to control events for parent consumption
        _viewModel.AddToMeetingRequested += (s, e) => AddToMeetingRequested?.Invoke(this, e);
        _viewModel.ItemSkipped += (s, e) => ItemSkipped?.Invoke(this, e);
        _viewModel.ItemResolved += (s, e) => ItemResolved?.Invoke(this, e);
    }

    /// <summary>
    /// Loads pending carry-forward items for a specific team member.
    /// </summary>
    public Task LoadForTeamMemberAsync(Guid teamMemberId) 
        => _viewModel.LoadForTeamMemberAsync(teamMemberId);

    /// <summary>
    /// Refreshes the pending items list.
    /// </summary>
    public Task RefreshAsync() 
        => _viewModel.RefreshAsync();
}
