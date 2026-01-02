using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.Services.Analytics;
using Xunit;

namespace Tracker.Tests.Services.Analytics
{
    public class WhatIfSimulatorTests
    {
        private readonly WhatIfSimulator _simulator = WhatIfSimulator.Instance;

        #region Test Helpers

        private PredictiveAnalyticsService.PredictionResult CreateTestPrediction(
            double currentProgress = 50,
            int daysRemaining = 30,
            TrajectoryPredictor.RiskLevel risk = TrajectoryPredictor.RiskLevel.AtRisk)
        {
            var startDate = DateTime.Today.AddDays(-30);
            var targetDate = DateTime.Today.AddDays(daysRemaining);

            // Create trajectory points for charting
            var trajectoryPoints = new List<TrajectoryPredictor.TrajectoryPoint>();
            for (int i = 0; i <= 30; i++)
            {
                trajectoryPoints.Add(new TrajectoryPredictor.TrajectoryPoint
                {
                    Date = startDate.AddDays(i),
                    ProjectedProgress = (currentProgress / 30.0) * i,
                    ExpectedProgress = (100.0 / 60.0) * i,
                    IsHistorical = true
                });
            }

            return new PredictiveAnalyticsService.PredictionResult
            {
                IsValid = true,
                EntityType = "OKR",
                EntityId = 1,
                EntityName = "Test OKR",
                Trend = new TrendAnalyzer.TrendResult
                {
                    Direction = TrendAnalyzer.TrendDirection.Stable,
                    Slope = currentProgress / 30.0,
                    DataPointCount = 30
                },
                Trajectory = new TrajectoryPredictor.TrajectoryResult
                {
                    Risk = risk,
                    IsOnTrack = risk == TrajectoryPredictor.RiskLevel.OnTrack,
                    CurrentProgress = currentProgress,
                    TargetDate = targetDate,
                    PredictedCompletionDate = DateTime.Today.AddDays(daysRemaining * 2)
                },
                TrajectoryPoints = trajectoryPoints,
                DataSufficiency = new DataSufficiencyChecker.SufficiencyResult
                {
                    IsSufficient = true,
                    Confidence = DataSufficiencyChecker.ConfidenceLevel.Medium,
                    ConfidenceScore = 0.7,
                    DataPointCount = 30,
                    Summary = "Sufficient data"
                }
            };
        }

        #endregion

        #region GetPredefinedScenarios Tests

        [Fact]
        public void GetPredefinedScenarios_ReturnsMultipleScenarios()
        {
            // Act
            var scenarios = _simulator.GetPredefinedScenarios();

            // Assert
            scenarios.Should().NotBeEmpty();
            scenarios.Count.Should().BeGreaterThanOrEqualTo(5);
        }

        [Fact]
        public void GetPredefinedScenarios_IncludesVelocityIncrease()
        {
            // Act
            var scenarios = _simulator.GetPredefinedScenarios();

            // Assert
            scenarios.Should().Contain(s => s.VelocityMultiplier > 1.0);
        }

        [Fact]
        public void GetPredefinedScenarios_IncludesVelocityDecrease()
        {
            // Act
            var scenarios = _simulator.GetPredefinedScenarios();

            // Assert
            scenarios.Should().Contain(s => s.VelocityMultiplier < 1.0);
        }

        [Fact]
        public void GetPredefinedScenarios_AllHaveNamesAndDescriptions()
        {
            // Act
            var scenarios = _simulator.GetPredefinedScenarios();

            // Assert
            scenarios.Should().AllSatisfy(s =>
            {
                s.Name.Should().NotBeNullOrEmpty();
                s.Description.Should().NotBeNullOrEmpty();
            });
        }

        #endregion

        #region Simulate Tests

        [Fact]
        public void Simulate_WithValidPrediction_ReturnsResult()
        {
            // Arrange
            var prediction = CreateTestPrediction();
            var scenario = new WhatIfSimulator.WhatIfScenario
            {
                Name = "Test",
                VelocityMultiplier = 1.2
            };

            // Act
            var result = _simulator.Simulate(prediction, scenario);

            // Assert
            result.Should().NotBeNull();
            result.Scenario.Should().BeSameAs(scenario);
            result.Baseline.Should().NotBeNull();
            result.Outcome.Should().NotBeNull();
            result.Impact.Should().NotBeNull();
        }

        [Fact]
        public void Simulate_WithVelocityIncrease_ProjectsHigherProgress()
        {
            // Arrange
            var prediction = CreateTestPrediction(currentProgress: 50, daysRemaining: 30);
            var scenario = new WhatIfSimulator.WhatIfScenario
            {
                Name = "20% Increase",
                VelocityMultiplier = 1.2
            };

            // Act
            var result = _simulator.Simulate(prediction, scenario);

            // Assert
            result.Outcome.NewVelocity.Should().BeGreaterThan(result.Baseline.CurrentVelocity);
            result.Outcome.ProjectedFinalProgress.Should().BeGreaterThanOrEqualTo(result.Baseline.ProjectedFinalProgress);
        }

        [Fact]
        public void Simulate_WithVelocityDecrease_ProjectsLowerProgress()
        {
            // Arrange
            var prediction = CreateTestPrediction(currentProgress: 50, daysRemaining: 30);
            var scenario = new WhatIfSimulator.WhatIfScenario
            {
                Name = "20% Decrease",
                VelocityMultiplier = 0.8
            };

            // Act
            var result = _simulator.Simulate(prediction, scenario);

            // Assert
            result.Outcome.NewVelocity.Should().BeLessThan(result.Baseline.CurrentVelocity);
        }

        [Fact]
        public void Simulate_WithAdditionalDailyProgress_IncreasesVelocity()
        {
            // Arrange
            var prediction = CreateTestPrediction();
            var scenario = new WhatIfSimulator.WhatIfScenario
            {
                Name = "Add Resource",
                VelocityMultiplier = 1.0,
                AdditionalDailyProgress = 1.0
            };

            // Act
            var result = _simulator.Simulate(prediction, scenario);

            // Assert
            result.Outcome.NewVelocity.Should().BeGreaterThan(result.Baseline.CurrentVelocity);
            result.Impact.VelocityChange.Should().BeApproximately(1.0, 0.1);
        }

        [Fact]
        public void Simulate_WithDelay_AccountsForDelayPeriod()
        {
            // Arrange
            var prediction = CreateTestPrediction(daysRemaining: 30);
            var scenario = new WhatIfSimulator.WhatIfScenario
            {
                Name = "Delayed Start",
                VelocityMultiplier = 1.5,
                DelayDays = 7
            };

            // Act
            var result = _simulator.Simulate(prediction, scenario);

            // Assert
            // Even with 50% increase, delay should reduce overall benefit
            result.Should().NotBeNull();
            result.Outcome.DaysToTarget.Should().BeGreaterThanOrEqualTo(7);
        }

        [Fact]
        public void Simulate_WithNullPrediction_ReturnsInvalidResult()
        {
            // Arrange
            var scenario = new WhatIfSimulator.WhatIfScenario { Name = "Test" };

            // Act
            var result = _simulator.Simulate(null, scenario);

            // Assert
            result.Should().NotBeNull();
            result.WillHitTarget.Should().BeFalse();
        }

        #endregion

        #region SimulateMultiple Tests

        [Fact]
        public void SimulateMultiple_WithMultipleScenarios_ReturnsAllResults()
        {
            // Arrange
            var prediction = CreateTestPrediction();
            var scenarios = _simulator.GetPredefinedScenarios().Take(3);

            // Act
            var results = _simulator.SimulateMultiple(prediction, scenarios);

            // Assert
            results.Should().HaveCount(3);
            results.Should().AllSatisfy(r => r.Should().NotBeNull());
        }

        #endregion

        #region CalculateRequiredVelocity Tests

        [Fact]
        public void CalculateRequiredVelocity_WithAtRiskGoal_ReturnsHigherMultiplier()
        {
            // Arrange
            var prediction = CreateTestPrediction(
                currentProgress: 30, 
                daysRemaining: 30,
                risk: TrajectoryPredictor.RiskLevel.AtRisk);

            // Act
            var scenario = _simulator.CalculateRequiredVelocity(prediction);

            // Assert
            scenario.VelocityMultiplier.Should().BeGreaterThan(1.0);
        }

        [Fact]
        public void CalculateRequiredVelocity_WithOnTrackGoal_ReturnsLowerMultiplier()
        {
            // Arrange
            var prediction = CreateTestPrediction(
                currentProgress: 80, 
                daysRemaining: 30,
                risk: TrajectoryPredictor.RiskLevel.OnTrack);

            // Act
            var scenario = _simulator.CalculateRequiredVelocity(prediction);

            // Assert
            scenario.VelocityMultiplier.Should().BeLessThanOrEqualTo(1.5);
        }

        #endregion

        #region CreateCustomScenario Tests

        [Fact]
        public void CreateCustomScenario_WithMultiplier_CreatesValidScenario()
        {
            // Act
            var scenario = _simulator.CreateCustomScenario(1.25);

            // Assert
            scenario.VelocityMultiplier.Should().Be(1.25);
            scenario.Name.Should().Contain("25%");
            scenario.Name.Should().Contain("Increase");
        }

        [Fact]
        public void CreateCustomScenario_WithDecrease_CreatesDecreaseScenario()
        {
            // Act
            var scenario = _simulator.CreateCustomScenario(0.75);

            // Assert
            scenario.VelocityMultiplier.Should().Be(0.75);
            scenario.Name.Should().Contain("25%");
            scenario.Name.Should().Contain("Decrease");
        }

        [Fact]
        public void CreateCustomScenario_WithCustomName_UsesProvidedName()
        {
            // Act
            var scenario = _simulator.CreateCustomScenario(1.5, "My Custom Scenario");

            // Assert
            scenario.Name.Should().Be("My Custom Scenario");
            scenario.VelocityMultiplier.Should().Be(1.5);
        }

        #endregion

        #region Impact Analysis Tests

        [Fact]
        public void Simulate_CalculatesVelocityChangePercent()
        {
            // Arrange
            var prediction = CreateTestPrediction();
            var scenario = new WhatIfSimulator.WhatIfScenario
            {
                Name = "Test",
                VelocityMultiplier = 1.5
            };

            // Act
            var result = _simulator.Simulate(prediction, scenario);

            // Assert
            result.Impact.VelocityChangePercent.Should().BeApproximately(50, 5);
        }

        [Fact]
        public void Simulate_GeneratesImpactDescription()
        {
            // Arrange
            var prediction = CreateTestPrediction();
            var scenario = new WhatIfSimulator.WhatIfScenario
            {
                Name = "Test",
                VelocityMultiplier = 1.5
            };

            // Act
            var result = _simulator.Simulate(prediction, scenario);

            // Assert
            result.Impact.ImpactDescription.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Simulate_GeneratesSummary()
        {
            // Arrange
            var prediction = CreateTestPrediction();
            var scenario = new WhatIfSimulator.WhatIfScenario
            {
                Name = "Test Scenario",
                VelocityMultiplier = 1.2
            };

            // Act
            var result = _simulator.Simulate(prediction, scenario);

            // Assert
            result.Summary.Should().NotBeNullOrEmpty();
            result.Summary.Should().Contain("Test Scenario");
        }

        #endregion
    }
}
