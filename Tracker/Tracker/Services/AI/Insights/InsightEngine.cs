using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.DTOs;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services.AI.Insights
{
    /// <summary>
    /// Coordinates insight analyzers and manages the insight generation lifecycle.
    /// Now uses IInsightRepository (Dapper) instead of SQLite InsightStore.
    /// </summary>
    public class InsightEngine : IDisposable
    {
        private static InsightEngine? _instance;
        private static readonly object _lock = new();
        private readonly ILogger _logger;

        private readonly List<IInsightAnalyzer> _analyzers = new();
        private IInsightRepository? _repository;
        private Guid _organizationId;
        private Guid _currentUserId;
        private CancellationTokenSource? _periodicAnalysisCts;
        private bool _isRunning;
        private bool _disposed;
        private bool _isInitialized;

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
        /// Whether the engine has been initialized with a repository.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// The registered analyzers.
        /// </summary>
        public IReadOnlyList<IInsightAnalyzer> Analyzers => _analyzers.AsReadOnly();

        private InsightEngine()
        {
            _logger = LoggingManager.GetComponentLogger("InsightEngine");
        }

        /// <summary>
        /// Initializes the insight engine with the repository.
        /// Must be called before using the engine.
        /// </summary>
        /// <param name="repository">The insight repository for data access.</param>
        /// <param name="organizationId">The current organization ID.</param>
        /// <param name="userId">The current user ID.</param>
        public async Task InitializeAsync(IInsightRepository repository, Guid organizationId, Guid userId)
        {
            if (_isInitialized)
            {
                _logger.Debug("InsightEngine already initialized");
                return;
            }

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _organizationId = organizationId;
            _currentUserId = userId;

            _logger.Info("Initializing InsightEngine with Dapper repository...");

            // Register default analyzers
            RegisterDefaultAnalyzers();

            // Cleanup old insights
            await _repository.CleanupOldInsightsAsync();

            _isInitialized = true;
            _logger.Info("InsightEngine initialized with {0} analyzers", _analyzers.Count);
        }

        /// <summary>
        /// Legacy initialization method for backward compatibility.
        /// Will attempt to get repository from OrganizationContext.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger.Debug("InsightEngine already initialized");
                return;
            }

            // For backward compatibility, just register analyzers
            // The repository will need to be set separately
            _logger.Warn("InsightEngine.InitializeAsync() called without repository - limited functionality");
            RegisterDefaultAnalyzers();
            _logger.Info("InsightEngine partially initialized with {0} analyzers (no repository)", _analyzers.Count);
        }

        /// <summary>
        /// Sets the repository after construction (for DI scenarios).
        /// </summary>
        public void SetRepository(IInsightRepository repository, Guid organizationId, Guid userId)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _organizationId = organizationId;
            _currentUserId = userId;
            _isInitialized = true;
            _logger.Info("InsightEngine repository set for organization {0}", organizationId);
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
            RegisterAnalyzer(new Analyzers.GoalTrajectoryAnalyzer(new Database.TrackerDbContext()));
            RegisterAnalyzer(new Analyzers.MetricGapAnalyzer());
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

                // Also run AI-powered insight generation if enabled
                try
                {
                    var aiInsights = await GenerateAIInsightsAsync(cancellationToken);
                    if (aiInsights.Any())
                    {
                        _logger.Info("AI generated {0} additional insights", aiInsights.Count);
                        allInsights.AddRange(aiInsights);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex, "AI insight generation failed");
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

        /// <summary>
        /// Generates AI-powered insights by gathering team data and calling AIInsightGenerator.
        /// Uses TrackerDataManager for data access (singleton-to-singleton, no DI needed).
        /// </summary>
        private async Task<List<Insight>> GenerateAIInsightsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Check if AI is enabled
                var settings = UserSettingsManager.Instance?.Settings?.AI;
                if (settings == null || !settings.IsEnabled)
                {
                    _logger.Debug("AI insights disabled in settings");
                    return new List<Insight>();
                }

                // Get data from TrackerDataManager (already loaded/cached)
                var dataManager = TrackerDataManager.Instance;
                var teamMembers = (await dataManager.GetTeamMembers()).ToList();
                var allTasks = (await dataManager.GetTasks()).ToList();
                var allGoals = (await dataManager.GetStrategicGoals()).ToList();

                // Filter for overdue tasks
                var overdueTasks = allTasks
                    .Where(t => !t.IsCompleted && t.DueDate < DateTime.Now)
                    .Select(t => new TrackerTask 
                    {
                        Id = t.Id,
                        Title = t.Title,
                        DueDate = t.DueDate,
                        Status = t.Status,
                        AssigneeId = t.AssigneeId
                    })
                    .ToList();

                // Filter for at-risk goals
                var atRiskGoals = GetAtRiskGoalsFromCollection(allGoals);

                var context = new TeamDataContext
                {
                    TeamMembers = teamMembers,
                    OverdueTasks = overdueTasks,
                    AtRiskGoals = atRiskGoals
                };

                // Generate AI insights
                return await AIInsightGenerator.Instance.GenerateInsightsAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to generate AI insights");
                return new List<Insight>();
            }
        }

        /// <summary>
        /// Gets goals that are at risk of missing their targets from a collection.
        /// </summary>
        private List<DataModels.Goal> GetAtRiskGoalsFromCollection(IEnumerable<DataModels.Goal> goals)
        {
            try
            {
                if (goals == null) return new List<DataModels.Goal>();

                var today = DateTime.Now;
                return goals.Where(g => 
                {
                    // Goal is at risk if progress is significantly behind where it should be
                    if (g.EndDate < today) return false;
                    
                    var totalDays = (g.EndDate - g.StartDate).TotalDays;
                    var elapsedDays = (today - g.StartDate).TotalDays;
                    if (totalDays <= 0) return false;
                    
                    var expectedProgress = (elapsedDays / totalDays) * 100;
                    return (double)g.ProgressPercent < (expectedProgress - 15); // More than 15% behind expected
                }).ToList();
            }
            catch
            {
                return new List<DataModels.Goal>();
            }
        }

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
