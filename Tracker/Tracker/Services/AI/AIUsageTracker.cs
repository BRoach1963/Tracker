using System.IO;
using System.Text.Json;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Tracks AI API usage and enforces budget limits.
    /// Gemini 2.5 Pro pricing (as of Dec 2025):
    /// - Input: $1.25 per 1M tokens (0-128K context)
    /// - Output: $5.00 per 1M tokens (0-128K context)
    /// - Tokens estimated at ~4 characters per token (conservative)
    /// </summary>
    public class AIUsageTracker
    {
        #region Constants

        // Gemini 2.5 Pro pricing (conservative estimates)
        // Input: $1.25 per 1M tokens = $0.0003125 per 1K tokens = $0.00000031 per token
        // Assuming 4 chars/token: $0.00000031 / 4 = $0.0000000775 per char
        private const decimal CostPerInputChar = 0.00000031m;    // $1.25/1M tokens, ~4 chars/token
        
        // Output: $5.00 per 1M tokens = $0.00125 per 1K tokens = $0.00000125 per token
        // Assuming 4 chars/token: $0.00000125 / 4 = $0.0000003125 per char
        private const decimal CostPerOutputChar = 0.00000125m;   // $5.00/1M tokens, ~4 chars/token
        
        private const string UsageFileName = "ai_usage.json";

        #endregion

        #region Singleton

        private static readonly Lazy<AIUsageTracker> _instance =
            new(() => new AIUsageTracker(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static AIUsageTracker Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly string _usageFilePath;
        private UsageData _usage;
        private readonly object _lock = new();

        #endregion

        #region Properties

        /// <summary>
        /// Monthly budget in dollars.
        /// </summary>
        public decimal MonthlyBudget => UserSettingsManager.Instance.Settings.AI.MonthlyBudget;

        /// <summary>
        /// Warning threshold as percentage (0-100).
        /// </summary>
        public int WarningThresholdPercent => UserSettingsManager.Instance.Settings.AI.BudgetWarningPercent;

        /// <summary>
        /// Current month's estimated cost.
        /// </summary>
        public decimal CurrentMonthCost
        {
            get
            {
                lock (_lock)
                {
                    EnsureCurrentMonth();
                    return _usage.EstimatedCost;
                }
            }
        }

        /// <summary>
        /// Current month's request count.
        /// </summary>
        public int CurrentMonthRequests
        {
            get
            {
                lock (_lock)
                {
                    EnsureCurrentMonth();
                    return _usage.RequestCount;
                }
            }
        }

        /// <summary>
        /// Percentage of budget used (0-100+).
        /// </summary>
        public decimal BudgetUsedPercent
        {
            get
            {
                if (MonthlyBudget <= 0) return 0;
                return Math.Round((CurrentMonthCost / MonthlyBudget) * 100, 2);
            }
        }

        /// <summary>
        /// Whether the warning threshold has been reached.
        /// </summary>
        public bool IsWarningThresholdReached => BudgetUsedPercent >= WarningThresholdPercent;

        /// <summary>
        /// Whether the budget limit has been reached.
        /// </summary>
        public bool IsBudgetExceeded => CurrentMonthCost >= MonthlyBudget && MonthlyBudget > 0;

        /// <summary>
        /// Whether AI is currently enabled (respects budget limit).
        /// </summary>
        public bool IsAIEnabled
        {
            get
            {
                var settings = UserSettingsManager.Instance.Settings.AI;
                if (!settings.IsEnabled) return false;
                if (settings.EnforceBudgetLimit && IsBudgetExceeded) return false;
                return true;
            }
        }

        #endregion

        #region Constructor

        private AIUsageTracker()
        {
            _logger = LoggingManager.GetComponentLogger("AIUsage");
            
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Tracker");
            
            Directory.CreateDirectory(appDataPath);
            _usageFilePath = Path.Combine(appDataPath, UsageFileName);
            
            _usage = LoadUsage();
            _logger.Info("AI Usage Tracker initialized. Current month: {0} requests, ${1:F4} estimated",
                _usage.RequestCount, _usage.EstimatedCost);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Records a completed API request.
        /// </summary>
        /// <param name="inputChars">Number of characters sent to API</param>
        /// <param name="outputChars">Number of characters received from API</param>
        public void RecordRequest(int inputChars, int outputChars)
        {
            lock (_lock)
            {
                EnsureCurrentMonth();

                _usage.RequestCount++;
                _usage.TotalInputChars += inputChars;
                _usage.TotalOutputChars += outputChars;
                
                var inputCost = inputChars * CostPerInputChar;
                var outputCost = outputChars * CostPerOutputChar;
                _usage.EstimatedCost += inputCost + outputCost;
                
                _usage.LastRequestTime = DateTime.UtcNow;

                SaveUsage();

                _logger.Debug("Recorded request: +{0} in, +{1} out, cost=${2:F6}, total=${3:F4}",
                    inputChars, outputChars, inputCost + outputCost, _usage.EstimatedCost);

                // Log warnings
                if (IsBudgetExceeded)
                {
                    _logger.Warn("AI BUDGET EXCEEDED! ${0:F2} of ${1:F2} budget used",
                        _usage.EstimatedCost, MonthlyBudget);
                }
                else if (IsWarningThresholdReached)
                {
                    _logger.Warn("AI budget warning: {0:F1}% of ${1:F2} budget used",
                        BudgetUsedPercent, MonthlyBudget);
                }
            }
        }

        /// <summary>
        /// Checks if a request can be made (respects budget limits).
        /// </summary>
        /// <returns>Tuple of (canProceed, warningMessage)</returns>
        public (bool CanProceed, string? Message) CheckCanMakeRequest()
        {
            var settings = UserSettingsManager.Instance.Settings.AI;

            if (!settings.IsEnabled)
            {
                return (false, "AI features are disabled in settings.");
            }

            if (settings.EnforceBudgetLimit && IsBudgetExceeded)
            {
                return (false, $"Monthly AI budget of ${MonthlyBudget:F2} has been reached. AI is disabled until next month or budget is increased.");
            }

            if (IsWarningThresholdReached)
            {
                return (true, $"⚠️ AI usage at {BudgetUsedPercent:F1}% of monthly budget (${CurrentMonthCost:F2} of ${MonthlyBudget:F2})");
            }

            return (true, null);
        }

        /// <summary>
        /// Gets a summary of current usage for display.
        /// </summary>
        public string GetUsageSummary()
        {
            lock (_lock)
            {
                EnsureCurrentMonth();
                
                return $"This month: {_usage.RequestCount:N0} requests, ${_usage.EstimatedCost:F4} estimated cost ({BudgetUsedPercent:F1}% of ${MonthlyBudget:F2} budget)";
            }
        }

        /// <summary>
        /// Resets usage data (for testing or manual reset).
        /// </summary>
        public void ResetUsage()
        {
            lock (_lock)
            {
                _usage = new UsageData
                {
                    MonthYear = DateTime.UtcNow.ToString("yyyy-MM")
                };
                SaveUsage();
                _logger.Info("AI usage data reset");
            }
        }

        /// <summary>
        /// Adds purchased credits to the account.
        /// </summary>
        public void AddPurchasedCredits(int credits)
        {
            lock (_lock)
            {
                _usage.PurchasedTokens = (_usage.PurchasedTokens ?? 0) + credits;
                SaveUsage();
                _logger.Info("Added {0} purchased credits", credits);
            }
        }

        #endregion

        #region Private Methods

        private void EnsureCurrentMonth()
        {
            var currentMonthYear = DateTime.UtcNow.ToString("yyyy-MM");
            
            if (_usage.MonthYear != currentMonthYear)
            {
                _logger.Info("New month detected, resetting usage counters");
                _usage = new UsageData
                {
                    MonthYear = currentMonthYear
                };
                SaveUsage();
            }
        }

        private UsageData LoadUsage()
        {
            try
            {
                if (File.Exists(_usageFilePath))
                {
                    var json = File.ReadAllText(_usageFilePath);
                    var data = JsonSerializer.Deserialize<UsageData>(json);
                    if (data != null)
                    {
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error loading AI usage data");
            }

            return new UsageData
            {
                MonthYear = DateTime.UtcNow.ToString("yyyy-MM")
            };
        }

        private void SaveUsage()
        {
            try
            {
                var json = JsonSerializer.Serialize(_usage, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_usageFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error saving AI usage data");
            }
        }

        #endregion

        #region Usage Data Model

        private class UsageData
        {
            public string MonthYear { get; set; } = "";
            public int RequestCount { get; set; }
            public long TotalInputChars { get; set; }
            public long TotalOutputChars { get; set; }
            public decimal EstimatedCost { get; set; }
            public DateTime? LastRequestTime { get; set; }
            public int? PurchasedTokens { get; set; }
        }

        #endregion
    }
}

