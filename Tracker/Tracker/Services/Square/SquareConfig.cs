namespace Tracker.Services.Square
{
    /// <summary>
    /// Square payment configuration for subscription billing.
    /// 
    /// SECURITY NOTE: 
    /// - NO API keys are stored in the client application
    /// - All payment operations go through Supabase Edge Functions
    /// - Square credentials are stored as Supabase secrets (environment variables)
    /// - The desktop app only knows plan IDs and pricing for display
    /// </summary>
    public static class SquareConfig
    {
        #region Environment

        /// <summary>
        /// Whether we're in sandbox/test mode.
        /// This affects which Supabase Edge Function endpoint to call.
        /// </summary>
        public static bool UseSandbox { get; set; } = true;

        /// <summary>
        /// Environment name for logging/display.
        /// </summary>
        public static string Environment => UseSandbox ? "Sandbox" : "Production";

        #endregion

        #region Supabase Edge Function Endpoints

        /// <summary>
        /// Base URL for Supabase Edge Functions.
        /// These functions hold the Square API keys securely.
        /// </summary>
        public static string SupabaseProjectUrl => Backend.SupabaseConfig.ProjectUrl;

        /// <summary>
        /// Endpoint to create a checkout session.
        /// </summary>
        public static string CreateCheckoutEndpoint => 
            $"{SupabaseProjectUrl}/functions/v1/square-create-checkout";

        /// <summary>
        /// Endpoint to get subscription status.
        /// </summary>
        public static string GetSubscriptionEndpoint => 
            $"{SupabaseProjectUrl}/functions/v1/square-get-subscription";

        /// <summary>
        /// Endpoint to cancel a subscription.
        /// </summary>
        public static string CancelSubscriptionEndpoint => 
            $"{SupabaseProjectUrl}/functions/v1/square-cancel-subscription";

        /// <summary>
        /// Endpoint to update payment method.
        /// </summary>
        public static string UpdatePaymentEndpoint => 
            $"{SupabaseProjectUrl}/functions/v1/square-update-payment";

        #endregion

        #region Plan Identifiers (Safe to include - just IDs, no secrets)

        /// <summary>
        /// Plan identifiers used when calling the checkout endpoint.
        /// The actual Square Plan Variation IDs are stored in Supabase secrets.
        /// </summary>
        public static class PlanIds
        {
            public const string StandardMonthly = "standard_monthly";
            public const string StandardAnnual = "standard_annual";
            public const string ProMonthly = "pro_monthly";
            public const string ProAnnual = "pro_annual";
        }

        /// <summary>
        /// Gets the plan ID for a given tier and cadence.
        /// </summary>
        public static string GetPlanId(string tier, string cadence)
        {
            var key = $"{tier.ToLower()}_{cadence.ToLower()}";
            return key switch
            {
                "standard_monthly" => PlanIds.StandardMonthly,
                "standard_annual" => PlanIds.StandardAnnual,
                "pro_monthly" => PlanIds.ProMonthly,
                "pro_annual" => PlanIds.ProAnnual,
                _ => PlanIds.StandardMonthly
            };
        }

        #endregion

        #region Pricing (for UI display only)

        /// <summary>
        /// Pricing information for UI display.
        /// Actual billing amounts are controlled by Square plan configuration.
        /// </summary>
        public static class Pricing
        {
            // Standard Tier
            public const decimal StandardMonthlyPrice = 9.99m;
            public const decimal StandardAnnualPrice = 99.99m;
            public const int StandardAnnualDiscount = 17; // ~2 months free

            // Pro Tier  
            public const decimal ProMonthlyPrice = 19.99m;
            public const decimal ProAnnualPrice = 199.99m;
            public const int ProAnnualDiscount = 17; // ~2 months free

            /// <summary>
            /// Gets the price for display.
            /// </summary>
            public static decimal GetPrice(string tier, bool isAnnual)
            {
                return (tier.ToLower(), isAnnual) switch
                {
                    ("standard", false) => StandardMonthlyPrice,
                    ("standard", true) => StandardAnnualPrice,
                    ("pro", false) => ProMonthlyPrice,
                    ("pro", true) => ProAnnualPrice,
                    _ => 0m
                };
            }

            /// <summary>
            /// Gets effective monthly price (for annual, shows per-month equivalent).
            /// </summary>
            public static decimal GetEffectiveMonthlyPrice(string tier, bool isAnnual)
            {
                return (tier.ToLower(), isAnnual) switch
                {
                    ("standard", false) => StandardMonthlyPrice,
                    ("standard", true) => StandardAnnualPrice / 12m,
                    ("pro", false) => ProMonthlyPrice,
                    ("pro", true) => ProAnnualPrice / 12m,
                    _ => 0m
                };
            }

            /// <summary>
            /// Gets formatted price string.
            /// </summary>
            public static string GetPriceDisplay(string tier, bool isAnnual)
            {
                var price = GetPrice(tier, isAnnual);
                return isAnnual ? $"${price:N2}/yr" : $"${price:N2}/mo";
            }

            /// <summary>
            /// Gets the discount percentage for annual plans.
            /// </summary>
            public static int GetAnnualDiscount(string tier)
            {
                return tier.ToLower() switch
                {
                    "standard" => StandardAnnualDiscount,
                    "pro" => ProAnnualDiscount,
                    _ => 0
                };
            }
        }

        #endregion
    }
}

