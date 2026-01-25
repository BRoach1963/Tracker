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
/// Service for managing meeting notes in Supabase.
/// Handles CRUD operations for personal (private) and shared notes.
/// </summary>
public class MeetingNoteService
{
    #region Singleton

    private static readonly Lazy<MeetingNoteService> _instance =
        new(() => new MeetingNoteService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static MeetingNoteService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "meeting_note_service.log");

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

    private MeetingNoteService() { }

    /// <summary>
    /// Gets all notes for a meeting, separated into my notes and shared notes.
    /// </summary>
    /// <param name="meetingId">The meeting ID.</param>
    /// <returns>Tuple of (myNotes, sharedNotes).</returns>
    public async Task<(List<MeetingNote> MyNotes, List<MeetingNote> SharedNotes)> GetNotesForMeetingAsync(Guid meetingId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var currentUserId = AuthService.Instance.CurrentProfile?.Id;

        if (client == null)
        {
            LastError = "Not authenticated";
            return (new List<MeetingNote>(), new List<MeetingNote>());
        }

        try
        {
            Log($"Loading notes for meeting: {meetingId}");

            var result = await client.From<MeetingNote>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            var allNotes = result.Models ?? new List<MeetingNote>();

            // Separate into my private notes and shared notes
            // IsShared=false means private, IsShared=true means shared
            var myNotes = allNotes
                .Where(n => !n.IsShared && n.AuthorId == currentUserId)
                .ToList();

            var sharedNotes = allNotes
                .Where(n => n.IsShared)
                .ToList();

            Log($"Loaded {myNotes.Count} personal notes, {sharedNotes.Count} shared notes");
            return (myNotes, sharedNotes);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetNotesForMeeting ERROR: {ex.Message}");
            return (new List<MeetingNote>(), new List<MeetingNote>());
        }
    }

    /// <summary>
    /// Creates a new meeting note using the procohere.insert_meeting_note RPC.
    /// The RPC returns the new UUID and handles organization_id, author_id internally.
    /// </summary>
    public async Task<MeetingNote?> CreateNoteAsync(
        Guid meetingId, 
        string content, 
        bool isPrivate = true,
        string? visibilityScope = null,
        string? sharedContext = null,
        string? privateContext = null,
        int? sortOrder = null,
        Guid? relatedAgendaItemId = null)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            var orgId = session.TeamMember.OrganizationId;
            var authorId = session.TeamMember.Id;
            Log($"Creating note for meeting: {meetingId} (private: {isPrivate})");

            // Default visibility_scope based on isPrivate
            visibilityScope ??= isPrivate ? "personal" : "meeting";

            // Use RPC to insert - it returns the new UUID
            // RPC signature: procohere.insert_meeting_note(p_meeting_id, p_content, p_is_shared)
            var rpcResult = await client.Rpc("insert_meeting_note", new
            {
                p_meeting_id = meetingId,
                p_content = content,
                p_is_shared = !isPrivate  // Note: RPC uses is_shared (inverted from isPrivate)
            });

            Log($"Insert meeting note RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"CreateNote ERROR: {LastError}");
                return null;
            }

            // Parse the returned UUID from the RPC result
            var newId = ParseUuidFromRpcResult(rpcResult?.Content);
            if (newId == Guid.Empty)
            {
                LastError = "Failed to parse UUID from RPC result";
                Log($"CreateNote ERROR: {LastError}");
                return null;
            }

            // Return a populated item with the database-assigned ID
            var created = new MeetingNote
            {
                Id = newId,
                OrganizationId = orgId,
                MeetingId = meetingId,
                AuthorId = authorId,
                Content = content,
                IsShared = !isPrivate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Log($"Note created: {created.Id}");
            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateNote ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses a UUID from the RPC result content.
    /// Expected format: "uuid-value" or just the raw UUID string.
    /// </summary>
    private Guid ParseUuidFromRpcResult(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Guid.Empty;

        // Remove quotes if present
        var cleaned = content.Trim().Trim('"');
        
        if (Guid.TryParse(cleaned, out var guid))
            return guid;

        return Guid.Empty;
    }

    /// <summary>
    /// Creates a quick personal note with default content.
    /// </summary>
    public async Task<MeetingNote?> CreateQuickNoteAsync(Guid meetingId, string content = "New note")
    {
        return await CreateNoteAsync(meetingId, content, isPrivate: true);
    }

    /// <summary>
    /// Creates a shared note visible to all meeting attendees.
    /// </summary>
    public async Task<MeetingNote?> CreateSharedNoteAsync(Guid meetingId, string content)
    {
        return await CreateNoteAsync(meetingId, content, isPrivate: false);
    }

    /// <summary>
    /// Updates a note's content using the procohere.update_meeting_note RPC.
    /// Only the author can update a note.
    /// </summary>
    public async Task<MeetingNote?> UpdateNoteAsync(MeetingNote note)
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

            // Use RPC to update - only author can update
            // RPC signature: procohere.update_meeting_note(p_id, p_content, p_is_shared)
            var rpcResult = await client.Rpc("update_meeting_note", new
            {
                p_id = note.Id,
                p_content = note.Content,
                p_is_shared = note.IsShared
            });

            Log($"Update meeting note RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdateNote ERROR: {LastError}");
                return null;
            }

            note.UpdatedAt = DateTime.UtcNow;
            Log($"Note updated: {note.Id}");
            return note;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateNote ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Deletes a note using the procohere.delete_meeting_note RPC.
    /// Only the author can delete a note.
    /// </summary>
    public async Task<bool> DeleteNoteAsync(Guid noteId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Deleting note: {noteId}");

            // Use RPC to delete - only author can delete
            // RPC signature: procohere.delete_meeting_note(p_id)
            var rpcResult = await client.Rpc("delete_meeting_note", new
            {
                p_id = noteId
            });

            Log($"Delete meeting note RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"DeleteNote ERROR: {LastError}");
                return false;
            }

            Log($"Note deleted: {noteId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteNote ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Toggles a note between private and shared using the update RPC.
    /// </summary>
    public async Task<bool> TogglePrivacyAsync(Guid noteId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Toggling note privacy: {noteId}");

            // First get the note to determine current state
            var getResult = await client.From<MeetingNote>()
                .Filter("id", Operator.Equals, noteId.ToString())
                .Single();

            if (getResult == null)
            {
                LastError = "Note not found";
                return false;
            }

            // Toggle: IsShared = false means private, IsShared = true means shared
            var newIsPrivate = getResult.IsShared; // flip it

            // Use RPC to update privacy
            var rpcResult = await client.Rpc("update_meeting_note", new
            {
                p_id = noteId,
                p_is_shared = !newIsPrivate
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"TogglePrivacy ERROR: {LastError}");
                return false;
            }

            Log($"Note visibility toggled: {noteId} (now private: {newIsPrivate})");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"TogglePrivacy ERROR: {ex.Message}");
            return false;
        }
    }
}
