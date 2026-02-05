using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Controls;
using ProCohere.Avalonia.Attributes;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Chronicle view - displays notes in a responsive card grid layout.
/// </summary>
[HelpContext("chronicle", ContextName = "ChronicleView")]
public partial class ChronicleView : UserControl
{
    private ChronicleViewModel? _viewModel;
    private Popup? _projectSelectorPopup;
    private ProjectSelectorPopover? _projectSelectorPopover;

    public ChronicleView()
    {
        InitializeComponent();
        
        _viewModel = new ChronicleViewModel();
        DataContext = _viewModel;
        
        Log("[ChronicleView] Constructor - ViewModel created");
        
        // Create the project selector popup
        _projectSelectorPopover = new ProjectSelectorPopover();
        _projectSelectorPopover.ProjectSelected += OnProjectSelected;
        
        _projectSelectorPopup = new Popup
        {
            Child = _projectSelectorPopover,
            Placement = PlacementMode.Pointer,
            IsLightDismissEnabled = true
        };
        
        // Subscribe to property changes to update empty state visibility
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Subscribe to project selector event
        _viewModel.ProjectSelectorRequested += OnProjectSelectorRequested;
        
        // Load notes when view is attached
        AttachedToVisualTree += OnAttachedToVisualTree;
        
        // Handle keyboard navigation
        KeyDown += OnKeyDown;
    }
    
    private void OnProjectSelectorRequested(object? sender, EventArgs e)
    {
        Log("[ChronicleView] ProjectSelectorRequested");
        if (_projectSelectorPopup != null)
        {
            _projectSelectorPopup.PlacementTarget = this;
            _projectSelectorPopup.IsOpen = true;
        }
    }
    
    private async void OnProjectSelected(object? sender, Project project)
    {
        Log($"[ChronicleView] ProjectSelected: {project.Name}");
        _projectSelectorPopup?.Close();
        _viewModel?.HideProjectSelector();
        
        if (_viewModel != null)
        {
            await _viewModel.LinkNoteToProjectAsync(project.Id, project.Name);
        }
    }

    private async void OnAttachedToVisualTree(object? sender, global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        Log("[ChronicleView] Attached to visual tree, loading notes...");
        
        if (_viewModel != null)
        {
            await _viewModel.LoadNotesCommand.ExecuteAsync(null);
            UpdateVisibility();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChronicleViewModel.TotalCount) ||
            e.PropertyName == nameof(ChronicleViewModel.IsLoading) ||
            e.PropertyName == nameof(ChronicleViewModel.HasError))
        {
            UpdateVisibility();
        }
    }

    /// <summary>
    /// Updates visibility of empty state and scroll viewer based on state.
    /// </summary>
    private void UpdateVisibility()
    {
        if (_viewModel == null) return;
        
        var isLoaded = !_viewModel.IsLoading && !_viewModel.HasError;
        var hasNotes = _viewModel.TotalCount > 0;
        
        // Show scroll viewer when loaded (regardless of note count - empty state is inside)
        if (NotesScrollViewer != null)
        {
            NotesScrollViewer.IsVisible = isLoaded;
        }
        
        // Show empty state only when loaded with no notes
        if (EmptyState != null)
        {
            EmptyState.IsVisible = isLoaded && !hasNotes;
        }
        
        Log($"[ChronicleView] Visibility update: Loaded={isLoaded}, HasNotes={hasNotes}, ScrollerVisible={NotesScrollViewer?.IsVisible}, EmptyVisible={EmptyState?.IsVisible}");
    }

    /// <summary>
    /// Handles Enter key press in search box to trigger search.
    /// </summary>
    private async void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel != null)
        {
            Log($"[ChronicleView] Search triggered: {_viewModel.SearchQuery}");
            await _viewModel.SearchCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Handles click on a NoteCard to select the note.
    /// </summary>
    private void OnNoteCardClicked(object? sender, NoteCardClickedEventArgs e)
    {
        if (_viewModel != null)
        {
            Log($"[ChronicleView] Note card clicked: {e.Note.Id}");
            _viewModel.SelectNoteCommand.Execute(e.Note);
        }
    }

    /// <summary>
    /// Handles keyboard navigation for notes.
    /// Arrow keys: Navigate between notes
    /// Enter: Edit selected note
    /// Delete: Delete selected note
    /// Escape: Close detail panel
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel == null) return;
        
        // Don't intercept if focus is in a text input
        if (e.Source is TextBox) return;

        switch (e.Key)
        {
            case Key.Escape:
                // Close detail panel or editor
                if (_viewModel.IsNoteEditorOpen)
                {
                    _viewModel.CloseNoteEditorCommand.Execute(null);
                    e.Handled = true;
                }
                else if (_viewModel.IsNoteDetailOpen)
                {
                    _viewModel.CloseNoteDetailCommand.Execute(null);
                    e.Handled = true;
                }
                break;

            case Key.Enter:
                // Edit selected note
                if (_viewModel.SelectedNote != null && !_viewModel.IsNoteEditorOpen)
                {
                    _viewModel.EditNoteCommand.Execute(_viewModel.SelectedNote);
                    e.Handled = true;
                }
                break;

            case Key.Delete:
                // Delete selected note
                if (_viewModel.SelectedNote != null && !_viewModel.IsNoteEditorOpen)
                {
                    _viewModel.DeleteNoteCommand.Execute(_viewModel.SelectedNote);
                    e.Handled = true;
                }
                break;

            case Key.Up:
            case Key.Left:
                // Navigate to previous note
                NavigateNotes(-1);
                e.Handled = true;
                break;

            case Key.Down:
            case Key.Right:
                // Navigate to next note
                NavigateNotes(1);
                e.Handled = true;
                break;

            case Key.N:
                // Ctrl+N: Create new note
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    _viewModel.CreateNewNoteCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// Navigate between notes in the list.
    /// </summary>
    /// <param name="direction">-1 for previous, 1 for next</param>
    private void NavigateNotes(int direction)
    {
        if (_viewModel == null) return;

        // Combine pinned and regular notes for navigation
        var allNotes = new List<Note>();
        allNotes.AddRange(_viewModel.PinnedNotes);
        allNotes.AddRange(_viewModel.Notes);

        if (allNotes.Count == 0) return;

        var currentIndex = -1;
        if (_viewModel.SelectedNote != null)
        {
            currentIndex = allNotes.FindIndex(n => n.Id == _viewModel.SelectedNote.Id);
        }

        var newIndex = currentIndex + direction;
        
        // Wrap around
        if (newIndex < 0) newIndex = allNotes.Count - 1;
        if (newIndex >= allNotes.Count) newIndex = 0;

        var newNote = allNotes[newIndex];
        _viewModel.SelectNoteCommand.Execute(newNote);
        
        Log($"[ChronicleView] Keyboard navigation: {direction}, selected note index {newIndex}");
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProCohere", "chronicle_view.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(message);
    }
}
