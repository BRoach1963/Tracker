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
    /// Creates a new meeting note.
    /// </summary>
    public async Task<MeetingNote?> CreateNoteAsync(Guid meetingId, string content, bool isPrivate = true)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var currentUserId = AuthService.Instance.CurrentProfile?.Id;

        if (client == null || currentUserId == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Creating note for meeting: {meetingId} (private: {isPrivate})");

            var note = new MeetingNote
            {
                Id = Guid.NewGuid(),
                MeetingId = meetingId,
                AuthorId = currentUserId.Value,
                Content = content,
                IsShared = !isPrivate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<MeetingNote>()
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
    /// Updates a note's content.
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

            note.UpdatedAt = DateTime.UtcNow;

            var result = await client.From<MeetingNote>()
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
    /// Deletes a note (hard delete - notes don't use soft delete pattern).
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

            await client.From<MeetingNote>()
                .Filter("id", Operator.Equals, noteId.ToString())
                .Delete();

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
    /// Toggles a note between private and shared.
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

            // First get the note
            var getResult = await client.From<MeetingNote>()
                .Filter("id", Operator.Equals, noteId.ToString())
                .Single();

            if (getResult == null)
            {
                LastError = "Note not found";
                return false;
            }

            getResult.IsPrivate = !getResult.IsPrivate;
            getResult.UpdatedAt = DateTime.UtcNow;

            var updateResult = await client.From<MeetingNote>()
                .Filter("id", Operator.Equals, noteId.ToString())
                .Update(getResult);

            var success = updateResult.Models?.Count > 0;
            Log($"Note privacy toggled: {success} (now private: {getResult.IsPrivate})");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"TogglePrivacy ERROR: {ex.Message}");
            return false;
        }
    }
}
