using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the DeferAgendaItemDialog.
/// </summary>
public partial class DeferAgendaItemDialogViewModel : ObservableObject
{
    private MeetingAgendaItem? _agendaItem;

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler<DeferAgendaItemResult?>? CloseRequested;

    /// <summary>
    /// Gets the agenda item title for display.
    /// </summary>
    [ObservableProperty]
    private string _agendaItemTitle = string.Empty;

    /// <summary>
    /// Gets the collection of team members for selection.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> TeamMembers { get; } = new();

    /// <summary>
    /// Gets or sets the selected team member.
    /// </summary>
    [ObservableProperty]
    private TeamMemberDetail? _selectedTeamMember;

    /// <summary>
    /// Gets or sets the selected expiration index.
    /// 0=1 week, 1=30 days, 2=60 days, 3=90 days
    /// </summary>
    [ObservableProperty]
    private int _selectedExpirationIndex = 1; // Default to 30 days

    /// <summary>
    /// Gets or sets the optional note.
    /// </summary>
    [ObservableProperty]
    private string _note = string.Empty;

    /// <summary>
    /// Initializes the dialog with an agenda item.
    /// </summary>
    public void Initialize(MeetingAgendaItem item)
    {
        _agendaItem = item;
        AgendaItemTitle = item.Title;
    }

    /// <summary>
    /// Sets the list of team members for selection.
    /// </summary>
    public void SetTeamMembers(System.Collections.Generic.IEnumerable<TeamMemberDetail> members)
    {
        TeamMembers.Clear();
        foreach (var member in members)
        {
            TeamMembers.Add(member);
        }
    }

    /// <summary>
    /// Pre-selects a team member (e.g., the meeting attendee).
    /// </summary>
    public void SetPreselectedMember(TeamMemberDetail? member)
    {
        if (member != null)
        {
            SelectedTeamMember = member;
        }
    }

    private int GetExpirationDays()
    {
        return SelectedExpirationIndex switch
        {
            0 => 7,
            1 => 30,
            2 => 60,
            3 => 90,
            _ => 30
        };
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, null);
    }

    [RelayCommand]
    private void Defer()
    {
        if (SelectedTeamMember == null)
        {
            return;
        }

        if (_agendaItem == null)
        {
            CloseRequested?.Invoke(this, null);
            return;
        }

        var result = new DeferAgendaItemResult
        {
            AgendaItemId = _agendaItem.Id,
            AnchorTeamMemberId = SelectedTeamMember.Id,
            ExpirationDays = GetExpirationDays(),
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
        };

        CloseRequested?.Invoke(this, result);
    }
}
