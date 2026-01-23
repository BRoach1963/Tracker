using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for editing prep item details including prep prompt, response, scope, assignee, and status.
/// </summary>
public partial class EditPrepItemDialog : Window
{
    private readonly MeetingPrepItem _item;
    private List<MeetingAttendee> _attendees = new();
    private Guid? _currentUserTeamMemberId;
    
    /// <summary>
    /// The updated item after saving (null if cancelled).
    /// </summary>
    public MeetingPrepItem? UpdatedItem { get; private set; }
    
    public EditPrepItemDialog()
    {
        InitializeComponent();
        _item = new MeetingPrepItem();
    }
    
    public EditPrepItemDialog(MeetingPrepItem item) : this()
    {
        _item = item;
        LoadItemData();
    }
    
    /// <summary>
    /// Sets the meeting attendees for the assignee picker.
    /// </summary>
    /// <param name="attendees">List of meeting attendees</param>
    /// <param name="currentUserTeamMemberId">Current user's team_member_id for filtering self from "assigned" options</param>
    public void SetAttendees(IEnumerable<MeetingAttendee> attendees, Guid currentUserTeamMemberId)
    {
        _currentUserTeamMemberId = currentUserTeamMemberId;
        // Filter out the current user for assignment options - you don't assign to yourself
        _attendees = attendees.Where(a => a.TeamMemberId != currentUserTeamMemberId).ToList();
        AssigneeComboBox.ItemsSource = _attendees;
        
        // Pre-select the current assignee if one exists
        if (_item.AssignedToTeamMemberId.HasValue)
        {
            var currentAssignee = _attendees.FirstOrDefault(a => a.TeamMemberId == _item.AssignedToTeamMemberId.Value);
            if (currentAssignee != null)
            {
                AssigneeComboBox.SelectedItem = currentAssignee;
            }
        }
    }
    
    private void LoadItemData()
    {
        // Basic fields
        TitleTextBox.Text = _item.Title;
        PrepPromptTextBox.Text = _item.PrepPrompt;
        BodyTextBox.Text = _item.Body;
        PrepResponseTextBox.Text = _item.PrepResponse;
        AssigneeNotesTextBox.Text = _item.AssigneeNotes;
        
        // Prepared status
        if (_item.IsPrepared)
        {
            PreparedStatusText.Text = _item.PreparedStatusDisplay;
        }
        
        // Linked entity display
        if (_item.HasLinkedEntity)
        {
            LinkedEntityBanner.IsVisible = true;
            LinkedEntityTypeText.Text = _item.LinkedEntityTypeDisplay?.ToUpperInvariant();
            LinkedEntityTitleText.Text = _item.LinkedEntityTitleSnapshot ?? "Linked Item";
            
            // Set icon based on type
            LinkedEntityIcon.Data = GetLinkedEntityIconData(_item.LinkedEntityType);
        }
        
        // Set scope combo - order is: meeting (0), assigned (1), personal (2)
        var scope = _item.VisibilityScope ?? "personal";
        var scopeIndex = scope switch
        {
            "meeting" => 0,
            "assigned" => 1,
            "personal" => 2,
            _ => 2
        };
        ScopeComboBox.SelectedIndex = scopeIndex;
        
        // Show assignee panel and notes panel if assigned scope
        var isAssigned = scope == "assigned";
        AssigneePanel.IsVisible = isAssigned;
        AssigneeNotesPanel.IsVisible = isAssigned;
        
        // Set status combo
        var status = _item.Status ?? "open";
        for (int i = 0; i < StatusComboBox.Items.Count; i++)
        {
            if (StatusComboBox.Items[i] is ComboBoxItem cbi && 
                cbi.Tag?.ToString() == status)
            {
                StatusComboBox.SelectedIndex = i;
                break;
            }
        }
    }
    
    private static Geometry? GetLinkedEntityIconData(string? entityType)
    {
        return entityType?.ToLowerInvariant() switch
        {
            "task" => Geometry.Parse("M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M9,13V19H7V13H9M15,15V19H17V15H15M11,11V19H13V11H11Z"),
            "goal" => Geometry.Parse("M5,9V21H1V9H5M9,21A2,2 0 0,1 7,19V9C7,8.45 7.22,7.95 7.59,7.59L14.17,1L15.23,2.06C15.5,2.33 15.67,2.7 15.67,3.11L15.64,3.43L14.69,8H21C22.11,8 23,8.9 23,10V12C23,12.26 22.95,12.5 22.86,12.73L19.84,19.78C19.54,20.5 18.83,21 18,21H9M9,19H18.03L21,12V10H12.21L13.34,4.68L9,9.03V19Z"),
            "metric" => Geometry.Parse("M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z"),
            _ => Geometry.Parse("M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8M12,10A2,2 0 0,0 10,12A2,2 0 0,0 12,14A2,2 0 0,0 14,12A2,2 0 0,0 12,10Z")
        };
    }
    
    private void ScopeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedItem = ScopeComboBox.SelectedItem as ComboBoxItem;
        var scope = selectedItem?.Tag?.ToString() ?? "personal";
        
        // Show assignee picker only for "assigned" scope
        var isAssigned = scope == "assigned";
        AssigneePanel.IsVisible = isAssigned;
        AssigneeNotesPanel.IsVisible = isAssigned;
        
        // Clear assignee selection when hiding
        if (!isAssigned)
        {
            AssigneeComboBox.SelectedItem = null;
        }
    }
    
    #region Dialog Actions
    
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        UpdatedItem = null;
        Close(false);
    }
    
    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            // Could show validation error
            return;
        }
        
        // Update item with form values
        _item.Title = title;
        _item.PrepPrompt = PrepPromptTextBox.Text?.Trim();
        _item.Body = BodyTextBox.Text?.Trim();
        _item.PrepResponse = PrepResponseTextBox.Text?.Trim();
        _item.AssigneeNotes = AssigneeNotesTextBox.Text?.Trim();
        
        // Scope
        var scopeItem = ScopeComboBox.SelectedItem as ComboBoxItem;
        var scope = scopeItem?.Tag?.ToString() ?? "personal";
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
                if (AssigneeComboBox.SelectedItem is MeetingAttendee assignee)
                {
                    _item.AssignedToTeamMemberId = assignee.TeamMemberId;
                    _item.AssignedToName = assignee.Name;
                }
                else
                {
                    // No assignee selected - show validation error or default to personal
                    _item.VisibilityScope = "personal";
                    _item.AssignedToTeamMemberId = _currentUserTeamMemberId;
                }
                break;
                
            case "personal":
                // Personal scope: assigned to self (current user)
                _item.AssignedToTeamMemberId = _currentUserTeamMemberId;
                // AssignedToName will be set by the service if needed
                break;
        }
        
        // Status
        var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;
        _item.Status = statusItem?.Tag?.ToString() ?? "open";
        
        // Track prepared_at if response was added
        if (!string.IsNullOrWhiteSpace(_item.PrepResponse) && !_item.PreparedAt.HasValue)
        {
            _item.PreparedAt = DateTime.UtcNow;
        }
        else if (string.IsNullOrWhiteSpace(_item.PrepResponse))
        {
            _item.PreparedAt = null;
        }
        
        // Save to database if this is an existing item with an ID
        if (_item.Id != Guid.Empty)
        {
            var success = await MeetingPrepItemService.Instance.UpdatePrepItemAsync(_item);
            if (!success)
            {
                // Could show error message
                // For now, just proceed with local changes
            }
        }
        
        UpdatedItem = _item;
        Close(true);
    }
    
    #endregion
}
