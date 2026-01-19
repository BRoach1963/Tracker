# Chronicle Notes Implementation Plan

## Overview

The Chronicle tab in ProCohere Avalonia provides a unified journal/notes system where users can capture thoughts, observations, and insights related to their work. Notes can optionally be linked to specific entities (team members, meetings, projects, goals, tasks, metrics, targets) for contextual organization.

This document details the complete implementation plan for the Notes feature within the Chronicle tab.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Database Schema](#database-schema)
3. [Models](#models)
4. [Services Layer](#services-layer)
5. [ViewModels](#viewmodels)
6. [Views & UI Components](#views--ui-components)
7. [Entity Linking System](#entity-linking-system)
8. [Data Flow](#data-flow)
9. [Implementation Phases](#implementation-phases)
10. [UI/UX Design](#uiux-design)

---

## Architecture Overview

### Layer Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         VIEWS (AXAML)                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │  ChronicleView  │  │ NoteDetailFlyout│  │ NoteEditorFlyout│ │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘ │
└───────────┼─────────────────────┼─────────────────────┼─────────┘
            │                     │                     │
            ▼                     ▼                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                       VIEWMODELS                                │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                   ChronicleViewModel                        ││
│  │  - Notes collection                                         ││
│  │  - Selected note                                            ││
│  │  - Filter/search state                                      ││
│  │  - CRUD commands                                            ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────────┐
│                       SERVICES                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                     NotesService                            ││
│  │  - GetAllNotesAsync()                                       ││
│  │  - GetNoteByIdAsync(id)                                     ││
│  │  - GetNotesForEntityAsync(entityType, entityId)             ││
│  │  - CreateNoteAsync(note)                                    ││
│  │  - UpdateNoteAsync(note)                                    ││
│  │  - DeleteNoteAsync(id)                                      ││
│  │  - SearchNotesAsync(query)                                  ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SUPABASE (PostgreSQL)                      │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                   procohere.notes                           ││
│  │  (29+ columns with entity linking FKs)                      ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### Key Architectural Principles

1. **MVVM Pattern**: Strict separation - Views contain no business logic
2. **Service Layer**: All data operations go through NotesService
3. **Supabase Direct**: Uses Supabase REST API (no Dapper in Avalonia app)
4. **CommunityToolkit.Mvvm**: ObservableObject, RelayCommand, source generators
5. **Entity Linking**: Notes can link to 0-7 different entity types

---

## Database Schema

### Table: `procohere.notes`

The notes table resides in the `procohere` schema and supports rich content with entity linking.

```sql
CREATE TABLE procohere.notes (
    -- Identity
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id         UUID NOT NULL REFERENCES procohere.organizations(id),
    author_team_member_id   UUID NOT NULL REFERENCES procohere.team_members(id),
    
    -- Content
    title                   TEXT,
    content                 TEXT NOT NULL,
    content_format          TEXT DEFAULT 'plain',  -- 'plain', 'markdown', 'html'
    
    -- Entity Linking (all nullable - note can be standalone)
    linked_team_member_id   UUID REFERENCES procohere.team_members(id) ON DELETE SET NULL,
    linked_meeting_id       UUID REFERENCES procohere.meetings(id) ON DELETE SET NULL,
    linked_project_id       UUID REFERENCES procohere.projects(id) ON DELETE SET NULL,
    linked_goal_id          UUID REFERENCES procohere.goals(id) ON DELETE SET NULL,
    linked_task_id          UUID REFERENCES procohere.tasks(id) ON DELETE SET NULL,
    linked_metric_id        UUID REFERENCES procohere.metrics(id) ON DELETE SET NULL,
    linked_target_id        UUID REFERENCES procohere.targets(id) ON DELETE SET NULL,
    
    -- Organization
    category                TEXT,                   -- User-defined category
    tags                    JSONB DEFAULT '[]',     -- Array of tag strings
    
    -- Status Flags
    is_private              BOOLEAN DEFAULT false,  -- Only visible to author
    is_pinned               BOOLEAN DEFAULT false,
    pinned_at               TIMESTAMPTZ,
    is_archived             BOOLEAN DEFAULT false,
    archived_at             TIMESTAMPTZ,
    
    -- AI Features
    ai_summary              TEXT,
    ai_suggested_actions    JSONB,
    
    -- Soft Delete
    is_deleted              BOOLEAN DEFAULT false,
    deleted_at              TIMESTAMPTZ,
    deleted_by              UUID,
    
    -- Sync/Audit
    created_at              TIMESTAMPTZ DEFAULT NOW(),
    updated_at              TIMESTAMPTZ DEFAULT NOW(),
    sync_status             TEXT DEFAULT 'synced',
    last_synced_at          TIMESTAMPTZ
);
```

### Required Schema Migration

Before implementation, run the following ALTER script to add metric/target linking:

```sql
-- Add linked_metric_id and linked_target_id to notes table
ALTER TABLE procohere.notes 
ADD COLUMN IF NOT EXISTS linked_metric_id UUID REFERENCES procohere.metrics(id) ON DELETE SET NULL;

ALTER TABLE procohere.notes 
ADD COLUMN IF NOT EXISTS linked_target_id UUID REFERENCES procohere.targets(id) ON DELETE SET NULL;

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_notes_linked_metric_id 
ON procohere.notes(linked_metric_id) WHERE linked_metric_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_notes_linked_target_id 
ON procohere.notes(linked_target_id) WHERE linked_target_id IS NOT NULL;
```

### Indexes for Performance

```sql
-- Essential indexes
CREATE INDEX idx_notes_organization_id ON procohere.notes(organization_id);
CREATE INDEX idx_notes_author_team_member_id ON procohere.notes(author_team_member_id);
CREATE INDEX idx_notes_created_at ON procohere.notes(created_at DESC);

-- Entity linking indexes (partial - only index non-null values)
CREATE INDEX idx_notes_linked_team_member_id ON procohere.notes(linked_team_member_id) 
    WHERE linked_team_member_id IS NOT NULL;
CREATE INDEX idx_notes_linked_meeting_id ON procohere.notes(linked_meeting_id) 
    WHERE linked_meeting_id IS NOT NULL;
CREATE INDEX idx_notes_linked_project_id ON procohere.notes(linked_project_id) 
    WHERE linked_project_id IS NOT NULL;
CREATE INDEX idx_notes_linked_goal_id ON procohere.notes(linked_goal_id) 
    WHERE linked_goal_id IS NOT NULL;
CREATE INDEX idx_notes_linked_task_id ON procohere.notes(linked_task_id) 
    WHERE linked_task_id IS NOT NULL;

-- Full-text search index
CREATE INDEX idx_notes_content_search ON procohere.notes 
    USING gin(to_tsvector('english', coalesce(title, '') || ' ' || content));
```

---

## Models

### Note.cs

Location: `ProCohere.Avalonia/Models/Note.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents a note/journal entry that can optionally be linked to various entities.
/// Maps to procohere.notes table.
/// </summary>
public class Note
{
    // Identity
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }
    
    [JsonPropertyName("author_team_member_id")]
    public Guid AuthorTeamMemberId { get; set; }
    
    // Content
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("content_format")]
    public string ContentFormat { get; set; } = "plain";
    
    // Entity Links (all nullable)
    [JsonPropertyName("linked_team_member_id")]
    public Guid? LinkedTeamMemberId { get; set; }
    
    [JsonPropertyName("linked_meeting_id")]
    public Guid? LinkedMeetingId { get; set; }
    
    [JsonPropertyName("linked_project_id")]
    public Guid? LinkedProjectId { get; set; }
    
    [JsonPropertyName("linked_goal_id")]
    public Guid? LinkedGoalId { get; set; }
    
    [JsonPropertyName("linked_task_id")]
    public Guid? LinkedTaskId { get; set; }
    
    [JsonPropertyName("linked_metric_id")]
    public Guid? LinkedMetricId { get; set; }
    
    [JsonPropertyName("linked_target_id")]
    public Guid? LinkedTargetId { get; set; }
    
    // Organization
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    // Status Flags
    [JsonPropertyName("is_private")]
    public bool IsPrivate { get; set; }
    
    [JsonPropertyName("is_pinned")]
    public bool IsPinned { get; set; }
    
    [JsonPropertyName("pinned_at")]
    public DateTime? PinnedAt { get; set; }
    
    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; set; }
    
    [JsonPropertyName("archived_at")]
    public DateTime? ArchivedAt { get; set; }
    
    // AI Features
    [JsonPropertyName("ai_summary")]
    public string? AiSummary { get; set; }
    
    [JsonPropertyName("ai_suggested_actions")]
    public List<string>? AiSuggestedActions { get; set; }
    
    // Soft Delete
    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }
    
    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
    
    [JsonPropertyName("deleted_by")]
    public Guid? DeletedBy { get; set; }
    
    // Timestamps
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    // Computed Properties (not mapped to DB)
    [JsonIgnore]
    public bool HasLinks => LinkedTeamMemberId.HasValue || 
                           LinkedMeetingId.HasValue || 
                           LinkedProjectId.HasValue || 
                           LinkedGoalId.HasValue || 
                           LinkedTaskId.HasValue ||
                           LinkedMetricId.HasValue ||
                           LinkedTargetId.HasValue;
    
    [JsonIgnore]
    public int LinkCount => (LinkedTeamMemberId.HasValue ? 1 : 0) +
                           (LinkedMeetingId.HasValue ? 1 : 0) +
                           (LinkedProjectId.HasValue ? 1 : 0) +
                           (LinkedGoalId.HasValue ? 1 : 0) +
                           (LinkedTaskId.HasValue ? 1 : 0) +
                           (LinkedMetricId.HasValue ? 1 : 0) +
                           (LinkedTargetId.HasValue ? 1 : 0);
    
    [JsonIgnore]
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) 
        ? (Content.Length > 50 ? Content[..50] + "..." : Content)
        : Title;
    
    [JsonIgnore]
    public string ContentPreview => Content.Length > 200 
        ? Content[..200] + "..." 
        : Content;
}
```

### NoteCategory.cs (Enum)

Location: `ProCohere.Avalonia/Models/NoteCategory.cs`

```csharp
namespace ProCohere.Avalonia.Models;

/// <summary>
/// Predefined note categories for organization.
/// </summary>
public static class NoteCategory
{
    public const string General = "general";
    public const string Observation = "observation";
    public const string Idea = "idea";
    public const string Feedback = "feedback";
    public const string Decision = "decision";
    public const string ActionItem = "action_item";
    public const string Question = "question";
    public const string Risk = "risk";
    public const string Success = "success";
    public const string Learning = "learning";
    
    public static readonly string[] All = new[]
    {
        General, Observation, Idea, Feedback, Decision,
        ActionItem, Question, Risk, Success, Learning
    };
}
```

### LinkedEntityType.cs (Enum)

Location: `ProCohere.Avalonia/Models/LinkedEntityType.cs`

```csharp
namespace ProCohere.Avalonia.Models;

/// <summary>
/// Types of entities that can be linked to a note.
/// </summary>
public enum LinkedEntityType
{
    None,
    TeamMember,
    Meeting,
    Project,
    Goal,
    Task,
    Metric,
    Target
}
```

---

## Services Layer

### INotesService.cs

Location: `ProCohere.Avalonia/Services/INotesService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service interface for note operations.
/// </summary>
public interface INotesService
{
    /// <summary>
    /// Gets all notes for the current organization (excluding archived/deleted).
    /// </summary>
    Task<List<Note>> GetAllNotesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a single note by ID.
    /// </summary>
    Task<Note?> GetNoteByIdAsync(Guid noteId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all notes linked to a specific entity.
    /// </summary>
    Task<List<Note>> GetNotesForEntityAsync(
        LinkedEntityType entityType, 
        Guid entityId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all pinned notes.
    /// </summary>
    Task<List<Note>> GetPinnedNotesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets archived notes.
    /// </summary>
    Task<List<Note>> GetArchivedNotesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches notes by content, title, or tags.
    /// </summary>
    Task<List<Note>> SearchNotesAsync(string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new note.
    /// </summary>
    Task<Note> CreateNoteAsync(Note note, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing note.
    /// </summary>
    Task<Note> UpdateNoteAsync(Note note, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft-deletes a note.
    /// </summary>
    Task<bool> DeleteNoteAsync(Guid noteId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Toggles the pinned status of a note.
    /// </summary>
    Task<Note> TogglePinnedAsync(Guid noteId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Archives a note.
    /// </summary>
    Task<Note> ArchiveNoteAsync(Guid noteId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restores an archived note.
    /// </summary>
    Task<Note> RestoreNoteAsync(Guid noteId, CancellationToken cancellationToken = default);
}
```

### NotesService.cs

Location: `ProCohere.Avalonia/Services/NotesService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using Supabase;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Supabase implementation of INotesService.
/// </summary>
public class NotesService : INotesService
{
    private readonly Client _supabase;
    private readonly IAuthService _authService;
    
    public NotesService(Client supabase, IAuthService authService)
    {
        _supabase = supabase;
        _authService = authService;
    }
    
    public async Task<List<Note>> GetAllNotesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _supabase
            .From<Note>("notes")
            .Filter("is_deleted", Postgrest.Constants.Operator.Equals, false)
            .Filter("is_archived", Postgrest.Constants.Operator.Equals, false)
            .Order("is_pinned", Postgrest.Constants.Ordering.Descending)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
            
        return response.Models;
    }
    
    public async Task<Note?> GetNoteByIdAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        var response = await _supabase
            .From<Note>("notes")
            .Filter("id", Postgrest.Constants.Operator.Equals, noteId.ToString())
            .Filter("is_deleted", Postgrest.Constants.Operator.Equals, false)
            .Single();
            
        return response;
    }
    
    public async Task<List<Note>> GetNotesForEntityAsync(
        LinkedEntityType entityType, 
        Guid entityId, 
        CancellationToken cancellationToken = default)
    {
        var columnName = entityType switch
        {
            LinkedEntityType.TeamMember => "linked_team_member_id",
            LinkedEntityType.Meeting => "linked_meeting_id",
            LinkedEntityType.Project => "linked_project_id",
            LinkedEntityType.Goal => "linked_goal_id",
            LinkedEntityType.Task => "linked_task_id",
            LinkedEntityType.Metric => "linked_metric_id",
            LinkedEntityType.Target => "linked_target_id",
            _ => throw new ArgumentException($"Invalid entity type: {entityType}")
        };
        
        var response = await _supabase
            .From<Note>("notes")
            .Filter(columnName, Postgrest.Constants.Operator.Equals, entityId.ToString())
            .Filter("is_deleted", Postgrest.Constants.Operator.Equals, false)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
            
        return response.Models;
    }
    
    public async Task<Note> CreateNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        note.Id = Guid.NewGuid();
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;
        note.OrganizationId = _authService.CurrentOrganizationId;
        note.AuthorTeamMemberId = _authService.CurrentTeamMemberId;
        
        var response = await _supabase
            .From<Note>("notes")
            .Insert(note);
            
        return response.Models.First();
    }
    
    public async Task<Note> UpdateNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        note.UpdatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<Note>("notes")
            .Filter("id", Postgrest.Constants.Operator.Equals, note.Id.ToString())
            .Update(note);
            
        return response.Models.First();
    }
    
    public async Task<bool> DeleteNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        var note = await GetNoteByIdAsync(noteId, cancellationToken);
        if (note == null) return false;
        
        note.IsDeleted = true;
        note.DeletedAt = DateTime.UtcNow;
        note.DeletedBy = _authService.CurrentTeamMemberId;
        
        await UpdateNoteAsync(note, cancellationToken);
        return true;
    }
    
    public async Task<Note> TogglePinnedAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        var note = await GetNoteByIdAsync(noteId, cancellationToken);
        if (note == null) throw new InvalidOperationException("Note not found");
        
        note.IsPinned = !note.IsPinned;
        note.PinnedAt = note.IsPinned ? DateTime.UtcNow : null;
        
        return await UpdateNoteAsync(note, cancellationToken);
    }
    
    // ... additional method implementations
}
```

---

## ViewModels

### ChronicleViewModel.cs

Location: `ProCohere.Avalonia/ViewModels/ChronicleViewModel.cs`

```csharp
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Chronicle tab containing Notes and Reports.
/// </summary>
public partial class ChronicleViewModel : ViewModelBase
{
    private readonly INotesService _notesService;
    
    // === Notes Collection ===
    [ObservableProperty]
    private ObservableCollection<Note> _notes = new();
    
    [ObservableProperty]
    private ObservableCollection<Note> _pinnedNotes = new();
    
    // === Selection State ===
    [ObservableProperty]
    private Note? _selectedNote;
    
    [ObservableProperty]
    private bool _isNoteDetailOpen;
    
    [ObservableProperty]
    private bool _isNoteEditorOpen;
    
    [ObservableProperty]
    private Note? _editingNote;
    
    // === Filter State ===
    [ObservableProperty]
    private string _searchQuery = string.Empty;
    
    [ObservableProperty]
    private string? _selectedCategory;
    
    [ObservableProperty]
    private LinkedEntityType? _filterEntityType;
    
    [ObservableProperty]
    private Guid? _filterEntityId;
    
    [ObservableProperty]
    private bool _showArchived;
    
    // === UI State ===
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    // === Sub-tab Selection ===
    [ObservableProperty]
    private int _selectedSubTab; // 0 = Notes, 1 = Reports
    
    public ChronicleViewModel(INotesService notesService)
    {
        _notesService = notesService;
    }
    
    // === Commands ===
    
    [RelayCommand]
    private async Task LoadNotesAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var notes = await _notesService.GetAllNotesAsync();
            
            Notes.Clear();
            PinnedNotes.Clear();
            
            foreach (var note in notes)
            {
                if (note.IsPinned)
                    PinnedNotes.Add(note);
                else
                    Notes.Add(note);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load notes: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void SelectNote(Note note)
    {
        SelectedNote = note;
        IsNoteDetailOpen = true;
        IsNoteEditorOpen = false;
    }
    
    [RelayCommand]
    private void CloseNoteDetail()
    {
        IsNoteDetailOpen = false;
        SelectedNote = null;
    }
    
    [RelayCommand]
    private void CreateNewNote()
    {
        EditingNote = new Note
        {
            ContentFormat = "plain"
        };
        IsNoteEditorOpen = true;
        IsNoteDetailOpen = false;
    }
    
    [RelayCommand]
    private void EditNote(Note note)
    {
        EditingNote = note;
        IsNoteEditorOpen = true;
        IsNoteDetailOpen = false;
    }
    
    [RelayCommand]
    private void CloseNoteEditor()
    {
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
            
            if (EditingNote.Id == Guid.Empty)
            {
                // Create new note
                var created = await _notesService.CreateNoteAsync(EditingNote);
                Notes.Insert(0, created);
            }
            else
            {
                // Update existing note
                var updated = await _notesService.UpdateNoteAsync(EditingNote);
                // Update in collection...
            }
            
            CloseNoteEditor();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save note: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task DeleteNoteAsync(Note note)
    {
        try
        {
            var success = await _notesService.DeleteNoteAsync(note.Id);
            if (success)
            {
                Notes.Remove(note);
                PinnedNotes.Remove(note);
                
                if (SelectedNote?.Id == note.Id)
                    CloseNoteDetail();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete note: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task TogglePinnedAsync(Note note)
    {
        try
        {
            var updated = await _notesService.TogglePinnedAsync(note.Id);
            
            // Move between pinned and unpinned collections
            if (updated.IsPinned)
            {
                Notes.Remove(note);
                PinnedNotes.Insert(0, updated);
            }
            else
            {
                PinnedNotes.Remove(note);
                Notes.Insert(0, updated);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update note: {ex.Message}";
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
            var results = await _notesService.SearchNotesAsync(SearchQuery);
            
            Notes.Clear();
            PinnedNotes.Clear();
            
            foreach (var note in results)
            {
                if (note.IsPinned)
                    PinnedNotes.Add(note);
                else
                    Notes.Add(note);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    // === Entity Link Navigation ===
    
    [RelayCommand]
    private void NavigateToLinkedEntity(LinkedEntityType entityType, Guid entityId)
    {
        // Raise navigation event to MainViewModel
        // MainViewModel handles switching tabs and opening detail flyout
    }
}
```

---

## Views & UI Components

### ChronicleView.axaml

Location: `ProCohere.Avalonia/Views/ChronicleView.axaml`

The Chronicle view uses a three-panel layout:
1. **Left Panel**: Notes/Reports sub-tab selector
2. **Center Panel**: Note list with search/filter
3. **Right Panel**: Detail flyout or editor

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ProCohere.Avalonia.ViewModels"
             xmlns:controls="using:ProCohere.Avalonia.Views.Controls"
             x:DataType="vm:ChronicleViewModel"
             x:Class="ProCohere.Avalonia.Views.ChronicleView">
    
    <Grid ColumnDefinitions="*,Auto">
        <!-- Main Content Area -->
        <Grid Grid.Column="0" RowDefinitions="Auto,*">
            
            <!-- Header with sub-tabs and actions -->
            <Grid Grid.Row="0" ColumnDefinitions="Auto,*,Auto" Margin="24,16">
                
                <!-- Notes / Reports Toggle -->
                <Border Background="{DynamicResource BrushSurface}" 
                        CornerRadius="8" Padding="4">
                    <StackPanel Orientation="Horizontal" Spacing="4">
                        <Button Content="Notes" 
                                Classes="toggle-button"
                                Classes.selected="{Binding SelectedSubTab, Converter={StaticResource EqualConverter}, ConverterParameter=0}"
                                Command="{Binding SelectSubTabCommand}"
                                CommandParameter="0"/>
                        <Button Content="Reports"
                                Classes="toggle-button"
                                Classes.selected="{Binding SelectedSubTab, Converter={StaticResource EqualConverter}, ConverterParameter=1}"
                                Command="{Binding SelectSubTabCommand}"
                                CommandParameter="1"/>
                    </StackPanel>
                </Border>
                
                <!-- Search Box -->
                <TextBox Grid.Column="1" 
                         Text="{Binding SearchQuery}"
                         Watermark="Search notes..."
                         Margin="16,0"/>
                
                <!-- New Note Button -->
                <Button Grid.Column="2"
                        Content="+ New Note"
                        Classes="primary"
                        Command="{Binding CreateNewNoteCommand}"/>
            </Grid>
            
            <!-- Notes List -->
            <ScrollViewer Grid.Row="1" IsVisible="{Binding SelectedSubTab, Converter={StaticResource EqualConverter}, ConverterParameter=0}">
                <StackPanel Margin="24,0">
                    
                    <!-- Pinned Notes Section -->
                    <TextBlock Text="Pinned" 
                               Classes="section-header"
                               IsVisible="{Binding PinnedNotes.Count, Converter={StaticResource GreaterThanZeroConverter}}"/>
                    <ItemsControl ItemsSource="{Binding PinnedNotes}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <controls:NoteListItem Note="{Binding}"
                                                       SelectCommand="{Binding $parent[UserControl].((vm:ChronicleViewModel)DataContext).SelectNoteCommand}"
                                                       TogglePinCommand="{Binding $parent[UserControl].((vm:ChronicleViewModel)DataContext).TogglePinnedCommand}"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    
                    <!-- All Notes Section -->
                    <TextBlock Text="All Notes" Classes="section-header" Margin="0,16,0,8"/>
                    <ItemsControl ItemsSource="{Binding Notes}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <controls:NoteListItem Note="{Binding}"
                                                       SelectCommand="{Binding $parent[UserControl].((vm:ChronicleViewModel)DataContext).SelectNoteCommand}"
                                                       TogglePinCommand="{Binding $parent[UserControl].((vm:ChronicleViewModel)DataContext).TogglePinnedCommand}"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    
                    <!-- Empty State -->
                    <controls:EmptyState IsVisible="{Binding !Notes.Count}"
                                         Icon="📝"
                                         Title="No notes yet"
                                         Subtitle="Create your first note to get started"
                                         ActionText="Create Note"
                                         ActionCommand="{Binding CreateNewNoteCommand}"/>
                </StackPanel>
            </ScrollViewer>
            
            <!-- Reports Content (placeholder) -->
            <Grid Grid.Row="1" IsVisible="{Binding SelectedSubTab, Converter={StaticResource EqualConverter}, ConverterParameter=1}">
                <TextBlock Text="Reports coming soon..." 
                           HorizontalAlignment="Center" 
                           VerticalAlignment="Center"/>
            </Grid>
        </Grid>
        
        <!-- Detail/Editor Flyout Panel -->
        <Border Grid.Column="1"
                Width="400"
                Background="{DynamicResource BrushSurface}"
                BorderBrush="{DynamicResource BrushBorder}"
                BorderThickness="1,0,0,0"
                IsVisible="{Binding IsNoteDetailOpen}">
            <controls:NoteDetailFlyout Note="{Binding SelectedNote}"
                                       CloseCommand="{Binding CloseNoteDetailCommand}"
                                       EditCommand="{Binding EditNoteCommand}"
                                       DeleteCommand="{Binding DeleteNoteCommand}"/>
        </Border>
        
        <Border Grid.Column="1"
                Width="400"
                Background="{DynamicResource BrushSurface}"
                BorderBrush="{DynamicResource BrushBorder}"
                BorderThickness="1,0,0,0"
                IsVisible="{Binding IsNoteEditorOpen}">
            <controls:NoteEditorFlyout Note="{Binding EditingNote}"
                                       CloseCommand="{Binding CloseNoteEditorCommand}"
                                       SaveCommand="{Binding SaveNoteCommand}"/>
        </Border>
    </Grid>
</UserControl>
```

### NoteListItem.axaml

Location: `ProCohere.Avalonia/Views/Controls/NoteListItem.axaml`

A reusable control for displaying a note in the list:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ProCohere.Avalonia.Views.Controls.NoteListItem">
    
    <Border Classes="note-card" 
            Padding="16" 
            Margin="0,0,0,8"
            Background="{DynamicResource BrushSurface}"
            CornerRadius="8"
            Cursor="Hand">
        <Border.Styles>
            <Style Selector="Border.note-card:pointerover">
                <Setter Property="Background" Value="{DynamicResource BrushSurfaceHover}"/>
            </Style>
        </Border.Styles>
        
        <Grid RowDefinitions="Auto,Auto,Auto,Auto">
            <!-- Header: Title + Pin Icon -->
            <Grid Grid.Row="0" ColumnDefinitions="*,Auto">
                <TextBlock Text="{Binding Note.DisplayTitle}"
                           Classes="note-title"
                           FontWeight="SemiBold"
                           TextTrimming="CharacterEllipsis"/>
                <Button Grid.Column="1"
                        Classes="icon-button"
                        Command="{Binding TogglePinCommand}"
                        CommandParameter="{Binding Note}">
                    <PathIcon Data="{Binding Note.IsPinned, Converter={StaticResource PinIconConverter}}"
                              Width="14" Height="14"/>
                </Button>
            </Grid>
            
            <!-- Content Preview -->
            <TextBlock Grid.Row="1"
                       Text="{Binding Note.ContentPreview}"
                       Classes="note-preview"
                       Opacity="0.7"
                       TextWrapping="Wrap"
                       MaxLines="3"
                       Margin="0,8,0,0"/>
            
            <!-- Entity Links (if any) -->
            <ItemsControl Grid.Row="2"
                          ItemsSource="{Binding Note, Converter={StaticResource NoteToLinkedEntitiesConverter}}"
                          IsVisible="{Binding Note.HasLinks}"
                          Margin="0,8,0,0">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel Orientation="Horizontal"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Classes="entity-badge" 
                                Background="{DynamicResource BrushSecondary}"
                                CornerRadius="4"
                                Padding="6,2"
                                Margin="0,0,4,4">
                            <StackPanel Orientation="Horizontal" Spacing="4">
                                <PathIcon Data="{Binding Icon}" Width="12" Height="12" Foreground="White"/>
                                <TextBlock Text="{Binding Label}" FontSize="11" Foreground="White"/>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
            
            <!-- Footer: Date + Tags -->
            <Grid Grid.Row="3" ColumnDefinitions="Auto,*" Margin="0,8,0,0">
                <TextBlock Text="{Binding Note.CreatedAt, StringFormat='{}{0:MMM d, yyyy}'}"
                           FontSize="11"
                           Opacity="0.5"/>
                <ItemsControl Grid.Column="1"
                              ItemsSource="{Binding Note.Tags}"
                              HorizontalAlignment="Right">
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <StackPanel Orientation="Horizontal" Spacing="4"/>
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border Background="{DynamicResource BrushHighlight}"
                                    CornerRadius="4"
                                    Padding="6,2">
                                <TextBlock Text="{Binding}" FontSize="10"/>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </Grid>
        </Grid>
        
        <Interaction.Behaviors>
            <EventTriggerBehavior EventName="PointerPressed">
                <InvokeCommandAction Command="{Binding SelectCommand}" 
                                     CommandParameter="{Binding Note}"/>
            </EventTriggerBehavior>
        </Interaction.Behaviors>
    </Border>
</UserControl>
```

### NoteDetailFlyout.axaml

Location: `ProCohere.Avalonia/Views/Controls/NoteDetailFlyout.axaml`

Displays full note details with tabs for Content and Activity:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ProCohere.Avalonia.Views.Controls.NoteDetailFlyout">
    
    <Grid RowDefinitions="Auto,Auto,*,Auto">
        
        <!-- Header with close button -->
        <Grid Grid.Row="0" ColumnDefinitions="*,Auto" Margin="16">
            <TextBlock Text="{Binding Note.DisplayTitle}"
                       Classes="flyout-title"
                       FontSize="18"
                       FontWeight="Bold"/>
            <Button Grid.Column="1" 
                    Classes="icon-button"
                    Command="{Binding CloseCommand}">
                <PathIcon Data="{StaticResource IconClose}" Width="16" Height="16"/>
            </Button>
        </Grid>
        
        <!-- Tab Headers -->
        <Border Grid.Row="1" Padding="16,0">
            <StackPanel Orientation="Horizontal" Spacing="16">
                <TextBlock Text="Content" Classes="tab-header"/>
                <TextBlock Text="Activity" Classes="tab-header"/>
            </StackPanel>
        </Border>
        
        <!-- Content Tab -->
        <ScrollViewer Grid.Row="2" Padding="16">
            <StackPanel Spacing="16">
                
                <!-- Full Content -->
                <TextBlock Text="{Binding Note.Content}"
                           TextWrapping="Wrap"/>
                
                <!-- Linked Entities Section -->
                <Border IsVisible="{Binding Note.HasLinks}"
                        Background="{DynamicResource BrushSurfaceAlt}"
                        CornerRadius="8"
                        Padding="12">
                    <StackPanel Spacing="8">
                        <TextBlock Text="Linked To" FontWeight="SemiBold" FontSize="12"/>
                        
                        <!-- Each linked entity as clickable chip -->
                        <WrapPanel Orientation="Horizontal">
                            <!-- Dynamically generated from note links -->
                        </WrapPanel>
                    </StackPanel>
                </Border>
                
                <!-- Tags Section -->
                <StackPanel IsVisible="{Binding Note.Tags.Count, Converter={StaticResource GreaterThanZeroConverter}}">
                    <TextBlock Text="Tags" FontWeight="SemiBold" FontSize="12" Margin="0,0,0,8"/>
                    <ItemsControl ItemsSource="{Binding Note.Tags}">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <WrapPanel/>
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background="{DynamicResource BrushHighlight}"
                                        CornerRadius="4"
                                        Padding="8,4"
                                        Margin="0,0,4,4">
                                    <TextBlock Text="{Binding}" FontSize="12"/>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
                
                <!-- Metadata -->
                <Border Background="{DynamicResource BrushSurfaceAlt}"
                        CornerRadius="8"
                        Padding="12">
                    <Grid RowDefinitions="Auto,Auto,Auto" ColumnDefinitions="Auto,*">
                        <TextBlock Grid.Row="0" Grid.Column="0" Text="Created" Opacity="0.6"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" 
                                   Text="{Binding Note.CreatedAt, StringFormat='{}{0:MMMM d, yyyy h:mm tt}'}"
                                   HorizontalAlignment="Right"/>
                        
                        <TextBlock Grid.Row="1" Grid.Column="0" Text="Updated" Opacity="0.6" Margin="0,4,0,0"/>
                        <TextBlock Grid.Row="1" Grid.Column="1" 
                                   Text="{Binding Note.UpdatedAt, StringFormat='{}{0:MMMM d, yyyy h:mm tt}'}"
                                   HorizontalAlignment="Right"
                                   Margin="0,4,0,0"/>
                        
                        <TextBlock Grid.Row="2" Grid.Column="0" Text="Category" Opacity="0.6" Margin="0,4,0,0"
                                   IsVisible="{Binding Note.Category, Converter={StaticResource NotNullConverter}}"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" 
                                   Text="{Binding Note.Category}"
                                   HorizontalAlignment="Right"
                                   Margin="0,4,0,0"
                                   IsVisible="{Binding Note.Category, Converter={StaticResource NotNullConverter}}"/>
                    </Grid>
                </Border>
            </StackPanel>
        </ScrollViewer>
        
        <!-- Footer Actions -->
        <Border Grid.Row="3" 
                BorderBrush="{DynamicResource BrushBorder}" 
                BorderThickness="0,1,0,0"
                Padding="16">
            <Grid ColumnDefinitions="*,Auto,Auto">
                <Button Content="Delete" 
                        Classes="danger"
                        Command="{Binding DeleteCommand}"
                        CommandParameter="{Binding Note}"/>
                <Button Grid.Column="1"
                        Content="Edit"
                        Classes="secondary"
                        Command="{Binding EditCommand}"
                        CommandParameter="{Binding Note}"
                        Margin="8,0"/>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

### NoteEditorFlyout.axaml

Location: `ProCohere.Avalonia/Views/Controls/NoteEditorFlyout.axaml`

Editor for creating/editing notes with entity linking:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ProCohere.Avalonia.Views.Controls.NoteEditorFlyout">
    
    <Grid RowDefinitions="Auto,*,Auto">
        
        <!-- Header -->
        <Grid Grid.Row="0" ColumnDefinitions="*,Auto" Margin="16">
            <TextBlock Text="{Binding Note.Id, Converter={StaticResource NewOrEditTitleConverter}, ConverterParameter='Note'}"
                       FontSize="18" FontWeight="Bold"/>
            <Button Grid.Column="1" Classes="icon-button" Command="{Binding CloseCommand}">
                <PathIcon Data="{StaticResource IconClose}" Width="16" Height="16"/>
            </Button>
        </Grid>
        
        <!-- Editor Content -->
        <ScrollViewer Grid.Row="1" Padding="16">
            <StackPanel Spacing="16">
                
                <!-- Title (optional) -->
                <StackPanel Spacing="4">
                    <TextBlock Text="Title (optional)" FontWeight="SemiBold" FontSize="12"/>
                    <TextBox Text="{Binding Note.Title}"
                             Watermark="Give your note a title..."/>
                </StackPanel>
                
                <!-- Content -->
                <StackPanel Spacing="4">
                    <TextBlock Text="Content" FontWeight="SemiBold" FontSize="12"/>
                    <TextBox Text="{Binding Note.Content}"
                             Watermark="Write your note..."
                             AcceptsReturn="True"
                             TextWrapping="Wrap"
                             MinHeight="200"/>
                </StackPanel>
                
                <!-- Category -->
                <StackPanel Spacing="4">
                    <TextBlock Text="Category" FontWeight="SemiBold" FontSize="12"/>
                    <ComboBox ItemsSource="{Binding Categories}"
                              SelectedItem="{Binding Note.Category}"
                              PlaceholderText="Select category..."/>
                </StackPanel>
                
                <!-- Tags -->
                <StackPanel Spacing="4">
                    <TextBlock Text="Tags" FontWeight="SemiBold" FontSize="12"/>
                    <TextBox Watermark="Add tags (comma separated)..."
                             Text="{Binding TagsText}"/>
                </StackPanel>
                
                <!-- Link to Entity Section -->
                <Border Background="{DynamicResource BrushSurfaceAlt}"
                        CornerRadius="8"
                        Padding="12">
                    <StackPanel Spacing="12">
                        <TextBlock Text="Link to..." FontWeight="SemiBold"/>
                        
                        <!-- Entity Type Selector -->
                        <ComboBox ItemsSource="{Binding EntityTypes}"
                                  SelectedItem="{Binding SelectedEntityType}"
                                  PlaceholderText="Select entity type..."/>
                        
                        <!-- Entity Selector (dynamic based on type) -->
                        <ComboBox ItemsSource="{Binding AvailableEntities}"
                                  SelectedItem="{Binding SelectedEntity}"
                                  IsVisible="{Binding SelectedEntityType, Converter={StaticResource NotNullConverter}}"
                                  PlaceholderText="Select entity..."/>
                        
                        <!-- Current Links Display -->
                        <ItemsControl ItemsSource="{Binding CurrentLinks}"
                                      IsVisible="{Binding CurrentLinks.Count}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Border Background="{DynamicResource BrushSecondary}"
                                            CornerRadius="4"
                                            Padding="8,4"
                                            Margin="0,0,4,4">
                                        <Grid ColumnDefinitions="Auto,*,Auto">
                                            <PathIcon Data="{Binding Icon}" Width="12" Height="12" Foreground="White"/>
                                            <TextBlock Grid.Column="1" Text="{Binding DisplayName}" Foreground="White" Margin="6,0"/>
                                            <Button Grid.Column="2" 
                                                    Classes="icon-button-small"
                                                    Command="{Binding RemoveLinkCommand}"
                                                    CommandParameter="{Binding}">
                                                <PathIcon Data="{StaticResource IconClose}" Width="10" Height="10" Foreground="White"/>
                                            </Button>
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>
                
                <!-- Privacy Toggle -->
                <CheckBox IsChecked="{Binding Note.IsPrivate}"
                          Content="Private note (only visible to you)"/>
            </StackPanel>
        </ScrollViewer>
        
        <!-- Footer -->
        <Border Grid.Row="2" 
                BorderBrush="{DynamicResource BrushBorder}" 
                BorderThickness="0,1,0,0"
                Padding="16">
            <Grid ColumnDefinitions="*,Auto">
                <Button Content="Cancel" 
                        Classes="secondary"
                        Command="{Binding CloseCommand}"/>
                <Button Grid.Column="1"
                        Content="Save Note"
                        Classes="primary"
                        Command="{Binding SaveCommand}"/>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

---

## Entity Linking System

### How Entity Links Work

Notes can be linked to multiple entity types simultaneously. The linking system:

1. **UI Flow**: User clicks "Link to..." → selects entity type → searches/selects specific entity
2. **Data Storage**: Each entity type has its own FK column (e.g., `linked_goal_id`)
3. **Display**: Linked entities shown as colored badges with icons
4. **Navigation**: Clicking a linked entity navigates to that entity's detail view

### Supported Entity Types

| Entity Type | FK Column | Icon | Badge Color |
|-------------|-----------|------|-------------|
| Team Member | `linked_team_member_id` | 👤 | Navy |
| Meeting | `linked_meeting_id` | 📅 | Navy |
| Project | `linked_project_id` | 📁 | Navy |
| Goal | `linked_goal_id` | 🎯 | Navy |
| Task | `linked_task_id` | ✓ | Navy |
| Metric | `linked_metric_id` | 📊 | Navy |
| Target | `linked_target_id` | 🎯 | Navy |

### LinkedEntityInfo Model

```csharp
public class LinkedEntityInfo
{
    public LinkedEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string DisplayName { get; set; }
    public string Icon { get; set; }
    
    // For removing links in editor
    public ICommand? RemoveLinkCommand { get; set; }
}
```

### NoteToLinkedEntitiesConverter

Converts a Note to a collection of `LinkedEntityInfo` for display:

```csharp
public class NoteToLinkedEntitiesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Note note) return Array.Empty<LinkedEntityInfo>();
        
        var links = new List<LinkedEntityInfo>();
        
        if (note.LinkedTeamMemberId.HasValue)
            links.Add(new LinkedEntityInfo 
            { 
                EntityType = LinkedEntityType.TeamMember, 
                EntityId = note.LinkedTeamMemberId.Value,
                DisplayName = "Team Member", // Would be resolved to actual name
                Icon = "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z"
            });
        
        // ... similar for other entity types
        
        return links;
    }
}
```

---

## Data Flow

### Loading Notes

```
┌─────────────┐     ┌─────────────────────┐     ┌─────────────┐     ┌──────────┐
│ ChronicleView│────▶│ChronicleViewModel  │────▶│NotesService │────▶│ Supabase │
│  (OnLoaded) │     │ LoadNotesCommand   │     │GetAllNotes()│     │  REST API│
└─────────────┘     └─────────────────────┘     └─────────────┘     └──────────┘
                              │                                            │
                              │◀───────────────────────────────────────────┘
                              │         List<Note>
                              ▼
                    ┌─────────────────────┐
                    │ Notes.Clear()       │
                    │ PinnedNotes.Clear() │
                    │ foreach note:       │
                    │   if pinned → Pinned│
                    │   else → Notes      │
                    └─────────────────────┘
```

### Creating a Note

```
┌───────────────┐     ┌─────────────────────┐     ┌─────────────┐
│ NoteEditor    │────▶│ChronicleViewModel  │────▶│NotesService │
│ SaveButton    │     │ SaveNoteCommand    │     │CreateNote() │
└───────────────┘     └─────────────────────┘     └─────────────┘
                              │                          │
                              │                          ▼
                              │                    ┌──────────┐
                              │                    │ Supabase │
                              │                    │ INSERT   │
                              │                    └──────────┘
                              │                          │
                              │◀─────────────────────────┘
                              │     Created Note
                              ▼
                    ┌─────────────────────┐
                    │ Notes.Insert(0, note)│
                    │ CloseNoteEditor()   │
                    └─────────────────────┘
```

### Navigating to Linked Entity

```
┌───────────────┐     ┌─────────────────────┐     ┌─────────────┐
│ NoteDetail    │────▶│ChronicleViewModel  │────▶│MainViewModel│
│ EntityBadge   │     │NavigateToLinked()  │     │ (Messenger) │
│ (Click)       │     │                     │     │             │
└───────────────┘     └─────────────────────┘     └─────────────┘
                                                        │
                                                        ▼
                                              ┌─────────────────┐
                                              │ Switch to Goals │
                                              │ tab, open Goal  │
                                              │ detail flyout   │
                                              └─────────────────┘
```

---

## Implementation Phases

### Phase 10A: Core Notes Infrastructure (2-3 hours)

1. **Create Models**
   - `Note.cs` with all properties and JSON attributes
   - `NoteCategory.cs` static class
   - `LinkedEntityType.cs` enum
   - `LinkedEntityInfo.cs` for display

2. **Create NotesService**
   - `INotesService.cs` interface
   - `NotesService.cs` implementation
   - Register in DI container

3. **Update MainViewModel**
   - Add ChronicleViewModel property
   - Handle navigation to Chronicle tab

### Phase 10B: Chronicle View Structure (2 hours)

1. **Create ChronicleView.axaml**
   - Notes/Reports sub-tab toggle
   - Search box
   - New Note button
   - Notes list area with pinned section
   - Flyout panel area

2. **Create ChronicleViewModel.cs**
   - Notes and PinnedNotes collections
   - Filter/search state
   - Selection state
   - CRUD commands

### Phase 10C: Note List Item (1-2 hours)

1. **Create NoteListItem.axaml**
   - Card layout with hover state
   - Title/preview display
   - Entity link badges
   - Date and tags
   - Pin toggle button

2. **Create NoteToLinkedEntitiesConverter**

### Phase 10D: Note Detail Flyout (2 hours)

1. **Create NoteDetailFlyout.axaml**
   - Content/Activity tabs
   - Full content display
   - Linked entities section (clickable)
   - Tags display
   - Metadata section
   - Edit/Delete actions

2. **Wire up navigation from linked entities**

### Phase 10E: Note Editor Flyout (2-3 hours)

1. **Create NoteEditorFlyout.axaml**
   - Title and content fields
   - Category dropdown
   - Tags input
   - Entity linking UI
   - Privacy toggle
   - Save/Cancel buttons

2. **Implement entity search/selection**
   - Load available entities by type
   - Filter/search within entity list
   - Add/remove links

### Phase 10F: Integration & Testing (1-2 hours)

1. **Wire everything together**
   - Register views and viewmodels
   - Connect navigation from other views
   - Test CRUD operations

2. **Polish UI**
   - Loading states
   - Error handling
   - Empty states
   - Animations

---

## UI/UX Design

### Color Scheme (from App Theme)

| Element | Color | Usage |
|---------|-------|-------|
| BrushSecondary | #FF2E3A5A (Navy) | Entity badges, buttons |
| BrushHighlight | #FFD0AF5F (Gold) | Tags, accents |
| BrushSurface | App surface color | Cards, flyout background |
| BrushBorder | Border color | Dividers, card borders |

### Note Card Visual Design

```
┌──────────────────────────────────────────────────────────┐
│ Meeting Notes from Q1 Planning                      📌   │
├──────────────────────────────────────────────────────────┤
│ Discussed roadmap priorities for Q1. Key decisions:     │
│ 1. Focus on mobile app launch                           │
│ 2. Hire 2 additional engineers...                       │
├──────────────────────────────────────────────────────────┤
│ [📅 Q1 Planning Meeting] [🎯 Mobile Launch]             │
├──────────────────────────────────────────────────────────┤
│ Jan 15, 2026              [planning] [strategy] [Q1]    │
└──────────────────────────────────────────────────────────┘
```

### Empty State Design

```
        ┌─────────────────────────────────────┐
        │                                     │
        │               📝                    │
        │                                     │
        │         No notes yet                │
        │                                     │
        │   Create your first note to        │
        │   start capturing your thoughts    │
        │                                     │
        │       [+ Create Note]              │
        │                                     │
        └─────────────────────────────────────┘
```

---

## File Locations Summary

| Component | File Path |
|-----------|-----------|
| Note Model | `Models/Note.cs` |
| NoteCategory | `Models/NoteCategory.cs` |
| LinkedEntityType | `Models/LinkedEntityType.cs` |
| LinkedEntityInfo | `Models/LinkedEntityInfo.cs` |
| INotesService | `Services/INotesService.cs` |
| NotesService | `Services/NotesService.cs` |
| ChronicleViewModel | `ViewModels/ChronicleViewModel.cs` |
| ChronicleView | `Views/ChronicleView.axaml` |
| NoteListItem | `Views/Controls/NoteListItem.axaml` |
| NoteDetailFlyout | `Views/Controls/NoteDetailFlyout.axaml` |
| NoteEditorFlyout | `Views/Controls/NoteEditorFlyout.axaml` |
| Converters | `Converters/NoteConverters.cs` |

---

## Dependencies

### NuGet Packages (Already Included)

- `CommunityToolkit.Mvvm` - MVVM infrastructure
- `Supabase` - Database client
- `Avalonia.Xaml.Behaviors` - Interaction behaviors

### Cross-Component Dependencies

- **AuthService**: Required for `CurrentOrganizationId` and `CurrentTeamMemberId`
- **MainViewModel**: For tab navigation when clicking linked entities
- **Other ViewModels**: GoalsViewModel, TasksViewModel, etc. for entity lookup

---

## Testing Considerations

### Unit Tests

1. **NotesService Tests**
   - CRUD operations
   - Entity filtering
   - Search functionality

2. **ChronicleViewModel Tests**
   - Collection management
   - Command execution
   - State transitions

### Integration Tests

1. **End-to-end note creation**
2. **Entity linking roundtrip**
3. **Navigation from linked entities

---

## Future Enhancements (Out of Scope for Phase 10)

1. **Rich Text Editor**: Markdown or WYSIWYG editing
2. **AI Features**: Auto-summarization, suggested actions
3. **Note Templates**: Pre-defined templates for common note types
4. **Bulk Operations**: Multi-select, bulk archive/delete
5. **Export**: Export notes to PDF, Markdown
6. **Note Sharing**: Share notes with team members
7. **Attachments**: File attachments to notes
8. **Note History**: Version history and restore

---

## Appendix: SQL Queries Reference

### Get All Active Notes

```sql
SELECT * FROM procohere.notes
WHERE is_deleted = false
  AND is_archived = false
ORDER BY is_pinned DESC, created_at DESC;
```

### Get Notes for Entity

```sql
SELECT * FROM procohere.notes
WHERE linked_goal_id = $1
  AND is_deleted = false
ORDER BY created_at DESC;
```

### Full-Text Search

```sql
SELECT * FROM procohere.notes
WHERE is_deleted = false
  AND to_tsvector('english', coalesce(title, '') || ' ' || content) @@ plainto_tsquery('english', $1)
ORDER BY ts_rank(to_tsvector('english', coalesce(title, '') || ' ' || content), plainto_tsquery('english', $1)) DESC;
```

---

*Document created: January 19, 2026*
*Last updated: January 19, 2026*
