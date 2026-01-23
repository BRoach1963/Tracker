using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing notes in Supabase procohere schema.
/// Handles CRUD operations, search, and entity linking.
/// </summary>
public class NotesService
{
    #region Singleton

    private static readonly Lazy<NotesService> _instance =
        new(() => new NotesService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static NotesService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "notes_service.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
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

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private NotesService() { }

    #region Read Operations

    /// <summary>
    /// Gets all notes for the organization (excluding deleted).
    /// Ordered by pinned first, then by creation date descending.
    /// </summary>
    public async Task<List<Note>> GetAllNotesAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<Note>();
        }

        try
        {
            Log("Loading all notes");

            var result = await client.From<Note>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("is_pinned", Ordering.Descending)
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Notes returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<Note>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetAllNotes ERROR: {ex.Message}");
            return new List<Note>();
        }
    }

    /// <summary>
    /// Gets a single note by ID.
    /// </summary>
    public async Task<Note?> GetNoteByIdAsync(Guid noteId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Getting note: {noteId}");

            var result = await client.From<Note>()
                .Filter("id", Operator.Equals, noteId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetNoteById ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all notes linked to a specific entity (legacy column-based linking).
    /// </summary>
    public async Task<List<Note>> GetNotesForEntityAsync(LinkedEntityType entityType, Guid entityId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<Note>();
        }

        if (entityType == LinkedEntityType.None)
        {
            LastError = "Invalid entity type";
            return new List<Note>();
        }

        try
        {
            var columnName = entityType.GetColumnName();
            Log($"Loading notes for {entityType}: {entityId}");

            var result = await client.From<Note>()
                .Filter(columnName, Operator.Equals, entityId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Notes for entity returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<Note>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetNotesForEntity ERROR: {ex.Message}");
            return new List<Note>();
        }
    }

    /// <summary>
    /// Gets all pinned notes.
    /// </summary>
    public async Task<List<Note>> GetPinnedNotesAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<Note>();
        }

        try
        {
            Log("Loading pinned notes");

            var result = await client.From<Note>()
                .Filter("is_pinned", Operator.Equals, "true")
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("pinned_at", Ordering.Descending)
                .Get();

            Log($"Pinned notes returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<Note>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetPinnedNotes ERROR: {ex.Message}");
            return new List<Note>();
        }
    }

    /// <summary>
    /// Searches notes by content or title.
    /// Uses case-insensitive ILIKE for simple search.
    /// </summary>
    public async Task<List<Note>> SearchNotesAsync(string query)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<Note>();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllNotesAsync();
        }

        try
        {
            Log($"Searching notes: {query}");

            var searchPattern = $"%{query}%";
            
            var result = await client.From<Note>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("title", Operator.ILike, searchPattern)
                .Order("created_at", Ordering.Descending)
                .Get();

            // Also search content separately and merge results
            var contentResult = await client.From<Note>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("content", Operator.ILike, searchPattern)
                .Order("created_at", Ordering.Descending)
                .Get();

            // Merge and deduplicate
            var allResults = (result.Models ?? new List<Note>())
                .Concat(contentResult.Models ?? new List<Note>())
                .DistinctBy(n => n.Id)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.CreatedAt)
                .ToList();

            Log($"Search results: {allResults.Count}");
            return allResults;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"SearchNotes ERROR: {ex.Message}");
            return new List<Note>();
        }
    }

    #endregion

    #region Create Operations

    /// <summary>
    /// Creates a new note.
    /// </summary>
    public async Task<Note?> CreateNoteAsync(Note note)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        var session = AuthService.Instance.CurrentSession_ProCohere;
        if (session?.TeamMember == null)
        {
            LastError = "No team member session";
            return null;
        }

        try
        {
            Log($"Creating note: {note.Title ?? "(untitled)"}");

            // Set required fields
            note.Id = Guid.NewGuid();
            note.OrganizationId = session.TeamMember.OrganizationId;
            note.AuthorTeamMemberId = session.TeamMember.Id;
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            note.IsDeleted = false;

            var result = await client.From<Note>()
                .Insert(note);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Note created: {created.Id}");
            }
            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateNote ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    public async Task<Note?> UpdateNoteAsync(Note note)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Updating note: {note.Id}");

            note.UpdatedAt = DateTime.UtcNow;

            var result = await client.From<Note>()
                .Filter("id", Operator.Equals, note.Id.ToString())
                .Update(note);

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
            {
                Log($"Note updated: {updated.Id}");
            }
            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateNote ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Toggles the pinned status of a note.
    /// </summary>
    public async Task<Note?> TogglePinnedAsync(Guid noteId)
    {
        LastError = null;
        
        var note = await GetNoteByIdAsync(noteId);
        if (note == null)
        {
            LastError = "Note not found";
            return null;
        }

        try
        {
            Log($"Toggling pinned status for note: {noteId}");

            note.IsPinned = !note.IsPinned;
            note.PinnedAt = note.IsPinned ? DateTime.UtcNow : null;

            return await UpdateNoteAsync(note);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"TogglePinned ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Soft-deletes a note.
    /// </summary>
    public async Task<bool> DeleteNoteAsync(Guid noteId)
    {
        LastError = null;
        
        var note = await GetNoteByIdAsync(noteId);
        if (note == null)
        {
            LastError = "Note not found";
            return false;
        }

        var session = AuthService.Instance.CurrentSession_ProCohere;

        try
        {
            Log($"Deleting note: {noteId}");

            note.IsDeleted = true;
            note.DeletedAt = DateTime.UtcNow;
            note.DeletedBy = session?.TeamMember?.Id;

            var result = await UpdateNoteAsync(note);
            return result != null;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteNote ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets linked entity info from a note using column-based links.
    /// Returns a list of all entities linked to this note.
    /// Note: Display names are placeholders - would need additional queries to resolve.
    /// </summary>
    public List<LinkedEntityInfo> GetLinkedEntities(Note note)
    {
        var links = new List<LinkedEntityInfo>();

        if (note.LinkedTeamMemberId.HasValue)
        {
            links.Add(new LinkedEntityInfo
            {
                EntityType = LinkedEntityType.TeamMember,
                EntityId = note.LinkedTeamMemberId.Value,
                DisplayName = "Team Member"
            });
        }

        if (note.LinkedMeetingId.HasValue)
        {
            links.Add(new LinkedEntityInfo
            {
                EntityType = LinkedEntityType.Meeting,
                EntityId = note.LinkedMeetingId.Value,
                DisplayName = "Meeting"
            });
        }

        if (note.LinkedProjectId.HasValue)
        {
            links.Add(new LinkedEntityInfo
            {
                EntityType = LinkedEntityType.Project,
                EntityId = note.LinkedProjectId.Value,
                DisplayName = "Project"
            });
        }

        if (note.LinkedGoalId.HasValue)
        {
            links.Add(new LinkedEntityInfo
            {
                EntityType = LinkedEntityType.Goal,
                EntityId = note.LinkedGoalId.Value,
                DisplayName = "Goal"
            });
        }

        if (note.LinkedTaskId.HasValue)
        {
            links.Add(new LinkedEntityInfo
            {
                EntityType = LinkedEntityType.Task,
                EntityId = note.LinkedTaskId.Value,
                DisplayName = "Task"
            });
        }

        return links;
    }

    /// <summary>
    /// Sets a link on a note for the specified entity type.
    /// </summary>
    public void SetNoteLink(Note note, LinkedEntityType entityType, Guid? entityId)
    {
        switch (entityType)
        {
            case LinkedEntityType.TeamMember:
                note.LinkedTeamMemberId = entityId;
                break;
            case LinkedEntityType.Meeting:
                note.LinkedMeetingId = entityId;
                break;
            case LinkedEntityType.Project:
                note.LinkedProjectId = entityId;
                break;
            case LinkedEntityType.Goal:
                note.LinkedGoalId = entityId;
                break;
            case LinkedEntityType.Task:
                note.LinkedTaskId = entityId;
                break;
            // Metric and Target not supported - columns don't exist in DB
        }
    }

    /// <summary>
    /// Clears all entity links from a note.
    /// </summary>
    public void ClearNoteLinks(Note note)
    {
        note.LinkedTeamMemberId = null;
        note.LinkedMeetingId = null;
        note.LinkedProjectId = null;
        note.LinkedGoalId = null;
        note.LinkedTaskId = null;
    }

    #endregion
}
