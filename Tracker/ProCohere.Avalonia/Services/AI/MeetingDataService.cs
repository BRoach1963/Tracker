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
        try
        {
            // Use DashboardService to get meetings
            var dashboardData = await DashboardService.Instance.LoadDashboardDataAsync();
            var now = DateTime.Now;
            var cutoff = now.AddDays(daysAhead);
            
            return dashboardData?.Meetings?
                .Where(m => m.ScheduledAt.HasValue && m.ScheduledAt >= now && m.ScheduledAt <= cutoff)
                .OrderBy(m => m.ScheduledAt)
                .ToList() ?? new List<MeetingDetail>();
        }
        catch (Exception)
        {
            return new List<MeetingDetail>();
        }
    }

    public async Task<List<MeetingDetail>> GetRecentMeetingsAsync(int limit = 10)
    {
        try
        {
            // Use DashboardService to get meetings
            var dashboardData = await DashboardService.Instance.LoadDashboardDataAsync();
            var now = DateTime.Now;
            
            return dashboardData?.Meetings?
                .Where(m => m.ScheduledAt.HasValue && m.ScheduledAt < now)
                .OrderByDescending(m => m.ScheduledAt)
                .Take(limit)
                .ToList() ?? new List<MeetingDetail>();
        }
        catch (Exception)
        {
            return new List<MeetingDetail>();
        }
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
    public async Task<List<MeetingDetail>> SearchMeetingsAsync(string? attendeeName = null, bool includePast = true, int limit = 10)
    {
        try
        {
            // Get all meetings from the dashboard
            var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();
            var meetings = dashboard.Meetings ?? new List<MeetingDetail>();

            // Filter by time if needed
            if (!includePast)
            {
                meetings = meetings.Where(m => m.ScheduledAt >= DateTime.Today).ToList();
            }

            // Filter by attendee name if provided
            if (!string.IsNullOrEmpty(attendeeName))
            {
                var searchTerm = attendeeName.ToLowerInvariant();
                meetings = meetings.Where(m =>
                    // Check attendees
                    (m.Attendees != null && m.Attendees.Any(a =>
                        (!string.IsNullOrEmpty(a.Name) && a.Name.ToLowerInvariant().Contains(searchTerm)) ||
                        (!string.IsNullOrEmpty(a.Email) && a.Email.ToLowerInvariant().Contains(searchTerm)))) ||
                    // Also check meeting title (might contain attendee name)
                    (!string.IsNullOrEmpty(m.Title) && m.Title.ToLowerInvariant().Contains(searchTerm))
                ).ToList();
            }

            // Sort by date descending (most recent first) and take limit
            return meetings
                .OrderByDescending(m => m.ScheduledAt)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MeetingDataService] Error searching meetings: {ex.Message}");
            return new List<MeetingDetail>();
        }
    }}