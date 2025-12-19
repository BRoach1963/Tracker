using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Tracker.Common.Enums;

namespace Tracker.Services.Backend.Models
{
    /// <summary>
    /// Subscription plan definition - matches the subscription_plans table in Supabase.
    /// Contains pricing and feature information for each plan/cadence combination.
    /// </summary>
    [Table("subscription_plans")]
    public class SubscriptionPlan : BaseModel
    {
        /// <summary>
        /// Plan identifier (e.g., "standard_monthly", "pro_annual").
        /// </summary>
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name (e.g., "Standard Monthly", "Pro Annual").
        /// </summary>
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Subscription tier: free, standard, pro, internal.
        /// </summary>
        [Column("tier")]
        public string TierString { get; set; } = "free";

        /// <summary>
        /// Billing cadence: monthly or annual.
        /// </summary>
        [Column("billing_cadence")]
        public string BillingCadence { get; set; } = "monthly";

        /// <summary>
        /// Price in cents (e.g., 999 = $9.99).
        /// </summary>
        [Column("price_cents")]
        public int PriceCents { get; set; }

        /// <summary>
        /// Original price before discounts, in cents.
        /// </summary>
        [Column("original_price_cents")]
        public int OriginalPriceCents { get; set; }

        /// <summary>
        /// Discount percentage (e.g., 17 for annual plans).
        /// </summary>
        [Column("discount_percent")]
        public int DiscountPercent { get; set; }

        /// <summary>
        /// Square plan ID (from Square Dashboard).
        /// </summary>
        [Column("square_plan_id")]
        public string? SquarePlanId { get; set; }

        /// <summary>
        /// Square plan variation ID (specific to this price/cadence).
        /// </summary>
        [Column("square_plan_variation_id")]
        public string? SquarePlanVariationId { get; set; }

        /// <summary>
        /// JSON array of feature descriptions for display.
        /// </summary>
        [Column("features")]
        public string? FeaturesJson { get; set; }

        /// <summary>
        /// Whether this plan is available for purchase.
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Sort order for display.
        /// </summary>
        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        #region Computed Properties

        /// <summary>
        /// Gets the subscription tier as an enum.
        /// </summary>
        public SubscriptionTier Tier => TierString?.ToLower() switch
        {
            "free" => SubscriptionTier.Free,
            "standard" => SubscriptionTier.Standard,
            "pro" => SubscriptionTier.Pro,
            "internal" => SubscriptionTier.Internal,
            _ => SubscriptionTier.Free
        };

        /// <summary>
        /// Whether this is an annual plan.
        /// </summary>
        public bool IsAnnual => BillingCadence == "annual";

        /// <summary>
        /// Whether this is a monthly plan.
        /// </summary>
        public bool IsMonthly => BillingCadence == "monthly";

        /// <summary>
        /// Price as decimal dollars.
        /// </summary>
        public decimal Price => PriceCents / 100m;

        /// <summary>
        /// Original price as decimal dollars.
        /// </summary>
        public decimal OriginalPrice => OriginalPriceCents / 100m;

        /// <summary>
        /// Amount saved with discount.
        /// </summary>
        public decimal Savings => OriginalPrice - Price;

        /// <summary>
        /// Effective monthly cost (for comparison).
        /// </summary>
        public decimal EffectiveMonthlyPrice => IsAnnual ? Price / 12m : Price;

        /// <summary>
        /// Formatted price display (e.g., "$9.99/mo").
        /// </summary>
        public string PriceDisplay => IsAnnual 
            ? $"${Price:N2}/yr" 
            : $"${Price:N2}/mo";

        /// <summary>
        /// Formatted effective monthly price.
        /// </summary>
        public string EffectiveMonthlyDisplay => $"${EffectiveMonthlyPrice:N2}/mo";

        /// <summary>
        /// Savings display text (e.g., "Save 17%").
        /// </summary>
        public string SavingsDisplay => DiscountPercent > 0 
            ? $"Save {DiscountPercent}%" 
            : string.Empty;

        /// <summary>
        /// Whether this plan has a discount.
        /// </summary>
        public bool HasDiscount => DiscountPercent > 0;

        /// <summary>
        /// Parses the features JSON into a list.
        /// </summary>
        public List<string> Features
        {
            get
            {
                if (string.IsNullOrEmpty(FeaturesJson))
                    return new List<string>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(FeaturesJson) 
                           ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
        }

        #endregion
    }
}


