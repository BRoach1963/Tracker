using System.Collections.ObjectModel;
using System.ComponentModel;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Analytics;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel providing predictive analytics data for UI binding.
    /// Can be used standalone or mixed into other ViewModels.
    /// 
    /// Usage:
    /// <code>
    /// // In a view or control
    /// &lt;controls:TrajectoryChartControl 
    ///     TrajectoryPoints="{Binding Analytics.TrajectoryPoints}"
    ///     RiskLevel="{Binding Analytics.RiskLevel}"/&gt;
    /// </code>
    /// </summary>
    public class PredictiveAnalyticsViewModel : BaseViewModel
    {
        #region Fields

        private readonly ILogger _logger;
        
        private PredictiveAnalyticsService.PredictionResult? _prediction;
        private ObservableCollection<TrajectoryPredictor.TrajectoryPoint>? _trajectoryPoints;
        private TrendAnalyzer.TrendDirection _trendDirection = TrendAnalyzer.TrendDirection.Stable;
        private DataSufficiencyChecker.ConfidenceLevel _confidenceLevel = DataSufficiencyChecker.ConfidenceLevel.Insufficient;
        private TrajectoryPredictor.RiskLevel _riskLevel = TrajectoryPredictor.RiskLevel.OnTrack;
        private DateTime? _predictedCompletionDate;
        private double _confidenceScore;
        private bool _hasSufficientData;
        private bool _isLoading;
        private string? _entityType;
        private int? _entityId;
        private string? _entityName;

        #endregion

        #region Constructor

        public PredictiveAnalyticsViewModel()
        {
            _logger = LoggingManager.GetComponentLogger("PredictiveAnalyticsVM");
        }

        #endregion

        #region Properties

        /// <summary>
        /// The full prediction result.
        /// </summary>
        public PredictiveAnalyticsService.PredictionResult? Prediction
        {
            get => _prediction;
            private set
            {
                _prediction = value;
                RaisePropertyChanged();
                UpdateDerivedProperties();
            }
        }

        /// <summary>
        /// Trajectory points for chart visualization.
        /// </summary>
        public ObservableCollection<TrajectoryPredictor.TrajectoryPoint>? TrajectoryPoints
        {
            get => _trajectoryPoints;
            private set
            {
                _trajectoryPoints = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Current trend direction.
        /// </summary>
        public TrendAnalyzer.TrendDirection TrendDirection
        {
            get => _trendDirection;
            private set
            {
                _trendDirection = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Data confidence level.
        /// </summary>
        public DataSufficiencyChecker.ConfidenceLevel ConfidenceLevel
        {
            get => _confidenceLevel;
            private set
            {
                _confidenceLevel = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Risk level for the trajectory.
        /// </summary>
        public TrajectoryPredictor.RiskLevel RiskLevel
        {
            get => _riskLevel;
            private set
            {
                _riskLevel = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Predicted completion date.
        /// </summary>
        public DateTime? PredictedCompletionDate
        {
            get => _predictedCompletionDate;
            private set
            {
                _predictedCompletionDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasPredictedDate));
            }
        }

        /// <summary>
        /// Confidence score (0-100).
        /// </summary>
        public double ConfidenceScore
        {
            get => _confidenceScore;
            private set
            {
                _confidenceScore = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether there's enough data for meaningful predictions.
        /// </summary>
        public bool HasSufficientData
        {
            get => _hasSufficientData;
            private set
            {
                _hasSufficientData = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether data is currently being loaded.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether a predicted date is available.
        /// </summary>
        public bool HasPredictedDate => PredictedCompletionDate.HasValue;

        /// <summary>
        /// Display-friendly trend text.
        /// </summary>
        public string TrendDisplayText => TrendDirection switch
        {
            TrendAnalyzer.TrendDirection.Improving => "Improving",
            TrendAnalyzer.TrendDirection.Stable => "Stable",
            TrendAnalyzer.TrendDirection.Declining => "Declining",
            TrendAnalyzer.TrendDirection.Insufficient => "Insufficient Data",
            _ => "Unknown"
        };

        /// <summary>
        /// Display-friendly risk text.
        /// </summary>
        public string RiskDisplayText => RiskLevel switch
        {
            TrajectoryPredictor.RiskLevel.OnTrack => "On Track",
            TrajectoryPredictor.RiskLevel.AtRisk => "At Risk",
            TrajectoryPredictor.RiskLevel.Critical => "Critical",
            _ => "Unknown"
        };

        /// <summary>
        /// Gets sparkline data points (progress values only).
        /// </summary>
        public List<double>? SparklineData => TrajectoryPoints?
            .Where(p => p.IsHistorical)
            .OrderBy(p => p.Date)
            .Select(p => p.ProjectedProgress)
            .ToList();

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads prediction data for an OKR.
        /// </summary>
        public async Task LoadForOkrAsync(int okrId, string name, DateTime? startDate = null, DateTime? targetDate = null)
        {
            await LoadPredictionAsync(SnapshotEntityType.OKR, okrId, name, startDate, targetDate);
        }

        /// <summary>
        /// Loads prediction data for a Key Result.
        /// </summary>
        public async Task LoadForKeyResultAsync(int keyResultId, string name, DateTime? startDate = null, DateTime? targetDate = null)
        {
            await LoadPredictionAsync(SnapshotEntityType.KeyResult, keyResultId, name, startDate, targetDate);
        }

        /// <summary>
        /// Loads prediction data for a KPI.
        /// </summary>
        public async Task LoadForKpiAsync(int kpiId, string name, DateTime? startDate = null, DateTime? targetDate = null)
        {
            await LoadPredictionAsync(SnapshotEntityType.KPI, kpiId, name, startDate, targetDate);
        }

        /// <summary>
        /// Loads prediction data for a Goal.
        /// </summary>
        public async Task LoadForGoalAsync(int goalId, string name, DateTime? startDate = null, DateTime? targetDate = null)
        {
            await LoadPredictionAsync("Goal", goalId, name, startDate, targetDate);
        }

        /// <summary>
        /// Loads prediction data for a Project.
        /// </summary>
        public async Task LoadForProjectAsync(int projectId, string name, DateTime? startDate = null, DateTime? targetDate = null)
        {
            await LoadPredictionAsync(SnapshotEntityType.Project, projectId, name, startDate, targetDate);
        }

        /// <summary>
        /// Clears all prediction data.
        /// </summary>
        public void Clear()
        {
            Prediction = null;
            TrajectoryPoints = null;
            TrendDirection = TrendAnalyzer.TrendDirection.Stable;
            ConfidenceLevel = DataSufficiencyChecker.ConfidenceLevel.Insufficient;
            RiskLevel = TrajectoryPredictor.RiskLevel.OnTrack;
            PredictedCompletionDate = null;
            ConfidenceScore = 0;
            HasSufficientData = false;
            _entityType = null;
            _entityId = null;
            _entityName = null;
        }

        /// <summary>
        /// Refreshes the current prediction.
        /// </summary>
        public async Task RefreshAsync()
        {
            if (_entityType != null && _entityId.HasValue && _entityName != null)
            {
                await LoadPredictionAsync(_entityType, _entityId.Value, _entityName, null, null);
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadPredictionAsync(string entityType, int entityId, string entityName, DateTime? startDate, DateTime? targetDate)
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                _entityType = entityType;
                _entityId = entityId;
                _entityName = entityName;

                _logger.Debug("Loading prediction for {0}:{1}", entityType, entityId);

                PredictiveAnalyticsService.PredictionResult prediction;
                var service = PredictiveAnalyticsService.Instance;

                switch (entityType)
                {
                    case SnapshotEntityType.OKR:
                        prediction = await service.AnalyzeOkrAsync(entityId, entityName, 
                            startDate ?? DateTime.Today.AddMonths(-3), 
                            targetDate ?? DateTime.Today.AddMonths(3));
                        break;
                    case SnapshotEntityType.KeyResult:
                        prediction = await service.AnalyzeKeyResultAsync(entityId, entityName,
                            startDate ?? DateTime.Today.AddMonths(-3),
                            targetDate ?? DateTime.Today.AddMonths(3), 100);
                        break;
                    case SnapshotEntityType.KPI:
                        prediction = await service.AnalyzeKpiAsync(entityId, entityName, targetDate, 100);
                        break;
                    case SnapshotEntityType.Project:
                        prediction = await service.AnalyzeProjectAsync(entityId, entityName,
                            startDate ?? DateTime.Today.AddMonths(-3),
                            targetDate ?? DateTime.Today.AddMonths(3));
                        break;
                    default:
                        _logger.Warn("Unknown entity type: {0}", entityType);
                        Clear();
                        return;
                }

                Prediction = prediction;
                _logger.Debug("Prediction loaded: IsValid={0}", prediction.IsValid);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error loading prediction for {0}:{1}", entityType, entityId);
                Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateDerivedProperties()
        {
            if (Prediction == null || !Prediction.IsValid)
            {
                Clear();
                return;
            }

            // Update trajectory points
            TrajectoryPoints = Prediction.TrajectoryPoints.Count > 0
                ? new ObservableCollection<TrajectoryPredictor.TrajectoryPoint>(Prediction.TrajectoryPoints)
                : null;

            // Update trend
            TrendDirection = Prediction.Trend?.Direction ?? TrendAnalyzer.TrendDirection.Stable;

            // Update confidence
            ConfidenceLevel = Prediction.DataSufficiency?.Confidence ?? DataSufficiencyChecker.ConfidenceLevel.Insufficient;
            ConfidenceScore = Prediction.DataSufficiency?.ConfidenceScore ?? 0;
            HasSufficientData = Prediction.DataSufficiency?.IsSufficient ?? false;

            // Update trajectory/risk
            RiskLevel = Prediction.Trajectory?.Risk ?? TrajectoryPredictor.RiskLevel.OnTrack;
            PredictedCompletionDate = Prediction.Trajectory?.PredictedCompletionDate;

            // Notify sparkline data changed
            RaisePropertyChanged(nameof(SparklineData));
            RaisePropertyChanged(nameof(TrendDisplayText));
            RaisePropertyChanged(nameof(RiskDisplayText));
        }

        #endregion
    }
}
