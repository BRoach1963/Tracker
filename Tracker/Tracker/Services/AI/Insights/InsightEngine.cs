using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI.Insights
{
    /// <summary>
    /// Coordinates insight analyzers and manages the insight generation lifecycle.
    /// </summary>
    public class InsightEngine : IDisposable
    {
        private static InsightEngine? _instance;
        private static readonly object _lock = new();
        private readonly ILogger _logger;

        private readonly List<IInsightAnalyzer> _analyzers = new();
        private readonly InsightStore _store;
        private CancellationTokenSource? _periodicAnalysisCts;
        private bool _isRunning;
        private bool _disposed;

        /// <summary>
        /// Event fired when a new insight is generated.
        /// </summary>
        public event EventHandler<InsightEventArgs>? InsightGenerated;

        /// <summary>
        /// Event fired when insights are updated (with count of new insights).
        /// </summary>
        public event EventHandler<int>? InsightsUpdated;

        /// <summary>
        /// Singleton instance of the InsightEngine.
        /// </summary>
        public static InsightEngine Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new InsightEngine();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Whether the engine is currently running analysis.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// The registered analyzers.
        /// </summary>
        public IReadOnlyList<IInsightAnalyzer> Analyzers => _analyzers.AsReadOnly();

        private InsightEngine()
        {
            _logger = LoggingManager.GetComponentLogger("InsightEngine");
            _store = InsightStore.Instance;
        }

        /// <summary>
        /// Initializes the insight engine and registers default analyzers.
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.Info("Initializing InsightEngine...");

            // Initialize the store
            await _store.InitializeAsync();

            // Register default analyzers
            RegisterDefaultAnalyzers();

            // Cleanup old insights
            await _store.CleanupOldInsightsAsync();

            _logger.Info("InsightEngine initialized with {0} analyzers", _analyzers.Count);
        }

        /// <summary>
        /// Registers the default set of analyzers.
        /// </summary>
        private void RegisterDefaultAnalyzers()
        {
            // Register built-in analyzers
            RegisterAnalyzer(new Analyzers.MeetingCadenceAnalyzer());
            RegisterAnalyzer(new Analyzers.PersonalDateAnalyzer());
            RegisterAnalyzer(new Analyzers.ActionItemStalenessAnalyzer());
            RegisterAnalyzer(new Analyzers.OkrTrajectoryAnalyzer());
            RegisterAnalyzer(new Analyzers.KpiGapAnalyzer());
            RegisterAnalyzer(new Analyzers.SurveySentimentAnalyzer());
        }

        /// <summary>
        /// Registers an analyzer with the engine.
        /// </summary>
        public void RegisterAnalyzer(IInsightAnalyzer analyzer)
        {
            if (!_analyzers.Contains(analyzer))
            {
                _analyzers.Add(analyzer);
                _logger.Debug("Registered analyzer: {0}", analyzer.Name);
            }
        }

        /// <summary>
        /// Runs all enabled analyzers and generates insights.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all new insights generated.</returns>
        public async Task<List<Insight>> RunAnalyzersAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning)
            {
                _logger.Warn("Analysis already in progress, skipping");
                return new List<Insight>();
            }

            _isRunning = true;
            var allInsights = new List<Insight>();

            try
            {
                _logger.Info("Running {0} insight analyzers...", _analyzers.Count);

                foreach (var analyzer in _analyzers.Where(a => a.IsEnabled))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        _logger.Debug("Running analyzer: {0}", analyzer.Name);
                        var insights = await analyzer.AnalyzeAsync(cancellationToken);
                        
                        if (insights.Any())
                        {
                            _logger.Info("Analyzer {0} generated {1} insights", 
                                analyzer.Name, insights.Count);
                            allInsights.AddRange(insights);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Exception(ex, "Analyzer {0} failed", analyzer.Name);
                    }
                }

                // Save all insights (deduplication handled by store)
                var newCount = await _store.SaveInsightsAsync(allInsights);
                
                _logger.Info("Analysis complete. {0} new insights saved, {1} total generated",
                    newCount, allInsights.Count);

                // Fire events for new insights
                foreach (var insight in allInsights.Take(newCount))
                {
                    InsightGenerated?.Invoke(this, new InsightEventArgs(insight));
                }

                if (newCount > 0)
                {
                    InsightsUpdated?.Invoke(this, newCount);
                }

                return allInsights;
            }
            finally
            {
                _isRunning = false;
            }
        }

        /// <summary>
        /// Generates a daily briefing with all relevant information.
        /// </summary>
        public async Task<DailyBriefing> GenerateDailyBriefingAsync()
        {
            _logger.Info("Generating daily briefing...");

            // Run analyzers first to ensure fresh data
            await RunAnalyzersAsync();

            // Get all active insights
            var activeInsights = await _store.GetActiveInsightsAsync();

            var briefing = new DailyBriefing
            {
                GeneratedAt = DateTime.Now,
                Greeting = DailyBriefing.GetGreeting(GetCurrentUserName()),
                CriticalInsights = activeInsights.Where(i => i.Severity == InsightSeverity.Critical).ToList(),
                WarningInsights = activeInsights.Where(i => i.Severity == InsightSeverity.Warning).ToList(),
                InfoInsights = activeInsights.Where(i => i.Severity == InsightSeverity.Info).ToList()
            };

            // TODO: Add meetings today, OKR stats, etc. from database

            _logger.Info("Daily briefing generated with {0} critical, {1} warning, {2} info insights",
                briefing.CriticalInsights.Count, briefing.WarningInsights.Count, briefing.InfoInsights.Count);

            return briefing;
        }

        /// <summary>
        /// Starts periodic analysis at the configured interval.
        /// </summary>
        /// <param name="intervalHours">Hours between analysis runs.</param>
        public void StartPeriodicAnalysis(int intervalHours = 4)
        {
            _periodicAnalysisCts?.Cancel();
            _periodicAnalysisCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!_periodicAnalysisCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(intervalHours), _periodicAnalysisCts.Token);
                        await RunAnalyzersAsync(_periodicAnalysisCts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Exception(ex, "Periodic analysis failed");
                    }
                }
            }, _periodicAnalysisCts.Token);

            _logger.Info("Started periodic analysis every {0} hours", intervalHours);
        }

        /// <summary>
        /// Stops periodic analysis.
        /// </summary>
        public void StopPeriodicAnalysis()
        {
            _periodicAnalysisCts?.Cancel();
            _logger.Info("Stopped periodic analysis");
        }

        /// <summary>
        /// Gets the count of unread insights.
        /// </summary>
        public Task<int> GetUnreadCountAsync() => _store.GetUnreadCountAsync();

        /// <summary>
        /// Gets all active insights.
        /// </summary>
        public Task<List<Insight>> GetActiveInsightsAsync() => _store.GetActiveInsightsAsync();

        /// <summary>
        /// Marks an insight as read.
        /// </summary>
        public Task MarkAsReadAsync(int insightId) => _store.MarkAsReadAsync(insightId);

        /// <summary>
        /// Marks all insights as read.
        /// </summary>
        public Task MarkAllAsReadAsync() => _store.MarkAllAsReadAsync();

        /// <summary>
        /// Dismisses an insight.
        /// </summary>
        public Task DismissInsightAsync(int insightId) => _store.DismissInsightAsync(insightId);

        /// <summary>
        /// Marks an insight as acted upon.
        /// </summary>
        public Task MarkAsActedOnAsync(int insightId) => _store.MarkAsActedOnAsync(insightId);

        private static string GetCurrentUserName()
        {
            // Try to get user's name from settings
            var settings = UserSettingsManager.Instance?.Settings;
            if (settings != null && !string.IsNullOrEmpty(settings.CurrentUser))
            {
                return settings.CurrentUser.Split(' ')[0]; // First name
            }
            return string.Empty;
        }

        public void Dispose()
        {
            if (_disposed) return;

            StopPeriodicAnalysis();
            _periodicAnalysisCts?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Event args for insight generation events.
    /// </summary>
    public class InsightEventArgs : EventArgs
    {
        public Insight Insight { get; }

        public InsightEventArgs(Insight insight)
        {
            Insight = insight;
        }
    }
}
