using Tracker.Common.Enums;

namespace Tracker.Services.Subscription
{
    /// <summary>
    /// Defines the feature limits for each subscription tier.
    /// </summary>
    public class SubscriptionLimits
    {
        #region Static Tier Definitions

        /// <summary>
        /// Free tier - Local DB only, limited team size, no AI/calendar/reports.
        /// </summary>
        public static readonly SubscriptionLimits Free = new()
        {
            Tier = SubscriptionTier.Free,
            DisplayName = "Free",
            MaxTeamMembers = 10,
            MaxOneOnOnesPerMonth = -1, // Unlimited
            MaxTasks = -1, // Unlimited
            MaxProjects = -1,
            MaxOKRs = -1,
            MaxKPIs = -1,
            MaxGoals = -1,
            HasAIAssistant = false,
            HasAIDataAnalysis = false,
            HasCalendarSync = false,
            HasBasicReports = false,
            HasAdvancedReports = false,
            HasEmailSupport = false,
            HasPrioritySupport = false,
            AllowsNetworkDatabase = false, // Local DB only
            MonthlyAIBudget = 0m
        };

        /// <summary>
        /// Standard tier - Local DB, larger team, AI help (no data), basic reports, calendar sync.
        /// </summary>
        public static readonly SubscriptionLimits Standard = new()
        {
            Tier = SubscriptionTier.Standard,
            DisplayName = "Standard",
            MaxTeamMembers = 100,
            MaxOneOnOnesPerMonth = -1,
            MaxTasks = -1,
            MaxProjects = -1,
            MaxOKRs = -1,
            MaxKPIs = -1,
            MaxGoals = -1,
            HasAIAssistant = true, // Help Bot enabled
            HasAIDataAnalysis = false, // But NO data analysis
            HasCalendarSync = true,
            HasBasicReports = true,
            HasAdvancedReports = false,
            HasEmailSupport = true,
            HasPrioritySupport = false,
            AllowsNetworkDatabase = false, // Local DB only
            MonthlyAIBudget = 2.00m // Limited AI budget (~$2/month)
        };

        /// <summary>
        /// Pro tier - Network DB, unlimited team, full AI with data analysis, full reports.
        /// </summary>
        public static readonly SubscriptionLimits Pro = new()
        {
            Tier = SubscriptionTier.Pro,
            DisplayName = "Pro",
            MaxTeamMembers = -1, // Unlimited
            MaxOneOnOnesPerMonth = -1,
            MaxTasks = -1,
            MaxProjects = -1,
            MaxOKRs = -1,
            MaxKPIs = -1,
            MaxGoals = -1,
            HasAIAssistant = true,
            HasAIDataAnalysis = true, // Full AI with data analysis
            HasCalendarSync = true,
            HasBasicReports = true,
            HasAdvancedReports = true,
            HasEmailSupport = true,
            HasPrioritySupport = true,
            AllowsNetworkDatabase = true, // Enterprise/Network DB
            MonthlyAIBudget = 10.00m // Generous AI budget
        };

        /// <summary>
        /// Internal/testing tier - everything unlocked.
        /// </summary>
        public static readonly SubscriptionLimits Internal = new()
        {
            Tier = SubscriptionTier.Internal,
            DisplayName = "Internal",
            MaxTeamMembers = -1,
            MaxOneOnOnesPerMonth = -1,
            MaxTasks = -1,
            MaxProjects = -1,
            MaxOKRs = -1,
            MaxKPIs = -1,
            MaxGoals = -1,
            HasAIAssistant = true,
            HasAIDataAnalysis = true,
            HasCalendarSync = true,
            HasBasicReports = true,
            HasAdvancedReports = true,
            HasEmailSupport = true,
            HasPrioritySupport = true,
            AllowsNetworkDatabase = true,
            MonthlyAIBudget = 1000m // Unlimited for testing
        };

        #endregion

        #region Properties

        /// <summary>
        /// The subscription tier.
        /// </summary>
        public SubscriptionTier Tier { get; init; }

        /// <summary>
        /// Display name for the tier.
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Maximum team members allowed (-1 = unlimited).
        /// </summary>
        public int MaxTeamMembers { get; init; }

        /// <summary>
        /// Maximum 1:1 meetings per month (-1 = unlimited).
        /// </summary>
        public int MaxOneOnOnesPerMonth { get; init; }

        /// <summary>
        /// Maximum tasks allowed (-1 = unlimited).
        /// </summary>
        public int MaxTasks { get; init; }

        /// <summary>
        /// Maximum projects allowed (-1 = unlimited).
        /// </summary>
        public int MaxProjects { get; init; }

        /// <summary>
        /// Maximum OKRs allowed (-1 = unlimited).
        /// </summary>
        public int MaxOKRs { get; init; }

        /// <summary>
        /// Maximum KPIs allowed (-1 = unlimited).
        /// </summary>
        public int MaxKPIs { get; init; }

        /// <summary>
        /// Maximum goals allowed (-1 = unlimited).
        /// </summary>
        public int MaxGoals { get; init; }

        /// <summary>
        /// Whether AI Assistant (Help Bot) is available.
        /// </summary>
        public bool HasAIAssistant { get; init; }

        /// <summary>
        /// Whether AI can analyze user data (Pro feature).
        /// Standard tier gets help-only, Pro gets data analysis.
        /// </summary>
        public bool HasAIDataAnalysis { get; init; }

        /// <summary>
        /// Whether calendar sync (Google/Outlook) is available.
        /// </summary>
        public bool HasCalendarSync { get; init; }

        /// <summary>
        /// Whether basic reports are available.
        /// </summary>
        public bool HasBasicReports { get; init; }

        /// <summary>
        /// Whether advanced/custom reports are available.
        /// </summary>
        public bool HasAdvancedReports { get; init; }

        /// <summary>
        /// Whether email support is available.
        /// </summary>
        public bool HasEmailSupport { get; init; }

        /// <summary>
        /// Whether priority support is available.
        /// </summary>
        public bool HasPrioritySupport { get; init; }

        /// <summary>
        /// Whether network/enterprise database connections are allowed.
        /// Free/Standard = Local DB only, Pro = Network DB supported.
        /// </summary>
        public bool AllowsNetworkDatabase { get; init; }

        /// <summary>
        /// Monthly AI budget in dollars.
        /// </summary>
        public decimal MonthlyAIBudget { get; init; }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the limits for a specific tier.
        /// </summary>
        public static SubscriptionLimits GetLimits(SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Free => Free,
                SubscriptionTier.Standard => Standard,
                SubscriptionTier.Pro => Pro,
                SubscriptionTier.Internal => Internal,
                _ => Free
            };
        }

        /// <summary>
        /// Checks if a limit value means unlimited.
        /// </summary>
        public static bool IsUnlimited(int limit) => limit < 0;

        /// <summary>
        /// Formats a limit value for display.
        /// </summary>
        public static string FormatLimit(int limit)
        {
            return IsUnlimited(limit) ? "Unlimited" : limit.ToString("N0");
        }

        #endregion
    }
}

