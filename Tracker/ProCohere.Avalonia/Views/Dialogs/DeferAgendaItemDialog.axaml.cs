using System.Collections.Generic;
using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for deferring an agenda item to a future meeting with a specific person.
/// </summary>
public partial class DeferAgendaItemDialog : Window
{
    private readonly DeferAgendaItemDialogViewModel _viewModel;

    /// <summary>
    /// Result of the dialog - the deferral data if confirmed, null if cancelled.
    /// </summary>
    public DeferAgendaItemResult? Result { get; private set; }

    public DeferAgendaItemDialog()
    {
        InitializeComponent();
        
        _viewModel = new DeferAgendaItemDialogViewModel();
        DataContext = _viewModel;
        
        _viewModel.CloseRequested += OnCloseRequested;
    }

    /// <summary>
    /// Sets the agenda item context for this dialog.
    /// </summary>
    public void SetAgendaItem(MeetingAgendaItem item)
    {
        _viewModel.Initialize(item);
    }

    /// <summary>
    /// Sets the list of team members for the anchor person dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> members)
    {
        _viewModel.SetTeamMembers(members);
    }

    /// <summary>
    /// Pre-selects a team member (e.g., the meeting attendee).
    /// </summary>
    public void SetPreselectedMember(TeamMemberDetail? member)
    {
        _viewModel.SetPreselectedMember(member);
    }

    private void OnCloseRequested(object? sender, DeferAgendaItemResult? result)
    {
        Result = result;
        Close();
    }
}
