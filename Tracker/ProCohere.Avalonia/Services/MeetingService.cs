using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

// Reminder integration - Phase 5

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
            var authUserId = AuthService.Instance.CurrentUser?.Id;

            Log($"Creating meeting: {meeting.Title}");
            Log($"  Auth user ID: {authUserId}");
            Log($"  Session org ID: {orgId}");
            Log($"  Session team member ID: {creatorId}");
            Log($"  Session TeamMember.LinkedUserId: {session.TeamMember.LinkedUserId}");
            Log($"  ProCohere client has session: {client.Auth.CurrentSession != null}");
            Log($"  ProCohere client access token: {client.Auth.CurrentSession?.AccessToken?.Substring(0, 20) ?? "NULL"}...");

            // Set required fields
            meeting.Id = Guid.NewGuid();
            meeting.OrganizationId = orgId;
            meeting.CreatedByTeamMemberId = creatorId;
            meeting.Status = meeting.Status ?? "scheduled";
            meeting.IsDeleted = false;
            meeting.CreatedAt = DateTime.UtcNow;
            meeting.UpdatedAt = DateTime.UtcNow;

            // Use RPC to insert meeting (bypasses RLS issues with direct INSERT)
            Log($"Calling insert_meeting RPC...");
            var rpcResult = await client.Rpc("insert_meeting", new
            {
                p_id = meeting.Id,
                p_organization_id = orgId,
                p_title = meeting.Title,
                p_meeting_type = meeting.MeetingType,
                p_status = meeting.Status,
                p_scheduled_at = meeting.ScheduledAt,
                p_duration_minutes = meeting.DurationMinutes,
                p_location = meeting.Location,
                p_video_link = meeting.VideoLink,
                p_description = meeting.Description,
                p_created_by = creatorId
            });

            Log($"RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content == null || rpcResult.Content.Contains("error"))
            {
                LastError = rpcResult?.Content ?? "Failed to create meeting via RPC";
                Log($"CreateMeeting ERROR: {LastError}");
                return null;
            }

            var createdMeeting = meeting; // The meeting object has all the data we need
            createdMeeting.Id = meeting.Id; // ID was set above

            Log($"Meeting created: {createdMeeting.Id}");

            // CRITICAL: Add creator as organizer attendee immediately (via RPC)
            var organizerAttendeeId = Guid.NewGuid();
            Log($"Adding creator as organizer attendee via RPC...");
            var orgAttendeeResult = await client.Rpc("insert_meeting_attendee", new
            {
                p_id = organizerAttendeeId,
                p_organization_id = orgId,
                p_meeting_id = createdMeeting.Id,
                p_team_member_id = creatorId,
                p_role = "organizer",
                p_response_status = "accepted"
            });
            Log($"Organizer attendee RPC result: {orgAttendeeResult?.Content ?? "NULL"}");

            // Add additional attendees if provided
            if (additionalAttendeeIds != null && additionalAttendeeIds.Count > 0)
            {
                foreach (var attendeeId in additionalAttendeeIds)
                {
                    // Skip if same as creator (already added as organizer)
                    if (attendeeId == creatorId) continue;

                    var newAttendeeId = Guid.NewGuid();
                    await client.Rpc("insert_meeting_attendee", new
                    {
                        p_id = newAttendeeId,
                        p_organization_id = orgId,
                        p_meeting_id = createdMeeting.Id,
                        p_team_member_id = attendeeId,
                        p_role = "attendee",
                        p_response_status = "pending"
                    });
                    Log($"Added attendee: {attendeeId}");
                }
            }

            // Reload meeting with attendees
            var reloadedMeeting = await GetMeetingAsync(createdMeeting.Id);
            
            // Create reminder for the meeting if enabled
            await CreateMeetingReminderIfEnabledAsync(reloadedMeeting);
            
            return reloadedMeeting;
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

            var rpcResult = await client.Rpc("update_meeting", new
            {
                p_id = meeting.Id,
                p_title = meeting.Title,
                p_description = meeting.Description,
                p_meeting_type = meeting.MeetingType,
                p_status = meeting.Status,
                p_scheduled_at = meeting.ScheduledAt,
                p_duration_minutes = meeting.DurationMinutes,
                p_location = meeting.Location,
                p_video_link = meeting.VideoLink
            });

            Log($"Update RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                return false;
            }

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

            var rpcResult = await client.Rpc("delete_meeting", new
            {
                p_id = meetingId
            });

            Log($"Delete RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                return false;
            }
            
            // Cancel any pending reminders for this meeting
            await CancelMeetingRemindersAsync(meetingId);

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

            // Use RPC to insert (bypasses RLS issues with direct INSERT)
            var rpcResult = await client.Rpc("insert_meeting_attendee", new
            {
                p_id = newAttendee.Id,
                p_organization_id = orgId,
                p_meeting_id = meetingId,
                p_team_member_id = teamMemberId,
                p_role = role
            });

            Log($"Insert attendee RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"AddAttendee ERROR: {LastError}");
                return null;
            }

            Log($"Attendee added: {teamMemberId}");
            return newAttendee;
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
                attendee.AvatarUrl = member.UserAvatarUrl;
            }
        }
    }

    #endregion

    #region Reminder Integration

    /// <summary>
    /// Creates a reminder for the meeting if reminders are enabled in settings.
    /// </summary>
    private async Task CreateMeetingReminderIfEnabledAsync(MeetingDetail? meeting)
    {
        if (meeting == null) return;
        
        try
        {
            var settings = ReminderSchedulerService.Instance.Settings;
            if (!settings.EnableReminders || !settings.ShowMeetingReminders)
            {
                Log("Meeting reminders disabled in settings");
                return;
            }
            
            // Check if reminder already exists
            var exists = await ReminderDataService.Instance.ReminderExistsAsync(
                "meeting", meeting.Id, ReminderType.Meeting);
            
            if (exists)
            {
                Log($"Reminder already exists for meeting {meeting.Id}");
                return;
            }
            
            var reminder = await ReminderDataService.Instance.CreateMeetingReminderAsync(
                meeting, settings.MeetingReminderMinutes);
            
            if (reminder != null)
            {
                Log($"Created reminder for meeting {meeting.Id}: remind at {reminder.RemindAt:u}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the meeting operation if reminder creation fails
            Log($"Failed to create meeting reminder: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels any pending reminders for a meeting.
    /// </summary>
    private async Task CancelMeetingRemindersAsync(Guid meetingId)
    {
        try
        {
            var cancelled = await ReminderDataService.Instance.CancelRemindersForEntityAsync("meeting", meetingId);
            if (cancelled > 0)
            {
                Log($"Cancelled {cancelled} reminder(s) for deleted meeting {meetingId}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the delete operation if reminder cancellation fails
            Log($"Failed to cancel meeting reminders: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the reminder for a meeting if the schedule changed.
    /// Cancels existing reminder and creates a new one with updated time.
    /// </summary>
    public async Task UpdateMeetingReminderAsync(MeetingDetail meeting)
    {
        try
        {
            var settings = ReminderSchedulerService.Instance.Settings;
            if (!settings.EnableReminders || !settings.ShowMeetingReminders)
            {
                return;
            }
            
            // Cancel existing reminder
            await ReminderDataService.Instance.CancelRemindersForEntityAsync("meeting", meeting.Id);
            
            // Create new reminder with updated time
            await ReminderDataService.Instance.CreateMeetingReminderAsync(
                meeting, settings.MeetingReminderMinutes);
            
            Log($"Updated reminder for meeting {meeting.Id}");
        }
        catch (Exception ex)
        {
            Log($"Failed to update meeting reminder: {ex.Message}");
        }
    }

    #endregion
}
