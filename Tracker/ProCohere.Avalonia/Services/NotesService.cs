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

    #region Note Links Operations

    /// <summary>
    /// Loads links for a single note from note_links table.
    /// </summary>
    public async Task<List<NoteLink>> GetLinksForNoteAsync(Guid noteId)
    {
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null) return new List<NoteLink>();

        try
        {
            var result = await client.From<NoteLink>()
                .Filter("note_id", Operator.Equals, noteId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("created_at", Ordering.Ascending)
                .Get();

            return result.Models ?? new List<NoteLink>();
        }
        catch (Exception ex)
        {
            Log($"GetLinksForNote ERROR: {ex.Message}");
            return new List<NoteLink>();
        }
    }

    /// <summary>
    /// Loads links for multiple notes in one query.
    /// </summary>
    public async Task<Dictionary<Guid, List<NoteLink>>> GetLinksForNotesAsync(IEnumerable<Guid> noteIds)
    {
        var result = new Dictionary<Guid, List<NoteLink>>();
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null) return result;

        var idList = noteIds.ToList();
        if (idList.Count == 0) return result;

        try
        {
            // Initialize empty lists for all note IDs
            foreach (var id in idList)
                result[id] = new List<NoteLink>();

            var queryResult = await client.From<NoteLink>()
                .Filter("note_id", Operator.In, idList.Select(id => id.ToString()).ToList())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            foreach (var link in queryResult.Models ?? new List<NoteLink>())
            {
                if (result.ContainsKey(link.NoteId))
                    result[link.NoteId].Add(link);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log($"GetLinksForNotes ERROR: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Populates the Links collection on notes by loading from note_links table.
    /// </summary>
    public async Task PopulateLinksAsync(IEnumerable<Note> notes)
    {
        var notesList = notes.ToList();
        if (notesList.Count == 0) return;

        var linksMap = await GetLinksForNotesAsync(notesList.Select(n => n.Id));
        foreach (var note in notesList)
        {
            note.Links = linksMap.TryGetValue(note.Id, out var links) ? links : new List<NoteLink>();
        }
    }

    /// <summary>
    /// Adds a link between a note and an entity.
    /// </summary>
    public async Task<NoteLink?> AddNoteLinkAsync(Guid noteId, string entityType, Guid entityId, string? entityTitle = null)
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
            Log($"Adding link: note={noteId}, type={entityType}, entity={entityId}");

            var link = new NoteLink
            {
                Id = Guid.NewGuid(),
                OrganizationId = session.TeamMember.OrganizationId,
                NoteId = noteId,
                EntityType = entityType,
                EntityId = entityId,
                EntityTitleSnapshot = entityTitle,
                CreatedByTeamMemberId = session.TeamMember.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                SortOrder = 0
            };

            var result = await client.From<NoteLink>().Insert(link);
            var created = result.Models?.FirstOrDefault();

            if (created != null)
                Log($"Link added: {created.Id}");

            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"AddNoteLink ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Removes a link (soft delete).
    /// </summary>
    public async Task<bool> RemoveNoteLinkAsync(Guid linkId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        var session = AuthService.Instance.CurrentSession_ProCohere;

        try
        {
            Log($"Removing link: {linkId}");

            // Get the existing link
            var existingResult = await client.From<NoteLink>()
                .Filter("id", Operator.Equals, linkId.ToString())
                .Single();

            if (existingResult == null)
            {
                LastError = "Link not found";
                return false;
            }

            existingResult.IsDeleted = true;
            existingResult.DeletedAt = DateTime.UtcNow;
            existingResult.DeletedBy = session?.User?.Id;

            await client.From<NoteLink>()
                .Filter("id", Operator.Equals, linkId.ToString())
                .Update(existingResult);

            Log($"Link removed: {linkId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"RemoveNoteLink ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes a specific link by note + entity (soft delete).
    /// </summary>
    public async Task<bool> RemoveNoteLinkAsync(Guid noteId, string entityType, Guid entityId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        var session = AuthService.Instance.CurrentSession_ProCohere;

        try
        {
            Log($"Removing link: note={noteId}, type={entityType}, entity={entityId}");

            var existingResult = await client.From<NoteLink>()
                .Filter("note_id", Operator.Equals, noteId.ToString())
                .Filter("entity_type", Operator.Equals, entityType)
                .Filter("entity_id", Operator.Equals, entityId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (existingResult == null)
            {
                Log("Link not found - may already be deleted");
                return true; // Idempotent - if it's gone, that's fine
            }

            existingResult.IsDeleted = true;
            existingResult.DeletedAt = DateTime.UtcNow;
            existingResult.DeletedBy = session?.User?.Id;

            await client.From<NoteLink>()
                .Filter("id", Operator.Equals, existingResult.Id.ToString())
                .Update(existingResult);

            Log($"Link removed");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"RemoveNoteLink ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all notes linked to a specific entity via note_links table.
    /// </summary>
    public async Task<List<Note>> GetNotesForEntityViaLinksAsync(string entityType, Guid entityId)
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
            Log($"Loading notes linked to {entityType}: {entityId}");

            // First get the note IDs from links
            var linksResult = await client.From<NoteLink>()
                .Filter("entity_type", Operator.Equals, entityType)
                .Filter("entity_id", Operator.Equals, entityId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var noteIds = linksResult.Models?.Select(l => l.NoteId).Distinct().ToList() 
                          ?? new List<Guid>();

            if (noteIds.Count == 0)
                return new List<Note>();

            // Then get the actual notes
            var notesResult = await client.From<Note>()
                .Filter("id", Operator.In, noteIds.Select(id => id.ToString()).ToList())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Notes for entity returned: {notesResult.Models?.Count ?? 0}");
            return notesResult.Models ?? new List<Note>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetNotesForEntityViaLinks ERROR: {ex.Message}");
            return new List<Note>();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets linked entity info from a note's Links collection.
    /// Returns a list of all entities linked to this note.
    /// </summary>
    public List<LinkedEntityInfo> GetLinkedEntities(Note note)
    {
        return note.Links.Select(link => new LinkedEntityInfo
        {
            EntityType = ParseEntityType(link.EntityType),
            EntityId = link.EntityId,
            DisplayName = link.EntityTitleSnapshot ?? link.EntityType
        }).ToList();
    }

    /// <summary>
    /// Parses entity type string to enum.
    /// </summary>
    private static LinkedEntityType ParseEntityType(string entityType)
    {
        return entityType.ToLowerInvariant() switch
        {
            "team_member" => LinkedEntityType.TeamMember,
            "meeting" => LinkedEntityType.Meeting,
            "project" => LinkedEntityType.Project,
            "goal" => LinkedEntityType.Goal,
            "task" => LinkedEntityType.Task,
            "metric" => LinkedEntityType.Metric,
            "target" => LinkedEntityType.Target,
            _ => LinkedEntityType.None
        };
    }

    #endregion
}
