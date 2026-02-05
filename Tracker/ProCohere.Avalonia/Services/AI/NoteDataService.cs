using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces.AI;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// AI data service implementation for note/chronicle operations.
/// Wraps NotesService with AI-friendly interface.
/// </summary>
public class NoteDataService : INoteDataService
{
    private readonly NotesService _notesService;

    public NoteDataService()
    {
        _notesService = NotesService.Instance;
    }

    public async Task<string> CreateNoteAsync(string title, string content, string? tags = null)
    {
        try
        {
            // Parse tags if provided
            var tagList = new List<string>();
            if (!string.IsNullOrEmpty(tags))
            {
                tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
            }

            // Create note
            var note = new Note
            {
                Title = title,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdNote = await _notesService.CreateNoteAsync(note);
            
            if (createdNote != null)
            {
                var tagText = tagList.Any() ? $" with tags: {string.Join(", ", tagList)}" : "";
                return $"✅ Created note '{title}'{tagText}";
            }
            else
            {
                return $"❌ Failed to create note: {_notesService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error creating note: {ex.Message}";
        }
    }

    public async Task<List<Note>> GetNotesAsync(int limit = 10)
    {
        try
        {
            var notes = await _notesService.GetAllNotesAsync();
            
            if (notes == null)
                return new List<Note>();

            return notes
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToList();
        }
        catch (Exception)
        {
            return new List<Note>();
        }
    }

    public async Task<List<Note>> SearchNotesAsync(string query, int limit = 10)
    {
        try
        {
            var allNotes = await _notesService.GetAllNotesAsync();
            
            if (allNotes == null || string.IsNullOrEmpty(query))
                return new List<Note>();

            var searchTerm = query.ToLower();
            return allNotes
                .Where(note =>
                    (note.Title?.ToLower().Contains(searchTerm) ?? false) ||
                    (note.Content?.ToLower().Contains(searchTerm) ?? false)
                )
                .OrderByDescending(n => n.UpdatedAt)
                .Take(limit)
                .ToList();
        }
        catch (Exception)
        {
            return new List<Note>();
        }
    }

    public async Task<string> UpdateNoteAsync(Guid noteId, string? title = null, string? content = null, string? tags = null)
    {
        try
        {
            // Get existing note
            var existingNote = await _notesService.GetNoteByIdAsync(noteId);
            
            if (existingNote == null)
            {
                return "❌ Note not found";
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(title))
                existingNote.Title = title;

            if (!string.IsNullOrEmpty(content))
                existingNote.Content = content;

            existingNote.UpdatedAt = DateTime.UtcNow;

            var updated = await _notesService.UpdateNoteAsync(existingNote);
            
            if (updated != null)
            {
                return "✅ Note updated successfully";
            }
            else
            {
                return $"❌ Failed to update note: {_notesService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error updating note: {ex.Message}";
        }
    }
}