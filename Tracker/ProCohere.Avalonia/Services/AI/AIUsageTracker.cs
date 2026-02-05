using System;
using System.Threading;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// Tracks AI usage for budget monitoring and cost control.
/// Singleton pattern for centralized usage management.
/// </summary>
public sealed class AIUsageTracker
{
    #region Singleton

    private static readonly Lazy<AIUsageTracker> _instance = 
        new(() => new AIUsageTracker(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static AIUsageTracker Instance => _instance.Value;

    #endregion

    #region Fields

    private long _totalInputTokens;
    private long _totalOutputTokens;
    private long _totalRequests;
    private readonly object _lock = new();

    // Budget limits (configurable)
    private const long DailyRequestLimit = 1000;
    private const long DailyTokenLimit = 100_000;

    #endregion

    #region Constructor

    private AIUsageTracker() { }

    #endregion

    #region Public Properties

    public long TotalRequests
    {
        get { lock (_lock) { return _totalRequests; } }
    }

    public long TotalInputTokens
    {
        get { lock (_lock) { return _totalInputTokens; } }
    }

    public long TotalOutputTokens
    {
        get { lock (_lock) { return _totalOutputTokens; } }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Checks if we can make another request within budget limits.
    /// </summary>
    /// <returns>Tuple of (canProceed, message)</returns>
    public (bool CanProceed, string Message) CheckCanMakeRequest()
    {
        lock (_lock)
        {
            if (_totalRequests >= DailyRequestLimit)
            {
                return (false, $"Daily request limit reached ({DailyRequestLimit}). Please try again tomorrow.");
            }

            if (_totalInputTokens + _totalOutputTokens >= DailyTokenLimit)
            {
                return (false, $"Daily token limit reached ({DailyTokenLimit}). Please try again tomorrow.");
            }

            return (true, string.Empty);
        }
    }

    /// <summary>
    /// Records usage for a completed request.
    /// </summary>
    /// <param name="inputTokens">Estimated input tokens (characters / 4)</param>
    /// <param name="outputTokens">Estimated output tokens (characters / 4)</param>
    public void RecordRequest(long inputTokens, long outputTokens)
    {
        lock (_lock)
        {
            _totalRequests++;
            _totalInputTokens += inputTokens;
            _totalOutputTokens += outputTokens;
        }
    }

    /// <summary>
    /// Gets a usage summary for display.
    /// </summary>
    public string GetUsageSummary()
    {
        lock (_lock)
        {
            return $"Requests: {_totalRequests}/{DailyRequestLimit} | " +
                   $"Tokens: {_totalInputTokens + _totalOutputTokens:N0}/{DailyTokenLimit:N0}";
        }
    }

    /// <summary>
    /// Resets daily usage counters (call at midnight).
    /// </summary>
    public void ResetDailyUsage()
    {
        lock (_lock)
        {
            _totalInputTokens = 0;
            _totalOutputTokens = 0;
            _totalRequests = 0;
        }
    }

    #endregion
}