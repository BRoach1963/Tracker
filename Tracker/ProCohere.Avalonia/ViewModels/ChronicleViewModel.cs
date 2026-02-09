using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Chronicle tab containing Notes.
/// Manages note display, CRUD operations, and filtering.
/// </summary>
public partial class ChronicleViewModel : ViewModelBase
{
    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "chronicle_vm.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion

    #region Loading State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Collections

    /// <summary>
    /// All pinned notes.
    /// </summary>
    public ObservableCollection<Note> PinnedNotes { get; } = new();

    /// <summary>
    /// All non-pinned notes.
    /// </summary>
    public ObservableCollection<Note> Notes { get; } = new();

    /// <summary>
    /// Available categories for filtering/selection.
    /// </summary>
    public ObservableCollection<string> Categories { get; } = new(NoteCategory.All);

    #endregion

    #region Selection State

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedNote))]
    private Note? _selectedNote;

    [ObservableProperty]
    private bool _isNoteDetailOpen;

    [ObservableProperty]
    private bool _isNoteEditorOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditingNoteIsPrivate))]
    private Note? _editingNote;

    public bool HasSelectedNote => SelectedNote != null;

    /// <summary>
    /// Wrapper property for EditingNote.IsPrivate to support INPC binding.
    /// </summary>
    public bool EditingNoteIsPrivate => EditingNote?.IsPrivate ?? true;

    /// <summary>
    /// Sets the privacy flag on the editing note and raises property changed.
    /// Called from the view code-behind for privacy toggle interaction.
    /// </summary>
    public void SetNotePrivacy(bool isPrivate)
    {
        if (EditingNote != null)
        {
            EditingNote.IsPrivate = isPrivate;
            OnPropertyChanged(nameof(EditingNoteIsPrivate));
            Log($"Note privacy set to: {(isPrivate ? "Private" : "Shared")}");
        }
    }

    #endregion

    #region Note Detail Tab State

    [ObservableProperty]
    private NoteDetailTab _noteDetailTab = NoteDetailTab.Content;

    /// <summary>
    /// Linked entities for the selected note (goals, contacts, etc.).
    /// </summary>
    public ObservableCollection<object> SelectedNoteLinkedEntities { get; } = new();

    [RelayCommand]
    private void SetNoteDetailTab(NoteDetailTab tab)
    {
        NoteDetailTab = tab;
    }

    #endregion

    #region Filter State

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCategoryFilterAll))]
    [NotifyPropertyChangedFor(nameof(IsCategoryFilterMeetingNotes))]
    [NotifyPropertyChangedFor(nameof(IsCategoryFilterIdeas))]
    [NotifyPropertyChangedFor(nameof(IsCategoryFilterActionItems))]
    [NotifyPropertyChangedFor(nameof(IsCategoryFilterResearch))]
    [NotifyPropertyChangedFor(nameof(IsCategoryFilterPersonal))]
    [NotifyPropertyChangedFor(nameof(IsCategoryFilterFollowUp))]
    private string? _selectedCategory;

    /// <summary>Category filter pill state - All selected when no category filter.</summary>
    public bool IsCategoryFilterAll => string.IsNullOrEmpty(SelectedCategory);

    /// <summary>Category filter pill state - Meeting Notes.</summary>
    public bool IsCategoryFilterMeetingNotes => SelectedCategory == "Meeting Notes";

    /// <summary>Category filter pill state - Ideas.</summary>
    public bool IsCategoryFilterIdeas => SelectedCategory == "Ideas";

    /// <summary>Category filter pill state - Action Items.</summary>
    public bool IsCategoryFilterActionItems => SelectedCategory == "Action Items";

    /// <summary>Category filter pill state - Research.</summary>
    public bool IsCategoryFilterResearch => SelectedCategory == "Research";

    /// <summary>Category filter pill state - Personal.</summary>
    public bool IsCategoryFilterPersonal => SelectedCategory == "Personal";

    /// <summary>Category filter pill state - Follow-up.</summary>
    public bool IsCategoryFilterFollowUp => SelectedCategory == "Follow-up";

    /// <summary>
    /// Debounce token for search queries.
    /// </summary>
    private CancellationTokenSource? _searchDebounceTokenSource;

    /// <summary>
    /// Called when SearchQuery changes - triggers debounced search.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        // Cancel any pending search
        _searchDebounceTokenSource?.Cancel();
        _searchDebounceTokenSource = new CancellationTokenSource();
        var token = _searchDebounceTokenSource.Token;

        // Debounce 300ms then search
        Task.Delay(300, token).ContinueWith(async t =>
        {
            if (!t.IsCanceled)
            {
                await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await SearchAsync();
                });
            }
        }, TaskScheduler.Default);
    }

    // Archive functionality deferred - columns not yet added to DB
    // [ObservableProperty]
    // private bool _showArchived;

    #endregion

    #region Stats

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCountText))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinnedCountText))]
    private int _pinnedCount;

    public string TotalCountText => TotalCount.ToString();
    public string PinnedCountText => PinnedCount.ToString();

    #endregion

    #region Sub-tab State

    /// <summary>
    /// 0 = Notes, 1 = Reports (future)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotesTabSelected))]
    [NotifyPropertyChangedFor(nameof(IsReportsTabSelected))]
    private int _selectedSubTab;

    public bool IsNotesTabSelected => SelectedSubTab == 0;
    public bool IsReportsTabSelected => SelectedSubTab == 1;

    #endregion

    #region Constructor

    public ChronicleViewModel()
    {
        Log("ChronicleViewModel created");
    }

    #endregion

    #region Load Commands

    [RelayCommand]
    private async Task LoadNotesAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;
            Log("Loading notes...");

            var notes = await NotesService.Instance.GetAllNotesAsync();

            // Load links for all notes in one batch query
            await NotesService.Instance.PopulateLinksAsync(notes);

            // Clear and repopulate collections
            PinnedNotes.Clear();
            Notes.Clear();

            foreach (var note in notes)
            {
                if (note.IsPinned)
                    PinnedNotes.Add(note);
                else
                    Notes.Add(note);
            }

            // Update stats
            PinnedCount = PinnedNotes.Count;
            TotalCount = Notes.Count + PinnedNotes.Count;

            Log($"Notes loaded: {TotalCount} total, {PinnedCount} pinned");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load notes: {ex.Message}";
            Log($"LoadNotes ERROR: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadNotesAsync();
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;
            Log($"Searching notes: {SearchQuery}");

            var results = await NotesService.Instance.SearchNotesAsync(SearchQuery);

            PinnedNotes.Clear();
            Notes.Clear();

            foreach (var note in results)
            {
                if (note.IsPinned)
                    PinnedNotes.Add(note);
                else
                    Notes.Add(note);
            }

            PinnedCount = PinnedNotes.Count;
            TotalCount = Notes.Count + PinnedNotes.Count;

            Log($"Search returned {TotalCount} results");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Search failed: {ex.Message}";
            Log($"Search ERROR: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Category filtering deferred - category column not yet added to DB
    // Use search functionality instead for now
    [RelayCommand]
    private async Task FilterByCategoryAsync(string? category)
    {
        SelectedCategory = category;

        if (string.IsNullOrEmpty(category))
        {
            await LoadNotesAsync();
            return;
        }

        // Deferred: Category filtering requires adding a category column to the notes table
        Log($"Category filter requested but column not yet available: {category}");
        await LoadNotesAsync();
    }

    #endregion

    #region Selection Commands

    [RelayCommand]
    private void SelectNote(Note? note)
    {
        Log($"Selecting note: {note?.Id}");
        
        if (note != null)
        {
            // Wire up IDetailEntity commands - ViewModel owns commands, entity references them
            note.CloseCommand = CloseNoteDetailCommand;
            note.EditCommand = new RelayCommand(() => EditNote(note));
            note.DeleteCommand = new AsyncRelayCommand(() => DeleteNoteAsync(note));
        }
        
        SelectedNote = note;
        IsNoteDetailOpen = note != null;
        IsNoteEditorOpen = false;
    }

    [RelayCommand]
    private void CloseNoteDetail()
    {
        Log("Closing note detail");
        IsNoteDetailOpen = false;
        SelectedNote = null;
    }

    #endregion

    #region Create/Edit Commands

    [RelayCommand]
    private void CreateNewNote()
    {
        Log("Creating new note");
        EditingNote = new Note
        {
            ContentFormat = "plain",
            Tags = new System.Collections.Generic.List<string>()
        };
        IsNoteEditorOpen = true;
        IsNoteDetailOpen = false;
        ResetLinkStaging();
    }

    [RelayCommand]
    private void EditNote(Note? note)
    {
        if (note == null) return;

        Log($"Editing note: {note.Id}");
        EditingNote = note;
        IsNoteEditorOpen = true;
        IsNoteDetailOpen = false;
        ResetLinkStaging();
        NotifyEntityLinkChanged();
    }

    [RelayCommand]
    private void CloseNoteEditor()
    {
        Log("Closing note editor");
        IsNoteEditorOpen = false;
        EditingNote = null;
        ResetLinkStaging();
    }

    [RelayCommand]
    private async Task SaveNoteAsync()
    {
        if (EditingNote == null) return;

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            Guid savedNoteId;

            if (EditingNote.Id == Guid.Empty)
            {
                // Create new note
                Log($"Creating note: {EditingNote.Title ?? "(untitled)"}");
                var created = await NotesService.Instance.CreateNoteAsync(EditingNote);
                
                if (created != null)
                {
                    savedNoteId = created.Id;

                    // Save any pending links
                    await SaveLinkChangesAsync(savedNoteId);

                    // Reload links onto the created note
                    created.Links = await NotesService.Instance.GetLinksForNoteAsync(savedNoteId);

                    // Add to appropriate collection
                    if (created.IsPinned)
                        PinnedNotes.Insert(0, created);
                    else
                        Notes.Insert(0, created);
                    
                    TotalCount++;
                    if (created.IsPinned) PinnedCount++;
                    
                    Log($"Note created: {created.Id}");
                    NotificationService.Instance.ShowSuccess("Note Created", $"'{created.Title ?? "Untitled"}' has been saved.");
                }
                else
                {
                    throw new Exception(NotesService.Instance.LastError ?? "Failed to create note");
                }
            }
            else
            {
                // Update existing note
                Log($"Updating note: {EditingNote.Id}");
                savedNoteId = EditingNote.Id;
                var updated = await NotesService.Instance.UpdateNoteAsync(EditingNote);
                
                if (updated != null)
                {
                    // Save any pending link changes
                    await SaveLinkChangesAsync(savedNoteId);

                    // Update in collection - reload to get fresh links
                    await LoadNotesAsync();
                    Log($"Note updated: {updated.Id}");
                    NotificationService.Instance.ShowSuccess("Note Updated", $"'{updated.Title ?? "Untitled"}' has been saved.");
                }
                else
                {
                    throw new Exception(NotesService.Instance.LastError ?? "Failed to update note");
                }
            }

            CloseNoteEditor();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to save note: {ex.Message}";
            Log($"SaveNote ERROR: {ex.Message}");
            NotificationService.Instance.ShowError("Save Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Delete Commands

    [RelayCommand]
    private async Task DeleteNoteAsync(Note? note)
    {
        if (note == null) return;

        var noteTitle = note.Title ?? "Untitled";
        
        // Show confirmation dialog for destructive action
        var confirmed = await ConfirmationService.Instance.ShowDestructiveConfirmationAsync(
            "Delete Note",
            $"Are you sure you want to delete '{noteTitle}'? This action cannot be undone.",
            "Delete Note",
            "Cancel");
        
        if (!confirmed)
            return;

        try
        {
            Log($"Deleting note: {note.Id}");
            var success = await NotesService.Instance.DeleteNoteAsync(note.Id);

            if (success)
            {
                // Remove from collections
                PinnedNotes.Remove(note);
                Notes.Remove(note);
                
                TotalCount--;
                if (note.IsPinned) PinnedCount--;

                if (SelectedNote?.Id == note.Id)
                    CloseNoteDetail();

                Log("Note deleted successfully");
                NotificationService.Instance.ShowSuccess("Note Deleted", $"'{noteTitle}' has been removed.");
            }
            else
            {
                throw new Exception(NotesService.Instance.LastError ?? "Failed to delete note");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to delete note: {ex.Message}";
            Log($"DeleteNote ERROR: {ex.Message}");
            NotificationService.Instance.ShowError("Delete Failed", ex.Message);
        }
    }

    #endregion

    #region Pin/Archive Commands

    [RelayCommand]
    private async Task TogglePinnedAsync(Note? note)
    {
        if (note == null) return;

        try
        {
            Log($"Toggling pin for note: {note.Id}");
            var updated = await NotesService.Instance.TogglePinnedAsync(note.Id);

            if (updated != null)
            {
                // Move between collections
                if (updated.IsPinned)
                {
                    Notes.Remove(note);
                    PinnedNotes.Insert(0, updated);
                    PinnedCount++;
                }
                else
                {
                    PinnedNotes.Remove(note);
                    Notes.Insert(0, updated);
                    PinnedCount--;
                }

                // Update selected note if it was the one toggled
                if (SelectedNote?.Id == note.Id)
                {
                    SelectedNote = updated;
                }

                Log($"Note pin toggled: now {(updated.IsPinned ? "pinned" : "unpinned")}");
            }
            else
            {
                throw new Exception(NotesService.Instance.LastError ?? "Failed to toggle pin");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to toggle pin: {ex.Message}";
            Log($"TogglePinned ERROR: {ex.Message}");
        }
    }

    // Archive functionality deferred - columns not yet added to DB
    [RelayCommand]
    private Task ArchiveNoteAsync(Note? note)
    {
        if (note == null) return Task.CompletedTask;

        // Archive not implemented yet - use delete instead
        Log($"Archive requested but not implemented yet for note: {note.Id}");
        ErrorMessage = "Archive feature coming soon. Use delete for now.";
        HasError = true;
        return Task.CompletedTask;
    }

    // Restore functionality deferred - columns not yet added to DB
    [RelayCommand]
    private Task RestoreNoteAsync(Note? note)
    {
        if (note == null) return Task.CompletedTask;

        // Restore not implemented yet
        Log($"Restore requested but not implemented yet for note: {note?.Id}");
        return Task.CompletedTask;
    }

    #endregion

    #region Sub-tab Commands

    [RelayCommand]
    private void SelectSubTab(int tabIndex)
    {
        Log($"Selecting sub-tab: {tabIndex}");
        SelectedSubTab = tabIndex;
    }

    #endregion

    #region View Toggle Commands

    // Archive view toggle deferred - columns not yet added to DB
    [RelayCommand]
    private Task ToggleArchivedViewAsync()
    {
        // Archive view not implemented yet
        Log("Archive view toggle requested but not implemented yet");
        return Task.CompletedTask;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets linked entity information for a note.
    /// </summary>
    public System.Collections.Generic.List<LinkedEntityInfo> GetLinkedEntities(Note note)
    {
        return NotesService.Instance.GetLinkedEntities(note);
    }

    #endregion

    #region Entity Linking

    /// <summary>
    /// Pending links to be added when note is saved.
    /// Uses a list to support multiple links per note.
    /// </summary>
    private readonly List<(string EntityType, Guid EntityId, string EntityTitle)> _pendingLinks = new();

    /// <summary>
    /// Links to be removed when note is saved.
    /// </summary>
    private readonly List<NoteLink> _linksToRemove = new();

    /// <summary>
    /// Gets all current links for the editing note (existing + pending).
    /// </summary>
    public IEnumerable<(string Type, Guid Id, string Title)> EditingNoteLinks
    {
        get
        {
            if (EditingNote == null) return Enumerable.Empty<(string, Guid, string)>();

            // Existing links that aren't being removed
            var existingLinks = EditingNote.Links
                .Where(l => !_linksToRemove.Any(r => r.Id == l.Id))
                .Select(l => (l.EntityType, l.EntityId, l.EntityTitleSnapshot ?? l.EntityType));

            // Plus pending new links
            return existingLinks.Concat(_pendingLinks.Select(p => (p.EntityType, p.EntityId, p.EntityTitle)));
        }
    }

    /// <summary>
    /// Whether the editing note has any links (existing or pending).
    /// </summary>
    public bool EditingNoteHasLink => EditingNoteLinks.Any();

    /// <summary>
    /// Adds an entity link to the editing note.
    /// Link is staged and saved when note is saved.
    /// </summary>
    public void AddEntityLink(string entityType, Guid entityId, string entityTitle)
    {
        if (EditingNote == null) return;

        // Normalize entity type for database
        var dbEntityType = entityType.ToLowerInvariant() switch
        {
            "person" => NoteLinkEntityTypes.TeamMember,
            "goal" => NoteLinkEntityTypes.Goal,
            "task" => NoteLinkEntityTypes.Task,
            "meeting" => NoteLinkEntityTypes.Meeting,
            "project" => NoteLinkEntityTypes.Project,
            _ => entityType.ToLowerInvariant()
        };

        // Check if already linked (existing or pending)
        var alreadyLinked = EditingNote.Links.Any(l => l.EntityType == dbEntityType && l.EntityId == entityId && !_linksToRemove.Any(r => r.Id == l.Id))
                           || _pendingLinks.Any(p => p.EntityType == dbEntityType && p.EntityId == entityId);

        if (alreadyLinked)
        {
            Log($"Entity already linked: {entityType} - {entityId}");
            return;
        }

        _pendingLinks.Add((dbEntityType, entityId, entityTitle));
        NotifyEntityLinkChanged();
        Log($"Entity link staged: {dbEntityType} - {entityTitle} ({entityId})");
    }

    /// <summary>
    /// Removes an entity link from the editing note.
    /// If it's an existing link, it's staged for removal.
    /// If it's a pending link, it's just removed from pending.
    /// </summary>
    public void RemoveEntityLink(string entityType, Guid entityId)
    {
        if (EditingNote == null) return;

        // Normalize entity type
        var dbEntityType = entityType.ToLowerInvariant() switch
        {
            "person" => NoteLinkEntityTypes.TeamMember,
            _ => entityType.ToLowerInvariant()
        };

        // Check if it's a pending link
        var pendingIndex = _pendingLinks.FindIndex(p => p.EntityType == dbEntityType && p.EntityId == entityId);
        if (pendingIndex >= 0)
        {
            _pendingLinks.RemoveAt(pendingIndex);
            NotifyEntityLinkChanged();
            Log($"Pending link removed: {dbEntityType} - {entityId}");
            return;
        }

        // Check if it's an existing link
        var existingLink = EditingNote.Links.FirstOrDefault(l => l.EntityType == dbEntityType && l.EntityId == entityId);
        if (existingLink != null)
        {
            _linksToRemove.Add(existingLink);
            NotifyEntityLinkChanged();
            Log($"Existing link staged for removal: {dbEntityType} - {entityId}");
        }
    }

    /// <summary>
    /// Clears all links from the editing note.
    /// </summary>
    public void ClearAllEntityLinks()
    {
        if (EditingNote == null) return;

        // Stage all existing links for removal
        _linksToRemove.AddRange(EditingNote.Links.Where(l => !_linksToRemove.Any(r => r.Id == l.Id)));
        _pendingLinks.Clear();

        NotifyEntityLinkChanged();
        Log("All entity links cleared");
    }

    /// <summary>
    /// Resets the link staging state (called when opening note editor).
    /// </summary>
    private void ResetLinkStaging()
    {
        _pendingLinks.Clear();
        _linksToRemove.Clear();
    }

    /// <summary>
    /// Saves pending link changes to the database.
    /// Called after note is saved.
    /// </summary>
    private async Task SaveLinkChangesAsync(Guid noteId)
    {
        // Remove links
        foreach (var link in _linksToRemove)
        {
            await NotesService.Instance.RemoveNoteLinkAsync(link.Id);
        }

        // Add new links
        foreach (var (entityType, entityId, entityTitle) in _pendingLinks)
        {
            await NotesService.Instance.AddNoteLinkAsync(noteId, entityType, entityId, entityTitle);
        }

        // Reset staging
        ResetLinkStaging();
    }

    /// <summary>
    /// Notifies the UI that entity link properties have changed.
    /// </summary>
    private void NotifyEntityLinkChanged()
    {
        OnPropertyChanged(nameof(EditingNoteLinks));
        OnPropertyChanged(nameof(EditingNoteHasLink));
    }

    #endregion

    #region Project Linking (Detail Flyout)
    
    /// <summary>
    /// Event raised when the project selector popover should be shown for linking.
    /// </summary>
    public event EventHandler? ProjectSelectorRequested;
    
    /// <summary>
    /// Whether the project selector popover is open.
    /// </summary>
    [ObservableProperty]
    private bool _isProjectSelectorOpen;
    
    /// <summary>
    /// Requests the View to show the project selector popover.
    /// </summary>
    [RelayCommand]
    private void ShowProjectSelector()
    {
        IsProjectSelectorOpen = true;
        ProjectSelectorRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Hides the project selector popover.
    /// </summary>
    public void HideProjectSelector()
    {
        IsProjectSelectorOpen = false;
    }
    
    /// <summary>
    /// Links the selected note to a project.
    /// Called by the View when a project is selected in the popover.
    /// </summary>
    public async Task LinkNoteToProjectAsync(Guid projectId, string projectTitle)
    {
        if (SelectedNote == null) return;
        
        try
        {
            IsLoading = true;
            
            // If already linked to a different project, remove old link first
            if (SelectedNote.ProjectId.HasValue && SelectedNote.ProjectId != projectId)
            {
                await ProjectService.Instance.RemoveProjectLinkAsync(
                    SelectedNote.ProjectId.Value,
                    "note",
                    SelectedNote.Id);
            }
            
            // Add new link
            var link = await ProjectService.Instance.AddProjectLinkAsync(
                projectId,
                "note",
                SelectedNote.Id,
                SelectedNote.DisplayTitle);
            
            if (link != null)
            {
                // Update local state
                SelectedNote.ProjectId = projectId;
                SelectedNote.ProjectTitle = projectTitle;
                
                // Notify UI
                OnPropertyChanged(nameof(SelectedNote));
                
                NotificationService.Instance.ShowSuccess(
                    "Note Linked", 
                    $"'{SelectedNote.DisplayTitle}' linked to '{projectTitle}'");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            NotificationService.Instance.ShowError("Link Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
            IsProjectSelectorOpen = false;
        }
    }
    
    /// <summary>
    /// Unlinks the selected note from its project.
    /// </summary>
    [RelayCommand]
    private async Task UnlinkNoteFromProject()
    {
        if (SelectedNote?.ProjectId == null) return;
        
        try
        {
            IsLoading = true;
            
            var success = await ProjectService.Instance.RemoveProjectLinkAsync(
                SelectedNote.ProjectId.Value,
                "note",
                SelectedNote.Id);
            
            if (success)
            {
                var projectTitle = SelectedNote.ProjectTitle;
                
                // Update local state
                SelectedNote.ProjectId = null;
                SelectedNote.ProjectTitle = null;
                
                // Notify UI
                OnPropertyChanged(nameof(SelectedNote));
                
                NotificationService.Instance.ShowInfo(
                    "Note Unlinked", 
                    $"Removed from '{projectTitle}'");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            NotificationService.Instance.ShowError("Unlink Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    #endregion
}

/// <summary>
/// Tabs for note detail flyout.
/// </summary>
public enum NoteDetailTab
{
    Content,
    Activity
}
