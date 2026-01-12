namespace Tracker.Common.Enums
{
    /// <summary>
    /// Severity level of an insight.
    /// </summary>
    public enum InsightSeverity
    {
        /// <summary>Informational insight - no action required.</summary>
        Info = 0,

        /// <summary>Low severity - minor attention needed.</summary>
        Low = 1,

        /// <summary>Warning severity - attention recommended.</summary>
        Warning = 2,

        /// <summary>Medium severity - should be addressed soon.</summary>
        Medium = 3,

        /// <summary>High severity - needs prompt attention.</summary>
        High = 4,

        /// <summary>Critical severity - immediate action required.</summary>
        Critical = 5
    }
}
