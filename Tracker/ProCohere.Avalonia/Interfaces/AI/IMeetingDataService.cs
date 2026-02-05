using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Interfaces.AI;

/// <summary>
/// AI-facing interface for meeting data operations.
/// Provides simplified, AI-friendly methods for meeting management.
/// </summary>
public interface IMeetingDataService
{
    /// <summary>
    /// Creates a new meeting with the specified details.
    /// </summary>
    /// <param name="title">Meeting title</param>
    /// <param name="attendeeEmails">Comma-separated list of attendee emails</param>
    /// <param name="dateTime">Meeting date and time</param>
    /// <param name="agenda">Meeting agenda or description</param>
    /// <returns>Created meeting details or error message</returns>
    Task<string> CreateMeetingAsync(string title, string? attendeeEmails = null, string? dateTime = null, string? agenda = null);

    /// <summary>
    /// Gets upcoming meetings for the specified number of days.
    /// </summary>
    /// <param name="daysAhead">Number of days to look ahead (default 7)</param>
    /// <returns>List of upcoming meetings</returns>
    Task<List<MeetingDetail>> GetUpcomingMeetingsAsync(int daysAhead = 7);

    /// <summary>
    /// Gets recent meetings.
    /// </summary>
    /// <param name="limit">Maximum number of meetings to return</param>
    /// <returns>List of recent meetings</returns>
    Task<List<MeetingDetail>> GetRecentMeetingsAsync(int limit = 10);

    /// <summary>
    /// Updates an existing meeting.
    /// </summary>
    /// <param name="meetingId">Meeting ID</param>
    /// <param name="title">New title (optional)</param>
    /// <param name="dateTime">New date/time (optional)</param>
    /// <param name="agenda">New agenda (optional)</param>
    /// <returns>Success message or error</returns>
    Task<string> UpdateMeetingAsync(Guid meetingId, string? title = null, string? dateTime = null, string? agenda = null);
}