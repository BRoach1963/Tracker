using System;
using System.Threading.Tasks;
using Tracker.Common.Enums;

namespace Tracker.Services.Subscription
{
    /// <summary>
    /// Interface for subscription management service.
    /// Enables unit testing by allowing mock implementations.
    /// </summary>
    public interface ISubscriptionService
    {
        #region Properties

        /// <summary>
        /// The current subscription tier.
        /// </summary>
        SubscriptionTier CurrentTier { get; }

        /// <summary>
        /// The current tier's limits.
        /// </summary>
        SubscriptionLimits Limits { get; }

        /// <summary>
        /// Display name for current tier.
        /// </summary>
        string TierDisplayName { get; }

        /// <summary>
        /// Whether the subscription is active (not expired).
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// When the subscription expires (null = never/lifetime).
        /// </summary>
        DateTime? ExpiryDate { get; }

        /// <summary>
        /// Whether this is a paid tier.
        /// </summary>
        bool IsPaidTier { get; }

        /// <summary>
        /// Whether the user has access to AI Help Bot.
        /// </summary>
        bool HasAIAccess { get; }

        /// <summary>
        /// Whether the user has AI data analysis (Pro feature).
        /// </summary>
        bool HasAIDataAnalysis { get; }

        /// <summary>
        /// Whether the user has calendar sync access.
        /// </summary>
        bool HasCalendarSync { get; }

        /// <summary>
        /// Whether the user can use network/enterprise databases.
        /// </summary>
        bool AllowsNetworkDatabase { get; }

        #endregion

        #region Events

        /// <summary>
        /// Fired when subscription tier changes.
        /// </summary>
        event EventHandler<SubscriptionTier>? TierChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Checks if a feature is available for the current subscription.
        /// </summary>
        /// <param name="featureName">Feature name (use FeatureNames constants)</param>
        /// <returns>True if feature is available</returns>
        bool HasFeature(string featureName);

        /// <summary>
        /// Checks if the user can add more of a resource type.
        /// </summary>
        /// <param name="resourceType">Type of resource (use ResourceTypes constants)</param>
        /// <param name="currentCount">Current count of that resource</param>
        /// <returns>Tuple of (canAdd, remainingCount, limitMessage)</returns>
        (bool CanAdd, int Remaining, string? Message) CheckLimit(string resourceType, int currentCount);

        /// <summary>
        /// Sets the subscription tier (for testing or after backend validation).
        /// </summary>
        void SetTier(SubscriptionTier tier, DateTime? expiry = null);

        /// <summary>
        /// Sets customer and subscription IDs (from payment provider).
        /// </summary>
        void SetCustomerInfo(string customerId, string subscriptionId);

        /// <summary>
        /// Validates subscription with backend.
        /// </summary>
        Task<bool> ValidateWithBackendAsync();

        /// <summary>
        /// Refreshes subscription status from backend.
        /// </summary>
        Task RefreshAsync();

        #endregion
    }
}
