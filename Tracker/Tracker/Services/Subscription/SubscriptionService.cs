using System;
using Tracker.Common.Enums;
using Tracker.Logging;
using Tracker.Services.Backend;

namespace Tracker.Services.Subscription
{
    /// <summary>
    /// Manages subscription state and feature access.
    /// Currently defaults to Internal tier for testing.
    /// Will integrate with backend service for production.
    /// </summary>
    public class SubscriptionService
    {
        #region Singleton

        private static readonly Lazy<SubscriptionService> _instance =
            new(() => new SubscriptionService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SubscriptionService Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private SubscriptionTier _currentTier;
        private SubscriptionLimits _currentLimits;
        private DateTime? _subscriptionExpiry;
        private string? _customerId;
        private string? _subscriptionId;

        #endregion

        #region Events

        /// <summary>
        /// Fired when subscription tier changes.
        /// </summary>
        public event EventHandler<SubscriptionTier>? TierChanged;

        #endregion

        #region Constructor

        private SubscriptionService()
        {
            _logger = LoggingManager.GetComponentLogger("Subscription");

            // DEFAULT TO INTERNAL TIER FOR TESTING
            // This allows test users to access all features
            // In production, this will be determined by backend validation
            _currentTier = SubscriptionTier.Internal;
            _currentLimits = SubscriptionLimits.Internal;

            _logger.Info("Subscription service initialized. Current tier: {0}", _currentTier);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The current subscription tier.
        /// </summary>
        public SubscriptionTier CurrentTier => _currentTier;

        /// <summary>
        /// The current tier's limits.
        /// </summary>
        public SubscriptionLimits Limits => _currentLimits;

        /// <summary>
        /// Display name for current tier.
        /// </summary>
        public string TierDisplayName => _currentLimits.DisplayName;

        /// <summary>
        /// Whether the subscription is active (not expired).
        /// </summary>
        public bool IsActive => !_subscriptionExpiry.HasValue || _subscriptionExpiry.Value > DateTime.UtcNow;

        /// <summary>
        /// When the subscription expires (null = never/lifetime).
        /// </summary>
        public DateTime? ExpiryDate => _subscriptionExpiry;

        /// <summary>
        /// Whether this is a paid tier.
        /// </summary>
        public bool IsPaidTier => _currentTier != SubscriptionTier.Free;

        /// <summary>
        /// Whether the user has access to AI Help Bot.
        /// </summary>
        public bool HasAIAccess => IsActive && _currentLimits.HasAIAssistant;

        /// <summary>
        /// Whether the user has AI data analysis (Pro feature).
        /// Standard tier can use AI for help/docs only, not user data analysis.
        /// </summary>
        public bool HasAIDataAnalysis => IsActive && _currentLimits.HasAIDataAnalysis;

        /// <summary>
        /// Whether the user has calendar sync access.
        /// </summary>
        public bool HasCalendarSync => IsActive && _currentLimits.HasCalendarSync;

        /// <summary>
        /// Whether the user can use network/enterprise databases.
        /// </summary>
        public bool AllowsNetworkDatabase => IsActive && _currentLimits.AllowsNetworkDatabase;

        #endregion

        #region Feature Checks

        /// <summary>
        /// Checks if a feature is available for the current subscription.
        /// </summary>
        public bool HasFeature(string featureName)
        {
            if (!IsActive) return false;

            return featureName.ToLower() switch
            {
                "ai" or "aiassistant" or "helpbot" => _currentLimits.HasAIAssistant,
                "aidataanalysis" or "dataanalysis" => _currentLimits.HasAIDataAnalysis,
                "calendar" or "calendarsync" => _currentLimits.HasCalendarSync,
                "basicreports" => _currentLimits.HasBasicReports,
                "reports" or "advancedreports" => _currentLimits.HasAdvancedReports,
                "emailsupport" => _currentLimits.HasEmailSupport,
                "support" or "prioritysupport" => _currentLimits.HasPrioritySupport,
                "networkdb" or "enterprisedb" => _currentLimits.AllowsNetworkDatabase,
                _ => true // Unknown features default to allowed
            };
        }

        /// <summary>
        /// Checks if the user can add more of a resource type.
        /// </summary>
        /// <param name="resourceType">Type of resource (team_members, tasks, etc.)</param>
        /// <param name="currentCount">Current count of that resource</param>
        /// <returns>Tuple of (canAdd, remainingCount, limitMessage)</returns>
        public (bool CanAdd, int Remaining, string? Message) CheckLimit(string resourceType, int currentCount)
        {
            if (!IsActive)
            {
                return (false, 0, "Your subscription has expired. Please renew to continue.");
            }

            var limit = resourceType.ToLower() switch
            {
                "team_members" or "teammembers" => _currentLimits.MaxTeamMembers,
                "tasks" => _currentLimits.MaxTasks,
                "projects" => _currentLimits.MaxProjects,
                "okrs" => _currentLimits.MaxOKRs,
                "kpis" => _currentLimits.MaxKPIs,
                "goals" => _currentLimits.MaxGoals,
                _ => -1 // Unknown = unlimited
            };

            // Unlimited
            if (SubscriptionLimits.IsUnlimited(limit))
            {
                return (true, -1, null);
            }

            var remaining = limit - currentCount;

            if (remaining <= 0)
            {
                var tierName = _currentTier == SubscriptionTier.Free ? "upgrade to Standard or Pro" : "upgrade to Pro";
                return (false, 0, $"You've reached the {resourceType.Replace("_", " ")} limit for your plan. Please {tierName} for more.");
            }

            // Warn when getting close (80%)
            if (remaining <= limit * 0.2)
            {
                return (true, remaining, $"You're approaching your {resourceType.Replace("_", " ")} limit ({currentCount}/{limit}).");
            }

            return (true, remaining, null);
        }

        #endregion

        #region Subscription Management

        /// <summary>
        /// Sets the subscription tier (for testing or after backend validation).
        /// </summary>
        public void SetTier(SubscriptionTier tier, DateTime? expiry = null)
        {
            var previousTier = _currentTier;
            _currentTier = tier;
            _currentLimits = SubscriptionLimits.GetLimits(tier);
            _subscriptionExpiry = expiry;

            _logger.Info("Subscription tier changed: {0} → {1}, expires: {2}",
                previousTier, tier, expiry?.ToString("yyyy-MM-dd") ?? "Never");

            TierChanged?.Invoke(this, tier);
        }

        /// <summary>
        /// Sets customer and subscription IDs (from payment provider).
        /// </summary>
        public void SetCustomerInfo(string customerId, string subscriptionId)
        {
            _customerId = customerId;
            _subscriptionId = subscriptionId;
            _logger.Debug("Customer info set: {0}", customerId);
        }

        /// <summary>
        /// Validates subscription with backend.
        /// </summary>
        public async Task<bool> ValidateWithBackendAsync()
        {
            try
            {
                var supabase = Backend.SupabaseService.Instance;
                
                if (!supabase.IsSignedIn)
                {
                    _logger.Debug("Subscription validation: No user signed in");
                    return true; // Allow offline use
                }

                await supabase.LoadUserDataAsync();
                
                if (supabase.CurrentSubscription != null)
                {
                    SetTier(supabase.CurrentSubscription.Tier,
                        supabase.CurrentSubscription.CurrentPeriodEnd);
                    
                    _logger.Info("Subscription validated: {0}", _currentTier);
                    return supabase.CurrentSubscription.IsActive;
                }

                _logger.Debug("Subscription validation: No subscription found, using Free tier");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error validating subscription");
                return true; // Allow offline use on error
            }
        }

        /// <summary>
        /// Refreshes subscription status from backend.
        /// </summary>
        public async Task RefreshAsync()
        {
            try
            {
                var supabase = Backend.SupabaseService.Instance;
                
                if (!supabase.IsSignedIn)
                {
                    _logger.Debug("Subscription refresh: No user signed in");
                    return;
                }

                await supabase.LoadUserDataAsync();
                
                if (supabase.CurrentSubscription != null)
                {
                    var newTier = supabase.CurrentSubscription.Tier;
                    if (newTier != _currentTier)
                    {
                        SetTier(newTier, supabase.CurrentSubscription.CurrentPeriodEnd);
                        _logger.Info("Subscription refreshed: {0}", _currentTier);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error refreshing subscription");
            }
        }

        #endregion

        #region Upgrade Prompts

        /// <summary>
        /// Gets the upgrade prompt for a blocked feature.
        /// </summary>
        public string GetUpgradePrompt(string feature)
        {
            var requiredTier = feature.ToLower() switch
            {
                "ai" or "aiassistant" or "helpbot" => "Standard",
                "aidataanalysis" or "dataanalysis" => "Pro",
                "calendar" or "calendarsync" => "Standard",
                "basicreports" => "Standard",
                "reports" or "advancedreports" => "Pro",
                "support" or "prioritysupport" => "Pro",
                "networkdb" or "enterprisedb" => "Pro",
                _ => "Standard"
            };

            return $"This feature requires a {requiredTier} subscription. Upgrade to unlock it!";
        }

        /// <summary>
        /// Gets the URL to the upgrade page.
        /// </summary>
        public string GetUpgradeUrl()
        {
            // TODO: Replace with actual upgrade URL
            return "https://tracker-app.com/upgrade";
        }

        #endregion
    }
}

