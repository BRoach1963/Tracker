using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Dialogs;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// UserControl for displaying meeting details in a flyout panel with vertical tabs.
/// Shows Overview, Agenda, Attendees, and Notes tabs.
/// Routes AgendaItemCard events to CircleViewModel commands.
/// </summary>
public partial class MeetingDetailFlyout : UserControl
{
    public MeetingDetailFlyout()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the CircleViewModel from DataContext.
    /// </summary>
    private CircleViewModel? ViewModel => DataContext as CircleViewModel;

    #region AgendaItemCard Event Handlers - Delegate to ViewModel

    private void OnAgendaItemCreateTaskRequested(object? sender, MeetingAgendaItem item)
    {
        ViewModel?.CreateTaskFromAgendaItemCommand?.Execute(item);
    }

    private async void OnAgendaItemRecordDecisionRequested(object? sender, MeetingAgendaItem item)
    {
        await ShowRecordOutcomeDialogAsync(item, OutcomeType.DecisionRecorded);
    }

    private async void OnAgendaItemCaptureFeedbackRequested(object? sender, MeetingAgendaItem item)
    {
        await ShowRecordOutcomeDialogAsync(item, OutcomeType.FeedbackCaptured);
    }

    private async void OnAgendaItemAddNoteRequested(object? sender, MeetingAgendaItem item)
    {
        await ShowRecordOutcomeDialogAsync(item, OutcomeType.NotesAdded);
    }

    private async Task ShowRecordOutcomeDialogAsync(MeetingAgendaItem item, string outcomeType)
    {
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow == null || ViewModel == null) return;

        var dialog = new RecordOutcomeDialog();
        dialog.SetAgendaItem(item);
        dialog.SetOutcomeType(outcomeType);

        await dialog.ShowDialog(parentWindow);

        if (dialog.Result != null)
        {
            await ViewModel.RecordOutcomeAsync(
                item.Id,
                dialog.Result.OutcomeType,
                dialog.Result.Content,
                dialog.Result.Visibility);
        }
    }

    private void OnAgendaItemSetOpenRequested(object? sender, MeetingAgendaItem item)
    {
        ViewModel?.SetAgendaItemOpenCommand?.Execute(item);
    }

    private void OnAgendaItemSetDiscussedRequested(object? sender, MeetingAgendaItem item)
    {
        ViewModel?.SetAgendaItemDiscussedCommand?.Execute(item);
    }

    private void OnAgendaItemSetDroppedRequested(object? sender, MeetingAgendaItem item)
    {
        ViewModel?.SetAgendaItemDroppedCommand?.Execute(item);
    }

    private async void OnAgendaItemLoadOutcomesRequested(object? sender, MeetingAgendaItem item)
    {
        if (ViewModel == null) return;
        
        var outcomes = await ViewModel.LoadAgendaItemOutcomesAsync(item.Id);
        
        if (sender is AgendaItemCard card)
        {
            card.SetOutcomes(outcomes);
        }
    }

    private async void OnAgendaItemDeferRequested(object? sender, MeetingAgendaItem item)
    {
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow == null || ViewModel == null) return;

        var dialog = new DeferAgendaItemDialog();
        dialog.SetAgendaItem(item);

        var teamMembers = await ViewModel.GetTeamMembersForDeferAsync();
        dialog.SetTeamMembers(teamMembers);

        // Pre-select the meeting attendee if this is a 1:1
        if (ViewModel.SelectedMeeting?.Attendees?.Count == 1)
        {
            var attendee = teamMembers.Find(tm => 
                ViewModel.SelectedMeeting.Attendees.Exists(a => a.Id == tm.Id));
            dialog.SetPreselectedMember(attendee);
        }

        await dialog.ShowDialog(parentWindow);

        if (dialog.Result != null)
        {
            await ViewModel.DeferAgendaItemWithCarryForwardAsync(
                item.Id,
                dialog.Result.AnchorTeamMemberId,
                dialog.Result.ExpirationDays);
        }
    }

    #endregion

    #region Meeting Action Handlers

    private void EditMeeting_Click(object? sender, RoutedEventArgs e)
    {
        // Execute the EditCommand on the selected meeting (wired to CircleViewModel.EditMeeting)
        ViewModel?.SelectedMeeting?.EditCommand?.Execute(null);
    }

    private void AddAgendaItem_Click(object? sender, RoutedEventArgs e)
    {
        // Request the dialog from the ViewModel
        ViewModel?.RequestAddAgendaItem();
    }

    private void AddAttendee_Click(object? sender, RoutedEventArgs e)
    {
        // Request the attendee picker from the ViewModel
        ViewModel?.RequestAddAttendee();
    }

    private void EditNotes_Click(object? sender, RoutedEventArgs e)
    {
        // Open the EditMeetingDialog to the Notes tab
        ViewModel?.RequestEditMeetingNotes();
    }

    #endregion
}
