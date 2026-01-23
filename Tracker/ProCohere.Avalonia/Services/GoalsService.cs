using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

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
            Log("Loading team-visible goals");

            // Get goals that are team-visible but not owned by the current user
            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("is_team_visible", Operator.Equals, "true")
                .Filter("owner_id", Operator.NotEqual, teamMember.Id.ToString())
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
    public async Task<List<GoalDetail>> GetSharedGoalsAsync(CancellationToken ct = default)
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
            Log("Loading shared goals (organization-visible)");

            // Get goals that are org-visible but not owned by the current user
            var result = await client.From<GoalDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("is_org_visible", Operator.Equals, "true")
                .Filter("owner_id", Operator.NotEqual, teamMember.Id.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            var goals = result.Models ?? new List<GoalDetail>();
            Log($"Shared goals returned: {goals.Count}");
            return goals;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetSharedGoals ERROR: {ex.Message}");
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
            goal.OwnerTeamMemberId = goal.OwnerTeamMemberId ?? session.TeamMember?.Id;
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

            var result = await client.From<GoalDetail>()
                .Insert(goal);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Goal created: {created.Id}");
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

            Log($"Goal deleted: {goalId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteGoal ERROR: {ex.Message}");
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
