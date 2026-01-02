using Tracker.DataModels;
using Tracker.Services.Analytics;
using Xunit;

namespace Tracker.Tests.Services.Analytics
{
    public class TrajectoryPredictorTests
    {
        private readonly TrajectoryPredictor _predictor;

        public TrajectoryPredictorTests()
        {
            _predictor = new TrajectoryPredictor();
        }

        #region Insufficient Data Tests

        [Fact]
        public void Predict_WithNoSnapshots_ReturnsInvalid()
        {
            // Arrange
            var snapshots = new List<ProgressSnapshot>();

            // Act
            var result = _predictor.Predict(
                snapshots,
                DateTime.Today.AddDays(-30),
                DateTime.Today.AddDays(30));

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Predict_WithOneSnapshot_ReturnsInvalid()
        {
            // Arrange
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today, 50)
            };

            // Act
            var result = _predictor.Predict(
                snapshots,
                DateTime.Today.AddDays(-30),
                DateTime.Today.AddDays(30));

            // Assert
            Assert.False(result.IsValid);
        }

        #endregion

        #region On Track Tests

        [Fact]
        public void Predict_WhenOnSchedule_ReturnsOnTrack()
        {
            // Arrange - 50% progress at midpoint
            var startDate = DateTime.Today.AddDays(-30);
            var targetDate = DateTime.Today.AddDays(30);
            var snapshots = CreateProgressingSnapshots(0, 10, 5); // 0% to 40% over 5 days

            // Act - simulate being at 50% at midpoint
            var midpointSnapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-4), 40),
                CreateSnapshot(DateTime.Today.AddDays(-3), 43),
                CreateSnapshot(DateTime.Today.AddDays(-2), 46),
                CreateSnapshot(DateTime.Today.AddDays(-1), 48),
                CreateSnapshot(DateTime.Today, 50)
            };

            var result = _predictor.Predict(midpointSnapshots, startDate, targetDate);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(TrajectoryPredictor.RiskLevel.OnTrack, result.Risk);
        }

        [Fact]
        public void Predict_AheadOfSchedule_ShowsPositiveGap()
        {
            // Arrange - more progress than expected
            var startDate = DateTime.Today.AddDays(-30);
            var targetDate = DateTime.Today.AddDays(30);
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-4), 60),
                CreateSnapshot(DateTime.Today.AddDays(-3), 63),
                CreateSnapshot(DateTime.Today.AddDays(-2), 66),
                CreateSnapshot(DateTime.Today.AddDays(-1), 69),
                CreateSnapshot(DateTime.Today, 72) // 72% when expected ~50%
            };

            // Act
            var result = _predictor.Predict(snapshots, startDate, targetDate);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.ProgressGap > 0, "Progress gap should be positive when ahead");
            Assert.True(result.IsOnTrack);
        }

        #endregion

        #region At Risk Tests

        [Fact]
        public void Predict_SlightlyBehind_ReturnsAtRisk()
        {
            // Arrange - slightly behind but with good enough trend to still complete close to target
            // At day 30 of 60 day project (midpoint), expected is 50%
            // Current is 40% (-10% gap), but making good progress (+2%/day)
            // With 60% remaining and 30 days left, need 2%/day - which matches trend
            // So predicted completion is ~on time, but gap still triggers AtRisk
            var startDate = DateTime.Today.AddDays(-30);
            var targetDate = DateTime.Today.AddDays(30);
            
            // Good trend that will catch up - 2% per day
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-4), 32),
                CreateSnapshot(DateTime.Today.AddDays(-3), 34),
                CreateSnapshot(DateTime.Today.AddDays(-2), 36),
                CreateSnapshot(DateTime.Today.AddDays(-1), 38),
                CreateSnapshot(DateTime.Today, 40) // 40% when expected ~50% (-10 gap)
            };

            // Act
            var result = _predictor.Predict(snapshots, startDate, targetDate);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.ProgressGap < 0, "Progress gap should be negative when behind");
            // With -10 gap but good trend predicting close-to-on-time completion
            Assert.Equal(TrajectoryPredictor.RiskLevel.AtRisk, result.Risk);
        }

        #endregion

        #region Critical Tests

        [Fact]
        public void Predict_SignificantlyBehind_ReturnsCritical()
        {
            // Arrange - very far behind
            var startDate = DateTime.Today.AddDays(-30);
            var targetDate = DateTime.Today.AddDays(30);
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-4), 10),
                CreateSnapshot(DateTime.Today.AddDays(-3), 11),
                CreateSnapshot(DateTime.Today.AddDays(-2), 12),
                CreateSnapshot(DateTime.Today.AddDays(-1), 13),
                CreateSnapshot(DateTime.Today, 15) // 15% when expected ~50%
            };

            // Act
            var result = _predictor.Predict(snapshots, startDate, targetDate);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(TrajectoryPredictor.RiskLevel.Critical, result.Risk);
        }

        [Fact]
        public void Predict_WithDecliningTrend_ReturnsCritical()
        {
            // Arrange - going backwards
            var startDate = DateTime.Today.AddDays(-30);
            var targetDate = DateTime.Today.AddDays(30);
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-4), 40),
                CreateSnapshot(DateTime.Today.AddDays(-3), 38),
                CreateSnapshot(DateTime.Today.AddDays(-2), 36),
                CreateSnapshot(DateTime.Today.AddDays(-1), 34),
                CreateSnapshot(DateTime.Today, 32)
            };

            // Act
            var result = _predictor.Predict(snapshots, startDate, targetDate);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(TrajectoryPredictor.RiskLevel.Critical, result.Risk);
            Assert.Null(result.PredictedCompletionDate); // Can't complete with declining trend
        }

        #endregion

        #region Required Daily Progress Tests

        [Fact]
        public void CalculateRequiredDailyProgress_WithValidInputs_ReturnsCorrectValue()
        {
            // Arrange
            var currentProgress = 50.0;
            var targetDate = DateTime.Today.AddDays(10);

            // Act
            var required = _predictor.CalculateRequiredDailyProgress(currentProgress, targetDate);

            // Assert
            Assert.NotNull(required);
            Assert.Equal(5.0, required.Value, 1); // 50% remaining / 10 days = 5% per day
        }

        [Fact]
        public void CalculateRequiredDailyProgress_AlreadyComplete_ReturnsZero()
        {
            // Act
            var required = _predictor.CalculateRequiredDailyProgress(100, DateTime.Today.AddDays(10));

            // Assert
            Assert.NotNull(required);
            Assert.Equal(0, required.Value);
        }

        [Fact]
        public void CalculateRequiredDailyProgress_PastDueDate_ReturnsNull()
        {
            // Act
            var required = _predictor.CalculateRequiredDailyProgress(50, DateTime.Today.AddDays(-1));

            // Assert
            Assert.Null(required);
        }

        #endregion

        #region Trajectory Points Tests

        [Fact]
        public void GenerateTrajectoryPoints_IncludesHistoricalAndProjected()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-10);
            var targetDate = DateTime.Today.AddDays(20);
            var snapshots = CreateProgressingSnapshots(10, 5, 5);
            var trendAnalyzer = new TrendAnalyzer();
            var trend = trendAnalyzer.Analyze(snapshots);

            // Act
            var points = _predictor.GenerateTrajectoryPoints(
                snapshots, startDate, targetDate, trend, 10);

            // Assert
            Assert.NotEmpty(points);
            Assert.Contains(points, p => p.IsHistorical);
            Assert.Contains(points, p => !p.IsHistorical);
        }

        [Fact]
        public void GenerateTrajectoryPoints_ProjectedProgressIsCapped()
        {
            // Arrange - fast progress starting at 50%
            var startDate = DateTime.Today.AddDays(-10);
            var targetDate = DateTime.Today.AddDays(50);
            var snapshots = CreateProgressingSnapshots(50, 5, 5); // 50% to 70% over 5 days
            var trendAnalyzer = new TrendAnalyzer();
            var trend = trendAnalyzer.Analyze(snapshots);

            // Act
            var points = _predictor.GenerateTrajectoryPoints(
                snapshots, startDate, targetDate, trend, 30);

            // Assert - only projected (future) points should be capped
            var projectedPoints = points.Where(p => !p.IsHistorical).ToList();
            Assert.All(projectedPoints, p => Assert.True(p.ProjectedProgress <= 100, 
                $"Projected progress should be capped at 100, but was {p.ProjectedProgress}"));
        }

        #endregion

        #region Helper Methods

        private static ProgressSnapshot CreateSnapshot(DateTime date, decimal progress)
        {
            return new ProgressSnapshot
            {
                EntityType = SnapshotEntityType.OKR,
                EntityId = 1,
                SnapshotDate = date,
                Progress = progress,
                CurrentValue = progress,
                TargetValue = 100,
                UserId = 1
            };
        }

        private static List<ProgressSnapshot> CreateProgressingSnapshots(
            decimal startProgress, 
            decimal dailyIncrease, 
            int days)
        {
            var snapshots = new List<ProgressSnapshot>();
            for (int i = 0; i < days; i++)
            {
                snapshots.Add(CreateSnapshot(
                    DateTime.Today.AddDays(-(days - 1) + i),
                    startProgress + (dailyIncrease * i)));
            }
            return snapshots;
        }

        #endregion
    }
}
