namespace Tracker.Common.Enums
{
    /// <summary>
    /// How often a KPI is updated/measured.
    /// </summary>
    public enum KpiFrequencyEnum
    {
        /// <summary>Updated daily.</summary>
        Daily,

        /// <summary>Updated weekly.</summary>
        Weekly,

        /// <summary>Updated bi-weekly (every two weeks).</summary>
        BiWeekly,

        /// <summary>Updated monthly.</summary>
        Monthly,

        /// <summary>Updated quarterly.</summary>
        Quarterly,

        /// <summary>Updated annually.</summary>
        Annually,

        /// <summary>Updated on-demand/manually.</summary>
        OnDemand
    }
}


