namespace Tracker.Interfaces
{
    /// <summary>
    /// Interface for entities that can provide progress to a Target.
    /// Implemented by Metric, Project, and TaskCollection.
    /// </summary>
    public interface IMeasurable
    {
        /// <summary>
        /// Unique identifier for the measurable entity (UUID).
        /// </summary>
        Guid GuidId { get; }

        /// <summary>
        /// Legacy int ID for backwards compatibility.
        /// </summary>
        [Obsolete("Use GuidId instead")]
        int Id { get; }

        /// <summary>
        /// Display name shown in UI when selecting or viewing this measurable.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Progress percentage (0-100) towards completion or target.
        /// </summary>
        decimal CurrentProgress { get; }
    }

    /// <summary>
    /// Types of entities that can be measured for Targets.
    /// </summary>
    public enum MeasurableType
    {
        /// <summary>A Metric (formerly KPI).</summary>
        Metric,

        /// <summary>A project with task-based progress.</summary>
        Project,

        /// <summary>A collection of tasks grouped as a single measurable.</summary>
        TaskCollection
    }
}


