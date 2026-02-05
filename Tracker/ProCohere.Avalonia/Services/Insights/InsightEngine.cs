using System;
using System.Collections.Generic;
using System.IO;
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
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "ProCohere", "insight_engine.log");

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.AppendAllText(_logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}\n");
        }
        catch { /* Ignore logging errors */ }
    }

    private InsightEngine()
    {
        _analyzers = new List<IInsightAnalyzer>();
        _repository = new InsightRepository();
        Log("[InsightEngine] Constructor called");
    }

    /// <summary>
    /// Registers an analyzer with the engine.
    /// Should be called during application startup for all analyzers.
    /// </summary>
    public void RegisterAnalyzer(IInsightAnalyzer analyzer)
    {
        if (_analyzers.Any(a => a.Name == analyzer.Name))
        {
            Log($"[InsightEngine] Analyzer {analyzer.Name} already registered");
            return;
        }

        _analyzers.Add(analyzer);
        Log($"[InsightEngine] Registered analyzer: {analyzer.Name}");
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
            Log("[InsightEngine] Analysis already running");
            return 0;
        }

        try
        {
            _isRunning = true;
            Log($"[InsightEngine] Starting analysis for user {userId} org {organizationId} with {_analyzers.Count} analyzers");

            var allInsights = new List<Insight>();
            var startTime = DateTime.UtcNow;

            // Run each analyzer
            foreach (var analyzer in _analyzers)
            {
                try
                {
                    Log($"[InsightEngine] Running analyzer: {analyzer.Name}");
                    var insights = await analyzer.AnalyzeAsync(userId, organizationId);
                    
                    Log($"[InsightEngine] {analyzer.Name} generated {insights.Count} insights");
                    allInsights.AddRange(insights);
                }
                catch (Exception ex)
                {
                    Log($"[InsightEngine] ERROR in {analyzer.Name}: {ex.Message}\n{ex.StackTrace}");
                    // Continue with other analyzers
                }
            }

            // Deduplicate and persist
            Log($"[InsightEngine] Total insights generated: {allInsights.Count}, now persisting...");
            var createdCount = await DeduplicateAndPersistAsync(allInsights, userId);

            var duration = DateTime.UtcNow - startTime;
            Log($"[InsightEngine] Analysis completed in {duration.TotalSeconds:F2}s: {createdCount} new insights created");

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
        Log($"[InsightEngine] Dismissing insight {insightId}");
        await _repository.DismissInsightAsync(insightId, userId);
    }

    /// <summary>
    /// Marks an insight as acted upon.
    /// </summary>
    public async Task ActOnInsightAsync(Guid insightId)
    {
        Log($"[InsightEngine] Acting on insight {insightId}");
        await _repository.MarkInsightActionedAsync(insightId);
    }

    /// <summary>
    /// Snoozes an insight until a specific time.
    /// </summary>
    public async Task SnoozeInsightAsync(Guid insightId, DateTime until)
    {
        Log($"[InsightEngine] Snoozing insight {insightId}");
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
                    Log($"[InsightEngine] Skipping duplicate: {insight.Type} for entity {insight.EntityId}");
                    continue;
                }

                // Create new insight
                Log($"[InsightEngine] Persisting insight: {insight.Type} - {insight.Title}");
                await _repository.CreateInsightAsync(insight);
                createdCount++;
            }
            catch (Exception ex)
            {
                Log($"[InsightEngine] ERROR persisting insight: {ex.Message}\n{ex.StackTrace}");
                // Continue with other insights
            }
        }

        return createdCount;
    }

    #endregion
}
