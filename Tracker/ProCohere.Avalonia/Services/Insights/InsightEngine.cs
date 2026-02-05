using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Singleton engine that coordinates all insight analyzers.
/// Runs analyzers in sequence, deduplicates insights, and persists to database.
/// </summary>
public class InsightEngine
{
    private static readonly Lazy<InsightEngine> _instance = new(() => new InsightEngine());
    public static InsightEngine Instance => _instance.Value;

    private readonly List<IInsightAnalyzer> _analyzers;
    private readonly IInsightRepository _repository;
    private bool _isRunning;

    private InsightEngine()
    {
        _analyzers = new List<IInsightAnalyzer>();
        _repository = new InsightRepository();
    }

    /// <summary>
    /// Registers an analyzer with the engine.
    /// Should be called during application startup for all analyzers.
    /// </summary>
    public void RegisterAnalyzer(IInsightAnalyzer analyzer)
    {
        if (_analyzers.Any(a => a.Name == analyzer.Name))
        {
            Console.WriteLine($"[InsightEngine] Analyzer {analyzer.Name} already registered");
            return;
        }

        _analyzers.Add(analyzer);
        Console.WriteLine($"[InsightEngine] Registered analyzer: {analyzer.Name}");
    }

    /// <summary>
    /// Runs all registered analyzers for a specific user.
    /// Deduplicates and persists generated insights.
    /// </summary>
    /// <param name="userId">The user to analyze.</param>
    /// <param name="organizationId">The organization context.</param>
    /// <returns>Number of insights created.</returns>
    public async Task<int> RunAnalysisAsync(Guid userId, Guid organizationId)
    {
        if (_isRunning)
        {
            Console.WriteLine("[InsightEngine] Analysis already running");
            return 0;
        }

        try
        {
            _isRunning = true;
            Console.WriteLine($"[InsightEngine] Starting analysis with {_analyzers.Count} analyzers");

            var allInsights = new List<Insight>();
            var startTime = DateTime.UtcNow;

            // Run each analyzer
            foreach (var analyzer in _analyzers)
            {
                try
                {
                    Console.WriteLine($"[InsightEngine] Running analyzer: {analyzer.Name}");
                    var insights = await analyzer.AnalyzeAsync(userId, organizationId);
                    
                    Console.WriteLine($"[InsightEngine] {analyzer.Name} generated {insights.Count} insights");
                    allInsights.AddRange(insights);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[InsightEngine] ERROR in {analyzer.Name}: {ex.Message}");
                    // Continue with other analyzers
                }
            }

            // Deduplicate and persist
            var createdCount = await DeduplicateAndPersistAsync(allInsights, userId);

            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"[InsightEngine] Analysis completed in {duration.TotalSeconds:F2}s: {createdCount} new insights");

            return createdCount;
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Gets the count of active insights for a user.
    /// </summary>
    public Task<int> GetActiveCountAsync(Guid userId)
    {
        return _repository.GetActiveCountAsync(userId);
    }

    /// <summary>
    /// Gets all active insights for a user.
    /// </summary>
    public Task<List<Insight>> GetActiveInsightsAsync(Guid userId)
    {
        return _repository.GetActiveInsightsAsync(userId);
    }

    /// <summary>
    /// Dismisses an insight.
    /// </summary>
    public async Task DismissInsightAsync(Guid insightId, Guid userId)
    {
        Console.WriteLine($"[InsightEngine] Dismissing insight {insightId}");
        await _repository.DismissInsightAsync(insightId, userId);
    }

    /// <summary>
    /// Marks an insight as acted upon.
    /// </summary>
    public async Task ActOnInsightAsync(Guid insightId)
    {
        Console.WriteLine($"[InsightEngine] Acting on insight {insightId}");
        await _repository.MarkInsightActionedAsync(insightId);
    }

    /// <summary>
    /// Snoozes an insight until a specific time.
    /// </summary>
    public async Task SnoozeInsightAsync(Guid insightId, DateTime until)
    {
        Console.WriteLine($"[InsightEngine] Snoozing insight {insightId}");
        await _repository.SnoozeInsightAsync(insightId, until);
    }

    /// <summary>
    /// Cleans up old dismissed/acted-on insights.
    /// </summary>
    public Task<int> CleanupOldInsightsAsync(int daysOld = 90)
    {
        // Note: Cleanup not implemented in current schema
        return Task.FromResult(0);
    }

    #region Private Methods

    private async Task<int> DeduplicateAndPersistAsync(List<Insight> insights, Guid userId)
    {
        var createdCount = 0;

        foreach (var insight in insights)
        {
            try
            {
                // Check for duplicate
                var isDuplicate = await _repository.InsightExistsAsync(
                    insight.OrganizationId,
                    userId,
                    insight.Type,
                    insight.EntityId
                );

                if (isDuplicate)
                {
                    Console.WriteLine($"[InsightEngine] Skipping duplicate: {insight.Type}");
                    continue;
                }

                // Create new insight
                await _repository.CreateInsightAsync(insight);
                createdCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InsightEngine] ERROR persisting insight: {ex.Message}");
                // Continue with other insights
            }
        }

        return createdCount;
    }

    #endregion
}
