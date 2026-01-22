using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for deferring an agenda item to a future meeting with a specific person.
/// </summary>
public partial class DeferAgendaItemDialog : Window
{
    private MeetingAgendaItem? _agendaItem;

    /// <summary>
    /// Result of the dialog - the deferral data if confirmed, null if cancelled.
    /// </summary>
    public DeferAgendaItemResult? Result { get; private set; }

    public DeferAgendaItemDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the agenda item context for this dialog.
    /// </summary>
    public void SetAgendaItem(MeetingAgendaItem item)
    {
        _agendaItem = item;
        AgendaItemTitleText.Text = item.Title;
    }

    /// <summary>
    /// Sets the list of team members for the anchor person dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> members)
    {
        AnchorPersonComboBox.ItemsSource = members;
    }

    /// <summary>
    /// Pre-selects a team member (e.g., the meeting attendee).
    /// </summary>
    public void SetPreselectedMember(TeamMemberDetail? member)
    {
        if (member != null)
        {
            AnchorPersonComboBox.SelectedItem = member;
        }
    }

    private int GetSelectedExpirationDays()
    {
        var selectedItem = ExpirationComboBox.SelectedItem as ComboBoxItem;
        if (selectedItem?.Tag != null && int.TryParse(selectedItem.Tag.ToString(), out var days))
        {
            return days;
        }
        return 30; // Default
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void DeferButton_Click(object? sender, RoutedEventArgs e)
    {
        var selectedMember = AnchorPersonComboBox.SelectedItem as TeamMemberDetail;
        
        if (selectedMember == null)
        {
            // Must select a person
            AnchorPersonComboBox.Focus();
            return;
        }

        if (_agendaItem == null)
        {
            Close();
            return;
        }

        Result = new DeferAgendaItemResult
        {
            AgendaItemId = _agendaItem.Id,
            AnchorTeamMemberId = selectedMember.Id,
            ExpirationDays = GetSelectedExpirationDays(),
            Note = NoteTextBox.Text?.Trim()
        };

        Close();
    }
}

/// <summary>
/// Result data from the DeferAgendaItemDialog.
/// </summary>
public class DeferAgendaItemResult
{
    public required Guid AgendaItemId { get; init; }
    public required Guid AnchorTeamMemberId { get; init; }
    public required int ExpirationDays { get; init; }
    public string? Note { get; init; }
}
