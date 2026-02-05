using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Interfaces.AI;

/// <summary>
/// AI-facing interface for note/chronicle data operations.
/// Provides simplified, AI-friendly methods for note management.
/// </summary>
public interface INoteDataService
{
    /// <summary>
    /// Creates a new note with the specified details.
    /// </summary>
    /// <param name="title">Note title</param>
    /// <param name="content">Note content</param>
    /// <param name="tags">Comma-separated tags for organization</param>
    /// <returns>Created note details or error message</returns>
    Task<string> CreateNoteAsync(string title, string content, string? tags = null);

    /// <summary>
    /// Gets recent notes.
    /// </summary>
    /// <param name="limit">Maximum number of notes to return (default 10)</param>
    /// <returns>List of recent notes</returns>
    Task<List<Note>> GetNotesAsync(int limit = 10);

    /// <summary>
    /// Searches notes by content or title.
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of matching notes</returns>
    Task<List<Note>> SearchNotesAsync(string query, int limit = 10);

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    /// <param name="noteId">Note ID</param>
    /// <param name="title">New title (optional)</param>
    /// <param name="content">New content (optional)</param>
    /// <param name="tags">New tags (optional)</param>
    /// <returns>Success message or error</returns>
    Task<string> UpdateNoteAsync(Guid noteId, string? title = null, string? content = null, string? tags = null);
}