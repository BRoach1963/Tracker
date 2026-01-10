namespace Tracker.Common.Enums
{
    /// <summary>
    /// Direction for metric targets.
    /// Maps to Supabase metric_target_direction enum.
    /// </summary>
    public enum MetricTargetDirection
    {
        /// <summary>Higher values are better (e.g., revenue, NPS).</summary>
        HigherIsBetter,

        /// <summary>Lower values are better (e.g., bug count, churn rate).</summary>
        LowerIsBetter,

        /// <summary>Exact target value is ideal.</summary>
        TargetValue
    }

    /// <summary>
    /// Frequency of metric updates.
    /// Maps to Supabase metric_frequency enum.
    /// </summary>
    public enum MetricFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annually
    }
}
