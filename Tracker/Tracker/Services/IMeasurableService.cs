using Tracker.DataModels;
using Tracker.Interfaces;

namespace Tracker.Services
{
    /// <summary>
    /// Service for resolving measurable progress from various sources (Metric, Project, TaskCollection).
    /// </summary>
    public interface IMeasurableService
    {
        /// <summary>
        /// Gets the progress percentage (0-100) for a measurable entity.
        /// </summary>
        Task<decimal> GetProgressAsync(IMeasurable measurable);

        /// <summary>
        /// Gets the human-readable display value for a measurable entity.
        /// </summary>
        Task<string> GetDisplayValueAsync(IMeasurable measurable);

        /// <summary>
        /// Resolves and returns all measurable entities linked to a Target.
        /// </summary>
        Task<List<TargetMeasurable>> GetMeasurablesForTargetAsync(Guid targetId);

        /// <summary>
        /// Resolves a single TargetMeasurable to its underlying IMeasurable entity.
        /// </summary>
        Task<IMeasurable?> ResolveMeasurableAsync(TargetMeasurable measurableLink);

        /// <summary>
        /// Calculates the aggregated current value for a Target based on its linked measurables.
        /// </summary>
        Task<decimal?> CalculateAggregatedValueAsync(Guid targetId);

        /// <summary>
        /// Gets all available measurables of a specific type for linking to Targets.
        /// </summary>
        Task<List<IMeasurable>> GetAvailableMeasurablesAsync(string type);
    }
}

