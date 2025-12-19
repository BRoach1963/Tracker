namespace Tracker.Interfaces
{
    /// <summary>
    /// Interface for entities that can provide progress to a Key Result.
    /// Implemented by KPI, Project, and TaskCollection.
    /// </summary>
    public interface IMeasurable
    {
        /// <summary>
        /// Unique identifier for the measurable entity.
        /// </summary>
        int MeasurableId { get; }

        /// <summary>
        /// Display name shown in UI when selecting or viewing this measurable.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Progress percentage (0-100) towards completion or target.
        /// </summary>
        decimal Progress { get; }

        /// <summary>
        /// Human-readable display of the current value.
        /// Examples: "75%", "3/4 tasks", "53 NPS"
        /// </summary>
        string DisplayValue { get; }

        /// <summary>
        /// The type of measurable (KPI, Project, or TaskCollection).
        /// </summary>
        MeasurableType MeasurableType { get; }
    }

    /// <summary>
    /// Types of entities that can be measured for Key Results.
    /// </summary>
    public enum MeasurableType
    {
        /// <summary>A Key Performance Indicator metric.</summary>
        Kpi,

        /// <summary>A project with task-based progress.</summary>
        Project,

        /// <summary>A collection of tasks grouped as a single measurable.</summary>
        TaskCollection
    }
}


