using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Panel that displays pending carry-forward agenda items for a specific team member.
/// Used in meeting prep to suggest items that should be discussed.
/// </summary>
public partial class CarryForwardSuggestionsPanel : UserControl
{
    private Guid? _teamMemberId;
    private List<MeetingAgendaItem> _pendingItems = new();

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
    }

    /// <summary>
    /// Loads pending carry-forward items for a specific team member.
    /// </summary>
    public async Task LoadForTeamMemberAsync(Guid teamMemberId)
    {
        _teamMemberId = teamMemberId;
        await RefreshAsync();
    }

    /// <summary>
    /// Refreshes the pending items list.
    /// </summary>
    public async Task RefreshAsync()
    {
        if (_teamMemberId == null)
        {
            UpdateUI(new List<MeetingAgendaItem>());
            return;
        }

        try
        {
            var carryForwardService = CarryForwardService.Instance;
            _pendingItems = (await carryForwardService.GetPendingCarryForwardsAsync(_teamMemberId.Value)).ToList();
            UpdateUI(_pendingItems);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading carry-forward items: {ex.Message}");
            UpdateUI(new List<MeetingAgendaItem>());
        }
    }

    private void UpdateUI(List<MeetingAgendaItem> items)
    {
        SuggestionsItemsControl.ItemsSource = items;
        CountBadge.Text = items.Count.ToString();
        
        EmptyStatePanel.IsVisible = items.Count == 0;
        SuggestionsItemsControl.IsVisible = items.Count > 0;
    }

    private void AddToMeetingButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is MeetingAgendaItem item)
        {
            System.Diagnostics.Debug.WriteLine($"Add to meeting requested for item: {item.Title}");
            AddToMeetingRequested?.Invoke(this, item);
        }
    }

    private void SkipButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is MeetingAgendaItem item)
        {
            System.Diagnostics.Debug.WriteLine($"Item skipped: {item.Title}");
            ItemSkipped?.Invoke(this, item);
        }
    }

    private async void MarkResolvedButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is MeetingAgendaItem item)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Marking item as resolved: {item.Title}");
                
                var carryForwardService = CarryForwardService.Instance;
                await carryForwardService.MarkAsResolvedAsync(item.Id);
                
                ItemResolved?.Invoke(this, item);
                
                // Refresh the list
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking item as resolved: {ex.Message}");
            }
        }
    }
}
