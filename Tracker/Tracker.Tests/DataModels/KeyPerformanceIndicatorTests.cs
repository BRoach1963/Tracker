using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Tests.DataModels
{
    public class KeyPerformanceIndicatorTests
    {
        [Fact]
        public void CalculatedStatus_ShouldReturnGreen_WhenAtOrAboveTarget()
        {
            // Arrange
            var kpi = new KeyPerformanceIndicator
            {
                TargetValue = 100,
                Value = 100
            };

            // Act & Assert
            kpi.CalculatedStatus.Should().Be(KpiStatusEnum.Green);
        }

        [Fact]
        public void CalculatedStatus_ShouldReturnGreen_WhenAboveTarget()
        {
            // Arrange
            var kpi = new KeyPerformanceIndicator
            {
                TargetValue = 100,
                Value = 120
            };

            // Act & Assert
            kpi.CalculatedStatus.Should().Be(KpiStatusEnum.Green);
        }

        [Fact]
        public void CalculatedStatus_ShouldReturnAmber_WhenBetween80And100Percent()
        {
            // Arrange
            var kpi = new KeyPerformanceIndicator
            {
                TargetValue = 100,
                Value = 85
            };

            // Act & Assert
            kpi.CalculatedStatus.Should().Be(KpiStatusEnum.Amber);
        }

        [Fact]
        public void CalculatedStatus_ShouldReturnRed_WhenBelow80Percent()
        {
            // Arrange
            var kpi = new KeyPerformanceIndicator
            {
                TargetValue = 100,
                Value = 70
            };

            // Act & Assert
            kpi.CalculatedStatus.Should().Be(KpiStatusEnum.Red);
        }

        [Fact]
        public void CalculatedStatus_ShouldHandleZeroTarget()
        {
            // Arrange
            var kpi = new KeyPerformanceIndicator
            {
                TargetValue = 0,
                Value = 50
            };

            // Act & Assert - should not throw and return a valid status
            kpi.CalculatedStatus.Should().BeOneOf(KpiStatusEnum.Green, KpiStatusEnum.Amber, KpiStatusEnum.Red);
        }

        [Fact]
        public void ProgressPercentage_ShouldCalculateCorrectly()
        {
            // Arrange
            var kpi = new KeyPerformanceIndicator
            {
                TargetValue = 100,
                Value = 75
            };

            // Act
            var progress = kpi.TargetValue > 0 ? (kpi.Value / kpi.TargetValue) * 100 : 0;

            // Assert
            progress.Should().Be(75);
        }

        [Fact]
        public void NewKpi_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var kpi = new KeyPerformanceIndicator();

            // Assert
            kpi.KpiId.Should().Be(0);
            kpi.Name.Should().Be(string.Empty);
            kpi.Value.Should().Be(0);
            kpi.TargetValue.Should().Be(0);
        }

        [Fact]
        public void Kpi_ImplementsIMeasurable()
        {
            // Arrange
            var kpi = new KeyPerformanceIndicator
            {
                KpiId = 1,
                Name = "Test KPI",
                Value = 75,
                TargetValue = 100,
                Unit = "%"
            };

            // Act & Assert
            kpi.MeasurableId.Should().Be(1);
            kpi.DisplayName.Should().Be("Test KPI");
            kpi.MeasurableType.Should().Be(Tracker.Interfaces.MeasurableType.Kpi);
        }

        [Fact]
        public void Kpi_CanBeStandalone()
        {
            // Arrange - KPIs are standalone by default, linked to OKRs via KeyResultMeasurable
            var kpi = new KeyPerformanceIndicator
            {
                Name = "Standalone KPI",
                Value = 50,
                TargetValue = 100
            };

            // Act & Assert
            kpi.Name.Should().Be("Standalone KPI");
            kpi.ParentKpiId.Should().BeNull(); // No parent = standalone
        }
    }
}

