using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces.AI;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// AI data service implementation for meeting operations.
/// Wraps MeetingService with AI-friendly interface.
/// </summary>
public class MeetingDataService : IMeetingDataService
{
    private readonly MeetingService _meetingService;
    private readonly TeamDataService _teamDataService;

    public MeetingDataService()
    {
        _meetingService = MeetingService.Instance;
        _teamDataService = new TeamDataService();
    }

    public async Task<string> CreateMeetingAsync(string title, string? attendeeEmails = null, string? dateTime = null, string? agenda = null)
    {
        try
        {
            // Parse date/time if provided
            DateTime? parsedDateTime = null;
            if (!string.IsNullOrEmpty(dateTime))
            {
                if (!DateTime.TryParse(dateTime, out var date))
                {
                    return $"Invalid date/time format '{dateTime}'. Please use a standard format like 'MM/dd/yyyy HH:mm'";
                }
                parsedDateTime = date;
            }

            // Find attendees if emails provided
            var attendeeIds = new List<Guid>();
            if (!string.IsNullOrEmpty(attendeeEmails))
            {
                var emails = attendeeEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .ToList();

                foreach (var email in emails)
                {
                    var teamMember = await _teamDataService.GetTeamMemberByEmailAsync(email);
                    if (teamMember != null)
                    {
                        attendeeIds.Add(teamMember.Id);
                    }
                }
            }

            // Create meeting
            var meeting = new MeetingDetail
            {
                Title = title,
                ScheduledAt = parsedDateTime,
                Description = agenda,
                CreatedAt = DateTime.UtcNow
            };

            var createdMeeting = await _meetingService.CreateMeetingAsync(meeting, attendeeIds.Count > 0 ? attendeeIds : null);
            
            if (createdMeeting != null)
            {
                var dateText = parsedDateTime.HasValue ? $" scheduled for {parsedDateTime:MM/dd/yyyy HH:mm}" : "";
                var attendeeText = attendeeIds.Any() ? $" with {attendeeIds.Count} attendee(s)" : "";
                return $"✅ Created meeting '{title}'{dateText}{attendeeText}";
            }
            else
            {
                return $"❌ Failed to create meeting: {_meetingService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error creating meeting: {ex.Message}";
        }
    }

    public async Task<List<MeetingDetail>> GetUpcomingMeetingsAsync(int daysAhead = 7)
    {
        // NOTE: ProCohere MeetingService doesn't have a GetAllMeetings or list method
        // This is a stub implementation - meetings would need to be queried via Dashboard or other service
        // TODO: Implement when ProCohere adds meeting listing capability
        await Task.CompletedTask;
        return new List<MeetingDetail>();
    }

    public async Task<List<MeetingDetail>> GetRecentMeetingsAsync(int limit = 10)
    {
        // NOTE: ProCohere MeetingService doesn't have a GetAllMeetings or list method
        // This is a stub implementation
        // TODO: Implement when ProCohere adds meeting listing capability
        await Task.CompletedTask;
        return new List<MeetingDetail>();
    }

    public async Task<string> UpdateMeetingAsync(Guid meetingId, string? title = null, string? dateTime = null, string? agenda = null)
    {
        try
        {
            // Get existing meeting
            var existingMeeting = await _meetingService.GetMeetingAsync(meetingId);
            
            if (existingMeeting == null)
            {
                return "❌ Meeting not found";
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(title))
                existingMeeting.Title = title;

            if (!string.IsNullOrEmpty(dateTime))
            {
                if (!DateTime.TryParse(dateTime, out var parsedDateTime))
                {
                    return $"Invalid date/time format '{dateTime}'. Please use a standard format like 'YYYY-MM-DD HH:mm'";
                }
                existingMeeting.ScheduledAt = parsedDateTime;
            }

            if (!string.IsNullOrEmpty(agenda))
                existingMeeting.Description = agenda;

            var updated = await _meetingService.UpdateMeetingAsync(existingMeeting);
            
            if (updated)
            {
                return "✅ Meeting updated successfully";
            }
            else
            {
                return $"❌ Failed to update meeting: {_meetingService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error updating meeting: {ex.Message}";
        }
    }
}