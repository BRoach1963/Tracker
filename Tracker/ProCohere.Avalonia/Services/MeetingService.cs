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
/// Service for managing meetings in Supabase.
/// Handles CRUD operations for meetings and attendees.
/// 
/// CRITICAL: When creating a meeting, the creator MUST be inserted as an attendee
/// with role='organizer' immediately after, or RLS will prevent them from seeing it.
/// </summary>
public class MeetingService
{
    #region Singleton

    private static readonly Lazy<MeetingService> _instance =
        new(() => new MeetingService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static MeetingService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "meeting_service.log");

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

    /// <summary>
    /// Valid meeting statuses.
    /// </summary>
    public static readonly string[] ValidStatuses =
    {
        "scheduled",
        "in_progress",
        "completed",
        "cancelled"
    };

    /// <summary>
    /// Valid meeting types.
    /// </summary>
    public static readonly string[] ValidMeetingTypes =
    {
        "one_on_one",
        "team",
        "project",
        "standup",
        "retrospective",
        "planning",
        "review",
        "other"
    };

    /// <summary>
    /// Valid attendee roles.
    /// </summary>
    public static readonly string[] ValidAttendeeRoles =
    {
        "organizer",
        "attendee",
        "optional"
    };

    private MeetingService() { }

    #region Meeting CRUD

    /// <summary>
    /// Gets a single meeting by ID with attendees populated.
    /// </summary>
    public async Task<MeetingDetail?> GetMeetingAsync(Guid meetingId)
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
            Log($"Getting meeting: {meetingId}");

            // Get the meeting
            var meeting = await client.From<MeetingDetail>()
                .Filter("id", Operator.Equals, meetingId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (meeting == null)
            {
                LastError = "Meeting not found";
                return null;
            }

            // Load attendees
            await LoadAttendeesForMeetingAsync(meeting);

            return meeting;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetMeeting ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a new meeting and automatically adds the creator as an organizer attendee.
    /// Returns the created meeting with attendees populated.
    /// </summary>
    /// <param name="meeting">Meeting details to create.</param>
    /// <param name="additionalAttendeeIds">Optional list of team member IDs to add as attendees.</param>
    /// <returns>Created meeting with attendees, or null on failure.</returns>
    public async Task<MeetingDetail?> CreateMeetingAsync(MeetingDetail meeting, List<Guid>? additionalAttendeeIds = null)
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
            var creatorId = session.TeamMember.Id;

            Log($"Creating meeting: {meeting.Title} for org={orgId}");

            // Set required fields
            meeting.Id = Guid.NewGuid();
            meeting.OrganizationId = orgId;
            meeting.CreatedByTeamMemberId = creatorId;
            meeting.Status = meeting.Status ?? "scheduled";
            meeting.IsDeleted = false;
            meeting.CreatedAt = DateTime.UtcNow;
            meeting.UpdatedAt = DateTime.UtcNow;

            // Insert the meeting
            var result = await client.From<MeetingDetail>().Insert(meeting);
            var createdMeeting = result.Models?.FirstOrDefault();

            if (createdMeeting == null)
            {
                LastError = "Failed to create meeting";
                Log($"CreateMeeting ERROR: Insert returned no model");
                return null;
            }

            Log($"Meeting created: {createdMeeting.Id}");

            // CRITICAL: Add creator as organizer attendee immediately
            var organizerAttendee = new MeetingAttendee
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                MeetingId = createdMeeting.Id,
                TeamMemberId = creatorId,
                Role = "organizer",
                ResponseStatus = "accepted",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await client.From<MeetingAttendee>().Insert(organizerAttendee);
            Log($"Added creator as organizer attendee");

            // Add additional attendees if provided
            if (additionalAttendeeIds != null && additionalAttendeeIds.Count > 0)
            {
                foreach (var attendeeId in additionalAttendeeIds)
                {
                    // Skip if same as creator (already added as organizer)
                    if (attendeeId == creatorId) continue;

                    var attendee = new MeetingAttendee
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = orgId,
                        MeetingId = createdMeeting.Id,
                        TeamMemberId = attendeeId,
                        Role = "attendee",
                        ResponseStatus = "pending",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await client.From<MeetingAttendee>().Insert(attendee);
                    Log($"Added attendee: {attendeeId}");
                }
            }

            // Reload meeting with attendees
            return await GetMeetingAsync(createdMeeting.Id);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateMeeting ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates an existing meeting. Only the organizer can update meeting details.
    /// </summary>
    public async Task<bool> UpdateMeetingAsync(MeetingDetail meeting)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Updating meeting: {meeting.Id}");

            meeting.UpdatedAt = DateTime.UtcNow;

            await client.From<MeetingDetail>()
                .Filter("id", Operator.Equals, meeting.Id.ToString())
                .Update(meeting);

            Log($"Meeting updated: {meeting.Id}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateMeeting ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Soft-deletes a meeting.
    /// </summary>
    public async Task<bool> DeleteMeetingAsync(Guid meetingId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.User == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Deleting meeting: {meetingId}");

            // Get the deleter's team member ID for deleted_by
            var deletedBy = session.TeamMember.Id;
            
            await client.From<MeetingDetail>()
                .Filter("id", Operator.Equals, meetingId.ToString())
                .Set(m => m.IsDeleted, true)
                .Set(m => m.DeletedAt!, DateTime.UtcNow)
                .Set(m => m.DeletedBy!, deletedBy)
                .Update();

            Log($"Meeting deleted: {meetingId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteMeeting ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Attendee Management

    /// <summary>
    /// Adds an attendee to a meeting. Uses upsert logic to prevent duplicates.
    /// Treat (meeting_id, team_member_id) as unique.
    /// </summary>
    public async Task<MeetingAttendee?> AddAttendeeAsync(Guid meetingId, Guid teamMemberId, string role = "attendee")
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
            Log($"Adding attendee {teamMemberId} to meeting {meetingId} with role={role}");

            // Check if attendee already exists (upsert logic)
            var existing = await client.From<MeetingAttendee>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Filter("team_member_id", Operator.Equals, teamMemberId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            if (existing.Models?.Any() == true)
            {
                Log($"Attendee already exists, updating role");
                var attendee = existing.Models.First();
                attendee.Role = role;
                attendee.UpdatedAt = DateTime.UtcNow;

                await client.From<MeetingAttendee>()
                    .Filter("id", Operator.Equals, attendee.Id.ToString())
                    .Update(attendee);

                return attendee;
            }

            // Create new attendee
            var newAttendee = new MeetingAttendee
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                MeetingId = meetingId,
                TeamMemberId = teamMemberId,
                Role = role,
                ResponseStatus = "pending",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<MeetingAttendee>().Insert(newAttendee);
            Log($"Attendee added: {teamMemberId}");

            return result.Models?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"AddAttendee ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Removes an attendee from a meeting (soft delete).
    /// Cannot remove the organizer.
    /// </summary>
    public async Task<bool> RemoveAttendeeAsync(Guid meetingId, Guid teamMemberId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.User == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Removing attendee {teamMemberId} from meeting {meetingId}");

            // Check if this is the organizer
            var attendee = await client.From<MeetingAttendee>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Filter("team_member_id", Operator.Equals, teamMemberId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (attendee == null)
            {
                LastError = "Attendee not found";
                return false;
            }

            if (attendee.Role == "organizer")
            {
                LastError = "Cannot remove the organizer from the meeting";
                return false;
            }

            // Get the deleter's team member ID for deleted_by
            var deletedBy = session.TeamMember.Id;

            // Soft delete
            await client.From<MeetingAttendee>()
                .Filter("id", Operator.Equals, attendee.Id.ToString())
                .Set(a => a.IsDeleted, true)
                .Set(a => a.DeletedAt!, DateTime.UtcNow)
                .Set(a => a.DeletedBy!, deletedBy)
                .Update();

            Log($"Attendee removed: {teamMemberId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"RemoveAttendee ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates an attendee's role.
    /// </summary>
    public async Task<bool> UpdateAttendeeRoleAsync(Guid meetingId, Guid teamMemberId, string newRole)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        if (!ValidAttendeeRoles.Contains(newRole))
        {
            LastError = $"Invalid role: {newRole}";
            return false;
        }

        try
        {
            Log($"Updating attendee {teamMemberId} role to {newRole}");

            await client.From<MeetingAttendee>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Filter("team_member_id", Operator.Equals, teamMemberId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Set(a => a.Role, newRole)
                .Set(a => a.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log($"Attendee role updated");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateAttendeeRole ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all attendees for a meeting.
    /// </summary>
    public async Task<List<MeetingAttendee>> GetAttendeesAsync(Guid meetingId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MeetingAttendee>();
        }

        try
        {
            Log($"Getting attendees for meeting: {meetingId}");

            var result = await client.From<MeetingAttendee>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var attendees = result.Models ?? new List<MeetingAttendee>();

            // Enrich with team member names
            await EnrichAttendeesWithNamesAsync(attendees);

            Log($"Found {attendees.Count} attendees");
            return attendees;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetAttendees ERROR: {ex.Message}");
            return new List<MeetingAttendee>();
        }
    }

    #endregion

    #region Permission Helpers

    /// <summary>
    /// Checks if the current user is the organizer of a meeting.
    /// </summary>
    public async Task<bool> IsCurrentUserOrganizerAsync(Guid meetingId)
    {
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
            return false;

        try
        {
            var attendee = await client.From<MeetingAttendee>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Filter("team_member_id", Operator.Equals, session.TeamMember.Id.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            return attendee?.Role == "organizer";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the current user is an attendee of a meeting.
    /// </summary>
    public async Task<bool> IsCurrentUserAttendeeAsync(Guid meetingId)
    {
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
            return false;

        try
        {
            var attendee = await client.From<MeetingAttendee>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Filter("team_member_id", Operator.Equals, session.TeamMember.Id.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            return attendee != null;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Loads attendees for a meeting and populates the Attendees collection.
    /// </summary>
    private async Task LoadAttendeesForMeetingAsync(MeetingDetail meeting)
    {
        meeting.Attendees = await GetAttendeesAsync(meeting.Id);
    }

    /// <summary>
    /// Enriches attendees with team member names.
    /// </summary>
    private async Task EnrichAttendeesWithNamesAsync(List<MeetingAttendee> attendees)
    {
        if (attendees.Count == 0) return;

        var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
        var teamMemberLookup = teamMembers.ToDictionary(t => t.Id, t => t);

        foreach (var attendee in attendees)
        {
            if (teamMemberLookup.TryGetValue(attendee.TeamMemberId, out var member))
            {
                attendee.Name = member.FullName;
                attendee.Email = member.Email ?? string.Empty;
                attendee.AvatarUrl = member.AvatarUrl;
            }
        }
    }

    #endregion
}
