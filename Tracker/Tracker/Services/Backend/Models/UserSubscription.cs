using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Tracker.Common.Enums;

namespace Tracker.Services.Backend.Models
{
    /// <summary>
    /// User subscription model - matches the subscriptions table in Supabase.
    /// Supports both legacy Stripe fields and new Square integration.
    /// </summary>
    [Table("subscriptions")]
    public class UserSubscription : BaseModel
    {
        #region Core Fields

        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("tier")]
        public string TierString { get; set; } = "free";

        [Column("status")]
        public string StatusString { get; set; } = "active";

        #endregion

        #region Billing Cadence

        /// <summary>
        /// Billing frequency: 'monthly' or 'annual'.
        /// </summary>
        [Column("billing_cadence")]
        public string BillingCadence { get; set; } = "monthly";

        /// <summary>
        /// Current subscription price in cents (e.g., 999 = $9.99).
        /// </summary>
        [Column("price_cents")]
        public int PriceCents { get; set; }

        /// <summary>
        /// Original price before discounts, in cents.
        /// </summary>
        [Column("original_price_cents")]
        public int OriginalPriceCents { get; set; }

        /// <summary>
        /// Discount percentage applied (e.g., 17 for annual savings).
        /// </summary>
        [Column("discount_percent")]
        public int DiscountPercent { get; set; }

        #endregion

        #region Square Payment Fields

        /// <summary>
        /// Square customer ID for payment processing.
        /// </summary>
        [Column("square_customer_id")]
        public string? SquareCustomerId { get; set; }

        /// <summary>
        /// Square subscription ID for recurring billing.
        /// </summary>
        [Column("square_subscription_id")]
        public string? SquareSubscriptionId { get; set; }

        /// <summary>
        /// Square plan variation ID (ties to specific price/cadence).
        /// </summary>
        [Column("square_plan_variation_id")]
        public string? SquarePlanVariationId { get; set; }

        /// <summary>
        /// Most recent Square invoice ID.
        /// </summary>
        [Column("square_invoice_id")]
        public string? SquareInvoiceId { get; set; }

        #endregion

        #region Legacy Stripe Fields (kept for backward compatibility)

        [Column("stripe_customer_id")]
        [Obsolete("Use SquareCustomerId instead")]
        public string? StripeCustomerId { get; set; }

        [Column("stripe_subscription_id")]
        [Obsolete("Use SquareSubscriptionId instead")]
        public string? StripeSubscriptionId { get; set; }

        [Column("stripe_price_id")]
        [Obsolete("Use SquarePlanVariationId instead")]
        public string? StripePriceId { get; set; }

        #endregion

        #region Period & Billing Dates

        [Column("current_period_start")]
        public DateTime? CurrentPeriodStart { get; set; }

        [Column("current_period_end")]
        public DateTime? CurrentPeriodEnd { get; set; }

        [Column("cancel_at_period_end")]
        public bool CancelAtPeriodEnd { get; set; }

        [Column("canceled_at")]
        public DateTime? CanceledAt { get; set; }

        [Column("trial_start")]
        public DateTime? TrialStart { get; set; }

        [Column("trial_end")]
        public DateTime? TrialEnd { get; set; }

        /// <summary>
        /// When subscription was first activated (first successful payment).
        /// </summary>
        [Column("activated_at")]
        public DateTime? ActivatedAt { get; set; }

        #endregion

        #region Payment Failure Tracking

        /// <summary>
        /// Last payment failure timestamp.
        /// </summary>
        [Column("payment_failed_at")]
        public DateTime? PaymentFailedAt { get; set; }

        /// <summary>
        /// Consecutive payment failure count.
        /// </summary>
        [Column("payment_failure_count")]
        public int PaymentFailureCount { get; set; }

        /// <summary>
        /// End of grace period after payment failure.
        /// </summary>
        [Column("grace_period_end")]
        public DateTime? GracePeriodEnd { get; set; }

        #endregion

        #region AI Usage Tracking

        [Column("ai_requests_this_month")]
        public int AiRequestsThisMonth { get; set; }

        [Column("ai_budget_used_cents")]
        public int AiBudgetUsedCents { get; set; }

        [Column("usage_reset_at")]
        public DateTime UsageResetAt { get; set; }

        #endregion

        #region Timestamps

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the subscription tier as an enum.
        /// </summary>
        public SubscriptionTier Tier
        {
            get
            {
                return TierString?.ToLower() switch
                {
                    "free" => SubscriptionTier.Free,
                    "standard" => SubscriptionTier.Standard,
                    "pro" => SubscriptionTier.Pro,
                    "internal" => SubscriptionTier.Internal,
                    _ => SubscriptionTier.Free
                };
            }
            set
            {
                TierString = value switch
                {
                    SubscriptionTier.Free => "free",
                    SubscriptionTier.Standard => "standard",
                    SubscriptionTier.Pro => "pro",
                    SubscriptionTier.Internal => "internal",
                    _ => "free"
                };
            }
        }

        /// <summary>
        /// Whether the subscription is billed annually.
        /// </summary>
        public bool IsAnnual => BillingCadence == "annual";

        /// <summary>
        /// Whether the subscription is billed monthly.
        /// </summary>
        public bool IsMonthly => BillingCadence == "monthly";

        /// <summary>
        /// Whether the subscription is currently active.
        /// </summary>
        public bool IsActive => StatusString == "active" || StatusString == "trialing";

        /// <summary>
        /// Whether the subscription is in trial period.
        /// </summary>
        public bool IsTrialing => StatusString == "trialing" && TrialEnd.HasValue && TrialEnd.Value > DateTime.UtcNow;

        /// <summary>
        /// Whether payment has failed and subscription is in grace period.
        /// </summary>
        public bool IsPastDue => PaymentFailureCount > 0 && GracePeriodEnd.HasValue && GracePeriodEnd.Value > DateTime.UtcNow;

        /// <summary>
        /// Whether the subscription has expired (grace period ended).
        /// </summary>
        public bool IsExpired => (PaymentFailureCount > 0 && GracePeriodEnd.HasValue && GracePeriodEnd.Value <= DateTime.UtcNow)
                                 || (CurrentPeriodEnd.HasValue && CurrentPeriodEnd.Value < DateTime.UtcNow && StatusString != "cancelled");

        /// <summary>
        /// Days remaining in current period (or trial).
        /// </summary>
        public int DaysRemaining
        {
            get
            {
                var endDate = IsTrialing ? TrialEnd : CurrentPeriodEnd;
                if (!endDate.HasValue) return -1;
                
                var days = (endDate.Value - DateTime.UtcNow).Days;
                return Math.Max(0, days);
            }
        }

        /// <summary>
        /// Current price as decimal dollars.
        /// </summary>
        public decimal Price => PriceCents / 100m;

        /// <summary>
        /// Original price as decimal dollars.
        /// </summary>
        public decimal OriginalPrice => OriginalPriceCents / 100m;

        /// <summary>
        /// Amount saved with current discount.
        /// </summary>
        public decimal Savings => OriginalPrice - Price;

        /// <summary>
        /// AI budget used as a decimal (dollars).
        /// </summary>
        public decimal AiBudgetUsed => AiBudgetUsedCents / 100m;

        /// <summary>
        /// Formatted price string (e.g., "$9.99/mo" or "$99.99/yr").
        /// </summary>
        public string PriceDisplay => IsAnnual 
            ? $"${Price:N2}/yr" 
            : $"${Price:N2}/mo";

        /// <summary>
        /// Formatted renewal date.
        /// </summary>
        public string RenewalDisplay => CurrentPeriodEnd.HasValue 
            ? $"Renews {CurrentPeriodEnd.Value:MMM d, yyyy}" 
            : "—";

        /// <summary>
        /// Effective monthly cost (for annual plans, shows per-month equivalent).
        /// </summary>
        public decimal EffectiveMonthlyPrice => IsAnnual ? Price / 12m : Price;

        #endregion
    }
}

