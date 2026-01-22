using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Result from the edit meeting dialog.
/// Contains either the saved/updated meeting, or deletion info.
/// </summary>
public class EditMeetingResult
{
    /// <summary>True if the meeting was deleted.</summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>The saved meeting (null if cancelled or deleted).</summary>
    public MeetingDetail? SavedMeeting { get; set; }
    
    /// <summary>The ID of the deleted meeting (if IsDeleted).</summary>
    public Guid? DeletedMeetingId { get; set; }
    
    /// <summary>Error message if save failed.</summary>
    public string? Error { get; set; }
}

/// <summary>
/// Agenda item model for the dialog that supports optional linking to entities.
/// </summary>
public class DialogAgendaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    
    // Linked entity (optional - for discussing existing tasks/goals/metrics)
    public Guid? LinkedEntityId { get; set; }
    public string? LinkedEntityType { get; set; } // "task", "goal", "metric", "project"
    public string? LinkedEntityTitle { get; set; }
    
    public bool HasLinkedEntity => LinkedEntityId.HasValue && !string.IsNullOrEmpty(LinkedEntityType);
    
    public string LinkedEntityTypeDisplay => LinkedEntityType?.ToLower() switch
    {
        "task" => "Task",
        "goal" => "Goal",
        "metric" => "Metric",
        "project" => "Project",
        _ => ""
    };
    
    public string TypeIcon => LinkedEntityType?.ToLower() switch
    {
        "task" => "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z",
        "goal" => "M5,16L3,5L8.5,10L12,4L15.5,10L21,5L19,16H5M19,19C19,19.55 18.55,20 18,20H6C5.45,20 5,19.55 5,19V18H19V19Z",
        "metric" => "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z",
        "project" => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z",
        _ => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
    };
    
    public global::Avalonia.Media.IBrush TypeColor => LinkedEntityType?.ToLower() switch
    {
        "task" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#3498DB")),
        "goal" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#27AE60")),
        "metric" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#9B59B6")),
        "project" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#E67E22")),
        _ => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#7F8C8D"))
    };
}

/// <summary>
/// Dialog for creating or editing meetings.
/// Now serves as a full meeting workspace with tabs for Prep, Agenda, Notes.
/// Uses MeetingService to persist changes to Supabase.
/// </summary>
public partial class EditMeetingDialog : Window
{
    private MeetingDetail? _existingMeeting;
    private List<TeamMemberDetail> _teamMembers = new();
    private bool _isSaving;
    
    // Workspace data (saved with meeting)
    private ObservableCollection<MeetingPrepItem> _prepItems = new();
    private ObservableCollection<DialogAgendaItem> _agendaItems = new();
    
    // Track which tab is active
    private enum WorkspaceTab { Prep, Agenda, Notes }
    private WorkspaceTab _currentTab = WorkspaceTab.Prep;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditMeetingResult? Result { get; private set; }
    
    public EditMeetingDialog()
    {
        InitializeComponent();
        
        // Set default date/time to next hour
        var now = DateTime.Now;
        var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0);
        DateTimeSelector.SelectedDateTime = nextHour;
        
        // Wire up meeting type change to show/hide attendee
        MeetingTypeComboBox.SelectionChanged += MeetingTypeComboBox_SelectionChanged;
        
        // Wire up prep visibility change to show/hide assignee
        PrepVisibilityComboBox.SelectionChanged += PrepVisibilityComboBox_SelectionChanged;
        
        // Initialize workspace lists
        PrepItemsControl.ItemsSource = _prepItems;
        AgendaItemsControl.ItemsSource = _agendaItems;
        
        // Set initial tab state
        SetActiveTab(WorkspaceTab.Prep);
    }
    
    /// <summary>
    /// Load an existing meeting for editing.
    /// </summary>
    public async void LoadMeeting(MeetingDetail meeting)
    {
        _existingMeeting = meeting;
        
        DialogTitle.Text = "Edit Meeting";
        DeleteButton.IsVisible = true;
        
        TitleTextBox.Text = meeting.Title;
        
        // Set meeting type
        for (int i = 0; i < MeetingTypeComboBox.Items.Count; i++)
        {
            var item = MeetingTypeComboBox.Items[i] as ComboBoxItem;
            if (item?.Tag?.ToString() == meeting.MeetingType)
            {
                MeetingTypeComboBox.SelectedIndex = i;
                break;
            }
        }
        
        // Set date/time
        if (meeting.ScheduledAt.HasValue)
        {
            DateTimeSelector.SelectedDateTime = meeting.ScheduledAt.Value;
        }
        
        // Set duration
        var durationTag = meeting.DurationMinutes?.ToString() ?? "30";
        for (int i = 0; i < DurationComboBox.Items.Count; i++)
        {
            var item = DurationComboBox.Items[i] as ComboBoxItem;
            if (item?.Tag?.ToString() == durationTag)
            {
                DurationComboBox.SelectedIndex = i;
                break;
            }
        }
        
        LocationTextBox.Text = meeting.Location ?? "";
        VideoLinkTextBox.Text = meeting.VideoLink ?? "";
        MeetingNotesTextBox.Text = meeting.Notes ?? "";
        
        // Set attendee if we have team members loaded
        if (meeting.TeamMemberId.HasValue)
        {
            var attendee = _teamMembers.FirstOrDefault(t => t.Id == meeting.TeamMemberId.Value);
            if (attendee != null)
            {
                AttendeeComboBox.SelectedItem = attendee;
            }
        }
        
        UpdateAttendeeVisibility();
        
        // Load existing prep items
        await LoadPrepItemsAsync();
        
        // Load existing agenda items
        await LoadAgendaItemsAsync();
    }
    
    private async Task LoadPrepItemsAsync()
    {
        if (_existingMeeting == null) return;
        
        try
        {
            var prepItems = await MeetingPrepItemService.Instance.GetPrepItemsForMeetingAsync(_existingMeeting.Id);
            _prepItems.Clear();
            foreach (var item in prepItems.Where(p => !p.IsDeleted))
            {
                _prepItems.Add(item);
            }
            UpdatePrepEmptyState();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EditMeetingDialog] Error loading prep items: {ex.Message}");
        }
    }
    
    private async Task LoadAgendaItemsAsync()
    {
        if (_existingMeeting == null) return;
        
        try
        {
            // Load agenda items from service
            var agendaItems = await MeetingAgendaItemService.Instance.GetAgendaItemsForMeetingAsync(_existingMeeting.Id);
            _agendaItems.Clear();
            foreach (var item in agendaItems)
            {
                _agendaItems.Add(new DialogAgendaItem
                {
                    Id = item.Id,
                    Title = item.Title,
                    LinkedEntityId = item.LinkedEntityId,
                    LinkedEntityType = item.LinkedEntityType,
                    LinkedEntityTitle = item.LinkedEntityTitle
                });
            }
            UpdateAgendaEmptyState();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EditMeetingDialog] Error loading agenda items: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Set the list of team members for the attendee and prep assignee dropdowns.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        _teamMembers = teamMembers.ToList();
        AttendeeComboBox.ItemsSource = _teamMembers;
        PrepAssigneeComboBox.ItemsSource = _teamMembers;
        
        // If editing and we have a team member id, select it
        if (_existingMeeting?.TeamMemberId.HasValue == true)
        {
            var attendee = _teamMembers.FirstOrDefault(t => t.Id == _existingMeeting.TeamMemberId.Value);
            if (attendee != null)
            {
                AttendeeComboBox.SelectedItem = attendee;
            }
        }
    }
    
    #region Tab Navigation
    
    private void SetActiveTab(WorkspaceTab tab)
    {
        _currentTab = tab;
        
        // Update tab button styles
        PrepTabButton.Classes.Set("selected", tab == WorkspaceTab.Prep);
        AgendaTabButton.Classes.Set("selected", tab == WorkspaceTab.Agenda);
        NotesTabButton.Classes.Set("selected", tab == WorkspaceTab.Notes);
        
        // Show/hide content
        PrepTabContent.IsVisible = tab == WorkspaceTab.Prep;
        AgendaTabContent.IsVisible = tab == WorkspaceTab.Agenda;
        NotesTabContent.IsVisible = tab == WorkspaceTab.Notes;
    }
    
    private void PrepTab_Click(object? sender, RoutedEventArgs e) => SetActiveTab(WorkspaceTab.Prep);
    private void AgendaTab_Click(object? sender, RoutedEventArgs e) => SetActiveTab(WorkspaceTab.Agenda);
    private void NotesTab_Click(object? sender, RoutedEventArgs e) => SetActiveTab(WorkspaceTab.Notes);
    
    #endregion
    
    #region Prep Items
    
    private void AddPrepItem_Click(object? sender, RoutedEventArgs e)
    {
        AddPrepPanel.IsVisible = true;
        NewPrepTitleTextBox.Text = "";
        NewPrepTitleTextBox.Focus();
    }
    
    private void CancelAddPrep_Click(object? sender, RoutedEventArgs e)
    {
        AddPrepPanel.IsVisible = false;
        NewPrepTitleTextBox.Text = "";
    }
    
    private async void ConfirmAddPrep_Click(object? sender, RoutedEventArgs e)
    {
        var title = NewPrepTitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title)) return;
        
        await AddPrepItemAsync(title);
        
        AddPrepPanel.IsVisible = false;
        NewPrepTitleTextBox.Text = "";
        PrepVisibilityComboBox.SelectedIndex = 0; // Reset to "Only me"
    }
    
    private void PrepVisibilityComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Show assignee dropdown when "Assigned person" is selected
        var selectedItem = PrepVisibilityComboBox.SelectedItem as ComboBoxItem;
        var visibility = selectedItem?.Tag?.ToString() ?? "personal";
        PrepAssigneePanel.IsVisible = visibility == "assigned";
    }
    
    private async void NewPrepTitleTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var title = NewPrepTitleTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(title))
            {
                await AddPrepItemAsync(title);
                NewPrepTitleTextBox.Text = "";
                // Keep panel open for rapid entry
            }
        }
        else if (e.Key == Key.Escape)
        {
            CancelAddPrep_Click(sender, e);
        }
    }
    
    private async Task AddPrepItemAsync(string title)
    {
        // Get visibility scope
        var visibilityItem = PrepVisibilityComboBox.SelectedItem as ComboBoxItem;
        var visibility = visibilityItem?.Tag?.ToString() ?? "personal";
        
        // Get assignee if applicable
        Guid? assigneeId = null;
        if (visibility == "assigned" && PrepAssigneeComboBox.SelectedItem is TeamMemberDetail assignee)
        {
            assigneeId = assignee.Id;
        }
        
        if (_existingMeeting != null)
        {
            // Meeting already exists - save to database
            MeetingPrepItem? newItem = null;
            
            if (visibility == "personal")
            {
                newItem = await MeetingPrepItemService.Instance.CreateQuickPrepAsync(
                    _existingMeeting.Id, title);
            }
            else if (visibility == "assigned" && assigneeId.HasValue)
            {
                newItem = await MeetingPrepItemService.Instance.CreateAssignedPrepAsync(
                    _existingMeeting.Id, title, assigneeId.Value);
            }
            else if (visibility == "meeting")
            {
                newItem = await MeetingPrepItemService.Instance.CreateTeamPrepAsync(
                    _existingMeeting.Id, title);
            }
            
            if (newItem != null)
            {
                _prepItems.Add(newItem);
            }
        }
        else
        {
            // New meeting - keep in memory until save
            _prepItems.Add(new MeetingPrepItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                VisibilityScope = visibility,
                AssignedToTeamMemberId = assigneeId,
                Status = "open",
                CreatedAt = DateTime.UtcNow
            });
        }
        UpdatePrepEmptyState();
    }
    
    /// <summary>
    /// Opens the entity picker to add a prep item from an existing task/goal/metric.
    /// </summary>
    private async void AddFromExistingPrep_Click(object? sender, RoutedEventArgs e)
    {
        var picker = new EntityPickerDialog();
        await picker.ShowDialog(this);
        
        if (picker.Result != null)
        {
            // Create a prep item with the linked entity title
            var title = $"Discuss: {picker.Result.EntityTitle}";
            
            // For now, just add as a simple prep item with the derived title
            // In the future, we could store the linked entity reference
            await AddPrepItemAsync(title);
        }
    }
    
    private async void PrepItem_CheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is MeetingPrepItem item)
        {
            if (_existingMeeting != null)
            {
                // Update in database
                var newStatus = item.IsComplete ? "completed" : "pending";
                await MeetingPrepItemService.Instance.UpdateStatusAsync(item.Id, newStatus);
            }
        }
    }
    
    private async void DeletePrepItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is MeetingPrepItem item)
        {
            if (_existingMeeting != null)
            {
                // Delete from database
                await MeetingPrepItemService.Instance.DeletePrepItemAsync(item.Id);
            }
            _prepItems.Remove(item);
            UpdatePrepEmptyState();
        }
    }
    
    private void UpdatePrepEmptyState()
    {
        PrepEmptyState.IsVisible = _prepItems.Count == 0;
    }
    
    #endregion
    
    #region Agenda Items
    
    private void AddAgendaItem_Click(object? sender, RoutedEventArgs e)
    {
        AddAgendaPanel.IsVisible = true;
        NewAgendaTitleTextBox.Text = "";
        NewAgendaTitleTextBox.Focus();
    }
    
    private void CancelAddAgenda_Click(object? sender, RoutedEventArgs e)
    {
        AddAgendaPanel.IsVisible = false;
        NewAgendaTitleTextBox.Text = "";
    }
    
    private void ConfirmAddAgenda_Click(object? sender, RoutedEventArgs e)
    {
        var title = NewAgendaTitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title)) return;
        
        AddAgendaItem(title);
        
        AddAgendaPanel.IsVisible = false;
        NewAgendaTitleTextBox.Text = "";
    }
    
    private void NewAgendaTitleTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var title = NewAgendaTitleTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(title))
            {
                AddAgendaItem(title);
                NewAgendaTitleTextBox.Text = "";
            }
        }
        else if (e.Key == Key.Escape)
        {
            CancelAddAgenda_Click(sender, e);
        }
    }
    
    private async void AddAgendaItem(string title, Guid? linkedEntityId = null, string? linkedEntityType = null, string? linkedEntityTitle = null)
    {
        var newItem = new DialogAgendaItem 
        { 
            Title = title,
            LinkedEntityId = linkedEntityId,
            LinkedEntityType = linkedEntityType,
            LinkedEntityTitle = linkedEntityTitle
        };
        
        _agendaItems.Add(newItem);
        UpdateAgendaEmptyState();
        
        // If meeting already exists, save to database
        if (_existingMeeting != null)
        {
            await MeetingAgendaItemService.Instance.CreateAgendaItemAsync(
                _existingMeeting.Id, 
                title,
                linkedEntityType: linkedEntityType,
                linkedEntityId: linkedEntityId);
        }
    }
    
    /// <summary>
    /// Opens the entity picker to link an existing task/goal/metric to the agenda.
    /// </summary>
    private async void LinkExistingAgendaItem_Click(object? sender, RoutedEventArgs e)
    {
        var picker = new EntityPickerDialog();
        await picker.ShowDialog(this);
        
        if (picker.Result != null)
        {
            // Create an agenda item linked to the selected entity
            var title = $"Discuss {picker.Result.EntityTitle}";
            AddAgendaItem(
                title, 
                picker.Result.EntityId, 
                picker.Result.EntityType,
                picker.Result.EntityTitle);
        }
    }
    
    private async void DeleteAgendaItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DialogAgendaItem item)
        {
            // If this was a saved item, delete from database
            if (_existingMeeting != null && item.Id != Guid.Empty)
            {
                await MeetingAgendaItemService.Instance.DeleteAgendaItemAsync(item.Id);
            }
            _agendaItems.Remove(item);
            UpdateAgendaEmptyState();
        }
    }
    
    private void UpdateAgendaEmptyState()
    {
        AgendaEmptyState.IsVisible = _agendaItems.Count == 0;
    }
    
    #endregion
    
    private void MeetingTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateAttendeeVisibility();
    }
    
    private void UpdateAttendeeVisibility()
    {
        var selectedItem = MeetingTypeComboBox.SelectedItem as ComboBoxItem;
        var meetingType = selectedItem?.Tag?.ToString() ?? "one_on_one";
        
        // Show attendee selector for 1:1 and performance meetings
        AttendeeSection.IsVisible = meetingType == "one_on_one" || meetingType == "performance";
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
    
    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isSaving) return;
        
        // Validate
        var title = TitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            TitleTextBox.Focus();
            return;
        }
        
        if (!DateTimeSelector.SelectedDateTime.HasValue)
        {
            DateTimeSelector.Focus();
            return;
        }
        
        _isSaving = true;
        SaveButton.IsEnabled = false;
        
        try
        {
            // Get meeting type
            var typeItem = MeetingTypeComboBox.SelectedItem as ComboBoxItem;
            var meetingType = typeItem?.Tag?.ToString() ?? "one_on_one";
            
            // Get duration
            var durationItem = DurationComboBox.SelectedItem as ComboBoxItem;
            var duration = int.Parse(durationItem?.Tag?.ToString() ?? "30");
            
            // Get datetime from selector
            var scheduledAt = DateTimeSelector.SelectedDateTime!.Value;
            
            // Get attendee for 1:1 meetings
            Guid? teamMemberId = null;
            if (AttendeeSection.IsVisible && AttendeeComboBox.SelectedItem is TeamMemberDetail member)
            {
                teamMemberId = member.Id;
            }
            
            MeetingDetail? savedMeeting;
            
            if (_existingMeeting != null)
            {
                // Update existing meeting
                _existingMeeting.Title = title;
                _existingMeeting.MeetingType = meetingType;
                _existingMeeting.ScheduledAt = scheduledAt;
                _existingMeeting.DurationMinutes = duration;
                _existingMeeting.Location = string.IsNullOrWhiteSpace(LocationTextBox.Text) ? null : LocationTextBox.Text.Trim();
                _existingMeeting.VideoLink = string.IsNullOrWhiteSpace(VideoLinkTextBox.Text) ? null : VideoLinkTextBox.Text.Trim();
                _existingMeeting.Notes = string.IsNullOrWhiteSpace(MeetingNotesTextBox.Text) ? null : MeetingNotesTextBox.Text.Trim();
                
                Debug.WriteLine($"[EditMeetingDialog] Updating meeting: {title}");
                var success = await MeetingService.Instance.UpdateMeetingAsync(_existingMeeting);
                
                if (success)
                {
                    // Update attendee if changed (for 1:1 meetings)
                    if (teamMemberId.HasValue && _existingMeeting.TeamMemberId != teamMemberId)
                    {
                        if (_existingMeeting.TeamMemberId.HasValue)
                        {
                            await MeetingService.Instance.RemoveAttendeeAsync(_existingMeeting.Id, _existingMeeting.TeamMemberId.Value);
                        }
                        await MeetingService.Instance.AddAttendeeAsync(_existingMeeting.Id, teamMemberId.Value, "attendee");
                    }
                    
                    savedMeeting = await MeetingService.Instance.GetMeetingAsync(_existingMeeting.Id);
                }
                else
                {
                    Result = new EditMeetingResult { Error = MeetingService.Instance.LastError ?? "Failed to update meeting" };
                    Debug.WriteLine($"[EditMeetingDialog] Update failed: {MeetingService.Instance.LastError}");
                    return;
                }
            }
            else
            {
                // Create new meeting
                var newMeeting = new MeetingDetail
                {
                    Title = title,
                    MeetingType = meetingType,
                    ScheduledAt = scheduledAt,
                    DurationMinutes = duration,
                    Location = string.IsNullOrWhiteSpace(LocationTextBox.Text) ? null : LocationTextBox.Text.Trim(),
                    VideoLink = string.IsNullOrWhiteSpace(VideoLinkTextBox.Text) ? null : VideoLinkTextBox.Text.Trim(),
                    Notes = string.IsNullOrWhiteSpace(MeetingNotesTextBox.Text) ? null : MeetingNotesTextBox.Text.Trim()
                };
                
                var attendeeIds = teamMemberId.HasValue ? new List<Guid> { teamMemberId.Value } : null;
                
                Debug.WriteLine($"[EditMeetingDialog] Creating meeting: {title}");
                savedMeeting = await MeetingService.Instance.CreateMeetingAsync(newMeeting, attendeeIds);
                
                if (savedMeeting == null)
                {
                    Result = new EditMeetingResult { Error = MeetingService.Instance.LastError ?? "Failed to create meeting" };
                    Debug.WriteLine($"[EditMeetingDialog] Create failed: {MeetingService.Instance.LastError}");
                    return;
                }
                
                // Save any prep items that were added before the meeting was created
                foreach (var prepItem in _prepItems)
                {
                    if (prepItem.VisibilityScope == "assigned" && prepItem.AssignedToTeamMemberId.HasValue)
                    {
                        await MeetingPrepItemService.Instance.CreateAssignedPrepAsync(
                            savedMeeting.Id, prepItem.Title, prepItem.AssignedToTeamMemberId.Value);
                    }
                    else if (prepItem.VisibilityScope == "meeting")
                    {
                        await MeetingPrepItemService.Instance.CreateTeamPrepAsync(
                            savedMeeting.Id, prepItem.Title);
                    }
                    else
                    {
                        await MeetingPrepItemService.Instance.CreateQuickPrepAsync(
                            savedMeeting.Id, prepItem.Title);
                    }
                }
                
                // Save any agenda items that were added before the meeting was created
                foreach (var agendaItem in _agendaItems)
                {
                    await MeetingAgendaItemService.Instance.CreateAgendaItemAsync(
                        savedMeeting.Id, 
                        agendaItem.Title,
                        linkedEntityType: agendaItem.LinkedEntityType,
                        linkedEntityId: agendaItem.LinkedEntityId);
                }
            }
            
            Result = new EditMeetingResult
            {
                SavedMeeting = savedMeeting,
                IsDeleted = false
            };
            
            Debug.WriteLine($"[EditMeetingDialog] Meeting saved successfully: {savedMeeting?.Id}");
            Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EditMeetingDialog] Error saving meeting: {ex.Message}");
            Result = new EditMeetingResult { Error = ex.Message };
        }
        finally
        {
            _isSaving = false;
            SaveButton.IsEnabled = true;
        }
    }
    
    private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_existingMeeting == null) return;
        
        // TODO: Show confirmation dialog
        
        Debug.WriteLine($"[EditMeetingDialog] Deleting meeting: {_existingMeeting.Id}");
        var success = await MeetingService.Instance.DeleteMeetingAsync(_existingMeeting.Id);
        
        if (success)
        {
            Result = new EditMeetingResult
            {
                IsDeleted = true,
                DeletedMeetingId = _existingMeeting.Id
            };
        }
        else
        {
            Result = new EditMeetingResult
            {
                Error = MeetingService.Instance.LastError ?? "Failed to delete meeting"
            };
        }
        
        Close();
    }
}
