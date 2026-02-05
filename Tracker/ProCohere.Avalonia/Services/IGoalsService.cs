using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service interface for goal operations.
/// 
/// Philosophy: "Goals express intent, Metrics observe reality, Humans decide."
/// - Goals are NOT automatically created, updated, or evaluated
/// - Health and lifecycle changes require explicit user action with reflection
/// - No progress bars, percentages, or red/yellow/green indicators
/// </summary>
public interface IGoalsService
{
    #region Queries

    /// <summary>
    /// Gets goals owned by the current user.
    /// </summary>
    Task<List<GoalDetail>> GetMyGoalsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets goals visible to the user's team (team + organization visibility).
    /// </summary>
    Task<List<GoalDetail>> GetTeamGoalsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets goals shared with the current user (participates but doesn't own).
    /// </summary>
    Task<List<GoalDetail>> GetSharedGoalsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a single goal by ID.
    /// </summary>
    Task<GoalDetail?> GetGoalByIdAsync(Guid goalId, CancellationToken ct = default);

    /// <summary>
    /// Searches goals by title or description.
    /// </summary>
    Task<List<GoalDetail>> SearchGoalsAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Gets goals by lifecycle state.
    /// </summary>
    Task<List<GoalDetail>> GetGoalsByLifecycleAsync(GoalLifecycle lifecycle, CancellationToken ct = default);

    /// <summary>
    /// Gets goals by health status.
    /// </summary>
    Task<List<GoalDetail>> GetGoalsByHealthAsync(GoalHealth health, CancellationToken ct = default);

    #endregion

    #region CRUD

    /// <summary>
    /// Creates a new goal. Always requires explicit user action.
    /// </summary>
    Task<GoalDetail?> CreateGoalAsync(GoalDetail goal, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing goal (title, description, time period, etc.).
    /// Does NOT update health or lifecycle - use dedicated methods for those.
    /// </summary>
    Task<GoalDetail?> UpdateGoalAsync(GoalDetail goal, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a goal.
    /// </summary>
    Task<bool> DeleteGoalAsync(Guid goalId, CancellationToken ct = default);

    #endregion

    #region Health & Lifecycle (Require Reflection)

    /// <summary>
    /// Updates the health status of a goal.
    /// REQUIRES a reason/reflection from the user ("What has changed?").
    /// </summary>
    /// <param name="goalId">The goal to update</param>
    /// <param name="health">New health status</param>
    /// <param name="reason">User's reflection on why the health changed</param>
    Task<GoalDetail?> UpdateHealthAsync(
        Guid goalId, 
        GoalHealth health, 
        string? reason, 
        CancellationToken ct = default);

    /// <summary>
    /// Updates the lifecycle state of a goal.
    /// REQUIRES a reason/reflection from the user.
    /// </summary>
    /// <param name="goalId">The goal to update</param>
    /// <param name="lifecycle">New lifecycle state</param>
    /// <param name="reason">User's reflection on why the lifecycle changed</param>
    /// <param name="supersededById">If transitioning to Superseded, the replacement goal ID</param>
    Task<GoalDetail?> UpdateLifecycleAsync(
        Guid goalId, 
        GoalLifecycle lifecycle, 
        string? reason, 
        Guid? supersededById = null,
        CancellationToken ct = default);

    #endregion

    #region Metric Association

    /// <summary>
    /// Associates a metric with a goal.
    /// Metrics are signals, not targets - they inform but don't determine goal health.
    /// </summary>
    Task<bool> AssociateMetricAsync(Guid goalId, Guid metricId, CancellationToken ct = default);

    /// <summary>
    /// Removes a metric association from a goal.
    /// </summary>
    Task<bool> RemoveMetricAssociationAsync(Guid goalId, Guid metricId, CancellationToken ct = default);

    /// <summary>
    /// Gets metrics associated with a goal.
    /// Note: Metrics are HIDDEN by default in goal views.
    /// </summary>
    Task<List<MetricDetail>> GetAssociatedMetricsAsync(Guid goalId, CancellationToken ct = default);

    /// <summary>
    /// Gets goals associated with a metric (reverse lookup).
    /// </summary>
    Task<List<GoalDetail>> GetGoalsForMetricAsync(Guid metricId, CancellationToken ct = default);

    #endregion

    #region Trajectory Prediction

    /// <summary>
    /// Gets trajectory prediction for a goal based on its linked metrics.
    /// Uses TrajectoryPredictor to analyze trends and predict completion probability.
    /// </summary>
    /// <param name="goalId">Goal ID to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Trajectory prediction result.</returns>
    Task<TrajectoryResult> GetGoalTrajectoryAsync(Guid goalId, CancellationToken ct = default);

    /// <summary>
    /// Gets trajectory predictions for multiple goals in batch.
    /// More efficient than calling GetGoalTrajectoryAsync for each goal.
    /// </summary>
    /// <param name="goalIds">Goal IDs to analyze. Pass null for all active goals.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of trajectory results.</returns>
    Task<List<TrajectoryResult>> GetGoalsTrajectoryBatchAsync(
        IEnumerable<Guid>? goalIds = null,
        CancellationToken ct = default);

    #endregion

    #region Error Handling

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    string? LastError { get; }

    #endregion
}
