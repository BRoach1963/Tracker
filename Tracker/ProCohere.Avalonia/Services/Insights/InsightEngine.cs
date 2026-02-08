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
    private readonly IInsightRpcService _rpcService;
    private readonly IInsightActionRepository _actionRepository;
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
        _rpcService = new InsightRpcService();
        _actionRepository = new InsightActionRepository(_rpcService);
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

            var startTime = DateTime.UtcNow;

            // Run all analyzers in PARALLEL for speed
            var analyzerTasks = _analyzers.Select(async analyzer =>
            {
                try
                {
                    Log($"[InsightEngine] Running analyzer: {analyzer.Name}");
                    var insights = await analyzer.AnalyzeAsync(userId, organizationId);
                    Log($"[InsightEngine] {analyzer.Name} generated {insights.Count} insights");
                    return insights;
                }
                catch (Exception ex)
                {
                    Log($"[InsightEngine] ERROR in {analyzer.Name}: {ex.Message}\n{ex.StackTrace}");
                    return new List<Insight>();
                }
            }).ToList();

            var results = await Task.WhenAll(analyzerTasks);
            var allInsights = results.SelectMany(r => r).ToList();

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
    /// Dismisses an insight by signature hash.
    /// </summary>
    public async Task DismissInsightAsync(string signatureHash)
    {
        Log($"[InsightEngine] Dismissing insight with signature {signatureHash}");
        await _actionRepository.DismissAsync(signatureHash);
    }

    /// <summary>
    /// Marks an insight as acted upon by signature hash.
    /// </summary>
    public async Task ActOnInsightAsync(string signatureHash)
    {
        Log($"[InsightEngine] Acting on insight with signature {signatureHash}");
        await _actionRepository.MarkActedAsync(signatureHash);
    }

    /// <summary>
    /// Snoozes an insight for a duration by signature hash.
    /// </summary>
    public async Task SnoozeInsightAsync(string signatureHash, TimeSpan duration)
    {
        Log($"[InsightEngine] Snoozing insight with signature {signatureHash} for {duration}");
        await _actionRepository.SnoozeAsync(signatureHash, duration);
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
        if (insights.Count == 0)
            return 0;

        // First pass: Generate signatures and filter duplicates locally
        var uniqueInsights = new List<Insight>();
        var seenSignatures = new HashSet<string>();

        foreach (var insight in insights)
        {
            // Need SubjectId to generate signature
            if (!insight.SubjectId.HasValue)
            {
                Log($"[InsightEngine] Skipping insight without SubjectId: {insight.Type}");
                continue;
            }
            
            // Generate signature for each insight
            insight.SignatureHash = InsightSignature.Generate(
                insight.Type,
                insight.SubjectType ?? "",
                insight.SubjectId.Value,
                insight.RuleKey ?? ""
            );
            insight.GeneratedAt = DateTime.UtcNow;

            // Skip local duplicates
            if (seenSignatures.Contains(insight.SignatureHash))
            {
                Log($"[InsightEngine] Skipping local duplicate: {insight.Type} - {insight.SignatureHash}");
                continue;
            }
            seenSignatures.Add(insight.SignatureHash);

            // Check if already exists in database
            var exists = await _repository.SignatureExistsAsync(
                insight.OrganizationId,
                userId,
                insight.SignatureHash
            );

            if (exists)
            {
                Log($"[InsightEngine] Skipping existing: {insight.Type} - {insight.SignatureHash}");
                continue;
            }

            uniqueInsights.Add(insight);
        }

        if (uniqueInsights.Count == 0)
        {
            Log("[InsightEngine] No new unique insights to persist");
            return 0;
        }

        // Batch create via RPC
        Log($"[InsightEngine] Persisting {uniqueInsights.Count} unique insights via RPC batch");
        try
        {
            var createdCount = await _rpcService.CreateInsightsBatchAsync(userId, uniqueInsights);
            Log($"[InsightEngine] RPC batch created {createdCount} insights");
            return createdCount;
        }
        catch (Exception ex)
        {
            Log($"[InsightEngine] ERROR in batch create: {ex.Message}\n{ex.StackTrace}");
            
            // Fallback: try one at a time
            var createdCount = 0;
            foreach (var insight in uniqueInsights)
            {
                try
                {
                    await _rpcService.CreateInsightAsync(userId, insight);
                    createdCount++;
                }
                catch (Exception innerEx)
                {
                    Log($"[InsightEngine] ERROR persisting insight: {innerEx.Message}");
                }
            }
            return createdCount;
        }
    }

    #endregion
}
