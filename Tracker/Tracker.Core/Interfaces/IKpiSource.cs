namespace Tracker.Core.Interfaces
{
    /// <summary>
    /// Interface for entities that can provide values to a KPI.
    /// Implemented by Project, TaskCollection (via query), and child KPIs (for composites).
    /// </summary>
    public interface IKpiSource
    {
        /// <summary>
        /// Unique identifier for the source entity.
        /// </summary>
        int SourceId { get; }

        /// <summary>
        /// Display name shown in UI when selecting or viewing this source.
        /// </summary>
        string SourceDisplayName { get; }

        /// <summary>
        /// Gets the numeric value to contribute to the KPI.
        /// </summary>
        decimal GetValue();

        /// <summary>
        /// The type of KPI source.
        /// </summary>
        KpiSourceType SourceType { get; }
    }

    /// <summary>
    /// Types of data sources that can feed a KPI value.
    /// </summary>
    public enum KpiSourceType
    {
        /// <summary>Project completion percentage.</summary>
        Project,

        /// <summary>Count of tasks matching criteria (completed, by status, etc.).</summary>
        TaskQuery,

        /// <summary>Another KPI (for composite KPIs).</summary>
        ChildKpi,

        /// <summary>Manually entered value.</summary>
        Manual
    }
}


