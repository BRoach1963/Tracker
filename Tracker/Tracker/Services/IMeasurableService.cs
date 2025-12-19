using Tracker.DataModels;
using Tracker.Interfaces;

namespace Tracker.Services
{
    /// <summary>
    /// Service for resolving measurable progress from various sources (KPI, Project, TaskCollection).
    /// This service abstracts the polymorphic nature of KeyResultMeasurable links.
    /// </summary>
    public interface IMeasurableService
    {
        /// <summary>
        /// Gets the progress percentage (0-100) for a measurable entity.
        /// </summary>
        /// <param name="measurable">The measurable entity (KPI, Project, or TaskCollection).</param>
        /// <returns>Progress percentage.</returns>
        Task<decimal> GetProgressAsync(IMeasurable measurable);

        /// <summary>
        /// Gets the human-readable display value for a measurable entity.
        /// Examples: "75%", "3/4 tasks", "53 NPS"
        /// </summary>
        /// <param name="measurable">The measurable entity.</param>
        /// <returns>Display value string.</returns>
        Task<string> GetDisplayValueAsync(IMeasurable measurable);

        /// <summary>
        /// Resolves and returns all measurable entities linked to a Key Result.
        /// Populates the DisplayName, CurrentProgress, and CurrentDisplayValue on each KeyResultMeasurable.
        /// </summary>
        /// <param name="keyResultId">The Key Result ID.</param>
        /// <returns>List of resolved measurables with their current values.</returns>
        Task<List<KeyResultMeasurable>> GetMeasurablesForKeyResultAsync(int keyResultId);

        /// <summary>
        /// Resolves a single KeyResultMeasurable to its underlying IMeasurable entity.
        /// </summary>
        /// <param name="measurableLink">The link record containing type and ID.</param>
        /// <returns>The resolved IMeasurable entity, or null if not found.</returns>
        Task<IMeasurable?> ResolveMeasurableAsync(KeyResultMeasurable measurableLink);

        /// <summary>
        /// Calculates the aggregated current value for a Key Result based on its linked measurables.
        /// Uses the aggregation type and weights defined on each measurable link.
        /// </summary>
        /// <param name="keyResultId">The Key Result ID.</param>
        /// <returns>The aggregated current value, or null if no measurables are linked.</returns>
        Task<decimal?> CalculateAggregatedValueAsync(int keyResultId);

        /// <summary>
        /// Gets all available measurables of a specific type for linking to Key Results.
        /// </summary>
        /// <param name="type">The type of measurable (KPI, Project, TaskCollection).</param>
        /// <returns>List of available measurables.</returns>
        Task<List<IMeasurable>> GetAvailableMeasurablesAsync(MeasurableType type);
    }
}

