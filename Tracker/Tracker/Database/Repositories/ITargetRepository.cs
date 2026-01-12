using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for Target operations (formerly KeyResult).
    /// Handles all data access for measurable outcomes tied to goals.
    /// </summary>
    public interface ITargetRepository
    {
        /// <summary>
        /// Retrieves all targets for the current user.
        /// </summary>
        Task<List<Target>> GetTargetsAsync();

        /// <summary>
        /// Retrieves a specific target by ID.
        /// </summary>
        Task<Target?> GetTargetByIdAsync(Guid id);

        /// <summary>
        /// Adds a new target.
        /// </summary>
        Task<Guid> AddTargetAsync(Target target);

        /// <summary>
        /// Updates an existing target.
        /// </summary>
        Task<bool> UpdateTargetAsync(Target target);

        /// <summary>
        /// Deletes a target by ID.
        /// </summary>
        Task<bool> DeleteTargetAsync(Guid id);

        /// <summary>
        /// Gets targets for a specific goal.
        /// </summary>
        Task<List<Target>> GetGoalTargetsAsync(Guid goalId);

        /// <summary>
        /// Gets targets with a specific status.
        /// If status is null, retrieves all targets.
        /// </summary>
        Task<List<Target>> GetTargetsByStatusAsync(OkrStatus? status);

        /// <summary>
        /// Gets measurables (data sources) for a target.
        /// </summary>
        Task<List<TargetMeasurable>> GetTargetMeasurablesAsync(Guid targetId);

        /// <summary>
        /// Links a measurable (metric/task collection) to a target.
        /// </summary>
        Task<bool> LinkMeasurableToTargetAsync(Guid targetId, Guid measurableId, string measurableType, decimal weight = 1.0m);

        /// <summary>
        /// Unlinks a measurable from a target.
        /// </summary>
        Task<bool> UnlinkMeasurableFromTargetAsync(Guid targetMeasurableId);

        /// <summary>
        /// Gets targets by progress status (on-track, at-risk, off-track).
        /// </summary>
        Task<List<Target>> GetTargetsByProgressAsync(string progressStatus);
    }
}
