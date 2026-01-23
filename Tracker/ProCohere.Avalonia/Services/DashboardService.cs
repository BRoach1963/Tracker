using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for loading dashboard data from Supabase.
/// </summary>
public class DashboardService
{
    #region Singleton

    private static readonly Lazy<DashboardService> _instance =
        new(() => new DashboardService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static DashboardService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "dashboard.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        Console.WriteLine(line);
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

    #region Error Tracking

    /// <summary>
    /// Last error message from data operations.
    /// </summary>
    public string? LastError { get; private set; }

    #endregion

    private DashboardService() { }

    /// <summary>
    /// Loads all dashboard data for the current user.
    /// </summary>
    public async Task<DashboardData> LoadDashboardDataAsync()
    {
        Log("LoadDashboardDataAsync starting...");
        var data = new DashboardData();
        LastError = null;
        var errors = new List<string>();

        // Use procohere client for app data (team_members, tasks, goals, etc.)
        var client = AuthService.Instance.GetProCohereClient();
        var profile = AuthService.Instance.CurrentProfile;

        Log($"Client: {(client != null ? "OK" : "NULL")}, Profile: {(profile != null ? profile.Email : "NULL")}");

        if (client == null || profile == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return data;
        }

        var userId = profile.Id;
        Log($"Loading data for user: {userId}");

        // Load all data in parallel for speed
        var teamMembersTask = LoadTeamMembersAsync(client, userId);
        var tasksTask = LoadTasksAsync(client, userId);
        var goalsTask = LoadGoalsAsync(client, userId);
        // NOTE: projects table doesn't exist in procohere schema
        var meetingsTask = LoadMeetingsAsync(client, userId);
        var feedbackTask = LoadFeedbackAsync(client, userId);
        var agendaItemsTask = LoadAgendaItemsAsync(client, userId);
        var attendeesTask = LoadAttendeesAsync(client, userId);

        try
        {
            Log("Awaiting parallel tasks...");
            await Task.WhenAll(teamMembersTask, tasksTask, goalsTask, meetingsTask, feedbackTask, agendaItemsTask, attendeesTask);
            Log("Parallel tasks complete");
        }
        catch (Exception ex)
        {
            Log($"Parallel load error: {ex.Message}");
            errors.Add($"Parallel load: {ex.Message}");
        }

        // Get results
        data.TeamMembers = await teamMembersTask;
        data.Tasks = await tasksTask;
        data.Goals = await goalsTask;
        data.Meetings = await meetingsTask;
        data.Feedback = await feedbackTask;
        var agendaItems = await agendaItemsTask;
        var attendees = await attendeesTask;
        Log($"RESULTS: {data.TeamMembers.Count} members, {data.Tasks.Count} tasks, {data.Goals.Count} goals, {data.Meetings.Count} meetings, {data.Feedback.Count} feedback, {agendaItems.Count} agenda items, {attendees.Count} attendees");

        // Enrich meetings with agenda items
        var agendaByMeeting = agendaItems
            .GroupBy(a => a.MeetingId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.SortOrder).ToList());

        foreach (var meeting in data.Meetings)
        {
            if (agendaByMeeting.TryGetValue(meeting.Id, out var items))
            {
                meeting.AgendaItems = items;
            }
        }

        // Enrich meetings with attendees (and set attendee names from team members)
        var attendeesByMeeting = attendees
            .GroupBy(a => a.MeetingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var memberDict = data.TeamMembers.ToDictionary(m => m.Id);
        foreach (var meeting in data.Meetings)
        {
            if (attendeesByMeeting.TryGetValue(meeting.Id, out var meetingAttendees))
            {
                // Enrich attendee names from team members
                foreach (var att in meetingAttendees)
                {
                    if (memberDict.TryGetValue(att.TeamMemberId, out var member))
                    {
                        att.Name = member.FullName;
                        att.Email = member.Email ?? string.Empty;
                        att.AvatarUrl = member.AvatarUrl;
                    }
                }
                meeting.Attendees = meetingAttendees;
            }
        }

        // Calculate stats
        data.Stats = new DashboardStats
        {
            TeamMemberCount = data.TeamMembers.Count,
            TotalTasks = data.Tasks.Count,
            CompletedTasks = data.Tasks.Count(t => t.Status == "completed"),
            TotalGoals = data.Goals.Count,
            GoalsOnTrack = data.Goals.Count(g => g.Health == GoalHealth.OnTrack),
            // NOTE: projects table doesn't exist in procohere schema
            ActiveProjectCount = 0
        };

        // Enrich team members with task/goal counts and last meeting date
        foreach (var member in data.TeamMembers)
        {
            member.OpenTaskCount = data.Tasks.Count(t => 
                t.OwnerTeamMemberId == member.Id && t.Status != "completed");
            member.ActiveGoalCount = data.Goals.Count(g => 
                g.OwnerTeamMemberId == member.Id && g.Status != "completed");
            
            // Find last meeting with this team member
            var lastMeeting = data.Meetings
                .Where(m => m.Attendees?.Any(a => a.TeamMemberId == member.Id) == true)
                .OrderByDescending(m => m.ScheduledAt)
                .FirstOrDefault();
            member.LastMeetingDate = lastMeeting?.ScheduledAt;
        }

        // Enrich tasks with owner names
        foreach (var task in data.Tasks)
        {
            if (task.OwnerTeamMemberId.HasValue && memberDict.TryGetValue(task.OwnerTeamMemberId.Value, out var owner))
            {
                task.OwnerName = owner.FullName;
            }
        }

        // Enrich goals with owner names
        foreach (var goal in data.Goals)
        {
            if (goal.OwnerTeamMemberId != Guid.Empty && memberDict.TryGetValue(goal.OwnerTeamMemberId, out var owner))
            {
                goal.OwnerName = owner.FullName;
            }
        }

        if (errors.Count > 0)
        {
            LastError = string.Join("; ", errors);
        }

        System.Diagnostics.Debug.WriteLine($"[DashboardService] Loaded: {data.TeamMembers.Count} members, {data.Tasks.Count} tasks, {data.Goals.Count} goals");

        return data;
    }

    private async Task<List<TeamMemberDetail>> LoadTeamMembersAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            Log($"Loading team members...");
            
            // Query all active team members (RLS filters by organization)
            var result = await client.From<TeamMemberDetail>()
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Order("first_name", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            Log($"Team members returned: {result.Models?.Count ?? 0}");
            if (result.Models != null)
            {
                foreach (var tm in result.Models.Take(3))
                {
                    Log($"  - {tm.FirstName} {tm.LastName}");
                }
            }

            return result.Models ?? new List<TeamMemberDetail>();
        }
        catch (Exception ex)
        {
            Log($"TEAM MEMBERS ERROR: {ex.Message}");
            Log($"TEAM MEMBERS STACK: {ex.StackTrace}");
            return new List<TeamMemberDetail>();
        }
    }

    private async Task<List<TaskDetail>> LoadTasksAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            Log($"Loading tasks...");
            
            // Query all non-deleted tasks (RLS filters by organization)
            var result = await client.From<TaskDetail>()
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("due_date", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            Log($"Tasks returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<TaskDetail>();
        }
        catch (Exception ex)
        {
            Log($"TASKS ERROR: {ex.Message}");
            Log($"TASKS STACK: {ex.StackTrace}");
            return new List<TaskDetail>();
        }
    }

    private async Task<List<GoalDetail>> LoadGoalsAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            Log($"Loading goals...");
            
            // Query all non-deleted goals (RLS filters by organization)
            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("title", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            Log($"Goals returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<GoalDetail>();
        }
        catch (Exception ex)
        {
            Log($"GOALS ERROR: {ex.Message}");
            Log($"GOALS STACK: {ex.StackTrace}");
            return new List<GoalDetail>();
        }
    }

    // NOTE: LoadActiveProjectCountAsync removed - procohere.projects table doesn't exist

    private async Task<List<MeetingDetail>> LoadMeetingsAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            Log($"Loading meetings for user: {userId}");
            
            // First, let's see ALL meetings (RLS will still apply)
            var allResult = await client.From<MeetingDetail>()
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("scheduled_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            Log($"All non-deleted meetings visible to this user: {allResult.Models?.Count ?? 0}");
            
            if (allResult.Models != null && allResult.Models.Count > 0)
            {
                foreach (var m in allResult.Models.Take(5))
                {
                    Log($"  - Meeting '{m.Title}' created_by: {m.CreatedByTeamMemberId}, scheduled: {m.ScheduledAt}");
                }
            }
            else
            {
                Log("  (no meetings returned from database)");
            }

            // RLS already filters by organization, return all meetings for this org
            Log($"Meetings for this user's org: {allResult.Models?.Count ?? 0}");
            return allResult.Models ?? new List<MeetingDetail>();
        }
        catch (Exception ex)
        {
            Log($"MEETINGS ERROR: {ex.Message}");
            Log($"MEETINGS STACK: {ex.StackTrace}");
            return new List<MeetingDetail>();
        }
    }

    private async Task<List<FeedbackDetail>> LoadFeedbackAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            Log($"Loading feedback...");
            
            // Query all non-deleted feedback (RLS filters by organization)
            var result = await client.From<FeedbackDetail>()
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            Log($"Feedback returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<FeedbackDetail>();
        }
        catch (Exception ex)
        {
            Log($"FEEDBACK ERROR: {ex.Message}");
            Log($"FEEDBACK STACK: {ex.StackTrace}");
            return new List<FeedbackDetail>();
        }
    }

    private async Task<List<MeetingAgendaItem>> LoadAgendaItemsAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            Log($"Loading agenda items...");
            
            // Query all non-deleted agenda items (RLS filters by organization)
            var result = await client.From<MeetingAgendaItem>()
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("sort_order", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            Log($"Agenda items returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<MeetingAgendaItem>();
        }
        catch (Exception ex)
        {
            Log($"AGENDA ITEMS ERROR: {ex.Message}");
            Log($"AGENDA ITEMS STACK: {ex.StackTrace}");
            return new List<MeetingAgendaItem>();
        }
    }

    private async Task<List<MeetingAttendee>> LoadAttendeesAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            Log($"Loading attendees...");
            
            // Query all non-deleted attendees (RLS filters by organization)
            var result = await client.From<MeetingAttendee>()
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Get();

            Log($"Attendees returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<MeetingAttendee>();
        }
        catch (Exception ex)
        {
            Log($"ATTENDEES ERROR: {ex.Message}");
            Log($"ATTENDEES STACK: {ex.StackTrace}");
            return new List<MeetingAttendee>();
        }
    }

    /// <summary>
    /// Gets the daily load (tasks + meetings) for the next 7 days.
    /// Used for the Briefing sparkline visualization.
    /// This is scoped to the current user's own obligations (not team).
    /// </summary>
    public async Task<List<Models.DailyLoad>> GetWeeklyLoadAsync()
    {
        Log("GetWeeklyLoadAsync starting...");
        var loads = new List<Models.DailyLoad>();
        
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;
        
        if (client == null || teamMember == null)
        {
            Log("GetWeeklyLoadAsync: No client or team member");
            // Return empty days with zero load
            for (int i = 0; i < 7; i++)
            {
                loads.Add(new Models.DailyLoad { Date = DateTime.Today.AddDays(i) });
            }
            return loads;
        }

        var teamMemberId = teamMember.Id;
        var organizationId = teamMember.OrganizationId;
        
        Log($"GetWeeklyLoadAsync: TeamMember={teamMemberId}, Org={organizationId}");

        // Initialize all 7 days with zero counts
        var today = DateTime.Today;
        var dayLoads = new Dictionary<DateTime, Models.DailyLoad>();
        for (int i = 0; i < 7; i++)
        {
            var date = today.AddDays(i);
            dayLoads[date] = new Models.DailyLoad { Date = date };
        }

        try
        {
            // Get tasks due in the next 7 days assigned to this user
            var endDate = today.AddDays(7);
            var tasksResult = await client.From<TaskForLoad>()
                .Filter("organization_id", Supabase.Postgrest.Constants.Operator.Equals, organizationId.ToString())
                .Filter("assigned_to", Supabase.Postgrest.Constants.Operator.Equals, teamMemberId.ToString())
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Filter("status", Supabase.Postgrest.Constants.Operator.NotEqual, "done")
                .Filter("due_date", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, today.ToString("yyyy-MM-dd"))
                .Filter("due_date", Supabase.Postgrest.Constants.Operator.LessThan, endDate.ToString("yyyy-MM-dd"))
                .Get();

            Log($"Tasks for load: {tasksResult.Models?.Count ?? 0}");

            if (tasksResult.Models != null)
            {
                foreach (var task in tasksResult.Models)
                {
                    if (task.DueDate.HasValue)
                    {
                        var dueDay = task.DueDate.Value.Date;
                        if (dayLoads.ContainsKey(dueDay))
                        {
                            dayLoads[dueDay].TasksDue++;
                        }
                    }
                }
            }

            // Get meetings in the next 7 days where this user is an attendee
            var meetingsResult = await client.From<MeetingForLoad>()
                .Filter("organization_id", Supabase.Postgrest.Constants.Operator.Equals, organizationId.ToString())
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Filter("scheduled_at", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, today.ToString("yyyy-MM-dd"))
                .Filter("scheduled_at", Supabase.Postgrest.Constants.Operator.LessThan, endDate.ToString("yyyy-MM-dd"))
                .Get();

            Log($"Meetings for load (all): {meetingsResult.Models?.Count ?? 0}");

            // Now filter meetings where this user is an attendee
            if (meetingsResult.Models != null)
            {
                foreach (var meeting in meetingsResult.Models)
                {
                    // Check if this user is an attendee
                    var isAttendee = await CheckIfMeetingAttendeeAsync(client, meeting.Id, teamMemberId, organizationId);
                    
                    if (isAttendee && meeting.ScheduledAt.HasValue)
                    {
                        var meetingDay = meeting.ScheduledAt.Value.Date;
                        if (dayLoads.ContainsKey(meetingDay))
                        {
                            dayLoads[meetingDay].Meetings++;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"GetWeeklyLoadAsync ERROR: {ex.Message}");
        }

        // Return as ordered list
        loads = dayLoads.Values.OrderBy(d => d.Date).ToList();
        
        Log($"GetWeeklyLoadAsync complete: {string.Join(", ", loads.Select(l => $"{l.DayLabel}:{l.TotalLoad}"))}");
        return loads;
    }

    private async Task<bool> CheckIfMeetingAttendeeAsync(Supabase.Client client, Guid meetingId, Guid teamMemberId, Guid organizationId)
    {
        try
        {
            var result = await client.From<MeetingAttendee>()
                .Filter("meeting_id", Supabase.Postgrest.Constants.Operator.Equals, meetingId.ToString())
                .Filter("team_member_id", Supabase.Postgrest.Constants.Operator.Equals, teamMemberId.ToString())
                .Filter("organization_id", Supabase.Postgrest.Constants.Operator.Equals, organizationId.ToString())
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Get();
            
            return result.Models?.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Minimal task model for load counting.
/// </summary>
[Table("tasks")]
internal class TaskForLoad : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("assigned_to")]
    public Guid? AssignedTo { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Minimal meeting model for load counting.
/// </summary>
[Table("meetings")]
internal class MeetingForLoad : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }
}

/// <summary>
/// Container for all dashboard data.
/// </summary>
public class DashboardData
{
    public DashboardStats Stats { get; set; } = new();
    public List<TeamMemberDetail> TeamMembers { get; set; } = new();
    public List<TaskDetail> Tasks { get; set; } = new();
    public List<GoalDetail> Goals { get; set; } = new();
    public List<MeetingDetail> Meetings { get; set; } = new();
    public List<FeedbackDetail> Feedback { get; set; } = new();

    /// <summary>
    /// Gets upcoming tasks (not completed, due within 7 days or overdue).
    /// </summary>
    public List<TaskDetail> UpcomingTasks => Tasks
        .Where(t => t.Status != "completed" && 
                   (t.DueDate == null || t.DueDate <= DateTime.UtcNow.AddDays(7)))
        .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
        .Take(10)
        .ToList();
}

// NOTE: ProjectForCount removed - procohere.projects table doesn't exist
