using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    private Note? _editingNote;

    public bool HasSelectedNote => SelectedNote != null;

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
    private string? _selectedCategory;

    [ObservableProperty]
    private bool _showArchived;

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

            var notes = ShowArchived 
                ? await NotesService.Instance.GetArchivedNotesAsync()
                : await NotesService.Instance.GetAllNotesAsync();

            // Clear and repopulate collections
            PinnedNotes.Clear();
            Notes.Clear();

            foreach (var note in notes)
            {
                if (note.IsPinned && !ShowArchived)
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

    [RelayCommand]
    private async Task FilterByCategoryAsync(string? category)
    {
        SelectedCategory = category;

        if (string.IsNullOrEmpty(category))
        {
            await LoadNotesAsync();
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;
            Log($"Filtering by category: {category}");

            var notes = await NotesService.Instance.GetNotesByCategoryAsync(category);

            PinnedNotes.Clear();
            Notes.Clear();

            foreach (var note in notes)
            {
                if (note.IsPinned)
                    PinnedNotes.Add(note);
                else
                    Notes.Add(note);
            }

            PinnedCount = PinnedNotes.Count;
            TotalCount = Notes.Count + PinnedNotes.Count;

            Log($"Category filter returned {TotalCount} results");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Filter failed: {ex.Message}";
            Log($"FilterByCategory ERROR: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Selection Commands

    [RelayCommand]
    private void SelectNote(Note? note)
    {
        Log($"Selecting note: {note?.Id}");
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
    }

    [RelayCommand]
    private void EditNote(Note? note)
    {
        if (note == null) return;

        Log($"Editing note: {note.Id}");
        EditingNote = note;
        IsNoteEditorOpen = true;
        IsNoteDetailOpen = false;
    }

    [RelayCommand]
    private void CloseNoteEditor()
    {
        Log("Closing note editor");
        IsNoteEditorOpen = false;
        EditingNote = null;
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

            if (EditingNote.Id == Guid.Empty)
            {
                // Create new note
                Log($"Creating note: {EditingNote.Title ?? "(untitled)"}");
                var created = await NotesService.Instance.CreateNoteAsync(EditingNote);
                
                if (created != null)
                {
                    // Add to appropriate collection
                    if (created.IsPinned)
                        PinnedNotes.Insert(0, created);
                    else
                        Notes.Insert(0, created);
                    
                    TotalCount++;
                    if (created.IsPinned) PinnedCount++;
                    
                    Log($"Note created: {created.Id}");
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
                var updated = await NotesService.Instance.UpdateNoteAsync(EditingNote);
                
                if (updated != null)
                {
                    // Update in collection
                    await LoadNotesAsync(); // Simplest approach - reload all
                    Log($"Note updated: {updated.Id}");
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

    [RelayCommand]
    private async Task ArchiveNoteAsync(Note? note)
    {
        if (note == null) return;

        try
        {
            Log($"Archiving note: {note.Id}");
            var updated = await NotesService.Instance.ArchiveNoteAsync(note.Id);

            if (updated != null)
            {
                // Remove from active collections
                PinnedNotes.Remove(note);
                Notes.Remove(note);
                
                TotalCount--;
                if (note.IsPinned) PinnedCount--;

                if (SelectedNote?.Id == note.Id)
                    CloseNoteDetail();

                Log("Note archived successfully");
            }
            else
            {
                throw new Exception(NotesService.Instance.LastError ?? "Failed to archive note");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to archive note: {ex.Message}";
            Log($"ArchiveNote ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RestoreNoteAsync(Note? note)
    {
        if (note == null) return;

        try
        {
            Log($"Restoring note: {note.Id}");
            var updated = await NotesService.Instance.RestoreNoteAsync(note.Id);

            if (updated != null)
            {
                // If viewing archived, remove from list
                if (ShowArchived)
                {
                    Notes.Remove(note);
                    TotalCount--;
                }

                Log("Note restored successfully");
            }
            else
            {
                throw new Exception(NotesService.Instance.LastError ?? "Failed to restore note");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to restore note: {ex.Message}";
            Log($"RestoreNote ERROR: {ex.Message}");
        }
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

    [RelayCommand]
    private async Task ToggleArchivedViewAsync()
    {
        ShowArchived = !ShowArchived;
        Log($"Show archived: {ShowArchived}");
        await LoadNotesAsync();
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
}

/// <summary>
/// Tabs for note detail flyout.
/// </summary>
public enum NoteDetailTab
{
    Content,
    Activity
}
