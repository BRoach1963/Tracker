using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// Tabs available in the meeting workspace area.
/// </summary>
public enum WorkspaceTab
{
    Prep,
    Agenda,
    Notes
}

/// <summary>
/// ViewModel for the Edit/Create Meeting dialog.
/// Handles all business logic, validation, and service calls.
/// </summary>
public partial class EditMeetingDialogViewModel : ObservableObject
{
    #region Fields
    
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "meeting_dialog.log");
    
    private MeetingDetail? _existingMeeting;
    private List<Team> _availableTeams = new();
    
    #endregion
    
    #region Observable Properties - State
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(DialogTitle))]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private Guid? _meetingId;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPrepTabActive))]
    [NotifyPropertyChangedFor(nameof(IsAgendaTabActive))]
    [NotifyPropertyChangedFor(nameof(IsNotesTabActive))]
    private WorkspaceTab _activeTab = WorkspaceTab.Prep;
    
    #endregion
    
    #region Observable Properties - Meeting Details
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _title = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAttendeeSelector))]
    [NotifyPropertyChangedFor(nameof(ShowTeamAttendeesSelector))]
    [NotifyPropertyChangedFor(nameof(MeetingTypeDescription))]
    private string _meetingType = "one_on_one";
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime? _scheduledDateTime;
    
    [ObservableProperty]
    private int _durationMinutes = 30;
    
    [ObservableProperty]
    private string? _location;
    
    [ObservableProperty]
    private string? _videoLink;
    
    [ObservableProperty]
    private string? _notes;
    
    [ObservableProperty]
    private TeamMemberDetail? _selectedAttendee;
    
    [ObservableProperty]
    private Team? _selectedTeam;
    
    #endregion
    
    #region Observable Properties - Add Prep Panel
    
    [ObservableProperty]
    private bool _isAddPrepPanelVisible;
    
    [ObservableProperty]
    private string _newPrepTitle = string.Empty;
    
    [ObservableProperty]
    private string? _newPrepPrompt;
    
    [ObservableProperty]
    private string? _newPrepBody;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPrepAssigneeSelector))]
    private string _newPrepVisibility = "personal";
    
    [ObservableProperty]
    private TeamMemberDetail? _newPrepAssignee;
    
    #endregion
    
    #region Observable Properties - Add Agenda Panel
    
    [ObservableProperty]
    private bool _isAddAgendaPanelVisible;
    
    [ObservableProperty]
    private string _newAgendaTitle = string.Empty;
    
    [ObservableProperty]
    private string? _newAgendaContext;
    
    [ObservableProperty]
    private string? _newAgendaTalkingPoints;
    
    [ObservableProperty]
    private string _newAgendaVisibility = "meeting";
    
    #endregion
    
    #region Collections
    
    public ObservableCollection<TeamMemberDetail> TeamMembers { get; } = new();
    public ObservableCollection<TeamMemberDetail> SelectableAttendees { get; } = new();
    public ObservableCollection<TeamMemberDetail> PrepAssignees { get; } = new();
    public ObservableCollection<MeetingPrepItem> PrepItems { get; } = new();
    public ObservableCollection<DialogAgendaItem> AgendaItems { get; } = new();
    public ObservableCollection<Team> AvailableTeams { get; } = new();
    
    #endregion
    
    #region Computed Properties
    
    public bool IsEditing => MeetingId.HasValue;
    public bool CanDelete => IsEditing;
    public string DialogTitle => IsEditing ? "Edit Meeting" : "New Meeting";
    
    public bool ShowAttendeeSelector => MeetingType == "one_on_one" || MeetingType == "performance";
    public bool ShowTeamAttendeesSelector => MeetingType == "team" || MeetingType == "project";
    public bool ShowPrepAssigneeSelector => NewPrepVisibility == "assigned";
    
    public bool IsPrepTabActive => ActiveTab == WorkspaceTab.Prep;
    public bool IsAgendaTabActive => ActiveTab == WorkspaceTab.Agenda;
    public bool IsNotesTabActive => ActiveTab == WorkspaceTab.Notes;
    
    public bool HasPrepItems => PrepItems.Count > 0;
    public bool HasAgendaItems => AgendaItems.Count > 0;
    
    public string MeetingTypeDescription => MeetingType switch
    {
        "one_on_one" => "Private conversation between you and one person",
        "team" => "Meeting with your team—add attendees from your direct reports",
        "project" => "Review progress and discuss blockers with project stakeholders",
        "performance" => "Confidential discussion about performance and growth",
        "other" => "General meeting—customize attendees as needed",
        _ => ""
    };
    
    #endregion
    
    #region Result
    
    /// <summary>
    /// The result of the dialog operation. Set before closing.
    /// </summary>
    public EditMeetingResult? Result { get; private set; }
    
    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler? CloseRequested;
    
    #endregion
    
    #region Constructor
    
    public EditMeetingDialogViewModel()
    {
        // Set default date/time to next hour
        var now = DateTime.Now;
        ScheduledDateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0);
        
        // Subscribe to collection changes to update empty states
        PrepItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasPrepItems));
        AgendaItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAgendaItems));
    }
    
    #endregion
    
    #region Public Methods - Initialization
    
    /// <summary>
    /// Initialize the ViewModel with team members for attendee selection.
    /// Call this before showing the dialog.
    /// </summary>
    public void Initialize(IEnumerable<TeamMemberDetail> teamMembers)
    {
        TeamMembers.Clear();
        SelectableAttendees.Clear();
        
        foreach (var member in teamMembers)
        {
            TeamMembers.Add(member);
            if (member.Relation != "self")
            {
                member.IsSelected = false;
                SelectableAttendees.Add(member);
            }
        }
        
        UpdatePrepAssigneeList();
        _ = LoadTeamsAsync();
    }
    
    /// <summary>
    /// Load an existing meeting for editing.
    /// </summary>
    public async Task LoadMeetingAsync(MeetingDetail meeting)
    {
        _existingMeeting = meeting;
        MeetingId = meeting.Id;
        
        Title = meeting.Title;
        MeetingType = meeting.MeetingType;
        ScheduledDateTime = meeting.ScheduledAt;
        DurationMinutes = meeting.DurationMinutes ?? 30;
        Location = meeting.Location;
        VideoLink = meeting.VideoLink;
        Notes = meeting.Notes;
        
        // Set attendee for 1:1 meetings
        if (meeting.TeamMemberId.HasValue)
        {
            SelectedAttendee = TeamMembers.FirstOrDefault(t => t.Id == meeting.TeamMemberId.Value);
        }
        
        // Mark existing attendees as selected for team meetings
        if (meeting.Attendees != null)
        {
            var attendeeIds = meeting.Attendees.Select(a => a.TeamMemberId).ToHashSet();
            foreach (var member in SelectableAttendees)
            {
                member.IsSelected = attendeeIds.Contains(member.Id);
            }
        }
        
        UpdatePrepAssigneeList();
        
        // Load prep and agenda items
        await LoadPrepItemsAsync();
        await LoadAgendaItemsAsync();
    }
    
    #endregion
    
    #region Commands - Tab Navigation
    
    [RelayCommand]
    private void SetPrepTab() => ActiveTab = WorkspaceTab.Prep;
    
    [RelayCommand]
    private void SetAgendaTab() => ActiveTab = WorkspaceTab.Agenda;
    
    [RelayCommand]
    private void SetNotesTab() => ActiveTab = WorkspaceTab.Notes;
    
    #endregion
    
    #region Commands - Prep Items
    
    [RelayCommand]
    private void ShowAddPrepPanel()
    {
        NewPrepTitle = string.Empty;
        NewPrepPrompt = null;
        NewPrepBody = null;
        NewPrepVisibility = "personal";
        NewPrepAssignee = null;
        IsAddPrepPanelVisible = true;
    }
    
    [RelayCommand]
    private void CancelAddPrep()
    {
        IsAddPrepPanelVisible = false;
    }
    
    [RelayCommand]
    private async Task ConfirmAddPrepAsync()
    {
        Log($"ConfirmAddPrepAsync called. NewPrepTitle='{NewPrepTitle}'");
        
        if (string.IsNullOrWhiteSpace(NewPrepTitle))
        {
            Log("NewPrepTitle is empty, returning");
            return;
        }
        
        Log($"Before AddPrepItemAsync. PrepItems.Count={PrepItems.Count}");
        await AddPrepItemAsync(NewPrepTitle.Trim(), NewPrepPrompt?.Trim(), NewPrepBody?.Trim());
        Log($"After AddPrepItemAsync. PrepItems.Count={PrepItems.Count}");
        
        IsAddPrepPanelVisible = false;
        NewPrepTitle = string.Empty;
        NewPrepPrompt = null;
        NewPrepBody = null;
        
        Log($"HasPrepItems={HasPrepItems}");
    }
    
    [RelayCommand]
    private async Task DeletePrepItemAsync(MeetingPrepItem item)
    {
        if (_existingMeeting != null)
        {
            await MeetingPrepItemService.Instance.DeletePrepItemAsync(item.Id);
        }
        PrepItems.Remove(item);
    }
    
    [RelayCommand]
    private async Task AddFromExistingPrepAsync(EntityPickerResult? result)
    {
        if (result == null) return;
        
        await AddLinkedPrepItemAsync(result.EntityType, result.EntityId, result.EntityTitle);
    }
    
    /// <summary>
    /// Event raised when entity picker should be shown for prep items.
    /// The View handles showing the dialog and calling AddFromExistingPrepCommand with result.
    /// </summary>
    public event EventHandler? EntityPickerForPrepRequested;
    
    [RelayCommand]
    private void ShowEntityPickerForPrep()
    {
        EntityPickerForPrepRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Event raised when a prep item should be edited.
    /// The View handles showing the edit dialog.
    /// </summary>
    public event EventHandler<MeetingPrepItem>? EditPrepItemRequested;
    
    [RelayCommand]
    private void EditPrepItem(MeetingPrepItem item)
    {
        EditPrepItemRequested?.Invoke(this, item);
    }
    
    #endregion
    
    #region Commands - Agenda Items
    
    [RelayCommand]
    private void ShowAddAgendaPanel()
    {
        NewAgendaTitle = string.Empty;
        NewAgendaContext = null;
        NewAgendaTalkingPoints = null;
        NewAgendaVisibility = "meeting";
        IsAddAgendaPanelVisible = true;
    }
    
    [RelayCommand]
    private void CancelAddAgenda()
    {
        IsAddAgendaPanelVisible = false;
    }
    
    [RelayCommand]
    private void ConfirmAddAgenda()
    {
        if (string.IsNullOrWhiteSpace(NewAgendaTitle))
            return;
        
        var talkingPoints = ParseTalkingPointsFromText(NewAgendaTalkingPoints);
        
        AddAgendaItem(
            NewAgendaTitle.Trim(), 
            visibilityScope: NewAgendaVisibility,
            sharedContext: NewAgendaContext?.Trim(),
            talkingPoints: talkingPoints);
        
        IsAddAgendaPanelVisible = false;
        NewAgendaTitle = string.Empty;
        NewAgendaContext = null;
        NewAgendaTalkingPoints = null;
    }
    
    [RelayCommand]
    private async Task DeleteAgendaItemAsync(DialogAgendaItem item)
    {
        if (_existingMeeting != null && item.Id != Guid.Empty)
        {
            await MeetingAgendaItemService.Instance.DeleteAgendaItemAsync(item.Id);
        }
        AgendaItems.Remove(item);
    }
    
    [RelayCommand]
    private void LinkExistingAgendaItem(EntityPickerResult? result)
    {
        if (result == null) return;
        
        AddAgendaItem(
            $"Discuss {result.EntityTitle}",
            linkedEntityId: result.EntityId,
            linkedEntityType: result.EntityType,
            linkedEntityTitle: result.EntityTitle);
    }
    
    /// <summary>
    /// Event raised when entity picker should be shown for agenda items.
    /// The View handles showing the dialog and calling LinkExistingAgendaItemCommand with result.
    /// </summary>
    public event EventHandler? EntityPickerForAgendaRequested;
    
    [RelayCommand]
    private void ShowEntityPickerForAgenda()
    {
        EntityPickerForAgendaRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Event raised when an agenda item should be edited.
    /// The View handles showing the edit dialog.
    /// </summary>
    public event EventHandler<DialogAgendaItem>? EditAgendaItemRequested;
    
    [RelayCommand]
    private void EditAgendaItem(DialogAgendaItem item)
    {
        EditAgendaItemRequested?.Invoke(this, item);
    }
    
    #endregion
    
    #region Commands - Dialog Actions
    
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (IsSaving) return;
        
        Log("SaveCommand executed");
        IsSaving = true;
        ErrorMessage = null;
        
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = "Title is required";
                return;
            }
            
            if (!ScheduledDateTime.HasValue)
            {
                ErrorMessage = "Date and time are required";
                return;
            }
            
            MeetingDetail? savedMeeting;
            
            if (_existingMeeting != null)
            {
                savedMeeting = await UpdateMeetingAsync();
            }
            else
            {
                savedMeeting = await CreateMeetingAsync();
            }
            
            if (savedMeeting != null)
            {
                Result = EditMeetingResult.Success(savedMeeting);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION in SaveCommand: {ex.GetType().Name}: {ex.Message}");
            ErrorMessage = ex.Message;
            Result = EditMeetingResult.Failed(ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }
    
    private bool CanSave() => !IsSaving && !string.IsNullOrWhiteSpace(Title) && ScheduledDateTime.HasValue;
    
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_existingMeeting == null) return;
        
        Log($"DeleteCommand executed for meeting: {_existingMeeting.Id}");
        
        var success = await MeetingService.Instance.DeleteMeetingAsync(_existingMeeting.Id);
        
        if (success)
        {
            Result = EditMeetingResult.Deleted(_existingMeeting.Id);
        }
        else
        {
            Result = EditMeetingResult.Failed(MeetingService.Instance.LastError ?? "Failed to delete meeting");
        }
        
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    
    #endregion
    
    #region Command Implementations - Create/Update
    
    private async Task<MeetingDetail?> CreateMeetingAsync()
    {
        Log("Creating new meeting");
        
        var newMeeting = new MeetingDetail
        {
            Title = Title.Trim(),
            MeetingType = MeetingType,
            ScheduledAt = ScheduledDateTime!.Value,
            DurationMinutes = DurationMinutes,
            Location = string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(),
            VideoLink = string.IsNullOrWhiteSpace(VideoLink) ? null : VideoLink.Trim(),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };
        
        // Get attendee IDs
        List<Guid>? attendeeIds = null;
        if (ShowAttendeeSelector && SelectedAttendee != null)
        {
            attendeeIds = new List<Guid> { SelectedAttendee.Id };
        }
        else if (ShowTeamAttendeesSelector)
        {
            attendeeIds = SelectableAttendees.Where(m => m.IsSelected).Select(m => m.Id).ToList();
            if (attendeeIds.Count == 0) attendeeIds = null;
        }
        
        Log($"Calling CreateMeetingAsync with {attendeeIds?.Count ?? 0} attendees");
        var savedMeeting = await MeetingService.Instance.CreateMeetingAsync(newMeeting, attendeeIds);
        
        if (savedMeeting == null)
        {
            ErrorMessage = MeetingService.Instance.LastError ?? "Failed to create meeting";
            return null;
        }
        
        // Save prep items
        Log($"Saving {PrepItems.Count} prep items");
        foreach (var prepItem in PrepItems)
        {
            await SavePrepItemForNewMeetingAsync(savedMeeting.Id, prepItem);
        }
        
        // Save agenda items
        Log($"Saving {AgendaItems.Count} agenda items");
        foreach (var agendaItem in AgendaItems)
        {
            await MeetingAgendaItemService.Instance.CreateAgendaItemAsync(
                savedMeeting.Id,
                agendaItem.Title,
                linkedEntityType: agendaItem.LinkedEntityType,
                linkedEntityId: agendaItem.LinkedEntityId,
                linkedEntityTitleSnapshot: agendaItem.LinkedEntityTitleSnapshot,
                visibilityScope: agendaItem.VisibilityScope,
                sharedContext: agendaItem.SharedContext,
                talkingPoints: agendaItem.TalkingPoints);
        }
        
        Log($"Meeting created successfully: {savedMeeting.Id}");
        return savedMeeting;
    }
    
    private async Task<MeetingDetail?> UpdateMeetingAsync()
    {
        if (_existingMeeting == null) return null;
        
        Log($"Updating existing meeting: {_existingMeeting.Id}");
        
        _existingMeeting.Title = Title.Trim();
        _existingMeeting.MeetingType = MeetingType;
        _existingMeeting.ScheduledAt = ScheduledDateTime!.Value;
        _existingMeeting.DurationMinutes = DurationMinutes;
        _existingMeeting.Location = string.IsNullOrWhiteSpace(Location) ? null : Location.Trim();
        _existingMeeting.VideoLink = string.IsNullOrWhiteSpace(VideoLink) ? null : VideoLink.Trim();
        _existingMeeting.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        
        var success = await MeetingService.Instance.UpdateMeetingAsync(_existingMeeting);
        
        if (!success)
        {
            ErrorMessage = MeetingService.Instance.LastError ?? "Failed to update meeting";
            return null;
        }
        
        // Update attendee if changed (for 1:1 meetings)
        if (ShowAttendeeSelector && SelectedAttendee != null && _existingMeeting.TeamMemberId != SelectedAttendee.Id)
        {
            if (_existingMeeting.TeamMemberId.HasValue)
            {
                await MeetingService.Instance.RemoveAttendeeAsync(_existingMeeting.Id, _existingMeeting.TeamMemberId.Value);
            }
            await MeetingService.Instance.AddAttendeeAsync(_existingMeeting.Id, SelectedAttendee.Id, "attendee");
        }
        
        // Save NEW prep items (those with empty Id)
        var newPrepItems = PrepItems.Where(p => p.Id == Guid.Empty).ToList();
        Log($"Saving {newPrepItems.Count} new prep items");
        foreach (var prepItem in newPrepItems)
        {
            await SavePrepItemForNewMeetingAsync(_existingMeeting.Id, prepItem);
        }
        
        // Save NEW agenda items (those with empty Id)
        var newAgendaItems = AgendaItems.Where(a => a.Id == Guid.Empty).ToList();
        Log($"Saving {newAgendaItems.Count} new agenda items");
        foreach (var agendaItem in newAgendaItems)
        {
            await MeetingAgendaItemService.Instance.CreateAgendaItemAsync(
                _existingMeeting.Id,
                agendaItem.Title,
                linkedEntityType: agendaItem.LinkedEntityType,
                linkedEntityId: agendaItem.LinkedEntityId,
                linkedEntityTitleSnapshot: agendaItem.LinkedEntityTitleSnapshot,
                visibilityScope: agendaItem.VisibilityScope,
                sharedContext: agendaItem.SharedContext,
                talkingPoints: agendaItem.TalkingPoints);
        }
        
        var savedMeeting = await MeetingService.Instance.GetMeetingAsync(_existingMeeting.Id);
        Log($"Meeting updated successfully: {savedMeeting?.Id}");
        
        return savedMeeting;
    }
    
    private async Task SavePrepItemForNewMeetingAsync(Guid meetingId, MeetingPrepItem prepItem)
    {
        if (prepItem.VisibilityScope == "assigned" && prepItem.AssignedToTeamMemberId.HasValue)
        {
            await MeetingPrepItemService.Instance.CreateAssignedPrepAsync(
                meetingId, prepItem.Title, prepItem.AssignedToTeamMemberId.Value);
        }
        else if (prepItem.VisibilityScope == "meeting")
        {
            await MeetingPrepItemService.Instance.CreateTeamPrepAsync(meetingId, prepItem.Title);
        }
        else
        {
            await MeetingPrepItemService.Instance.CreateQuickPrepAsync(meetingId, prepItem.Title);
        }
    }
    
    #endregion
    
    #region Private Helpers - Data Loading
    
    private async Task LoadPrepItemsAsync()
    {
        if (_existingMeeting == null) return;
        
        try
        {
            var prepItems = await MeetingPrepItemService.Instance.GetPrepItemsForMeetingAsync(_existingMeeting.Id);
            PrepItems.Clear();
            foreach (var item in prepItems.Where(p => !p.IsDeleted))
            {
                PrepItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Log($"Error loading prep items: {ex.Message}");
        }
    }
    
    private async Task LoadAgendaItemsAsync()
    {
        if (_existingMeeting == null) return;
        
        try
        {
            var agendaItems = await MeetingAgendaItemService.Instance.GetAgendaItemsForMeetingAsync(_existingMeeting.Id);
            AgendaItems.Clear();
            foreach (var item in agendaItems)
            {
                AgendaItems.Add(DialogAgendaItem.FromModel(item));
            }
        }
        catch (Exception ex)
        {
            Log($"Error loading agenda items: {ex.Message}");
        }
    }
    
    private async Task LoadTeamsAsync()
    {
        try
        {
            var teams = await TeamMembershipService.Instance.GetMyTeamsAsync();
            _availableTeams = teams;
            
            AvailableTeams.Clear();
            foreach (var team in teams)
            {
                AvailableTeams.Add(team);
            }
            
            Log($"Loaded {_availableTeams.Count} teams");
        }
        catch (Exception ex)
        {
            Log($"Failed to load teams: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Private Helpers - Prep Items
    
    /// <summary>
    /// Adds a prep item to the local collection. 
    /// Items are saved to database only when Save is clicked.
    /// </summary>
    private Task AddPrepItemAsync(string title, string? prepPrompt = null, string? body = null)
    {
        Log($"AddPrepItemAsync: title='{title}'");
        
        Guid? assigneeId = null;
        if (NewPrepVisibility == "assigned" && NewPrepAssignee != null)
        {
            assigneeId = NewPrepAssignee.Id;
        }
        
        // Always add to local collection - Save will persist to database
        // Leave Id as Guid.Empty to indicate this is a new item not yet saved
        var newItem = new MeetingPrepItem
        {
            Id = Guid.Empty,
            MeetingId = _existingMeeting?.Id ?? Guid.Empty, // Will be set properly on Save for new meetings
            Title = title,
            PrepPrompt = prepPrompt,
            Body = body,
            VisibilityScope = NewPrepVisibility,
            AssignedToTeamMemberId = assigneeId,
            Status = "open",
            CreatedAt = DateTime.UtcNow
        };
        
        PrepItems.Add(newItem);
        Log($"AddPrepItemAsync: Added to PrepItems, count now {PrepItems.Count}");
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Adds a linked prep item to the local collection.
    /// Items are saved to database only when Save is clicked.
    /// </summary>
    private Task AddLinkedPrepItemAsync(string entityType, Guid entityId, string entityTitle)
    {
        Log($"AddLinkedPrepItemAsync: entityType='{entityType}', entityTitle='{entityTitle}'");
        
        Guid? assigneeId = null;
        if (NewPrepVisibility == "assigned" && NewPrepAssignee != null)
        {
            assigneeId = NewPrepAssignee.Id;
        }
        
        var newItem = new MeetingPrepItem
        {
            Id = Guid.Empty,
            MeetingId = _existingMeeting?.Id ?? Guid.Empty,
            Title = $"Discuss: {entityTitle}",
            LinkedEntityType = entityType,
            LinkedEntityId = entityId,
            LinkedEntityTitleSnapshot = entityTitle,
            VisibilityScope = NewPrepVisibility,
            AssignedToTeamMemberId = assigneeId,
            Status = "open",
            CreatedAt = DateTime.UtcNow
        };
        
        PrepItems.Add(newItem);
        Log($"AddLinkedPrepItemAsync: Added to PrepItems, count now {PrepItems.Count}");
        
        return Task.CompletedTask;
    }
    
    private void UpdatePrepAssigneeList()
    {
        PrepAssignees.Clear();
        
        // Add self first
        var self = TeamMembers.FirstOrDefault(t => t.Relation == "self");
        if (self != null)
        {
            PrepAssignees.Add(self);
        }
        
        if (ShowAttendeeSelector)
        {
            // For 1:1 meetings, only the selected attendee
            if (SelectedAttendee != null)
            {
                PrepAssignees.Add(SelectedAttendee);
            }
        }
        else if (ShowTeamAttendeesSelector)
        {
            // For team meetings, all selected team members
            foreach (var member in SelectableAttendees.Where(m => m.IsSelected))
            {
                PrepAssignees.Add(member);
            }
        }
    }
    
    #endregion
    
    #region Private Helpers - Agenda Items
    
    private void AddAgendaItem(
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
            LinkedEntityTitleSnapshot = linkedEntityTitle,
            VisibilityScope = visibilityScope,
            SharedContext = sharedContext,
            TalkingPoints = talkingPoints ?? new List<TalkingPoint>()
        };
        
        AgendaItems.Add(newItem);
    }
    
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
    
    #endregion
    
    #region Private Helpers - Team Selection
    
    /// <summary>
    /// Called when a team is selected from the picker.
    /// Auto-selects team members as attendees.
    /// </summary>
    public async Task OnTeamSelectedAsync(Team team)
    {
        try
        {
            var teamMembers = await TeamMembershipService.Instance.GetTeamMemberDetailsAsync(team.Id);
            var teamMemberIds = teamMembers.Select(m => m.Id).ToHashSet();
            
            foreach (var member in SelectableAttendees)
            {
                member.IsSelected = teamMemberIds.Contains(member.Id);
            }
            
            UpdatePrepAssigneeList();
            Log($"Auto-selected {teamMembers.Count} attendees from team '{team.Name}'");
        }
        catch (Exception ex)
        {
            Log($"Failed to load team members: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Logging
    
    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch { }
    }
    
    #endregion
}
