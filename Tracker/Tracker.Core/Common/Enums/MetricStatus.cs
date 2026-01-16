namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Status of a metric relative to its target.
    /// Used for display purposes in metric status filters and visualizations.
    /// </summary>
    public enum MetricStatus
    {
        /// <summary>Metric is meeting or exceeding target.</summary>
        OnTarget,

        /// <summary>Metric is close to target but not quite there.</summary>
        CloseToTarget,

        /// <summary>Metric is significantly below target.</summary>
        OffTarget,

        /// <summary>Metric status is unknown or not applicable.</summary>
        Unknown
    }
}
