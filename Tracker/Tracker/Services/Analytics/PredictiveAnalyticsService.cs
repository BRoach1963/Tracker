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
            public string EntityType { get; init; } = string.Empty;

            /// <summary>Entity ID.</summary>
            public int EntityId { get; init; }

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
            public static PredictionResult Invalid(string entityType, int entityId, string entityName, string reason) => new()
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
        /// Analyzes an OKR and generates predictions.
        /// </summary>
        public async Task<PredictionResult> AnalyzeOkrAsync(int objectiveId, string objectiveName, DateTime startDate, DateTime endDate)
        {
            try
            {
                var snapshots = await ProgressSnapshotService.Instance.GetHistoryAsync(
                    SnapshotEntityType.OKR, objectiveId);

                return AnalyzeEntity(
                    SnapshotEntityType.OKR,
                    objectiveId,
                    objectiveName,
                    snapshots,
                    startDate,
                    endDate);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing OKR {0}: {1}", objectiveId, ex.Message);
                return PredictionResult.Invalid(SnapshotEntityType.OKR, objectiveId, objectiveName, "Error analyzing OKR");
            }
        }

        /// <summary>
        /// Analyzes a Key Result and generates predictions.
        /// </summary>
        public async Task<PredictionResult> AnalyzeKeyResultAsync(
            int keyResultId, 
            string keyResultName, 
            DateTime startDate, 
            DateTime endDate,
            decimal targetValue)
        {
            try
            {
                var snapshots = await ProgressSnapshotService.Instance.GetHistoryAsync(
                    SnapshotEntityType.KeyResult, keyResultId);

                return AnalyzeEntity(
                    SnapshotEntityType.KeyResult,
                    keyResultId,
                    keyResultName,
                    snapshots,
                    startDate,
                    endDate,
                    (double)targetValue);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing Key Result {0}: {1}", keyResultId, ex.Message);
                return PredictionResult.Invalid(SnapshotEntityType.KeyResult, keyResultId, keyResultName, "Error analyzing Key Result");
            }
        }

        /// <summary>
        /// Analyzes a KPI and generates predictions.
        /// </summary>
        public async Task<PredictionResult> AnalyzeKpiAsync(
            int kpiId, 
            string kpiName, 
            DateTime? targetDate,
            double targetValue)
        {
            try
            {
                var snapshots = await ProgressSnapshotService.Instance.GetHistoryAsync(
                    SnapshotEntityType.KPI, kpiId);

                // KPIs may not have a start date, use first snapshot or 90 days ago
                var startDate = snapshots.FirstOrDefault()?.SnapshotDate ?? DateTime.Today.AddDays(-90);

                return AnalyzeEntity(
                    SnapshotEntityType.KPI,
                    kpiId,
                    kpiName,
                    snapshots,
                    startDate,
                    targetDate,
                    targetValue);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing KPI {0}: {1}", kpiId, ex.Message);
                return PredictionResult.Invalid(SnapshotEntityType.KPI, kpiId, kpiName, "Error analyzing KPI");
            }
        }

        /// <summary>
        /// Analyzes a Project and generates predictions.
        /// </summary>
        public async Task<PredictionResult> AnalyzeProjectAsync(
            int projectId, 
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
        /// Analyzes all OKRs and returns a summary.
        /// </summary>
        public async Task<PredictionSummary> AnalyzeAllOkrsAsync()
        {
            var results = new List<PredictionResult>();

            try
            {
                var okrs = await TrackerDataManager.Instance.GetOKRs();
                if (okrs == null) return CreateEmptySummary();

                foreach (var okr in okrs.Where(o => o.EndDate >= DateTime.Today))
                {
                    var result = await AnalyzeOkrAsync(
                        okr.ObjectiveId, 
                        okr.Title, 
                        okr.StartDate, 
                        okr.EndDate);

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing all OKRs: {0}", ex.Message);
            }

            return CreateSummary(results);
        }

        /// <summary>
        /// Analyzes all KPIs and returns a summary.
        /// </summary>
        public async Task<PredictionSummary> AnalyzeAllKpisAsync()
        {
            var results = new List<PredictionResult>();

            try
            {
                var kpis = await TrackerDataManager.Instance.GetKPIs();
                if (kpis == null) return CreateEmptySummary();

                foreach (var kpi in kpis.Where(k => k.TargetValue > 0))
                {
                    var result = await AnalyzeKpiAsync(
                        kpi.KpiId, 
                        kpi.Name, 
                        null, // KPIs typically don't have target dates
                        kpi.TargetValue);

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing all KPIs: {0}", ex.Message);
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
                // Analyze OKRs
                var okrs = await TrackerDataManager.Instance.GetOKRs();
                if (okrs != null)
                {
                    foreach (var okr in okrs.Where(o => o.EndDate >= DateTime.Today && o.CompletionPercentage < 100))
                    {
                        var result = await AnalyzeOkrAsync(okr.ObjectiveId, okr.Title, okr.StartDate, okr.EndDate);
                        allResults.Add(result);
                    }
                }

                // Analyze Projects
                var projects = await TrackerDataManager.Instance.GetProjects();
                if (projects != null)
                {
                    foreach (var project in projects.Where(p => p.EndDate >= DateTime.Today && p.Progress < 100))
                    {
                        var result = await AnalyzeProjectAsync(project.ID, project.Name, project.StartDate, project.EndDate);
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
            string entityType, 
            int entityId)
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
        public async Task<bool> HasSufficientDataAsync(string entityType, int entityId)
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
            string entityType,
            int entityId,
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
