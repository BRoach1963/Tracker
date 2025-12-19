using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.DataModels
{
    public class IndividualTaskTests
    {
        [Fact]
        public void NewTask_ShouldHaveDefaultValues()
        {
            var task = new IndividualTask();

            task.Id.Should().Be(0);
            task.Description.Should().Be(string.Empty);
            task.IsCompleted.Should().BeFalse();
            task.Priority.Should().Be(TaskPriorityEnum.Medium);
        }

        [Fact]
        public void TestDataBuilder_ShouldCreateValidTask()
        {
            var owner = TestDataBuilder.CreateTeamMember();
            var task = TestDataBuilder.CreateTask(owner, description: "Complete review");

            task.Description.Should().Be("Complete review");
            task.Owner.Should().Be(owner);
            task.IsCompleted.Should().BeFalse();
        }

        [Theory]
        [InlineData(TaskPriorityEnum.Low)]
        [InlineData(TaskPriorityEnum.Medium)]
        [InlineData(TaskPriorityEnum.High)]
        [InlineData(TaskPriorityEnum.Critical)]
        public void Priority_ShouldAcceptAllValidValues(TaskPriorityEnum priority)
        {
            var task = new IndividualTask { Priority = priority };
            task.Priority.Should().Be(priority);
        }

        [Fact]
        public void IsCompleted_ShouldBeSettable()
        {
            var task = new IndividualTask { IsCompleted = false };
            
            task.IsCompleted = true;

            task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public void DueDate_ShouldBeSettable()
        {
            var dueDate = DateTime.Today.AddDays(7);
            var task = new IndividualTask { DueDate = dueDate };

            task.DueDate.Should().Be(dueDate);
        }
    }
}

