using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private IDialogService? _dialogService;
    
    // Track items to delete - actual deletion happens on Save
    private readonly List<Guid> _prepItemsToDelete = new();
    private readonly List<Guid> _agendaItemsToDelete = new();
    private readonly List<Guid> _notesToDelete = new();
    
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
    
    // Note: _notes removed - now using MeetingNotes collection
    
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
    public ObservableCollection<DialogMeetingNote> MeetingNotes { get; } = new();
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
    public bool HasMeetingNotes => MeetingNotes.Count > 0;
    
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
    
    #region Dialog Service
    
    /// <summary>
    /// Sets the dialog service. Must be called by the View before any dialog commands are used.
    /// This allows the View to provide its Window reference without the ViewModel knowing about it.
    /// </summary>
    public void SetDialogService(IDialogService dialogService)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }
    
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
        // Unsubscribe from existing members
        foreach (var member in SelectableAttendees)
        {
            member.PropertyChanged -= OnAttendeePropertyChanged;
        }
        
        TeamMembers.Clear();
        SelectableAttendees.Clear();
        
        foreach (var member in teamMembers)
        {
            TeamMembers.Add(member);
            if (member.Relation != "self")
            {
                member.IsSelected = false;
                member.PropertyChanged += OnAttendeePropertyChanged;
                SelectableAttendees.Add(member);
            }
        }
        
        UpdatePrepAssigneeList();
        _ = LoadTeamsAsync();
    }
    
    /// <summary>
    /// Pre-select an attendee for the meeting.
    /// Useful for "Schedule Meeting with [Person]" scenarios.
    /// Call this after Initialize and before showing the dialog.
    /// </summary>
    public void PreSelectAttendee(TeamMemberDetail attendee)
    {
        // Find the attendee in our loaded team members
        var member = TeamMembers.FirstOrDefault(t => t.Id == attendee.Id);
        if (member != null)
        {
            SelectedAttendee = member;
            MeetingType = "one_on_one";
            Log($"Pre-selected attendee: {member.FullName}");
        }
        else
        {
            Log($"Warning: Pre-selected attendee {attendee.FullName} not found in team members");
        }
    }
    
    /// <summary>
    /// Handler for attendee property changes - updates PrepAssignees when selection changes.
    /// </summary>
    private void OnAttendeePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TeamMemberDetail.IsSelected))
        {
            UpdatePrepAssigneeList();
        }
    }
    
    /// <summary>
    /// Load an existing meeting for editing.
    /// </summary>
    public async Task LoadMeetingAsync(MeetingDetail meeting)
    {
        _existingMeeting = meeting;
        MeetingId = meeting.Id;
        
        // Clear any pending delete operations from previous load
        _prepItemsToDelete.Clear();
        _agendaItemsToDelete.Clear();
        _notesToDelete.Clear();
        
        Title = meeting.Title;
        MeetingType = meeting.MeetingType;
        ScheduledDateTime = meeting.ScheduledAt;
        DurationMinutes = meeting.DurationMinutes ?? 30;
        Location = meeting.Location;
        VideoLink = meeting.VideoLink;
        
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
        
        // Load prep, agenda, and notes
        await LoadPrepItemsAsync();
        await LoadAgendaItemsAsync();
        await LoadNotesAsync();
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
    
    /// <summary>
    /// Shows dialog to add a new prep item.
    /// All dialog handling is in the ViewModel - proper MVVM.
    /// </summary>
    [RelayCommand]
    private async Task AddPrepItemAsync()
    {
        if (_dialogService == null)
        {
            Log("AddPrepItem: DialogService not set");
            return;
        }
        
        // Create a new empty prep item for the dialog
        var newItem = new MeetingPrepItem
        {
            Id = Guid.Empty,
            Title = string.Empty,
            VisibilityScope = "personal",
            Status = "open"
        };
        
        // Get attendees for assignee picker
        var attendees = GetMeetingAttendeesForAssignment();
        var currentUserTeamMemberId = GetCurrentUserTeamMemberId();
        
        var result = await _dialogService.ShowEditPrepItemDialogAsync(
            newItem, 
            attendees, 
            currentUserTeamMemberId);
        
        if (result == null || string.IsNullOrWhiteSpace(result.Title))
            return;
        
        Log($"AddPrepItem: Adding '{result.Title}'");
        await AddPrepItemInternalAsync(
            result.Title.Trim(),
            result.PrepPrompt?.Trim(),
            result.Body?.Trim(),
            result.VisibilityScope,
            result.AssignedToTeamMemberId,
            result.AssignedToName);
    }
    
    /// <summary>
    /// Shows dialog to edit an existing prep item.
    /// All result processing is in the ViewModel - proper MVVM.
    /// </summary>
    [RelayCommand]
    private async Task EditPrepItemAsync(MeetingPrepItem item)
    {
        if (_dialogService == null)
        {
            Log("EditPrepItem: DialogService not set");
            return;
        }
        
        var attendees = GetMeetingAttendeesForAssignment();
        var currentUserTeamMemberId = GetCurrentUserTeamMemberId();
        
        var result = await _dialogService.ShowEditPrepItemDialogAsync(
            item, 
            attendees, 
            currentUserTeamMemberId);
        
        if (result == null)
            return;
        
        // Apply result to the item (ViewModel handles the business logic)
        item.Title = result.Title;
        item.Body = result.Body;
        item.PrepPrompt = result.PrepPrompt;
        item.PrepResponse = result.PrepResponse;
        item.AssigneeNotes = result.AssigneeNotes;
        item.VisibilityScope = result.VisibilityScope ?? "personal";
        item.AssignedToTeamMemberId = result.AssignedToTeamMemberId;
        item.AssignedToName = result.AssignedToName ?? string.Empty;
        item.Status = result.Status ?? "open";
        item.PreparedAt = result.PreparedAt;
        item.IsDirty = true;
        
        Log($"EditPrepItem: Updated '{item.Title}'");
    }
    
    /// <summary>
    /// Shows entity picker to link an existing entity as prep.
    /// </summary>
    [RelayCommand]
    private async Task ShowEntityPickerForPrepAsync()
    {
        if (_dialogService == null)
        {
            Log("ShowEntityPickerForPrep: DialogService not set");
            return;
        }
        
        var result = await _dialogService.ShowEntityPickerAsync();
        
        if (result == null)
            return;
        
        await AddLinkedPrepItemAsync(result.EntityType, result.EntityId, result.EntityTitle);
    }
    
    [RelayCommand]
    private void DeletePrepItem(MeetingPrepItem item)
    {
        // If this is an existing item (has an Id), mark it for deletion on Save
        if (item.Id != Guid.Empty)
        {
            _prepItemsToDelete.Add(item.Id);
            Log($"Marked prep item for deletion: {item.Id}");
        }
        // Remove from UI immediately
        PrepItems.Remove(item);
    }
    
    #endregion
    
    #region Commands - Agenda Items
    
    /// <summary>
    /// Shows dialog to add a new agenda item.
    /// All dialog handling is in the ViewModel - proper MVVM.
    /// </summary>
    [RelayCommand]
    private async Task AddAgendaItemAsync()
    {
        if (_dialogService == null)
        {
            Log("AddAgendaItem: DialogService not set");
            return;
        }
        
        // Create a new empty agenda item for the dialog
        var newItem = new DialogAgendaItem
        {
            Id = Guid.Empty,
            Title = string.Empty,
            VisibilityScope = "meeting"
        };
        
        var result = await _dialogService.ShowEditAgendaItemDialogAsync(newItem);
        
        if (result == null || string.IsNullOrWhiteSpace(result.Title))
            return;
        
        Log($"AddAgendaItem: Adding '{result.Title}'");
        AddAgendaItem(
            result.Title.Trim(),
            visibilityScope: result.VisibilityScope ?? "meeting",
            sharedContext: result.SharedContext?.Trim(),
            privateContext: result.PrivateContext?.Trim(),
            talkingPoints: result.TalkingPoints);
    }
    
    /// <summary>
    /// Shows dialog to edit an existing agenda item.
    /// All result processing is in the ViewModel - proper MVVM.
    /// </summary>
    [RelayCommand]
    private async Task EditAgendaItemAsync(DialogAgendaItem item)
    {
        if (_dialogService == null)
        {
            Log("EditAgendaItem: DialogService not set");
            return;
        }
        
        var result = await _dialogService.ShowEditAgendaItemDialogAsync(item);
        
        if (result == null)
            return;
        
        // Apply result to the item (ViewModel handles the business logic)
        item.Title = result.Title;
        item.DisplayTitle = result.DisplayTitle;
        item.SharedContext = result.SharedContext;
        item.PrivateContext = result.PrivateContext;
        item.VisibilityScope = result.VisibilityScope ?? "meeting";
        item.IsDirty = true;
        
        // Update talking points
        item.TalkingPoints.Clear();
        foreach (var tp in result.TalkingPoints)
        {
            item.TalkingPoints.Add(tp);
        }
        
        Log($"EditAgendaItem: Updated '{item.Title}'");
    }
    
    /// <summary>
    /// Shows entity picker to link an existing entity as agenda item.
    /// </summary>
    [RelayCommand]
    private async Task ShowEntityPickerForAgendaAsync()
    {
        if (_dialogService == null)
        {
            Log("ShowEntityPickerForAgenda: DialogService not set");
            return;
        }
        
        var result = await _dialogService.ShowEntityPickerAsync();
        
        if (result == null)
            return;
        
        AddAgendaItem(
            $"Discuss {result.EntityTitle}",
            linkedEntityId: result.EntityId,
            linkedEntityType: result.EntityType,
            linkedEntityTitle: result.EntityTitle);
    }
    
    [RelayCommand]
    private void DeleteAgendaItem(DialogAgendaItem item)
    {
        // If this is an existing item (has an Id), mark it for deletion on Save
        if (item.Id != Guid.Empty)
        {
            _agendaItemsToDelete.Add(item.Id);
            Log($"Marked agenda item for deletion: {item.Id}");
        }
        // Remove from UI immediately
        AgendaItems.Remove(item);
    }
    
    #endregion
    
    #region Commands - Notes
    
    [RelayCommand]
    private void AddNote()
    {
        var currentUserId = AuthService.Instance.CurrentProfile?.Id ?? Guid.Empty;
        var currentUserName = AuthService.Instance.CurrentProfile?.DisplayName ?? "Me";
        var meetingId = _existingMeeting?.Id ?? Guid.Empty;
        
        var newNote = DialogMeetingNote.CreateNew(meetingId, currentUserId, currentUserName, isShared: false);
        MeetingNotes.Insert(0, newNote); // Add at top
        OnPropertyChanged(nameof(HasMeetingNotes));
        Log($"Added new note, total: {MeetingNotes.Count}");
    }
    
    [RelayCommand]
    private void EditNote(DialogMeetingNote note)
    {
        note.BeginEdit();
    }
    
    [RelayCommand]
    private void SaveNoteEdit(DialogMeetingNote note)
    {
        note.ConfirmEdit();
        OnPropertyChanged(nameof(HasMeetingNotes));
    }
    
    [RelayCommand]
    private void CancelNoteEdit(DialogMeetingNote note)
    {
        // If this is a new note with no content, remove it entirely
        if (note.Id == Guid.Empty && string.IsNullOrWhiteSpace(note.Content))
        {
            MeetingNotes.Remove(note);
            OnPropertyChanged(nameof(HasMeetingNotes));
        }
        else
        {
            note.CancelEdit();
        }
    }
    
    [RelayCommand]
    private void DeleteNote(DialogMeetingNote note)
    {
        // If this is an existing note (has an Id), mark it for deletion on Save
        if (note.Id != Guid.Empty)
        {
            _notesToDelete.Add(note.Id);
            Log($"Marked note for deletion: {note.Id}");
        }
        // Remove from UI immediately
        MeetingNotes.Remove(note);
        OnPropertyChanged(nameof(HasMeetingNotes));
    }
    
    [RelayCommand]
    private void ToggleNoteVisibility(DialogMeetingNote note)
    {
        note.IsShared = !note.IsShared;
        note.IsDirty = true;
    }
    
    /// <summary>
    /// Toggles a tag on/off for a note. The command parameter is a Tuple of (note, tag).
    /// </summary>
    [RelayCommand]
    private void ToggleNoteTag(object? parameter)
    {
        if (parameter is not Tuple<DialogMeetingNote, NoteTag> tuple) return;
        var (note, tag) = tuple;
        
        var existingTag = note.Tags.FirstOrDefault(t => t.Id == tag.Id);
        if (existingTag != null)
        {
            var newTags = note.Tags.Where(t => t.Id != tag.Id).ToList();
            note.Tags = newTags;  // Setting the property triggers PropertyChanged
        }
        else
        {
            note.Tags = new List<NoteTag>(note.Tags) { tag };  // Setting the property triggers PropertyChanged
        }
        note.IsDirty = true;
    }
    
    /// <summary>
    /// Available tags for notes.
    /// </summary>
    public List<NoteTag> AvailableNoteTags => NoteTag.StandardTags;
    
    #endregion
    
    #region Commands - Dialog Actions
    
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (IsSaving) return;
        
        Log("SaveCommand executed");
        IsSaving = true;
        ErrorMessage = null;
        
        // Auto-confirm any notes still being edited
        Log($"Checking {MeetingNotes.Count} notes for auto-confirm...");
        foreach (var note in MeetingNotes.Where(n => n.IsEditing))
        {
            Log($"Auto-confirming note: EditContent='{note.EditContent}' -> Content");
            note.ConfirmEdit();
            Log($"After confirm: Content='{note.Content}', HasContent={note.HasContent}");
        }
        
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
            VideoLink = string.IsNullOrWhiteSpace(VideoLink) ? null : VideoLink.Trim()
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
        
        // Save notes
        Log($"Saving {MeetingNotes.Count} notes (filtering by HasContent)");
        var notesWithContent = MeetingNotes.Where(n => n.HasContent).ToList();
        Log($"Found {notesWithContent.Count} notes with content");
        foreach (var note in notesWithContent)
        {
            Log($"Saving note: Content='{note.Content}' (length={note.Content.Length}), IsShared={note.IsShared}");
            await MeetingNoteService.Instance.CreateNoteAsync(
                savedMeeting.Id,
                note.Content,
                isPrivate: !note.IsShared);
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
        
        // Update EXISTING dirty prep items
        var dirtyPrepItems = PrepItems.Where(p => p.Id != Guid.Empty && p.IsDirty).ToList();
        Log($"Updating {dirtyPrepItems.Count} modified prep items");
        foreach (var prepItem in dirtyPrepItems)
        {
            await MeetingPrepItemService.Instance.UpdatePrepItemAsync(prepItem);
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
        
        // Update EXISTING dirty agenda items
        var dirtyAgendaItems = AgendaItems.Where(a => a.Id != Guid.Empty && a.IsDirty).ToList();
        Log($"Updating {dirtyAgendaItems.Count} modified agenda items");
        foreach (var agendaItem in dirtyAgendaItems)
        {
            await MeetingAgendaItemService.Instance.UpdateAgendaItemAsync(
                agendaItem.Id,
                title: agendaItem.Title,
                displayTitle: agendaItem.DisplayTitle,
                sharedContext: agendaItem.SharedContext,
                privateContext: agendaItem.PrivateContext,
                visibilityScope: agendaItem.VisibilityScope,
                talkingPoints: agendaItem.TalkingPoints);
        }
        
        // Delete prep items marked for deletion
        Log($"Deleting {_prepItemsToDelete.Count} prep items");
        foreach (var prepItemId in _prepItemsToDelete)
        {
            await MeetingPrepItemService.Instance.DeletePrepItemAsync(prepItemId);
        }
        _prepItemsToDelete.Clear();
        
        // Delete agenda items marked for deletion
        Log($"Deleting {_agendaItemsToDelete.Count} agenda items");
        foreach (var agendaItemId in _agendaItemsToDelete)
        {
            await MeetingAgendaItemService.Instance.DeleteAgendaItemAsync(agendaItemId);
        }
        _agendaItemsToDelete.Clear();
        
        // Save NEW notes (those with empty Id)
        var newNotes = MeetingNotes.Where(n => n.Id == Guid.Empty && n.HasContent).ToList();
        Log($"Saving {newNotes.Count} new notes");
        foreach (var note in newNotes)
        {
            await MeetingNoteService.Instance.CreateNoteAsync(
                _existingMeeting.Id,
                note.Content,
                isPrivate: !note.IsShared);
        }
        
        // Update EXISTING dirty notes
        var dirtyNotes = MeetingNotes.Where(n => n.Id != Guid.Empty && n.IsDirty && n.HasContent).ToList();
        Log($"Updating {dirtyNotes.Count} modified notes");
        foreach (var note in dirtyNotes)
        {
            var meetingNote = new MeetingNote
            {
                Id = note.Id,
                Content = note.Content,
                IsShared = note.IsShared,
                Tags = note.GetTagCategories()
            };
            await MeetingNoteService.Instance.UpdateNoteAsync(meetingNote);
        }
        
        // Delete notes marked for deletion
        Log($"Deleting {_notesToDelete.Count} notes");
        foreach (var noteId in _notesToDelete)
        {
            await MeetingNoteService.Instance.DeleteNoteAsync(noteId);
        }
        _notesToDelete.Clear();
        
        var savedMeeting = await MeetingService.Instance.GetMeetingAsync(_existingMeeting.Id);
        Log($"Meeting updated successfully: {savedMeeting?.Id}");
        
        return savedMeeting;
    }
    
    private async Task SavePrepItemForNewMeetingAsync(Guid meetingId, MeetingPrepItem prepItem)
    {
        if (prepItem.LinkedEntityType != null && prepItem.LinkedEntityId.HasValue)
        {
            // Linked prep item
            await MeetingPrepItemService.Instance.CreateLinkedPrepAsync(
                meetingId,
                prepItem.LinkedEntityType,
                prepItem.LinkedEntityId.Value,
                prepItem.LinkedEntityTitleSnapshot ?? prepItem.Title,
                prepItem.PrepPrompt,
                prepItem.VisibilityScope,
                prepItem.AssignedToTeamMemberId);
        }
        else if (prepItem.VisibilityScope == "assigned" && prepItem.AssignedToTeamMemberId.HasValue)
        {
            await MeetingPrepItemService.Instance.CreateAssignedPrepAsync(
                meetingId, prepItem.Title, prepItem.AssignedToTeamMemberId.Value, 
                prepItem.Body, prepItem.DueAt);
        }
        else if (prepItem.VisibilityScope == "meeting")
        {
            await MeetingPrepItemService.Instance.CreateTeamPrepAsync(
                meetingId, prepItem.Title, prepItem.Body);
        }
        else
        {
            // Personal/quick prep - need to pass body and prepPrompt
            await MeetingPrepItemService.Instance.CreateQuickPrepAsync(
                meetingId, prepItem.Title, prepItem.Body, prepItem.PrepPrompt);
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
    
    private async Task LoadNotesAsync()
    {
        if (_existingMeeting == null) return;
        
        try
        {
            var (myNotes, sharedNotes) = await MeetingNoteService.Instance.GetNotesForMeetingAsync(_existingMeeting.Id);
            MeetingNotes.Clear();
            
            // Add all notes - personal first, then shared
            foreach (var note in myNotes)
            {
                MeetingNotes.Add(DialogMeetingNote.FromMeetingNote(note));
            }
            foreach (var note in sharedNotes)
            {
                // Get author name if available (would need lookup in real implementation)
                MeetingNotes.Add(DialogMeetingNote.FromMeetingNote(note));
            }
            
            OnPropertyChanged(nameof(HasMeetingNotes));
            Log($"Loaded {MeetingNotes.Count} notes ({myNotes.Count} personal, {sharedNotes.Count} shared)");
        }
        catch (Exception ex)
        {
            Log($"Error loading notes: {ex.Message}");
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
    /// Gets meeting attendees for the assignee picker in dialogs.
    /// </summary>
    private List<MeetingAttendee> GetMeetingAttendeesForAssignment()
    {
        var attendees = new List<MeetingAttendee>();
        
        // Convert selected team members to MeetingAttendee format
        foreach (var member in SelectableAttendees.Where(m => m.IsSelected))
        {
            attendees.Add(new MeetingAttendee
            {
                TeamMemberId = member.Id,
                Name = member.FullName
            });
        }
        
        return attendees;
    }
    
    /// <summary>
    /// Gets the current user's team member ID.
    /// </summary>
    private Guid GetCurrentUserTeamMemberId()
    {
        // Find self in the team members list
        var self = TeamMembers.FirstOrDefault(m => m.Relation == "self");
        return self?.Id ?? Guid.Empty;
    }
    
    /// <summary>
    /// Adds a prep item to the local collection. 
    /// Items are saved to database only when Save is clicked.
    /// </summary>
    private Task AddPrepItemInternalAsync(
        string title, 
        string? prepPrompt = null, 
        string? body = null, 
        string? visibilityScope = null,
        Guid? assignedToTeamMemberId = null,
        string? assignedToName = null)
    {
        Log($"AddPrepItemAsync: title='{title}'");
        
        // Always add to local collection - Save will persist to database
        // Leave Id as Guid.Empty to indicate this is a new item not yet saved
        var newItem = new MeetingPrepItem
        {
            Id = Guid.Empty,
            MeetingId = _existingMeeting?.Id ?? Guid.Empty, // Will be set properly on Save for new meetings
            Title = title,
            PrepPrompt = prepPrompt,
            Body = body,
            VisibilityScope = visibilityScope ?? "personal",
            AssignedToTeamMemberId = assignedToTeamMemberId,
            AssignedToName = assignedToName ?? string.Empty,
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
        string? privateContext = null,
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
            PrivateContext = privateContext,
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
