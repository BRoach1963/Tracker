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
/// Enhanced to be a "conversation container" with context, talking points, and outcomes.
/// </summary>
public class DialogAgendaItem : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    
    public Guid Id { get; set; } = Guid.NewGuid();
    
    private string _title = string.Empty;
    public string Title 
    { 
        get => _title;
        set { _title = value; OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(EffectiveTitle)); }
    }
    
    private string? _displayTitle;
    /// <summary>
    /// Optional styled/formatted title independent of linked entity.
    /// </summary>
    public string? DisplayTitle 
    { 
        get => _displayTitle;
        set { _displayTitle = value; OnPropertyChanged(nameof(DisplayTitle)); OnPropertyChanged(nameof(EffectiveTitle)); }
    }
    
    private string? _sharedContext;
    /// <summary>
    /// Context visible to all meeting attendees.
    /// "Why are we talking about this?" / "What's changed?"
    /// </summary>
    public string? SharedContext 
    { 
        get => _sharedContext;
        set { _sharedContext = value; OnPropertyChanged(nameof(SharedContext)); OnPropertyChanged(nameof(HasContext)); }
    }
    
    private string? _privateContext;
    /// <summary>
    /// Creator-only notes not visible to other attendees.
    /// </summary>
    public string? PrivateContext 
    { 
        get => _privateContext;
        set { _privateContext = value; OnPropertyChanged(nameof(PrivateContext)); OnPropertyChanged(nameof(HasContext)); }
    }
    
    private string _visibilityScope = "meeting";
    /// <summary>
    /// Visibility: 'meeting' (shared with attendees) or 'personal' (private reminder).
    /// </summary>
    public string VisibilityScope 
    { 
        get => _visibilityScope;
        set { _visibilityScope = value; OnPropertyChanged(nameof(VisibilityScope)); OnPropertyChanged(nameof(IsPersonalAgenda)); OnPropertyChanged(nameof(VisibilityIcon)); }
    }
    
    // Linked entity (optional - for discussing existing tasks/goals/metrics)
    public Guid? LinkedEntityId { get; set; }
    public string? LinkedEntityType { get; set; } // "task", "goal", "metric", "project"
    public string? LinkedEntityTitle { get; set; }
    public string? LinkedEntityTitleSnapshot { get; set; }
    
    // Outcome tracking (captured during/after meeting)
    private string? _outcomeType;
    public string? OutcomeType 
    { 
        get => _outcomeType;
        set { _outcomeType = value; OnPropertyChanged(nameof(OutcomeType)); OnPropertyChanged(nameof(HasOutcome)); OnPropertyChanged(nameof(OutcomeTypeDisplay)); OnPropertyChanged(nameof(OutcomeBadgeColor)); }
    }
    
    private string? _outcomeSummary;
    public string? OutcomeSummary 
    { 
        get => _outcomeSummary;
        set { _outcomeSummary = value; OnPropertyChanged(nameof(OutcomeSummary)); OnPropertyChanged(nameof(HasOutcome)); }
    }
    
    // Talking points (JSON stored but edited as list)
    private List<TalkingPoint> _talkingPoints = new();
    public List<TalkingPoint> TalkingPoints 
    { 
        get => _talkingPoints;
        set { _talkingPoints = value ?? new(); OnPropertyChanged(nameof(TalkingPoints)); OnPropertyChanged(nameof(HasTalkingPoints)); OnPropertyChanged(nameof(TalkingPointsCount)); }
    }
    
    // Computed properties
    public bool HasLinkedEntity => LinkedEntityId.HasValue && !string.IsNullOrEmpty(LinkedEntityType);
    public bool IsPersonalAgenda => VisibilityScope == "personal";
    public bool HasContext => !string.IsNullOrWhiteSpace(SharedContext) || !string.IsNullOrWhiteSpace(PrivateContext);
    public bool HasTalkingPoints => TalkingPoints.Count > 0;
    public int TalkingPointsCount => TalkingPoints.Count;
    public bool HasOutcome => !string.IsNullOrWhiteSpace(OutcomeType);
    
    /// <summary>
    /// Effective title for display - prefers DisplayTitle, falls back to LinkedEntityTitleSnapshot or Title.
    /// </summary>
    public string EffectiveTitle => !string.IsNullOrWhiteSpace(DisplayTitle)
        ? DisplayTitle
        : !string.IsNullOrWhiteSpace(LinkedEntityTitleSnapshot)
            ? LinkedEntityTitleSnapshot
            : Title;
    
    public string VisibilityIcon => IsPersonalAgenda
        ? "M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17M18,8A2,2 0 0,1 20,10V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V10C4,8.89 4.9,8 6,8H7V6A5,5 0 0,1 12,1A5,5 0 0,1 17,6V8H18M12,3A3,3 0 0,0 9,6V8H15V6A3,3 0 0,0 12,3Z"  // Lock
        : "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z"; // People
    
    public string VisibilityTooltip => IsPersonalAgenda
        ? "Personal reminder - only you can see this"
        : "Shared with meeting attendees";
    
    /// <summary>
    /// Display text for outcome type - matches DB constraint values.
    /// </summary>
    public string OutcomeTypeDisplay => OutcomeType?.ToLower() switch
    {
        "discussed" => "Discussed",
        "decision" => "Decision",
        "deferred" => "Deferred",
        "blocked" => "Blocked",
        _ => ""
    };
    
    /// <summary>
    /// Badge color based on outcome type.
    /// </summary>
    public global::Avalonia.Media.IBrush OutcomeBadgeColor => OutcomeType?.ToLower() switch
    {
        "discussed" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#27AE60")), // Green
        "decision" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#3498DB")),  // Blue
        "deferred" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#F39C12")),  // Orange
        "blocked" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#E74C3C")),   // Red
        _ => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#7F8C8D"))
    };
    
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
    
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
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
    private List<TeamMemberDetail> _currentAttendees = new(); // Track current meeting attendees for prep assignment
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
        
        // Wire up attendee change to update prep assignee list
        AttendeeComboBox.SelectionChanged += AttendeeComboBox_SelectionChanged;
        
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
                    DisplayTitle = item.DisplayTitle,
                    SharedContext = item.SharedContext,
                    PrivateContext = item.PrivateContext,
                    VisibilityScope = item.VisibilityScope ?? "meeting",
                    LinkedEntityId = item.LinkedEntityId,
                    LinkedEntityType = item.LinkedEntityType,
                    LinkedEntityTitle = item.LinkedEntityTitle,
                    LinkedEntityTitleSnapshot = item.LinkedEntityTitleSnapshot,
                    OutcomeType = item.OutcomeType,
                    OutcomeSummary = item.OutcomeSummary,
                    TalkingPoints = item.TalkingPoints
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
        // Note: PrepAssigneeComboBox is populated by UpdatePrepAssigneeList() based on current attendees
        
        // Populate team attendees list (excluding self)
        var teamAttendeesListBox = this.FindControl<ItemsControl>("TeamAttendeesListBox");
        if (teamAttendeesListBox != null)
        {
            // Filter to only non-self members (direct reports and others)
            var selectableMembers = _teamMembers.Where(m => m.Relation != "self").ToList();
            
            // Reset selection state
            foreach (var member in selectableMembers)
            {
                member.IsSelected = false;
            }
            
            teamAttendeesListBox.ItemsSource = selectableMembers;
        }
        
        // If editing and we have a team member id, select it
        if (_existingMeeting?.TeamMemberId.HasValue == true)
        {
            var attendee = _teamMembers.FirstOrDefault(t => t.Id == _existingMeeting.TeamMemberId.Value);
            if (attendee != null)
            {
                AttendeeComboBox.SelectedItem = attendee;
            }
        }
        
        // Initialize attendees from existing meeting if editing
        if (_existingMeeting?.Attendees != null && _existingMeeting.Attendees.Count > 0)
        {
            // Map meeting attendees to team member details
            _currentAttendees = _existingMeeting.Attendees
                .Select(a => _teamMembers.FirstOrDefault(t => t.Id == a.TeamMemberId))
                .Where(t => t != null)
                .Cast<TeamMemberDetail>()
                .ToList();
        }
        
        // Update prep assignee dropdown
        UpdatePrepAssigneeList();
    }
    
    /// <summary>
    /// Updates the prep assignee dropdown to show only current meeting attendees.
    /// </summary>
    private void UpdatePrepAssigneeList()
    {
        // Build list of assignable team members based on current attendees
        var assignableMembers = new List<TeamMemberDetail>();
        
        // Get meeting type
        var meetingTypeItem = MeetingTypeComboBox.SelectedItem as ComboBoxItem;
        var meetingType = meetingTypeItem?.Tag?.ToString() ?? "one_on_one";
        
        if (meetingType == "one_on_one" || meetingType == "performance")
        {
            // For 1:1 meetings, only the selected attendee can be assigned
            if (AttendeeComboBox.SelectedItem is TeamMemberDetail selectedAttendee)
            {
                assignableMembers.Add(selectedAttendee);
            }
            // Also add self (current user)
            var self = _teamMembers.FirstOrDefault(t => t.Relation == "self");
            if (self != null && !assignableMembers.Contains(self))
            {
                assignableMembers.Insert(0, self);
            }
        }
        else if (meetingType == "team" || meetingType == "project")
        {
            // For team meetings, use selected team members
            var selectedTeamMembers = _teamMembers.Where(m => m.IsSelected && m.Relation != "self").ToList();
            assignableMembers.AddRange(selectedTeamMembers);
            
            // Also add self (current user)
            var self = _teamMembers.FirstOrDefault(t => t.Relation == "self");
            if (self != null)
            {
                assignableMembers.Insert(0, self);
            }
        }
        else
        {
            // For other meeting types, fall back to all team members
            assignableMembers = _teamMembers.ToList();
        }
        
        // If editing existing meeting, use actual attendees
        if (_currentAttendees.Count > 0)
        {
            assignableMembers = _currentAttendees.ToList();
        }
        
        // Update the dropdown
        PrepAssigneeComboBox.ItemsSource = assignableMembers;
        
        // Update the "no attendees" warning visibility
        UpdatePrepAssigneeWarning(assignableMembers.Count <= 1); // <= 1 because self is always included
    }
    
    /// <summary>
    /// Shows/hides the warning when there are no attendees to assign prep to.
    /// </summary>
    private void UpdatePrepAssigneeWarning(bool showWarning)
    {
        // Find or create a warning text block
        var warningBlock = this.FindControl<TextBlock>("PrepAssigneeWarning");
        if (warningBlock != null)
        {
            warningBlock.IsVisible = showWarning && PrepAssigneePanel.IsVisible;
            warningBlock.Text = "Add attendees to assign prep items";
        }
    }
    
    /// <summary>
    /// Handles attendee selection change (for 1:1 meetings).
    /// </summary>
    private void AttendeeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdatePrepAssigneeList();
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
        NewPrepPromptTextBox.Text = "";
        NewPrepBodyTextBox.Text = "";
        PrepVisibilityComboBox.SelectedIndex = 0;
        NewPrepTitleTextBox.Focus();
    }
    
    private void CancelAddPrep_Click(object? sender, RoutedEventArgs e)
    {
        AddPrepPanel.IsVisible = false;
        NewPrepTitleTextBox.Text = "";
        NewPrepPromptTextBox.Text = "";
        NewPrepBodyTextBox.Text = "";
    }
    
    private async void ConfirmAddPrep_Click(object? sender, RoutedEventArgs e)
    {
        var title = NewPrepTitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title)) return;
        
        var prepPrompt = NewPrepPromptTextBox.Text?.Trim();
        var body = NewPrepBodyTextBox.Text?.Trim();
        
        await AddPrepItemAsync(title, prepPrompt, body);
        
        AddPrepPanel.IsVisible = false;
        NewPrepTitleTextBox.Text = "";
        NewPrepPromptTextBox.Text = "";
        NewPrepBodyTextBox.Text = "";
        PrepVisibilityComboBox.SelectedIndex = 0; // Reset to "Meeting" (default scope)
    }
    
    private void PrepVisibilityComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Guard against event firing before UI is fully loaded
        if (PrepVisibilityComboBox == null || PrepAssigneePanel == null) return;
        
        // Show assignee dropdown when "Assigned person" is selected
        var selectedItem = PrepVisibilityComboBox.SelectedItem as ComboBoxItem;
        var visibility = selectedItem?.Tag?.ToString() ?? "personal";
        PrepAssigneePanel.IsVisible = visibility == "assigned";
        
        // Update assignee list to ensure it's filtered to current attendees
        if (visibility == "assigned")
        {
            UpdatePrepAssigneeList();
        }
    }
    
    private async void NewPrepTitleTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrEmpty(NewPrepPromptTextBox.Text?.Trim()))
        {
            // If there's also a prompt, Ctrl+Enter to submit
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                var title = NewPrepTitleTextBox.Text?.Trim();
                if (!string.IsNullOrEmpty(title))
                {
                    var prepPrompt = NewPrepPromptTextBox.Text?.Trim();
                    var body = NewPrepBodyTextBox.Text?.Trim();
                    await AddPrepItemAsync(title, prepPrompt, body);
                    NewPrepTitleTextBox.Text = "";
                    NewPrepPromptTextBox.Text = "";
                    NewPrepBodyTextBox.Text = "";
                }
            }
        }
        else if (e.Key == Key.Enter)
        {
            var title = NewPrepTitleTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(title))
            {
                await AddPrepItemAsync(title, null, null);
                NewPrepTitleTextBox.Text = "";
                // Keep panel open for rapid entry
            }
        }
        else if (e.Key == Key.Escape)
        {
            CancelAddPrep_Click(sender, e);
        }
    }
    
    private async Task AddPrepItemAsync(string title, string? prepPrompt = null, string? body = null)
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
            var newItem = new MeetingPrepItem
            {
                MeetingId = _existingMeeting.Id,
                Title = title,
                PrepPrompt = prepPrompt,
                Body = body,
                VisibilityScope = visibility,
                AssignedToTeamMemberId = assigneeId,
                Status = "open"
            };
            
            var created = await MeetingPrepItemService.Instance.CreatePrepItemAsync(newItem);
            if (created != null)
            {
                _prepItems.Add(created);
            }
        }
        else
        {
            // New meeting - keep in memory until save
            _prepItems.Add(new MeetingPrepItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                PrepPrompt = prepPrompt,
                Body = body,
                VisibilityScope = visibility,
                AssignedToTeamMemberId = assigneeId,
                Status = "open",
                CreatedAt = DateTime.UtcNow
            });
        }
        UpdatePrepEmptyState();
    }
    
    /// <summary>
    /// Opens edit dialog for a prep item.
    /// </summary>
    private async void EditPrepItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is MeetingPrepItem item)
        {
            var dialog = new EditPrepItemDialog(item);
            
            // Pass meeting attendees and current user's team member ID to the dialog
            if (_existingMeeting?.Attendees != null)
            {
                var currentTeamMemberId = AuthService.Instance.CurrentTeamMember?.Id ?? Guid.Empty;
                dialog.SetAttendees(_existingMeeting.Attendees, currentTeamMemberId);
            }
            
            var result = await dialog.ShowDialog<bool>(this);
            
            if (result)
            {
                // Refresh the item in the list
                var index = _prepItems.IndexOf(item);
                if (index >= 0)
                {
                    _prepItems[index] = dialog.UpdatedItem ?? item;
                }
            }
        }
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
            await AddLinkedPrepItemAsync(
                entityType: picker.Result.EntityType,
                entityId: picker.Result.EntityId,
                entityTitle: picker.Result.EntityTitle);
        }
    }
    
    /// <summary>
    /// Adds a prep item linked to an existing entity (task/goal/metric).
    /// </summary>
    private async Task AddLinkedPrepItemAsync(string entityType, Guid entityId, string entityTitle)
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
            var newItem = new MeetingPrepItem
            {
                MeetingId = _existingMeeting.Id,
                Title = $"Discuss: {entityTitle}",
                LinkedEntityType = entityType,
                LinkedEntityId = entityId,
                LinkedEntityTitleSnapshot = entityTitle,
                VisibilityScope = visibility,
                AssignedToTeamMemberId = assigneeId,
                Status = "open"
            };
            
            var created = await MeetingPrepItemService.Instance.CreatePrepItemAsync(newItem);
            if (created != null)
            {
                _prepItems.Add(created);
            }
        }
        else
        {
            // New meeting - keep in memory until save
            _prepItems.Add(new MeetingPrepItem
            {
                Id = Guid.NewGuid(),
                Title = $"Discuss: {entityTitle}",
                LinkedEntityType = entityType,
                LinkedEntityId = entityId,
                LinkedEntityTitleSnapshot = entityTitle,
                VisibilityScope = visibility,
                AssignedToTeamMemberId = assigneeId,
                Status = "open",
                CreatedAt = DateTime.UtcNow
            });
        }
        UpdatePrepEmptyState();
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
        NewAgendaContextTextBox.Text = "";
        NewAgendaTalkingPointsTextBox.Text = "";
        AgendaVisibilityComboBox.SelectedIndex = 0; // Default to "All attendees"
        NewAgendaTitleTextBox.Focus();
    }
    
    private void CancelAddAgenda_Click(object? sender, RoutedEventArgs e)
    {
        AddAgendaPanel.IsVisible = false;
        NewAgendaTitleTextBox.Text = "";
        NewAgendaContextTextBox.Text = "";
        NewAgendaTalkingPointsTextBox.Text = "";
    }
    
    private void ConfirmAddAgenda_Click(object? sender, RoutedEventArgs e)
    {
        var title = NewAgendaTitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title)) return;
        
        // Get visibility scope from combo
        var visibilityItem = AgendaVisibilityComboBox.SelectedItem as ComboBoxItem;
        var visibility = visibilityItem?.Tag?.ToString() ?? "meeting";
        
        // Get shared context
        var sharedContext = NewAgendaContextTextBox.Text?.Trim();
        
        // Parse talking points from multi-line text
        var talkingPointsText = NewAgendaTalkingPointsTextBox.Text?.Trim();
        var talkingPoints = ParseTalkingPointsFromText(talkingPointsText);
        
        AddAgendaItem(title, visibilityScope: visibility, sharedContext: sharedContext, talkingPoints: talkingPoints);
        
        AddAgendaPanel.IsVisible = false;
        NewAgendaTitleTextBox.Text = "";
        NewAgendaContextTextBox.Text = "";
        NewAgendaTalkingPointsTextBox.Text = "";
    }
    
    /// <summary>
    /// Parses multi-line text into TalkingPoint objects.
    /// Each non-empty line becomes a talking point.
    /// </summary>
    private static List<TalkingPoint> ParseTalkingPointsFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<TalkingPoint>();
        
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var points = new List<TalkingPoint>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line))
            {
                points.Add(TalkingPoint.Create(line, i));
            }
        }
        
        return points;
    }
    
    private void NewAgendaTitleTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var title = NewAgendaTitleTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(title))
            {
                // Get visibility scope
                var visibilityItem = AgendaVisibilityComboBox.SelectedItem as ComboBoxItem;
                var visibility = visibilityItem?.Tag?.ToString() ?? "meeting";
                
                // Get shared context
                var sharedContext = NewAgendaContextTextBox.Text?.Trim();
                
                // Parse talking points from multi-line text
                var talkingPointsText = NewAgendaTalkingPointsTextBox.Text?.Trim();
                var talkingPoints = ParseTalkingPointsFromText(talkingPointsText);
                
                AddAgendaItem(title, visibilityScope: visibility, sharedContext: sharedContext, talkingPoints: talkingPoints);
                NewAgendaTitleTextBox.Text = "";
                NewAgendaContextTextBox.Text = "";
                NewAgendaTalkingPointsTextBox.Text = "";
                AddAgendaPanel.IsVisible = false;
            }
        }
        else if (e.Key == Key.Escape)
        {
            CancelAddAgenda_Click(sender, e);
        }
    }
    
    private async void AddAgendaItem(
        string title, 
        Guid? linkedEntityId = null, 
        string? linkedEntityType = null, 
        string? linkedEntityTitle = null,
        string visibilityScope = "meeting",
        string? sharedContext = null,
        List<TalkingPoint>? talkingPoints = null)
    {
        var newItem = new DialogAgendaItem 
        { 
            Title = title,
            LinkedEntityId = linkedEntityId,
            LinkedEntityType = linkedEntityType,
            LinkedEntityTitle = linkedEntityTitle,
            LinkedEntityTitleSnapshot = linkedEntityTitle, // Snapshot at link time
            VisibilityScope = visibilityScope,
            SharedContext = sharedContext,
            TalkingPoints = talkingPoints ?? new List<TalkingPoint>()
        };
        
        _agendaItems.Add(newItem);
        UpdateAgendaEmptyState();
        
        // If meeting already exists, save to database
        if (_existingMeeting != null)
        {
            var savedItem = await MeetingAgendaItemService.Instance.CreateAgendaItemAsync(
                _existingMeeting.Id, 
                title,
                linkedEntityType: linkedEntityType,
                linkedEntityId: linkedEntityId,
                linkedEntityTitleSnapshot: linkedEntityTitle,
                visibilityScope: visibilityScope,
                sharedContext: sharedContext,
                talkingPoints: talkingPoints);
            
            // Update the local item with the saved ID
            if (savedItem != null)
            {
                newItem.Id = savedItem.Id;
            }
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
    
    /// <summary>
    /// Opens the edit agenda item dialog to modify context, talking points, etc.
    /// </summary>
    private async void EditAgendaItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DialogAgendaItem item)
        {
            var dialog = new EditAgendaItemDialog(item);
            await dialog.ShowDialog(this);
            
            if (dialog.Result != null && dialog.Result.WasSaved)
            {
                // Update local item with dialog changes
                item.Title = dialog.Result.Title;
                item.DisplayTitle = dialog.Result.DisplayTitle;
                item.SharedContext = dialog.Result.SharedContext;
                item.PrivateContext = dialog.Result.PrivateContext;
                item.VisibilityScope = dialog.Result.VisibilityScope;
                item.TalkingPoints = dialog.Result.TalkingPoints;
                
                // Persist to database if meeting exists
                if (_existingMeeting != null && item.Id != Guid.Empty)
                {
                    await MeetingAgendaItemService.Instance.UpdateAgendaItemAsync(
                        item.Id,
                        title: item.Title,
                        displayTitle: item.DisplayTitle,
                        sharedContext: item.SharedContext,
                        privateContext: item.PrivateContext,
                        visibilityScope: item.VisibilityScope,
                        talkingPoints: item.TalkingPoints);
                }
            }
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
        UpdateMeetingTypeDescription();
        UpdatePrepAssigneeList(); // Refresh assignee dropdown when meeting type changes
    }
    
    private void UpdateAttendeeVisibility()
    {
        var selectedItem = MeetingTypeComboBox.SelectedItem as ComboBoxItem;
        var meetingType = selectedItem?.Tag?.ToString() ?? "one_on_one";
        
        // Show single attendee selector for 1:1 and performance meetings
        AttendeeSection.IsVisible = meetingType == "one_on_one" || meetingType == "performance";
        
        // Show team attendees selector for team meetings
        var teamAttendeesSection = this.FindControl<StackPanel>("TeamAttendeesSection");
        if (teamAttendeesSection != null)
        {
            teamAttendeesSection.IsVisible = meetingType == "team" || meetingType == "project";
        }
    }
    
    private void UpdateMeetingTypeDescription()
    {
        var selectedItem = MeetingTypeComboBox.SelectedItem as ComboBoxItem;
        var meetingType = selectedItem?.Tag?.ToString() ?? "one_on_one";
        
        var description = meetingType switch
        {
            "one_on_one" => "Private conversation between you and one person",
            "team" => "Meeting with your team—add attendees from your direct reports",
            "project" => "Review progress and discuss blockers with project stakeholders",
            "performance" => "Confidential discussion about performance and growth",
            "other" => "General meeting—customize attendees as needed",
            _ => ""
        };
        
        if (this.FindControl<TextBlock>("MeetingTypeDescription") is TextBlock descBlock)
        {
            descBlock.Text = description;
        }
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
    
    private void CancelBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Result = null;
        Close();
    }
    
    private void SaveBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Delegate to the existing save logic
        SaveButton_Click(sender, new RoutedEventArgs());
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
        SaveBorder.IsHitTestVisible = false;
        SaveBorder.Opacity = 0.5;
        
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
            
            // Get attendees for team meetings
            List<Guid>? teamAttendeeIds = null;
            var teamAttendeesSection = this.FindControl<StackPanel>("TeamAttendeesSection");
            if (teamAttendeesSection?.IsVisible == true)
            {
                var teamAttendeesListBox = this.FindControl<ItemsControl>("TeamAttendeesListBox");
                if (teamAttendeesListBox?.ItemsSource is IEnumerable<TeamMemberDetail> teamMembers)
                {
                    teamAttendeeIds = teamMembers.Where(m => m.IsSelected).Select(m => m.Id).ToList();
                    if (teamAttendeeIds.Count == 0)
                    {
                        teamAttendeeIds = null;
                    }
                }
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
                
                var attendeeIds = teamMemberId.HasValue 
                    ? new List<Guid> { teamMemberId.Value } 
                    : teamAttendeeIds;
                
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
            SaveBorder.IsHitTestVisible = true;
            SaveBorder.Opacity = 1.0;
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
