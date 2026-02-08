using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

// Reminder integration - Phase 5

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing goals in Supabase.
/// 
/// Philosophy: "Goals express intent, Metrics observe reality, Humans decide."
/// - NO automatic goal creation or updates
/// - Health and lifecycle changes require explicit user reflection
/// - NO progress bars, percentages, or red/yellow/green indicators
/// </summary>
public class GoalsService : IGoalsService
{
    #region Singleton

    private static readonly Lazy<GoalsService> _instance =
        new(() => new GoalsService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static GoalsService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "goals_service.log");

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

    #region Properties

    /// <inheritdoc />
    public string? LastError { get; private set; }

    /// <summary>
    /// Raised when goals are created, updated, or deleted.
    /// Subscribe to this to know when to refresh goal-dependent views.
    /// </summary>
    public event EventHandler? GoalsChanged;

    /// <summary>
    /// Raises the GoalsChanged event.
    /// </summary>
    private void OnGoalsChanged()
    {
        Log("GoalsChanged event raised");
        GoalsChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    private GoalsService() { }

    #region Queries

    /// <inheritdoc />
    public async Task<List<GoalDetail>> GetMyGoalsAsync(CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return new List<GoalDetail>();
        }

        try
        {
            Log($"Loading my goals for team member: {teamMember.Id}");

            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("owner_id", Operator.Equals, teamMember.Id.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            var goals = result.Models ?? new List<GoalDetail>();
            Log($"My goals returned: {goals.Count}");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetMyGoals ERROR: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    /// <summary>
    /// Gets active goals not linked to any project.
    /// Useful for linking existing free goals to a new project.
    /// </summary>
    public async Task<List<GoalDetail>> GetLinkableGoalsAsync(CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return new List<GoalDetail>();
        }

        try
        {
            Log("Loading linkable goals (unlinked, active)");

            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("owner_id", Operator.Equals, teamMember.Id.ToString())
                .Filter("status", Operator.Equals, "active")
                .Filter("project_id", Operator.Is, "null")
                .Order("created_at", Ordering.Descending)
                .Get();

            var goals = result.Models ?? new List<GoalDetail>();
            Log($"Linkable goals returned: {goals.Count}");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetLinkableGoals ERROR: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<List<GoalDetail>> GetTeamGoalsAsync(CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return new List<GoalDetail>();
        }

        try
        {
            Log("Loading team goals (visibility_scope='team')");

            // Get goals with team visibility scope
            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("visibility_scope", Operator.Equals, "team")
                .Order("created_at", Ordering.Descending)
                .Get();

            var goals = result.Models ?? new List<GoalDetail>();
            Log($"Team goals returned: {goals.Count}");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetTeamGoals ERROR: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<GoalDetail?> GetGoalByIdAsync(Guid goalId, CancellationToken ct = default)
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
            Log($"Getting goal: {goalId}");

            var result = await client.From<GoalDetail>()
                .Filter("id", Operator.Equals, goalId.ToString())
                .Single();

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGoalById ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<GoalDetail>> SearchGoalsAsync(string query, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<GoalDetail>();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<GoalDetail>();
        }

        try
        {
            Log($"Searching goals: '{query}'");

            // Search by title (case-insensitive using ilike)
            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("title", Operator.ILike, $"%{query}%")
                .Order("created_at", Ordering.Descending)
                .Get();

            var goals = result.Models ?? new List<GoalDetail>();
            Log($"Search returned: {goals.Count} goals");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"SearchGoals ERROR: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<List<GoalDetail>> GetGoalsByLifecycleAsync(GoalLifecycle lifecycle, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<GoalDetail>();
        }

        try
        {
            // Map lifecycle to status values
            var statusValues = lifecycle switch
            {
                GoalLifecycle.Active => new[] { "active", "on_track", "in_progress", "needs_attention", "at_risk", "not_started" },
                GoalLifecycle.Evolving => new[] { "evolving", "reframing_needed" },
                GoalLifecycle.Paused => new[] { "paused" },
                GoalLifecycle.Superseded => new[] { "superseded" },
                GoalLifecycle.Retired => new[] { "retired", "completed" },
                _ => new[] { "active" }
            };

            Log($"Loading goals by lifecycle: {lifecycle} (status in: {string.Join(", ", statusValues)})");

            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("status", Operator.In, statusValues)
                .Order("created_at", Ordering.Descending)
                .Get();

            var goals = result.Models ?? new List<GoalDetail>();
            Log($"Goals by lifecycle returned: {goals.Count}");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGoalsByLifecycle ERROR: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<List<GoalDetail>> GetGoalsByHealthAsync(GoalHealth health, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<GoalDetail>();
        }

        try
        {
            // Map health to status value
            var statusValue = health switch
            {
                GoalHealth.OnTrack => "on_track",
                GoalHealth.NeedsAttention => "needs_attention",
                GoalHealth.AtRisk => "at_risk",
                GoalHealth.ReframingNeeded => "reframing_needed",
                _ => "on_track"
            };
            Log($"Loading goals by health: {health} (status: {statusValue})");

            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("status", Operator.Equals, statusValue)
                .Order("created_at", Ordering.Descending)
                .Get();

            var goals = result.Models ?? new List<GoalDetail>();
            Log($"Goals by health returned: {goals.Count}");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGoalsByHealth ERROR: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    #endregion

    #region CRUD

    /// <inheritdoc />
    public async Task<GoalDetail?> CreateGoalAsync(GoalDetail goal, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;
        var profile = AuthService.Instance.CurrentProfile;

        if (client == null || session == null || profile == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Creating goal: {goal.Title}");

            // Set required fields
            goal.Id = goal.Id == Guid.Empty ? Guid.NewGuid() : goal.Id;
            goal.OrganizationId = session.TeamMember?.OrganizationId ?? Guid.Empty;
            // OwnerTeamMemberId is non-nullable - only set default if empty
            if (goal.OwnerTeamMemberId == Guid.Empty)
            {
                goal.OwnerTeamMemberId = session.TeamMember?.Id ?? Guid.Empty;
            }
            goal.CreatedAt = DateTime.UtcNow;
            goal.UpdatedAt = DateTime.UtcNow;
            goal.IsDeleted = false;

            // Set defaults if not specified
            if (string.IsNullOrEmpty(goal.Status))
            {
                goal.Status = "active";
            }
            if (string.IsNullOrEmpty(goal.GoalTypeValue))
            {
                goal.GoalType = GoalType.Execution;
            }
            // Default visibility to personal if not specified
            if (string.IsNullOrEmpty(goal.VisibilityScope))
            {
                goal.VisibilityScope = "personal";
            }

            var result = await client.From<GoalDetail>()
                .Insert(goal);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Goal created: {created.Id}");
                
                // Create reminder for the goal if enabled
                await CreateGoalReminderIfEnabledAsync(created);
                
                // Notify subscribers of change
                OnGoalsChanged();
            }
            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateGoal ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a minimal goal with just a title.
    /// Used for title-only bootstrapping during project creation.
    /// </summary>
    public async Task<GoalDetail?> CreateMinimalGoalAsync(string title, Guid? projectId = null, CancellationToken ct = default)
    {
        var goal = new GoalDetail
        {
            Title = title,
            ProjectId = projectId,
            GoalType = GoalType.Execution,
            Status = "active"
        };

        return await CreateGoalAsync(goal, ct);
    }

    /// <summary>
    /// Gets all goals linked to a specific project.
    /// </summary>
    public async Task<List<GoalDetail>> GetGoalsByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<GoalDetail>();
        }

        try
        {
            Log($"Loading goals for project: {projectId}");
            
            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("project_id", Operator.Equals, projectId.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            var goals = result.Models ?? new List<GoalDetail>();
            Log($"Goals for project returned: {goals.Count}");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGoalsByProject ERROR: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<GoalDetail?> UpdateGoalAsync(GoalDetail goal, CancellationToken ct = default)
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
            Log($"Updating goal: {goal.Id} - {goal.Title}");

            goal.UpdatedAt = DateTime.UtcNow;

            var result = await client.From<GoalDetail>()
                .Filter("id", Operator.Equals, goal.Id.ToString())
                .Update(goal);

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
            {
                Log($"Goal updated: {updated.Id}");
                OnGoalsChanged();
            }
            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateGoal ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteGoalAsync(Guid goalId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var profile = AuthService.Instance.CurrentProfile;

        if (client == null || profile == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Soft-deleting goal: {goalId}");

            // Get the goal first
            var goal = await GetGoalByIdAsync(goalId, ct);
            if (goal == null)
            {
                LastError = "Goal not found";
                return false;
            }

            // Soft delete
            goal.IsDeleted = true;
            goal.DeletedAt = DateTime.UtcNow;
            goal.DeletedBy = profile.Id;

            var result = await client.From<GoalDetail>()
                .Filter("id", Operator.Equals, goalId.ToString())
                .Update(goal);
            
            // Cancel any pending reminders for this goal
            await CancelGoalRemindersAsync(goalId);

            Log($"Goal deleted: {goalId}");
            OnGoalsChanged();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteGoal ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Links an existing goal to a project by setting its project_id.
    /// </summary>
    public async Task<bool> LinkGoalToProjectAsync(Guid goalId, Guid projectId, CancellationToken ct = default)
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
            Log($"Linking goal {goalId} to project {projectId}");

            // Get the goal first
            var goal = await GetGoalByIdAsync(goalId, ct);
            if (goal == null)
            {
                LastError = "Goal not found";
                return false;
            }

            // Update project_id
            goal.ProjectId = projectId;
            goal.UpdatedAt = DateTime.UtcNow;

            await client.From<GoalDetail>()
                .Filter("id", Operator.Equals, goalId.ToString())
                .Update(goal);

            Log($"Goal {goalId} linked to project {projectId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"LinkGoalToProject ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Health & Lifecycle Updates

    /// <inheritdoc />
    public async Task<GoalDetail?> UpdateHealthAsync(
        Guid goalId, 
        GoalHealth health, 
        string? reason, 
        CancellationToken ct = default)
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
            // Map health to status value
            var statusValue = health switch
            {
                GoalHealth.OnTrack => "on_track",
                GoalHealth.NeedsAttention => "needs_attention",
                GoalHealth.AtRisk => "at_risk",
                GoalHealth.ReframingNeeded => "reframing_needed",
                _ => "on_track"
            };

            Log($"Updating goal health: {goalId} -> {health} (status: {statusValue})");

            var result = await client.From<GoalDetail>()
                .Where(g => g.Id == goalId)
                .Set(g => g.Status, statusValue)
                .Set(g => g.UpdatedAt, DateTime.UtcNow)
                .Update();

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
            {
                Log($"Goal health updated: {goalId} -> {health}");
            }
            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateHealth ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<GoalDetail?> UpdateLifecycleAsync(
        Guid goalId, 
        GoalLifecycle lifecycle, 
        string? reason, 
        Guid? supersededById = null,
        CancellationToken ct = default)
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
            // Map lifecycle to status value
            var statusValue = lifecycle switch
            {
                GoalLifecycle.Active => "active",
                GoalLifecycle.Evolving => "evolving",
                GoalLifecycle.Paused => "paused",
                GoalLifecycle.Superseded => "superseded",
                GoalLifecycle.Retired => "retired",
                _ => "active"
            };

            Log($"Updating goal lifecycle: {goalId} -> {lifecycle} (status: {statusValue})");

            var result = await client.From<GoalDetail>()
                .Where(g => g.Id == goalId)
                .Set(g => g.Status, statusValue)
                .Set(g => g.UpdatedAt, DateTime.UtcNow)
                .Update();

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
            {
                Log($"Goal lifecycle updated: {goalId} -> {lifecycle}");
            }
            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateLifecycle ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Metric Association

    /// <inheritdoc />
    public async Task<bool> AssociateMetricAsync(Guid goalId, Guid metricId, CancellationToken ct = default)
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
            Log($"Associating metric {metricId} with goal {goalId}");

            // Insert into goal_metrics association table
            var association = new GoalMetricAssociation
            {
                GoalId = goalId,
                MetricId = metricId,
                CreatedAt = DateTime.UtcNow
            };

            await client.From<GoalMetricAssociation>()
                .Insert(association);

            Log($"Metric associated successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"AssociateMetric ERROR: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveMetricAssociationAsync(Guid goalId, Guid metricId, CancellationToken ct = default)
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
            Log($"Removing metric {metricId} from goal {goalId}");

            await client.From<GoalMetricAssociation>()
                .Filter("goal_id", Operator.Equals, goalId.ToString())
                .Filter("metric_id", Operator.Equals, metricId.ToString())
                .Delete();

            Log($"Metric association removed successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"RemoveMetricAssociation ERROR: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<MetricDetail>> GetAssociatedMetricsAsync(Guid goalId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MetricDetail>();
        }

        try
        {
            Log($"Getting metrics associated with goal: {goalId}");

            // Get associations first
            var associations = await client.From<GoalMetricAssociation>()
                .Filter("goal_id", Operator.Equals, goalId.ToString())
                .Get();

            var metricIds = associations.Models?.Select(a => a.MetricId).ToList() ?? new List<Guid>();

            if (metricIds.Count == 0)
            {
                return new List<MetricDetail>();
            }

            // Get the actual metrics
            var metrics = new List<MetricDetail>();
            foreach (var metricId in metricIds)
            {
                var metric = await client.From<MetricDetail>()
                    .Filter("id", Operator.Equals, metricId.ToString())
                    .Filter("is_deleted", Operator.Equals, "false")
                    .Single();

                if (metric != null)
                {
                    metrics.Add(metric);
                }
            }

            Log($"Associated metrics returned: {metrics.Count}");
            return metrics;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetAssociatedMetrics ERROR: {ex.Message}");
            return new List<MetricDetail>();
        }
    }

    /// <summary>
    /// Gets derived health for multiple goals in a single batch RPC call.
    /// Uses procohere.get_goal_health_batch_v2 which computes health from linked metrics
    /// using latest metric values, targets, and trend analysis (last 3 values).
    /// </summary>
    /// <param name="goalIds">Goal IDs to compute health for. Pass null or empty to get all goals.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of health results with goal_id, linked_metrics_count, and derived_health.</returns>
    public async Task<List<GoalHealthBatchResult>> GetGoalHealthBatchAsync(
        IEnumerable<Guid>? goalIds = null, 
        CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<GoalHealthBatchResult>();
        }

        try
        {
            var idsArray = goalIds?.ToArray();
            Log($"Getting goal health batch for {idsArray?.Length ?? 0} goals (null = all)");

            // Call the RPC with goal IDs array (null = all visible goals)
            var rpcResult = await client.Rpc("get_goal_health_batch_v2", new
            {
                p_goal_ids = idsArray
            });

            ct.ThrowIfCancellationRequested();

            if (rpcResult?.Content == null)
            {
                Log("RPC returned no content");
                return new List<GoalHealthBatchResult>();
            }

            Log($"RPC response length: {rpcResult.Content.Length}");

            // Deserialize the JSON array response
            var results = JsonSerializer.Deserialize<List<GoalHealthBatchResult>>(
                rpcResult.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<GoalHealthBatchResult>();

            Log($"Goal health batch returned: {results.Count} results");
            return results;
        }
        catch (OperationCanceledException)
        {
            Log("GetGoalHealthBatch cancelled");
            throw;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGoalHealthBatch ERROR: {ex.Message}");
            return new List<GoalHealthBatchResult>();
        }
    }

    /// <inheritdoc />
    public async Task<List<GoalDetail>> GetGoalsForMetricAsync(Guid metricId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<GoalDetail>();
        }

        try
        {
            Log($"Getting goals associated with metric: {metricId}");

            // Get associations first
            var associations = await client.From<GoalMetricAssociation>()
                .Filter("metric_id", Operator.Equals, metricId.ToString())
                .Get();

            var goalIds = associations.Models?.Select(a => a.GoalId).ToList() ?? new List<Guid>();

            if (goalIds.Count == 0)
            {
                return new List<GoalDetail>();
            }

            // Get the actual goals
            var goals = new List<GoalDetail>();
            foreach (var goalId in goalIds)
            {
                var goal = await client.From<GoalDetail>()
                    .Filter("id", Operator.Equals, goalId.ToString())
                    .Filter("is_deleted", Operator.Equals, "false")
                    .Single();

                if (goal != null)
                {
                    goals.Add(goal);
                }
            }

            Log($"Goals for metric returned: {goals.Count}");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGoalsForMetric ERROR: {ex.Message}");
            return new List<GoalDetail>();
        }
    }

    #endregion

    #region Reminder Integration

    /// <summary>
    /// Creates a reminder for the goal if reminders are enabled in settings.
    /// </summary>
    private async Task CreateGoalReminderIfEnabledAsync(GoalDetail? goal)
    {
        if (goal == null || goal.DueDate == null) return;
        
        try
        {
            var settings = ReminderSchedulerService.Instance.Settings;
            if (!settings.EnableReminders || !settings.ShowGoalReminders)
            {
                Log("Goal reminders disabled in settings");
                return;
            }
            
            // Check if reminder already exists
            var exists = await ReminderDataService.Instance.ReminderExistsAsync(
                "goal", goal.Id, ReminderType.Goal);
            
            if (exists)
            {
                Log($"Reminder already exists for goal {goal.Id}");
                return;
            }
            
            var reminder = await ReminderDataService.Instance.CreateGoalReminderAsync(
                goal, settings.GoalReminderDays);
            
            if (reminder != null)
            {
                Log($"Created reminder for goal {goal.Id}: remind at {reminder.RemindAt:u}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the goal operation if reminder creation fails
            Log($"Failed to create goal reminder: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels any pending reminders for a goal.
    /// </summary>
    private async Task CancelGoalRemindersAsync(Guid goalId)
    {
        try
        {
            var cancelled = await ReminderDataService.Instance.CancelRemindersForEntityAsync("goal", goalId);
            if (cancelled > 0)
            {
                Log($"Cancelled {cancelled} reminder(s) for deleted goal {goalId}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the delete operation if reminder cancellation fails
            Log($"Failed to cancel goal reminders: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the reminder for a goal if the due date changed.
    /// Cancels existing reminder and creates a new one with updated time.
    /// </summary>
    public async Task UpdateGoalReminderAsync(GoalDetail goal)
    {
        try
        {
            var settings = ReminderSchedulerService.Instance.Settings;
            if (!settings.EnableReminders || !settings.ShowGoalReminders)
            {
                return;
            }
            
            // Cancel existing reminder
            await ReminderDataService.Instance.CancelRemindersForEntityAsync("goal", goal.Id);
            
            // Create new reminder with updated time (if goal still has a due date)
            if (goal.DueDate != null)
            {
                await ReminderDataService.Instance.CreateGoalReminderAsync(
                    goal, settings.GoalReminderDays);
            }
            
            Log($"Updated reminder for goal {goal.Id}");
        }
        catch (Exception ex)
        {
            Log($"Failed to update goal reminder: {ex.Message}");
        }
    }

    #endregion

    #region Trajectory Prediction

    /// <summary>
    /// Gets trajectory prediction for a goal based on its linked metrics.
    /// Uses TrajectoryPredictor to analyze trends and predict completion probability.
    /// </summary>
    /// <param name="goalId">Goal ID to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Trajectory prediction result.</returns>
    public async Task<TrajectoryResult> GetGoalTrajectoryAsync(
        Guid goalId,
        CancellationToken ct = default)
    {
        LastError = null;

        try
        {
            Log($"Getting trajectory for goal: {goalId}");

            // Load goal
            var goal = await GetGoalByIdAsync(goalId, ct);
            if (goal == null)
            {
                LastError = "Goal not found";
                return TrajectoryResult.NoDueDate(new GoalDetail { Id = goalId, Title = "Unknown" });
            }

            // Load linked metrics
            var linkedMetrics = await GetAssociatedMetricsAsync(goalId, ct);
            
            // Load history for each metric
            var metricHistories = new Dictionary<Guid, List<MetricHistoryEntry>>();
            foreach (var metric in linkedMetrics)
            {
                ct.ThrowIfCancellationRequested();
                var history = await MetricsService.Instance.GetHistoryAsync(metric.Id, limit: 30, ct);
                metricHistories[metric.Id] = history;
            }

            // Predict trajectory
            var predictor = new TrajectoryPredictor();
            var result = predictor.PredictTrajectory(goal, linkedMetrics, metricHistories);

            Log($"Trajectory for {goalId}: {result.Status} ({result.ProbabilityDisplay}, {result.ConfidenceLevel} confidence)");
            return result;
        }
        catch (OperationCanceledException)
        {
            Log("GetGoalTrajectory cancelled");
            throw;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGoalTrajectory ERROR: {ex.Message}");
            return TrajectoryResult.NoDueDate(new GoalDetail { Id = goalId, Title = "Unknown" });
        }
    }

    /// <summary>
    /// Gets trajectory predictions for multiple goals in batch.
    /// More efficient than calling GetGoalTrajectoryAsync for each goal.
    /// </summary>
    /// <param name="goalIds">Goal IDs to analyze. Pass null for all active goals.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of trajectory results.</returns>
    public async Task<List<TrajectoryResult>> GetGoalsTrajectoryBatchAsync(
        IEnumerable<Guid>? goalIds = null,
        CancellationToken ct = default)
    {
        LastError = null;
        var results = new List<TrajectoryResult>();

        try
        {
            // If no specific IDs, get all active goals (my goals)
            var goals = goalIds == null
                ? await GetMyGoalsAsync(ct)
                : new List<GoalDetail>();

            if (goalIds != null)
            {
                foreach (var id in goalIds)
                {
                    ct.ThrowIfCancellationRequested();
                    var goal = await GetGoalByIdAsync(id, ct);
                    if (goal != null) goals.Add(goal);
                }
            }

            Log($"Getting trajectory batch for {goals.Count} goals");

            // Process each goal (could be parallelized in future)
            foreach (var goal in goals)
            {
                ct.ThrowIfCancellationRequested();
                var trajectory = await GetGoalTrajectoryAsync(goal.Id, ct);
                results.Add(trajectory);
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            Log("GetGoalsTrajectoryBatch cancelled");
            throw;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGoalsTrajectoryBatch ERROR: {ex.Message}");
            return results;
        }
    }

    #endregion
}

/// <summary>
/// Association model for goal_metrics join table.
/// </summary>
[Supabase.Postgrest.Attributes.Table("goal_metrics")]
public class GoalMetricAssociation : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.Column("goal_id")]
    public Guid GoalId { get; set; }

    [Supabase.Postgrest.Attributes.Column("metric_id")]
    public Guid MetricId { get; set; }

    [Supabase.Postgrest.Attributes.Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
