using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Dialogs;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// UserControl for displaying meeting details in a flyout panel with vertical tabs.
/// Shows Overview, Agenda, Attendees, and Notes tabs.
/// </summary>
public partial class MeetingDetailFlyout : UserControl
{
    public MeetingDetailFlyout()
    {
        InitializeComponent();
    }

    #region AgendaItemCard Event Handlers

    /// <summary>
    /// Gets the CircleViewModel from DataContext.
    /// </summary>
    private CircleViewModel? ViewModel => DataContext as CircleViewModel;

    private void OnAgendaItemCreateTaskRequested(object? sender, MeetingAgendaItem item)
    {
        // Delegate to the ViewModel's CreateTaskFromAgendaItem command
        if (ViewModel?.CreateTaskFromAgendaItemCommand?.CanExecute(item) == true)
        {
            ViewModel.CreateTaskFromAgendaItemCommand.Execute(item);
        }
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
        try
        {
            var dialog = new RecordOutcomeDialog();
            dialog.SetAgendaItem(item);
            dialog.SetOutcomeType(outcomeType);

            var parentWindow = TopLevel.GetTopLevel(this) as Window;
            if (parentWindow != null)
            {
                await dialog.ShowDialog(parentWindow);

                if (dialog.Result != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Recording {dialog.Result.OutcomeType} for agenda item: {item.Title}");

                    var outcomeService = AgendaItemOutcomeService.Instance;
                    
                    switch (dialog.Result.OutcomeType)
                    {
                        case OutcomeType.DecisionRecorded:
                            await outcomeService.RecordDecisionAsync(
                                item.Id,
                                dialog.Result.Content,
                                dialog.Result.Visibility);
                            break;
                        case OutcomeType.FeedbackCaptured:
                            await outcomeService.CaptureFeedbackAsync(
                                item.Id,
                                dialog.Result.Content,
                                dialog.Result.Visibility);
                            break;
                        case OutcomeType.NotesAdded:
                            await outcomeService.AddNotesAsync(
                                item.Id,
                                dialog.Result.Content,
                                dialog.Result.Visibility);
                            break;
                    }

                    // Refresh the meeting to show updated outcomes
                    // Note: For now, the data was already updated in the service
                    // A full ViewModel refresh would reload all data
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error recording outcome: {ex.Message}");
        }
    }

    private void OnAgendaItemStatusChanged(object? sender, (MeetingAgendaItem Item, string NewStatus) args)
    {
        // The status has already been updated by the AgendaItemCard
        System.Diagnostics.Debug.WriteLine($"Agenda item status changed: {args.Item.Title} -> {args.NewStatus}");
    }

    private async void OnAgendaItemDeferRequested(object? sender, MeetingAgendaItem item)
    {
        try
        {
            var dialog = new DeferAgendaItemDialog();
            dialog.SetAgendaItem(item);

            // Get team members for the anchor person dropdown
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers);

            // Pre-select the meeting attendee if this is a 1:1
            var selectedMeeting = ViewModel?.SelectedMeeting;
            if (selectedMeeting?.Attendees?.Count == 1)
            {
                var attendee = teamMembers.FirstOrDefault(tm => 
                    selectedMeeting.Attendees.Any(a => a.Id == tm.Id));
                dialog.SetPreselectedMember(attendee);
            }

            var parentWindow = TopLevel.GetTopLevel(this) as Window;
            if (parentWindow != null)
            {
                await dialog.ShowDialog(parentWindow);

                if (dialog.Result != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Deferring agenda item: {item.Title} to team member {dialog.Result.AnchorTeamMemberId}");

                    var carryForwardService = CarryForwardService.Instance;
                    await carryForwardService.DeferAgendaItemAsync(
                        item.Id,
                        dialog.Result.AnchorTeamMemberId,
                        dialog.Result.ExpirationDays);

                    // Note: The service has updated the item's state
                    // A full refresh would reload from the server
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deferring agenda item: {ex.Message}");
        }
    }

    #endregion
}
