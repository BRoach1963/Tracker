using Tracker.DataModels;
using Tracker.Services.Analytics;
using Xunit;

namespace Tracker.Tests.Services.Analytics
{
    public class DataSufficiencyCheckerTests
    {
        private readonly DataSufficiencyChecker _checker;

        public DataSufficiencyCheckerTests()
        {
            _checker = new DataSufficiencyChecker();
        }

        #region Insufficient Data Tests

        [Fact]
        public void Evaluate_WithNoData_ReturnsInsufficient()
        {
            // Act
            var result = _checker.Evaluate(new List<ProgressSnapshot>());

            // Assert
            Assert.Equal(DataSufficiencyChecker.ConfidenceLevel.Insufficient, result.Confidence);
            Assert.False(result.IsSufficient);
            Assert.Equal(0, result.DataPointCount);
        }

        [Fact]
        public void Evaluate_WithTwoPoints_ReturnsInsufficient()
        {
            // Arrange
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-1), 10),
                CreateSnapshot(DateTime.Today, 20)
            };

            // Act
            var result = _checker.Evaluate(snapshots);

            // Assert
            Assert.Equal(DataSufficiencyChecker.ConfidenceLevel.Insufficient, result.Confidence);
            Assert.False(result.IsSufficient);
        }

        #endregion

        #region Low Confidence Tests

        [Fact]
        public void Evaluate_WithMinimumData_ReturnsSufficient()
        {
            // Arrange - exactly 3 points (minimum)
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-2), 10),
                CreateSnapshot(DateTime.Today.AddDays(-1), 20),
                CreateSnapshot(DateTime.Today, 30)
            };

            // Act
            var result = _checker.Evaluate(snapshots);

            // Assert
            Assert.True(result.IsSufficient);
            Assert.Equal(3, result.DataPointCount);
        }

        [Fact]
        public void Evaluate_WithFewDataPoints_ReturnsLowConfidence()
        {
            // Arrange - just barely minimum
            var snapshots = CreateSnapshots(4, 1); // 4 days of data

            // Act
            var result = _checker.Evaluate(snapshots);

            // Assert
            Assert.True(result.Confidence == DataSufficiencyChecker.ConfidenceLevel.Low ||
                        result.Confidence == DataSufficiencyChecker.ConfidenceLevel.VeryLow);
        }

        #endregion

        #region Medium Confidence Tests

        [Fact]
        public void Evaluate_WithWeekOfData_ReturnsAtLeastLowConfidence()
        {
            // Arrange - 7 days of data
            var snapshots = CreateSnapshots(7, 5); // 7 days, +5% each day
            var trendAnalyzer = new TrendAnalyzer();
            var trendResult = trendAnalyzer.Analyze(snapshots);

            // Act
            var result = _checker.Evaluate(snapshots, trendResult);

            // Assert
            Assert.True(result.Confidence >= DataSufficiencyChecker.ConfidenceLevel.Low);
            Assert.True(result.ConfidenceScore > 30);
        }

        #endregion

        #region High Confidence Tests

        [Fact]
        public void Evaluate_WithThreeWeeksOfData_ReturnsHigherConfidence()
        {
            // Arrange - 21 days of consistent data
            var snapshots = CreateSnapshots(21, 3); // 21 days, +3% each day
            var trendAnalyzer = new TrendAnalyzer();
            var trendResult = trendAnalyzer.Analyze(snapshots);

            // Act
            var result = _checker.Evaluate(snapshots, trendResult);

            // Assert
            Assert.True(result.Confidence <= DataSufficiencyChecker.ConfidenceLevel.Medium,
                $"Expected Medium or better confidence, got {result.Confidence}");
            Assert.True(result.ConfidenceScore >= 50);
        }

        #endregion

        #region Data Quality Tests

        [Fact]
        public void Evaluate_WithConsistentData_HasHigherQualityScore()
        {
            // Arrange - perfectly consistent daily updates
            var snapshots = CreateSnapshots(10, 5);

            // Act
            var result = _checker.Evaluate(snapshots);

            // Assert
            Assert.True(result.DataQualityScore > 50, 
                $"Consistent data should have quality > 50, got {result.DataQualityScore}");
        }

        [Fact]
        public void Evaluate_WithGaps_HasLowerQualityScore()
        {
            // Arrange - gaps in data
            var snapshots = new List<ProgressSnapshot>
            {
                CreateSnapshot(DateTime.Today.AddDays(-20), 10),
                CreateSnapshot(DateTime.Today.AddDays(-10), 30), // 10-day gap
                CreateSnapshot(DateTime.Today.AddDays(-5), 40),
                CreateSnapshot(DateTime.Today, 60) // 5-day gap
            };

            // Act
            var result = _checker.Evaluate(snapshots);

            // Assert
            Assert.True(result.DataQualityScore < 80, 
                $"Data with gaps should have lower quality score, got {result.DataQualityScore}");
        }

        #endregion

        #region R-Squared Impact Tests

        [Fact]
        public void Evaluate_WithHighRSquared_IncreasesConfidence()
        {
            // Arrange
            var snapshots = CreateSnapshots(7, 5);
            var trendWithHighR = new TrendAnalyzer.TrendResult
            {
                Direction = TrendAnalyzer.TrendDirection.Improving,
                Slope = 5,
                RSquared = 0.95, // Very high
                DataPointCount = 7
            };
            var trendWithLowR = new TrendAnalyzer.TrendResult
            {
                Direction = TrendAnalyzer.TrendDirection.Improving,
                Slope = 5,
                RSquared = 0.3, // Low
                DataPointCount = 7
            };

            // Act
            var resultHighR = _checker.Evaluate(snapshots, trendWithHighR);
            var resultLowR = _checker.Evaluate(snapshots, trendWithLowR);

            // Assert
            Assert.True(resultHighR.ConfidenceScore > resultLowR.ConfidenceScore,
                $"High R² ({resultHighR.ConfidenceScore}) should give higher confidence than low R² ({resultLowR.ConfidenceScore})");
        }

        #endregion

        #region Suggestions Tests

        [Fact]
        public void Evaluate_WithLowData_ProvidesSuggestions()
        {
            // Arrange
            var snapshots = CreateSnapshots(5, 5);

            // Act
            var result = _checker.Evaluate(snapshots);

            // Assert
            Assert.NotEmpty(result.Suggestions);
            Assert.Contains(result.Suggestions, s => s.Contains("days") || s.Contains("data"));
        }

        [Fact]
        public void Evaluate_WithSufficientData_HasPositiveSuggestion()
        {
            // Arrange - plenty of good data
            var snapshots = CreateSnapshots(21, 3);
            var trendAnalyzer = new TrendAnalyzer();
            var trend = trendAnalyzer.Analyze(snapshots);

            // Act
            var result = _checker.Evaluate(snapshots, trend);

            // Assert
            Assert.NotEmpty(result.Suggestions);
        }

        #endregion

        #region HasMinimumData Tests

        [Fact]
        public void HasMinimumData_WithThreePoints_ReturnsTrue()
        {
            // Arrange
            var snapshots = CreateSnapshots(3, 10);

            // Act & Assert
            Assert.True(_checker.HasMinimumData(snapshots));
        }

        [Fact]
        public void HasMinimumData_WithTwoPoints_ReturnsFalse()
        {
            // Arrange
            var snapshots = CreateSnapshots(2, 10);

            // Act & Assert
            Assert.False(_checker.HasMinimumData(snapshots));
        }

        #endregion

        #region Display Helper Tests

        [Fact]
        public void GetConfidenceDisplayText_ReturnsReadableText()
        {
            // Assert all levels have text
            Assert.Equal("High Confidence", DataSufficiencyChecker.GetConfidenceDisplayText(DataSufficiencyChecker.ConfidenceLevel.High));
            Assert.Equal("Medium Confidence", DataSufficiencyChecker.GetConfidenceDisplayText(DataSufficiencyChecker.ConfidenceLevel.Medium));
            Assert.Equal("Low Confidence", DataSufficiencyChecker.GetConfidenceDisplayText(DataSufficiencyChecker.ConfidenceLevel.Low));
            Assert.Equal("Very Low Confidence", DataSufficiencyChecker.GetConfidenceDisplayText(DataSufficiencyChecker.ConfidenceLevel.VeryLow));
            Assert.Equal("Insufficient Data", DataSufficiencyChecker.GetConfidenceDisplayText(DataSufficiencyChecker.ConfidenceLevel.Insufficient));
        }

        [Fact]
        public void GetConfidenceColorHint_ReturnsColors()
        {
            // Assert
            Assert.Equal("Green", DataSufficiencyChecker.GetConfidenceColorHint(DataSufficiencyChecker.ConfidenceLevel.High));
            Assert.Equal("Orange", DataSufficiencyChecker.GetConfidenceColorHint(DataSufficiencyChecker.ConfidenceLevel.Medium));
            Assert.Equal("Red", DataSufficiencyChecker.GetConfidenceColorHint(DataSufficiencyChecker.ConfidenceLevel.Low));
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

        private static List<ProgressSnapshot> CreateSnapshots(int days, decimal dailyIncrease)
        {
            var snapshots = new List<ProgressSnapshot>();
            for (int i = 0; i < days; i++)
            {
                snapshots.Add(CreateSnapshot(
                    DateTime.Today.AddDays(-(days - 1) + i),
                    10 + (dailyIncrease * i)));
            }
            return snapshots;
        }

        #endregion
    }
}
