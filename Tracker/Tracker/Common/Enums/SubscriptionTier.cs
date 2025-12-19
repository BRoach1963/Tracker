namespace Tracker.Common.Enums
{
    /// <summary>
    /// Subscription tiers for Tracker SaaS.
    /// </summary>
    public enum SubscriptionTier
    {
        /// <summary>
        /// Free tier with limited features.
        /// No AI, limited team members, basic reports.
        /// </summary>
        Free = 0,

        /// <summary>
        /// Standard tier for growing teams.
        /// More capacity, calendar sync, full reports.
        /// </summary>
        Standard = 1,

        /// <summary>
        /// Pro tier with all features.
        /// Unlimited everything, AI Help Bot, priority support.
        /// </summary>
        Pro = 2,

        /// <summary>
        /// Internal/Admin tier for testing and development.
        /// All features unlocked, no limits.
        /// </summary>
        Internal = 99
    }
}

