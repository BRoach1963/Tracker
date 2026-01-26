using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels.Controls;

/// <summary>
/// ViewModel for the CarryForwardSuggestionsPanel control.
/// </summary>
public partial class CarryForwardSuggestionsPanelViewModel : ObservableObject
{
    private Guid? _teamMemberId;

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

    /// <summary>
    /// Gets the collection of pending carry-forward items.
    /// </summary>
    public ObservableCollection<MeetingAgendaItem> PendingItems { get; } = new();

    /// <summary>
    /// Gets the count of pending items as a string for the badge.
    /// </summary>
    [ObservableProperty]
    private string _itemCount = "0";

    /// <summary>
    /// Gets whether there are items to display.
    /// </summary>
    [ObservableProperty]
    private bool _hasItems;

    /// <summary>
    /// Gets whether the empty state should be shown.
    /// </summary>
    [ObservableProperty]
    private bool _isEmpty = true;

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
            UpdateItems(Array.Empty<MeetingAgendaItem>());
            return;
        }

        try
        {
            var carryForwardService = CarryForwardService.Instance;
            var items = await carryForwardService.GetPendingCarryForwardsAsync(_teamMemberId.Value);
            UpdateItems(items);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading carry-forward items: {ex.Message}");
            UpdateItems(Array.Empty<MeetingAgendaItem>());
        }
    }

    private void UpdateItems(System.Collections.Generic.IEnumerable<MeetingAgendaItem> items)
    {
        PendingItems.Clear();
        foreach (var item in items)
        {
            PendingItems.Add(item);
        }

        ItemCount = PendingItems.Count.ToString();
        HasItems = PendingItems.Count > 0;
        IsEmpty = PendingItems.Count == 0;
    }

    [RelayCommand]
    private void AddToMeeting(MeetingAgendaItem? item)
    {
        if (item == null) return;
        
        System.Diagnostics.Debug.WriteLine($"Add to meeting requested for item: {item.Title}");
        AddToMeetingRequested?.Invoke(this, item);
    }

    [RelayCommand]
    private void Skip(MeetingAgendaItem? item)
    {
        if (item == null) return;
        
        System.Diagnostics.Debug.WriteLine($"Item skipped: {item.Title}");
        ItemSkipped?.Invoke(this, item);
    }

    [RelayCommand]
    private async Task MarkResolvedAsync(MeetingAgendaItem? item)
    {
        if (item == null) return;

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
