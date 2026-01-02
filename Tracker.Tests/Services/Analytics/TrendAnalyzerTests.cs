using Tracker.DataModels;
using Tracker.Services.Analytics;
using Xunit;

namespace Tracker.Tests.Services.Analytics
{
    public class TrendAnalyzerTests
    {
        private readonly TrendAnalyzer _analyzer;

        public TrendAnalyzerTests()
        {
            _analyzer = new TrendAnalyzer();
        }

        #region Insufficient Data Tests

        [Fact]
        public void Analyze_WithNullSnapshots_ReturnsInsufficientData()
        {
            // Act
            var result = _analyzer.Analyze((IEnumerable<ProgressSnapshot>)null!);

            // Assert
            Assert.Equal(TrendAnalyzer.TrendDirection.Insufficient, result.Direction);
            Assert.Equal(0, result.DataPointCount);
        }

        [Fact]
        public void Analyze_WithEmptySnapshots_ReturnsInsufficientData()
        {
            // Act
            var result = _analyzer.Analyze(new List<ProgressSnapshot>());

            // Assert
            Assert.Equal(TrendAnalyzer.TrendDirection.Insufficient, result.Direction);
        }

        [Fact]
        public void Analyze_WithTwoDataPoints_ReturnsInsufficientData()
        {
            // Arrange
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-1), 10),
                CreateSnapshot(DateTime.Today, 20)
            };

            // Act
            var result = _analyzer.Analyze(snapshots);

            // Assert
            Assert.Equal(TrendAnalyzer.TrendDirection.Insufficient, result.Direction);
        }

        #endregion

        #region Improving Trend Tests

        [Fact]
        public void Analyze_WithIncreasingProgress_ReturnsImproving()
        {
            // Arrange - consistent daily increase of 5%
            var snapshots = CreateProgressingSnapshots(0, 5, 10); // 10 days, +5% each

            // Act
            var result = _analyzer.Analyze(snapshots);

            // Assert
            Assert.Equal(TrendAnalyzer.TrendDirection.Improving, result.Direction);
            Assert.True(result.Slope > 0);
            Assert.Equal(10, result.DataPointCount);
        }

        [Fact]
        public void Analyze_WithStrongIncrease_HasHighRSquared()
        {
            // Arrange - perfect linear increase
            var snapshots = CreateProgressingSnapshots(0, 10, 5);

            // Act
            var result = _analyzer.Analyze(snapshots);

            // Assert
            Assert.True(result.RSquared > 0.95, $"R² should be > 0.95 for perfect linear data, but was {result.RSquared}");
        }

        #endregion

        #region Declining Trend Tests

        [Fact]
        public void Analyze_WithDecreasingProgress_ReturnsDeclining()
        {
            // Arrange - decreasing progress (regression scenario)
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-4), 50),
                CreateSnapshot(DateTime.Today.AddDays(-3), 48),
                CreateSnapshot(DateTime.Today.AddDays(-2), 45),
                CreateSnapshot(DateTime.Today.AddDays(-1), 42),
                CreateSnapshot(DateTime.Today, 40)
            };

            // Act
            var result = _analyzer.Analyze(snapshots);

            // Assert
            Assert.Equal(TrendAnalyzer.TrendDirection.Declining, result.Direction);
            Assert.True(result.Slope < 0);
        }

        #endregion

        #region Stable Trend Tests

        [Fact]
        public void Analyze_WithStableProgress_ReturnsStable()
        {
            // Arrange - minimal change
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-4), 50),
                CreateSnapshot(DateTime.Today.AddDays(-3), 50.1m),
                CreateSnapshot(DateTime.Today.AddDays(-2), 49.9m),
                CreateSnapshot(DateTime.Today.AddDays(-1), 50.2m),
                CreateSnapshot(DateTime.Today, 50)
            };

            // Act
            var result = _analyzer.Analyze(snapshots);

            // Assert
            Assert.Equal(TrendAnalyzer.TrendDirection.Stable, result.Direction);
        }

        #endregion

        #region Projection Tests

        [Fact]
        public void ProjectCompletionDate_WithPositiveSlope_ReturnsValidDate()
        {
            // Arrange
            var snapshots = CreateProgressingSnapshots(0, 5, 10); // 0% to 45% over 10 days
            var result = _analyzer.Analyze(snapshots);
            var lastSnapshot = snapshots.Last();

            // Act
            var completionDate = _analyzer.ProjectCompletionDate(
                result,
                lastSnapshot.SnapshotDate,
                (double)lastSnapshot.Progress,
                100);

            // Assert
            Assert.NotNull(completionDate);
            Assert.True(completionDate > DateTime.Today);
        }

        [Fact]
        public void ProjectCompletionDate_WithZeroSlope_ReturnsNull()
        {
            // Arrange - stable, no progress
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-4), 50),
                CreateSnapshot(DateTime.Today.AddDays(-3), 50),
                CreateSnapshot(DateTime.Today.AddDays(-2), 50),
                CreateSnapshot(DateTime.Today.AddDays(-1), 50),
                CreateSnapshot(DateTime.Today, 50)
            };
            var result = _analyzer.Analyze(snapshots);

            // Act
            var completionDate = _analyzer.ProjectCompletionDate(
                result,
                DateTime.Today,
                50,
                100);

            // Assert
            Assert.Null(completionDate);
        }

        [Fact]
        public void ProjectCompletionDate_AlreadyComplete_ReturnsCurrentDate()
        {
            // Arrange
            var snapshots = CreateProgressingSnapshots(80, 5, 5); // 80% to 100%
            var result = _analyzer.Analyze(snapshots);

            // Act
            var completionDate = _analyzer.ProjectCompletionDate(
                result,
                DateTime.Today,
                100,
                100);

            // Assert
            Assert.NotNull(completionDate);
            Assert.Equal(DateTime.Today, completionDate.Value.Date);
        }

        #endregion

        #region Tuple Input Tests

        [Fact]
        public void Analyze_WithTupleInput_WorksCorrectly()
        {
            // Arrange
            var dataPoints = new List<(DateTime Date, double Progress)>
            {
                (DateTime.Today.AddDays(-4), 10),
                (DateTime.Today.AddDays(-3), 20),
                (DateTime.Today.AddDays(-2), 30),
                (DateTime.Today.AddDays(-1), 40),
                (DateTime.Today, 50)
            };

            // Act
            var result = _analyzer.Analyze(dataPoints);

            // Assert
            Assert.Equal(TrendAnalyzer.TrendDirection.Improving, result.Direction);
            Assert.Equal(5, result.DataPointCount);
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
