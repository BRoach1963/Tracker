using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Tests.DataModels
{
    public class IndividualGoalTests
    {
        [Fact]
        public void IsOverdue_ShouldReturnTrue_WhenPastTargetDateAndNotCompleted()
        {
            // Arrange
            var goal = new IndividualGoal
            {
                TargetDate = DateTime.Today.AddDays(-1),
                Status = GoalStatus.InProgress
            };

            // Act & Assert
            goal.IsOverdue.Should().BeTrue();
        }

        [Fact]
        public void IsOverdue_ShouldReturnFalse_WhenCompleted()
        {
            // Arrange
            var goal = new IndividualGoal
            {
                TargetDate = DateTime.Today.AddDays(-1),
                Status = GoalStatus.Completed
            };

            // Act & Assert
            goal.IsOverdue.Should().BeFalse();
        }

        [Fact]
        public void IsOverdue_ShouldReturnFalse_WhenNoTargetDate()
        {
            // Arrange
            var goal = new IndividualGoal
            {
                TargetDate = null,
                Status = GoalStatus.InProgress
            };

            // Act & Assert
            goal.IsOverdue.Should().BeFalse();
        }

        [Fact]
        public void DaysRemaining_ShouldCalculateCorrectly()
        {
            // Arrange
            var goal = new IndividualGoal
            {
                TargetDate = DateTime.Today.AddDays(10)
            };

            // Act & Assert
            goal.DaysRemaining.Should().Be(10);
        }

        [Fact]
        public void DaysRemaining_ShouldReturnNull_WhenNoTargetDate()
        {
            // Arrange
            var goal = new IndividualGoal { TargetDate = null };

            // Act & Assert
            goal.DaysRemaining.Should().BeNull();
        }

        [Theory]
        [InlineData(GoalStatus.NotStarted)]
        [InlineData(GoalStatus.InProgress)]
        [InlineData(GoalStatus.OnTrack)]
        [InlineData(GoalStatus.AtRisk)]
        [InlineData(GoalStatus.Completed)]
        [InlineData(GoalStatus.OnHold)]
        [InlineData(GoalStatus.Cancelled)]
        public void AllGoalStatuses_ShouldBeValid(GoalStatus status)
        {
            // Arrange
            var goal = new IndividualGoal { Status = status };

            // Act & Assert
            goal.Status.Should().Be(status);
        }

        [Theory]
        [InlineData(GoalCategory.Career)]
        [InlineData(GoalCategory.SkillDevelopment)]
        [InlineData(GoalCategory.Certification)]
        [InlineData(GoalCategory.Leadership)]
        [InlineData(GoalCategory.Communication)]
        [InlineData(GoalCategory.Technical)]
        [InlineData(GoalCategory.Personal)]
        public void AllGoalCategories_ShouldBeValid(GoalCategory category)
        {
            // Arrange
            var goal = new IndividualGoal { Category = category };

            // Act & Assert
            goal.Category.Should().Be(category);
        }

        [Fact]
        public void ProgressPercent_ShouldBeBoundedCorrectly()
        {
            // Arrange & Act
            var goal = new IndividualGoal { ProgressPercent = 50 };

            // Assert
            goal.ProgressPercent.Should().BeInRange(0, 100);
        }

        [Fact]
        public void Milestones_ShouldBeInitializedEmpty()
        {
            // Arrange & Act
            var goal = new IndividualGoal();

            // Assert
            goal.Milestones.Should().NotBeNull();
            goal.Milestones.Should().BeEmpty();
        }
    }
}

