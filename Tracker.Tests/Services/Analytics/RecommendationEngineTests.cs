using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.Services.Analytics;
using Xunit;

namespace Tracker.Tests.Services.Analytics
{
    public class RecommendationEngineTests
    {
        private readonly RecommendationEngine _engine = RecommendationEngine.Instance;

        #region Test Helpers

        private PredictiveAnalyticsService.PredictionResult CreateTestPrediction(
            TrajectoryPredictor.RiskLevel risk = TrajectoryPredictor.RiskLevel.AtRisk,
            TrendAnalyzer.TrendDirection trend = TrendAnalyzer.TrendDirection.Stable,
            int daysRemaining = 30,
            bool sufficientData = true)
        {
            var targetDate = DateTime.Today.AddDays(daysRemaining);

            return new PredictiveAnalyticsService.PredictionResult
            {
                IsValid = true,
                EntityType = "OKR",
                EntityId = 1,
                EntityName = "Test OKR",
                Trend = new TrendAnalyzer.TrendResult
                {
                    Direction = trend,
                    Slope = 1.5,
                    DataPointCount = 10
                },
                Trajectory = new TrajectoryPredictor.TrajectoryResult
                {
                    Risk = risk,
                    IsOnTrack = risk == TrajectoryPredictor.RiskLevel.OnTrack,
                    CurrentProgress = 50,
                    TargetDate = targetDate,
                    PredictedCompletionDate = risk == TrajectoryPredictor.RiskLevel.OnTrack 
                        ? DateTime.Today.AddDays(daysRemaining - 5) 
                        : DateTime.Today.AddDays(daysRemaining + 20)
                },
                TrajectoryPoints = new List<TrajectoryPredictor.TrajectoryPoint>
                {
                    new() { Date = DateTime.Today.AddDays(-10), ProjectedProgress = 30 },
                    new() { Date = DateTime.Today, ProjectedProgress = 50 }
                },
                DataSufficiency = new DataSufficiencyChecker.SufficiencyResult
                {
                    IsSufficient = sufficientData,
                    Confidence = sufficientData 
                        ? DataSufficiencyChecker.ConfidenceLevel.Medium 
                        : DataSufficiencyChecker.ConfidenceLevel.Insufficient,
                    ConfidenceScore = sufficientData ? 0.7 : 0.2,
                    DataPointCount = sufficientData ? 15 : 2,
                    Summary = sufficientData ? "Sufficient data" : "Need more data points"
                }
            };
        }

        #endregion

        #region GenerateRecommendations Tests

        [Fact]
        public void GenerateRecommendations_WithValidPrediction_ReturnsResult()
        {
            // Arrange
            var prediction = CreateTestPrediction();

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Should().NotBeNull();
            result.Summary.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateRecommendations_WithNullPrediction_ReturnsInsufficientDataResult()
        {
            // Act
            var result = _engine.GenerateRecommendations(null);

            // Assert
            result.Should().NotBeNull();
            result.Recommendations.Should().HaveCount(1);
            result.Recommendations[0].Type.Should().Be(RecommendationEngine.RecommendationType.DataQuality);
        }

        [Fact]
        public void GenerateRecommendations_WithCriticalRisk_ReturnsHighPriorityRecommendations()
        {
            // Arrange
            var prediction = CreateTestPrediction(risk: TrajectoryPredictor.RiskLevel.Critical);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.HasCriticalRecommendations.Should().BeTrue();
            result.Recommendations.Should().Contain(r => 
                r.Urgency == RecommendationEngine.Urgency.Critical);
        }

        [Fact]
        public void GenerateRecommendations_WithAtRisk_ReturnsActionRecommendations()
        {
            // Arrange
            var prediction = CreateTestPrediction(risk: TrajectoryPredictor.RiskLevel.AtRisk);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Recommendations.Should().Contain(r => 
                r.Urgency == RecommendationEngine.Urgency.High);
        }

        [Fact]
        public void GenerateRecommendations_WithOnTrack_ReturnsMaintainRecommendation()
        {
            // Arrange
            var prediction = CreateTestPrediction(risk: TrajectoryPredictor.RiskLevel.OnTrack);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Recommendations.Should().Contain(r => 
                r.Type == RecommendationEngine.RecommendationType.Celebration);
        }

        [Fact]
        public void GenerateRecommendations_WithDecliningTrend_IncludesDeclineWarning()
        {
            // Arrange
            var prediction = CreateTestPrediction(trend: TrendAnalyzer.TrendDirection.Declining);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Recommendations.Should().Contain(r => 
                r.Title.Contains("Declining", StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains("slower", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GenerateRecommendations_WithInsufficientData_IncludesDataQualityRecommendation()
        {
            // Arrange
            var prediction = CreateTestPrediction(sufficientData: false);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Recommendations.Should().Contain(r => 
                r.Type == RecommendationEngine.RecommendationType.DataQuality);
        }

        [Fact]
        public void GenerateRecommendations_WithFinalWeek_IncludesUrgentTimeRecommendation()
        {
            // Arrange
            var prediction = CreateTestPrediction(
                risk: TrajectoryPredictor.RiskLevel.AtRisk,
                daysRemaining: 5);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Recommendations.Should().Contain(r => 
                r.Urgency == RecommendationEngine.Urgency.Critical &&
                (r.Title.Contains("Week", StringComparison.OrdinalIgnoreCase) ||
                 r.Title.Contains("Decision", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void GenerateRecommendations_WithPastDeadline_IncludesRetrospectiveRecommendation()
        {
            // Arrange
            var prediction = CreateTestPrediction(daysRemaining: -5);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Recommendations.Should().Contain(r => 
                r.Title.Contains("Past", StringComparison.OrdinalIgnoreCase) ||
                r.Title.Contains("Passed", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Recommendation Sorting Tests

        [Fact]
        public void GenerateRecommendations_SortsByUrgencyFirst()
        {
            // Arrange
            var prediction = CreateTestPrediction(
                risk: TrajectoryPredictor.RiskLevel.Critical,
                trend: TrendAnalyzer.TrendDirection.Declining,
                sufficientData: false);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            if (result.Recommendations.Count > 1)
            {
                var urgencies = result.Recommendations.Select(r => r.Urgency).ToList();
                urgencies.Should().BeInAscendingOrder();
            }
        }

        [Fact]
        public void GenerateRecommendations_PrimaryRecommendationIsHighestPriority()
        {
            // Arrange
            var prediction = CreateTestPrediction(risk: TrajectoryPredictor.RiskLevel.Critical);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.PrimaryRecommendation.Should().NotBeNull();
            result.PrimaryRecommendation.Urgency.Should().Be(
                result.Recommendations.Min(r => r.Urgency));
        }

        #endregion

        #region Recommendation Content Tests

        [Fact]
        public void Recommendations_HaveActionSteps()
        {
            // Arrange
            var prediction = CreateTestPrediction(risk: TrajectoryPredictor.RiskLevel.Critical);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Recommendations.Should().AllSatisfy(r =>
            {
                r.ActionSteps.Should().NotBeNull();
                if (r.Type != RecommendationEngine.RecommendationType.Celebration)
                {
                    r.ActionSteps.Count.Should().BeGreaterThan(0);
                }
            });
        }

        [Fact]
        public void Recommendations_HaveIcons()
        {
            // Arrange
            var prediction = CreateTestPrediction();

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Recommendations.Should().AllSatisfy(r =>
            {
                r.Icon.Should().NotBeNullOrEmpty();
            });
        }

        [Fact]
        public void Recommendations_HaveUniqueIds()
        {
            // Arrange
            var prediction = CreateTestPrediction(
                risk: TrajectoryPredictor.RiskLevel.Critical,
                trend: TrendAnalyzer.TrendDirection.Declining);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            var ids = result.Recommendations.Select(r => r.Id).ToList();
            ids.Distinct().Count().Should().Be(ids.Count);
        }

        #endregion

        #region GetPrimaryRecommendation Tests

        [Fact]
        public void GetPrimaryRecommendation_ReturnsFirstRecommendation()
        {
            // Arrange
            var prediction = CreateTestPrediction();

            // Act
            var primary = _engine.GetPrimaryRecommendation(prediction);

            // Assert
            primary.Should().NotBeNull();
        }

        [Fact]
        public void GetPrimaryRecommendation_WithCritical_ReturnsCriticalRecommendation()
        {
            // Arrange
            var prediction = CreateTestPrediction(risk: TrajectoryPredictor.RiskLevel.Critical);

            // Act
            var primary = _engine.GetPrimaryRecommendation(prediction);

            // Assert
            primary.Urgency.Should().Be(RecommendationEngine.Urgency.Critical);
        }

        #endregion

        #region GetRecommendationsByType Tests

        [Fact]
        public void GetRecommendationsByType_FiltersCorrectly()
        {
            // Arrange
            var prediction = CreateTestPrediction(sufficientData: false);

            // Act
            var dataQuality = _engine.GetRecommendationsByType(
                prediction, 
                RecommendationEngine.RecommendationType.DataQuality);

            // Assert
            dataQuality.Should().NotBeEmpty();
            dataQuality.Should().AllSatisfy(r => 
                r.Type.Should().Be(RecommendationEngine.RecommendationType.DataQuality));
        }

        #endregion

        #region Summary Generation Tests

        [Fact]
        public void GenerateRecommendations_CriticalSummary_ContainsCriticalCount()
        {
            // Arrange
            var prediction = CreateTestPrediction(risk: TrajectoryPredictor.RiskLevel.Critical);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Summary.ToLower().Should().Contain("critical");
        }

        [Fact]
        public void GenerateRecommendations_OnTrackSummary_IsPositive()
        {
            // Arrange
            var prediction = CreateTestPrediction(risk: TrajectoryPredictor.RiskLevel.OnTrack);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.Summary.ToLower().Should().Contain("on track");
        }

        #endregion

        #region RecommendationCounts Tests

        [Fact]
        public void RecommendationCounts_GroupsByUrgency()
        {
            // Arrange
            var prediction = CreateTestPrediction(
                risk: TrajectoryPredictor.RiskLevel.Critical,
                trend: TrendAnalyzer.TrendDirection.Declining);

            // Act
            var result = _engine.GenerateRecommendations(prediction);

            // Assert
            result.RecommendationCounts.Should().NotBeEmpty();
            var totalCount = result.RecommendationCounts.Values.Sum();
            totalCount.Should().Be(result.Recommendations.Count);
        }

        #endregion
    }
}
