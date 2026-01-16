using System;
using System.Collections.Generic;
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
        Console.WriteLine("[DashboardService] LoadDashboardDataAsync starting...");
        var data = new DashboardData();
        LastError = null;
        var errors = new List<string>();

        var client = AuthService.Instance.GetSupabaseClient();
        var profile = AuthService.Instance.CurrentProfile;

        Console.WriteLine($"[DashboardService] Client: {(client != null ? "OK" : "NULL")}, Profile: {(profile != null ? profile.Email : "NULL")}");

        if (client == null || profile == null)
        {
            LastError = "Not authenticated";
            Console.WriteLine($"[DashboardService] ERROR: {LastError}");
            return data;
        }

        var userId = profile.Id;
        Console.WriteLine($"[DashboardService] Loading data for user: {userId}");

        // Load all data in parallel for speed
        var teamMembersTask = LoadTeamMembersAsync(client, userId);
        var tasksTask = LoadTasksAsync(client, userId);
        var goalsTask = LoadGoalsAsync(client, userId);
        var projectCountTask = LoadActiveProjectCountAsync(client, userId);

        try
        {
            Console.WriteLine("[DashboardService] Awaiting parallel tasks...");
            await Task.WhenAll(teamMembersTask, tasksTask, goalsTask, projectCountTask);
            Console.WriteLine("[DashboardService] Parallel tasks complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DashboardService] Parallel load error: {ex.Message}");
            errors.Add($"Parallel load: {ex.Message}");
        }

        // Get results
        data.TeamMembers = await teamMembersTask;
        data.Tasks = await tasksTask;
        data.Goals = await goalsTask;
        Console.WriteLine($"[DashboardService] Results: {data.TeamMembers.Count} members, {data.Tasks.Count} tasks, {data.Goals.Count} goals");

        // Calculate stats
        data.Stats = new DashboardStats
        {
            TeamMemberCount = data.TeamMembers.Count,
            TotalTasks = data.Tasks.Count,
            CompletedTasks = data.Tasks.Count(t => t.Status == "completed"),
            TotalGoals = data.Goals.Count,
            GoalsOnTrack = data.Goals.Count(g => g.IsOnTrack),
            ActiveProjectCount = await projectCountTask
        };

        // Enrich team members with task/goal counts
        foreach (var member in data.TeamMembers)
        {
            member.OpenTaskCount = data.Tasks.Count(t => 
                t.OwnerTeamMemberId == member.Id && t.Status != "completed");
            member.ActiveGoalCount = data.Goals.Count(g => 
                g.OwnerTeamMemberId == member.Id && g.Status != "completed");
        }

        // Enrich tasks with owner names
        var memberDict = data.TeamMembers.ToDictionary(m => m.Id);
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
            if (goal.OwnerTeamMemberId.HasValue && memberDict.TryGetValue(goal.OwnerTeamMemberId.Value, out var owner))
            {
                goal.OwnerName = owner.FullName;
            }
        }

        if (errors.Count > 0)
        {
            LastError = string.Join("; ", errors);
        }

        System.Diagnostics.Debug.WriteLine($"[DashboardService] Loaded: {data.TeamMembers.Count} members, " +
            $"{data.Tasks.Count} tasks, {data.Goals.Count} goals, {data.Stats.ActiveProjectCount} projects");

        return data;
    }

    private async Task<List<TeamMemberDetail>> LoadTeamMembersAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            var result = await client.From<TeamMemberDetail>()
                .Filter("manager_user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Order("first_name", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            return result.Models ?? new List<TeamMemberDetail>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardService] TeamMembers error: {ex.Message}");
            return new List<TeamMemberDetail>();
        }
    }

    private async Task<List<TaskDetail>> LoadTasksAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            var result = await client.From<TaskDetail>()
                .Filter("created_by_user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("due_date", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            return result.Models ?? new List<TaskDetail>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardService] Tasks error: {ex.Message}");
            return new List<TaskDetail>();
        }
    }

    private async Task<List<GoalDetail>> LoadGoalsAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            var result = await client.From<GoalDetail>()
                .Filter("created_by_user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("title", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            return result.Models ?? new List<GoalDetail>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardService] Goals error: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    private async Task<int> LoadActiveProjectCountAsync(Supabase.Client client, Guid userId)
    {
        try
        {
            var result = await client.From<ProjectForCount>()
                .Filter("created_by_user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "in_progress")
                .Get();

            return result.Models?.Count ?? 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardService] Projects error: {ex.Message}");
            return 0;
        }
    }
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

/// <summary>
/// Minimal model for counting projects.
/// </summary>
[Table("projects")]
internal class ProjectForCount : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    [Column("status")]
    public string? Status { get; set; }
}
