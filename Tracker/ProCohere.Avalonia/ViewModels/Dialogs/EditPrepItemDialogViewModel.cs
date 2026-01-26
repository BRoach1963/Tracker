using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the EditPrepItemDialog.
/// </summary>
public partial class EditPrepItemDialogViewModel : ObservableObject
{
    private MeetingPrepItem _item;
    private Guid? _currentUserTeamMemberId;
    
    /// <summary>
    /// The updated item after saving (null if cancelled).
    /// </summary>
    public MeetingPrepItem? UpdatedItem { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action<bool>? CloseRequested;
    
    #region Observable Properties
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _prepPrompt = string.Empty;
    
    [ObservableProperty]
    private string _body = string.Empty;
    
    [ObservableProperty]
    private string _prepResponse = string.Empty;
    
    [ObservableProperty]
    private string _assigneeNotes = string.Empty;
    
    [ObservableProperty]
    private string _preparedStatusText = string.Empty;
    
    [ObservableProperty]
    private bool _hasLinkedEntity;
    
    [ObservableProperty]
    private string _linkedEntityType = string.Empty;
    
    [ObservableProperty]
    private string _linkedEntityTitle = string.Empty;
    
    [ObservableProperty]
    private string _linkedEntityIconData = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAssigneePanelVisible))]
    private int _scopeIndex = 2; // Default to personal
    
    [ObservableProperty]
    private int _statusIndex;
    
    [ObservableProperty]
    private MeetingAttendee? _selectedAttendee;
    
    #endregion
    
    /// <summary>
    /// Meeting attendees available for assignment (excluding current user).
    /// </summary>
    public ObservableCollection<MeetingAttendee> Attendees { get; } = new();
    
    /// <summary>
    /// Whether the assignee panel should be visible (scope == "assigned").
    /// </summary>
    public bool IsAssigneePanelVisible => ScopeIndex == 1;
    
    // Scope tag values matching XAML order: meeting (0), assigned (1), personal (2)
    private static readonly string[] ScopeTags = { "meeting", "assigned", "personal" };
    
    // Status tag values matching XAML order: open (0), pending (1), completed (2)
    private static readonly string[] StatusTags = { "open", "pending", "completed" };
    
    // Icon paths for linked entity types
    private static readonly Dictionary<string, string> EntityIconPaths = new()
    {
        ["task"] = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M9,13V19H7V13H9M15,15V19H17V15H15M11,11V19H13V11H11Z",
        ["goal"] = "M5,9V21H1V9H5M9,21A2,2 0 0,1 7,19V9C7,8.45 7.22,7.95 7.59,7.59L14.17,1L15.23,2.06C15.5,2.33 15.67,2.7 15.67,3.11L15.64,3.43L14.69,8H21C22.11,8 23,8.9 23,10V12C23,12.26 22.95,12.5 22.86,12.73L19.84,19.78C19.54,20.5 18.83,21 18,21H9M9,19H18.03L21,12V10H12.21L13.34,4.68L9,9.03V19Z",
        ["metric"] = "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z",
        ["default"] = "M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8M12,10A2,2 0 0,0 10,12A2,2 0 0,0 12,14A2,2 0 0,0 14,12A2,2 0 0,0 12,10Z"
    };
    
    public EditPrepItemDialogViewModel()
    {
        _item = new MeetingPrepItem();
    }
    
    public EditPrepItemDialogViewModel(MeetingPrepItem item) : this()
    {
        _item = item;
        LoadItemData();
    }
    
    /// <summary>
    /// Sets the meeting attendees for the assignee picker.
    /// </summary>
    public void SetAttendees(IEnumerable<MeetingAttendee> attendees, Guid currentUserTeamMemberId)
    {
        _currentUserTeamMemberId = currentUserTeamMemberId;
        
        Attendees.Clear();
        // Filter out the current user for assignment options - you don't assign to yourself
        foreach (var attendee in attendees.Where(a => a.TeamMemberId != currentUserTeamMemberId))
        {
            Attendees.Add(attendee);
        }
        
        // Pre-select the current assignee if one exists
        if (_item.AssignedToTeamMemberId.HasValue)
        {
            SelectedAttendee = Attendees.FirstOrDefault(a => a.TeamMemberId == _item.AssignedToTeamMemberId.Value);
        }
    }
    
    private void LoadItemData()
    {
        // Basic fields
        Title = _item.Title ?? string.Empty;
        PrepPrompt = _item.PrepPrompt ?? string.Empty;
        Body = _item.Body ?? string.Empty;
        PrepResponse = _item.PrepResponse ?? string.Empty;
        AssigneeNotes = _item.AssigneeNotes ?? string.Empty;
        
        // Prepared status
        if (_item.IsPrepared)
        {
            PreparedStatusText = _item.PreparedStatusDisplay ?? string.Empty;
        }
        
        // Linked entity display
        HasLinkedEntity = _item.HasLinkedEntity;
        if (HasLinkedEntity)
        {
            LinkedEntityType = _item.LinkedEntityTypeDisplay?.ToUpperInvariant() ?? string.Empty;
            LinkedEntityTitle = _item.LinkedEntityTitleSnapshot ?? "Linked Item";
            LinkedEntityIconData = GetLinkedEntityIconPath(_item.LinkedEntityType);
        }
        
        // Set scope - order is: meeting (0), assigned (1), personal (2)
        var scope = _item.VisibilityScope ?? "personal";
        ScopeIndex = scope switch
        {
            "meeting" => 0,
            "assigned" => 1,
            "personal" => 2,
            _ => 2
        };
        
        // Set status
        var status = _item.Status ?? "open";
        StatusIndex = Array.IndexOf(StatusTags, status);
        if (StatusIndex < 0) StatusIndex = 0;
    }
    
    private static string GetLinkedEntityIconPath(string? entityType)
    {
        var key = entityType?.ToLowerInvariant() ?? "default";
        return EntityIconPaths.TryGetValue(key, out var path) ? path : EntityIconPaths["default"];
    }
    
    [RelayCommand]
    private void Cancel()
    {
        UpdatedItem = null;
        CloseRequested?.Invoke(false);
    }
    
    [RelayCommand]
    private void Save()
    {
        var title = Title?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }
        
        // Update item with form values
        _item.Title = title;
        _item.PrepPrompt = string.IsNullOrWhiteSpace(PrepPrompt) ? null : PrepPrompt.Trim();
        _item.Body = string.IsNullOrWhiteSpace(Body) ? null : Body.Trim();
        _item.PrepResponse = string.IsNullOrWhiteSpace(PrepResponse) ? null : PrepResponse.Trim();
        _item.AssigneeNotes = string.IsNullOrWhiteSpace(AssigneeNotes) ? null : AssigneeNotes.Trim();
        
        // Scope
        var scope = ScopeIndex >= 0 && ScopeIndex < ScopeTags.Length ? ScopeTags[ScopeIndex] : "personal";
        _item.VisibilityScope = scope;
        
        // Handle AssignedToTeamMemberId based on scope
        switch (scope)
        {
            case "meeting":
                // Meeting scope: no specific assignee - everyone sees it
                _item.AssignedToTeamMemberId = null;
                _item.AssignedToName = string.Empty;
                break;
                
            case "assigned":
                // Assigned scope: must have an assignee
                if (SelectedAttendee != null)
                {
                    _item.AssignedToTeamMemberId = SelectedAttendee.TeamMemberId;
                    _item.AssignedToName = SelectedAttendee.Name;
                }
                else
                {
                    // No assignee selected - default to personal
                    _item.VisibilityScope = "personal";
                    _item.AssignedToTeamMemberId = _currentUserTeamMemberId;
                }
                break;
                
            case "personal":
                // Personal scope: assigned to self (current user)
                _item.AssignedToTeamMemberId = _currentUserTeamMemberId;
                break;
        }
        
        // Status
        _item.Status = StatusIndex >= 0 && StatusIndex < StatusTags.Length ? StatusTags[StatusIndex] : "open";
        
        // Track prepared_at if response was added
        if (!string.IsNullOrWhiteSpace(_item.PrepResponse) && !_item.PreparedAt.HasValue)
        {
            _item.PreparedAt = DateTime.UtcNow;
        }
        else if (string.IsNullOrWhiteSpace(_item.PrepResponse))
        {
            _item.PreparedAt = null;
        }
        
        // Mark as dirty so main Save knows to persist this
        _item.IsDirty = true;
        
        UpdatedItem = _item;
        CloseRequested?.Invoke(true);
    }
}
