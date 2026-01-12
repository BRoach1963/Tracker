using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.Analytics
{
    /// <summary>
    /// Main orchestrator for predictive analytics, integrating trend analysis,
    /// trajectory prediction, and data sufficiency evaluation.
    /// </summary>
    public class PredictiveAnalyticsService
    {
        #region Singleton

        private static readonly Lazy<PredictiveAnalyticsService> _instance = 
            new(() => new PredictiveAnalyticsService());

        public static PredictiveAnalyticsService Instance => _instance.Value;

        #endregion

        #region Result Classes

        /// <summary>
        /// Complete prediction result for an entity.
        /// </summary>
        public class PredictionResult
        {
            /// <summary>Type of entity being analyzed.</summary>
            public SnapshotEntityType EntityType { get; init; } = SnapshotEntityType.Goal;

            /// <summary>Entity ID.</summary>
            public Guid EntityId { get; init; }

            /// <summary>Entity name for display.</summary>
            public string EntityName { get; init; } = string.Empty;

            /// <summary>Whether the prediction is valid.</summary>
            public bool IsValid { get; init; }

            /// <summary>Trend analysis result.</summary>
            public TrendAnalyzer.TrendResult? Trend { get; init; }

            /// <summary>Trajectory prediction result.</summary>
            public TrajectoryPredictor.TrajectoryResult? Trajectory { get; init; }

            /// <summary>Data sufficiency evaluation.</summary>
            public DataSufficiencyChecker.SufficiencyResult? DataSufficiency { get; init; }

            /// <summary>Trajectory points for charting.</summary>
            public List<TrajectoryPredictor.TrajectoryPoint> TrajectoryPoints { get; init; } = new();

            /// <summary>When this prediction was generated.</summary>
            public DateTime GeneratedAt { get; init; } = DateTime.Now;

            /// <summary>Overall summary combining all analyses.</summary>
            public string Summary { get; init; } = string.Empty;

            /// <summary>
            /// Creates an invalid result.
            /// </summary>
            public static PredictionResult Invalid(SnapshotEntityType entityType, Guid entityId, string entityName, string reason) => new()
            {
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                IsValid = false,
                Summary = reason
            };
        }

        /// <summary>
        /// Summary of predictions for dashboard display.
        /// </summary>
        public class PredictionSummary
        {
            /// <summary>Total entities analyzed.</summary>
            public int TotalEntities { get; init; }

            /// <summary>Entities on track.</summary>
            public int OnTrackCount { get; init; }

            /// <summary>Entities at risk.</summary>
            public int AtRiskCount { get; init; }

            /// <summary>Entities in critical state.</summary>
            public int CriticalCount { get; init; }

            /// <summary>Entities with insufficient data.</summary>
            public int InsufficientDataCount { get; init; }

            /// <summary>Entities improving.</summary>
            public int ImprovingCount { get; init; }

            /// <summary>Entities declining.</summary>
            public int DecliningCount { get; init; }

            /// <summary>Items predicted to complete late.</summary>
            public List<PredictionResult> LateCompletions { get; init; } = new();

            /// <summary>Items in critical state.</summary>
            public List<PredictionResult> CriticalItems { get; init; } = new();
        }

        #endregion

        #region Dependencies

        private readonly TrendAnalyzer _trendAnalyzer;
        private readonly TrajectoryPredictor _trajectoryPredictor;
        private readonly DataSufficiencyChecker _sufficiencyChecker;
        private readonly ILogger _logger;

        #endregion

        #region Constructor

        private PredictiveAnalyticsService()
        {
            _trendAnalyzer = new TrendAnalyzer();
            _trajectoryPredictor = new TrajectoryPredictor(_trendAnalyzer);
            _sufficiencyChecker = new DataSufficiencyChecker();
            _logger = LoggingManager.GetComponentLogger(nameof(PredictiveAnalyticsService));
        }

        /// <summary>
        /// Constructor for testing with injected dependencies.
        /// </summary>
        internal PredictiveAnalyticsService(
            TrendAnalyzer trendAnalyzer,
            TrajectoryPredictor trajectoryPredictor,
            DataSufficiencyChecker sufficiencyChecker)
        {
            _trendAnalyzer = trendAnalyzer;
            _trajectoryPredictor = trajectoryPredictor;
            _sufficiencyChecker = sufficiencyChecker;
            _logger = LoggingManager.GetComponentLogger(nameof(PredictiveAnalyticsService));
        }

        #endregion

        #region Public Methods - Single Entity Analysis

        /// <summary>
        /// Analyzes a Goal and generates predictions.
        /// </summary>
        public async Task<PredictionResult> AnalyzeGoalAsync(Guid goalId, string goalName, DateTime startDate, DateTime endDate)
        {
            try
            {
                var snapshots = await ProgressSnapshotService.Instance.GetHistoryAsync(
                    SnapshotEntityType.Goal, goalId);

                return AnalyzeEntity(
                    SnapshotEntityType.Goal,
                    goalId,
                    goalName,
                    snapshots,
                    startDate,
                    endDate);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing Goal {0}: {1}", goalId, ex.Message);
                return PredictionResult.Invalid(SnapshotEntityType.Goal, goalId, goalName, "Error analyzing Goal");
            }
        }

        /// <summary>
        /// Analyzes a Target and generates predictions.
        /// </summary>
        public async Task<PredictionResult> AnalyzeTargetAsync(
            Guid targetId, 
            string targetName, 
            DateTime startDate, 
            DateTime endDate,
            decimal targetValue)
        {
            try
            {
                var snapshots = await ProgressSnapshotService.Instance.GetHistoryAsync(
                    SnapshotEntityType.Target, targetId);

                return AnalyzeEntity(
                    SnapshotEntityType.Target,
                    targetId,
                    targetName,
                    snapshots,
                    startDate,
                    endDate,
                    (double)targetValue);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing Target {0}: {1}", targetId, ex.Message);
                return PredictionResult.Invalid(SnapshotEntityType.Target, targetId, targetName, "Error analyzing Target");
            }
        }

        /// <summary>
        /// Analyzes a Project and generates predictions.
        /// </summary>
        public async Task<PredictionResult> AnalyzeProjectAsync(
            Guid projectId, 
            string projectName, 
            DateTime startDate, 
            DateTime? dueDate)
        {
            try
            {
                var snapshots = await ProgressSnapshotService.Instance.GetHistoryAsync(
                    SnapshotEntityType.Project, projectId);

                return AnalyzeEntity(
                    SnapshotEntityType.Project,
                    projectId,
                    projectName,
                    snapshots,
                    startDate,
                    dueDate);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing Project {0}: {1}", projectId, ex.Message);
                return PredictionResult.Invalid(SnapshotEntityType.Project, projectId, projectName, "Error analyzing Project");
            }
        }

        #endregion

        #region Public Methods - Batch Analysis

        /// <summary>
        /// Analyzes all Goals and returns a summary.
        /// </summary>
        public async Task<PredictionSummary> AnalyzeAllGoalsAsync()
        {
            var results = new List<PredictionResult>();

            try
            {
                var goals = await TrackerDataManager.Instance.GetStrategicGoals();
                if (goals == null) return CreateEmptySummary();

                foreach (var goal in goals.Where(g => g.EndDate >= DateTime.Today))
                {
                    var result = await AnalyzeGoalAsync(
                        goal.Id, 
                        goal.Title, 
                        goal.StartDate, 
                        goal.EndDate);

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing all Goals: {0}", ex.Message);
            }

            return CreateSummary(results);
        }

        /// <summary>
        /// Gets overall prediction summary for dashboard.
        /// </summary>
        public async Task<PredictionSummary> GetOverallSummaryAsync()
        {
            var allResults = new List<PredictionResult>();

            try
            {
                // Analyze Goals
                var goals = await TrackerDataManager.Instance.GetStrategicGoals();
                if (goals != null)
                {
                    foreach (var goal in goals.Where(g => g.EndDate >= DateTime.Today && g.EffectiveProgress < 100))
                    {
                        var result = await AnalyzeGoalAsync(goal.Id, goal.Title, goal.StartDate, goal.EndDate);
                        allResults.Add(result);
                    }
                }

                // Analyze Projects
                var projects = await TrackerDataManager.Instance.GetProjects();
                if (projects != null)
                {
                    foreach (var project in projects.Where(p => p.TargetEndDate >= DateTime.Today && p.ProgressPercent < 100))
                    {
                        var result = await AnalyzeProjectAsync(project.Id, project.Name, project.StartDate ?? DateTime.Today, project.TargetEndDate);
                        allResults.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting overall summary: {0}", ex.Message);
            }

            return CreateSummary(allResults);
        }

        #endregion

        #region Public Methods - Quick Access

        /// <summary>
        /// Gets quick trend info for an entity (for inline display).
        /// </summary>
        public async Task<(TrendAnalyzer.TrendDirection Direction, string Description)?> GetQuickTrendAsync(
            SnapshotEntityType entityType, 
            Guid entityId)
        {
            try
            {
                var snapshots = await ProgressSnapshotService.Instance.GetHistoryAsync(entityType, entityId, 30);

                if (snapshots.Count < 3)
                    return null;

                var trend = _trendAnalyzer.Analyze(snapshots);

                if (trend.Direction == TrendAnalyzer.TrendDirection.Insufficient)
                    return null;

                return (trend.Direction, trend.Description);
            }
            catch (Exception ex)
            {
                _logger.Debug("Error getting quick trend for {0}/{1}: {2}", entityType, entityId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Checks if there's enough data for predictions.
        /// </summary>
        public async Task<bool> HasSufficientDataAsync(SnapshotEntityType entityType, Guid entityId)
        {
            var count = await ProgressSnapshotService.Instance.GetSnapshotCountAsync(entityType, entityId);
            return count >= _sufficiencyChecker.MinimumDataPoints;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Core analysis method for any entity type.
        /// </summary>
        private PredictionResult AnalyzeEntity(
            SnapshotEntityType entityType,
            Guid entityId,
            string entityName,
            List<ProgressSnapshot> snapshots,
            DateTime startDate,
            DateTime? targetDate,
            double targetValue = 100)
        {
            // Check for minimum data
            if (snapshots.Count < _sufficiencyChecker.MinimumDataPoints)
            {
                return new PredictionResult
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    EntityName = entityName,
                    IsValid = false,
                    DataSufficiency = _sufficiencyChecker.Evaluate(snapshots),
                    Summary = $"Need at least {_sufficiencyChecker.MinimumDataPoints} data points for predictions (currently {snapshots.Count})"
                };
            }

            // Analyze trend
            var trend = _trendAnalyzer.Analyze(snapshots);

            // Evaluate data sufficiency
            var sufficiency = _sufficiencyChecker.Evaluate(snapshots, trend);

            // Predict trajectory
            var trajectory = _trajectoryPredictor.Predict(snapshots, startDate, targetDate, targetValue);

            // Generate trajectory points for charting
            var trajectoryPoints = trajectory.IsValid
                ? _trajectoryPredictor.GenerateTrajectoryPoints(snapshots, startDate, targetDate, trend)
                : new List<TrajectoryPredictor.TrajectoryPoint>();

            // Generate overall summary
            var summary = GenerateOverallSummary(entityName, trend, trajectory, sufficiency);

            return new PredictionResult
            {
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                IsValid = true,
                Trend = trend,
                Trajectory = trajectory,
                DataSufficiency = sufficiency,
                TrajectoryPoints = trajectoryPoints,
                Summary = summary
            };
        }

        /// <summary>
        /// Generates a comprehensive summary from all analyses.
        /// </summary>
        private string GenerateOverallSummary(
            string entityName,
            TrendAnalyzer.TrendResult trend,
            TrajectoryPredictor.TrajectoryResult trajectory,
            DataSufficiencyChecker.SufficiencyResult sufficiency)
        {
            var parts = new List<string>();

            // Trend summary
            if (trend.Direction != TrendAnalyzer.TrendDirection.Insufficient)
            {
                parts.Add(trend.Description);
            }

            // Trajectory summary
            if (trajectory.IsValid)
            {
                parts.Add(trajectory.Summary);
            }

            // Confidence note
            if (sufficiency.Confidence <= DataSufficiencyChecker.ConfidenceLevel.Low)
            {
                parts.Add($"({sufficiency.Summary})");
            }

            return string.Join(". ", parts);
        }

        /// <summary>
        /// Creates a summary from a list of prediction results.
        /// </summary>
        private PredictionSummary CreateSummary(List<PredictionResult> results)
        {
            var validResults = results.Where(r => r.IsValid && r.Trajectory != null).ToList();

            return new PredictionSummary
            {
                TotalEntities = results.Count,
                OnTrackCount = validResults.Count(r => r.Trajectory!.Risk == TrajectoryPredictor.RiskLevel.OnTrack),
                AtRiskCount = validResults.Count(r => r.Trajectory!.Risk == TrajectoryPredictor.RiskLevel.AtRisk),
                CriticalCount = validResults.Count(r => r.Trajectory!.Risk == TrajectoryPredictor.RiskLevel.Critical),
                InsufficientDataCount = results.Count(r => !r.IsValid),
                ImprovingCount = validResults.Count(r => r.Trend?.Direction == TrendAnalyzer.TrendDirection.Improving),
                DecliningCount = validResults.Count(r => r.Trend?.Direction == TrendAnalyzer.TrendDirection.Declining),
                LateCompletions = validResults
                    .Where(r => r.Trajectory!.DaysFromTarget.HasValue && r.Trajectory.DaysFromTarget.Value < 0)
                    .OrderBy(r => r.Trajectory!.DaysFromTarget)
                    .Take(5)
                    .ToList(),
                CriticalItems = validResults
                    .Where(r => r.Trajectory!.Risk == TrajectoryPredictor.RiskLevel.Critical)
                    .ToList()
            };
        }

        /// <summary>
        /// Creates an empty summary for error cases.
        /// </summary>
        private PredictionSummary CreateEmptySummary()
        {
            return new PredictionSummary
            {
                TotalEntities = 0,
                OnTrackCount = 0,
                AtRiskCount = 0,
                CriticalCount = 0,
                InsufficientDataCount = 0,
                ImprovingCount = 0,
                DecliningCount = 0,
                LateCompletions = new List<PredictionResult>(),
                CriticalItems = new List<PredictionResult>()
            };
        }

        #endregion
    }
}
