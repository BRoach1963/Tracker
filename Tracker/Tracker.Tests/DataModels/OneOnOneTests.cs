using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.DataModels
{
    public class OneOnOneTests
    {
        [Fact]
        public void NewOneOnOne_ShouldHaveDefaultValues()
        {
            var oneOnOne = new OneOnOne();

            oneOnOne.Id.Should().Be(0);
            oneOnOne.Description.Should().Be(string.Empty);
            oneOnOne.Status.Should().Be(MeetingStatusEnum.Scheduled);
            oneOnOne.Duration.Should().Be(30);
            oneOnOne.AgendaItems.Should().BeEmpty();
            oneOnOne.Tasks.Should().BeEmpty();
        }

        [Fact]
        public void TaskCount_ShouldReturnCorrectCount()
        {
            var oneOnOne = new OneOnOne();
            oneOnOne.Tasks.Add(new MeetingTask { Description = "Task 1" });
            oneOnOne.Tasks.Add(new MeetingTask { Description = "Task 2" });

            oneOnOne.TaskCount.Should().Be(2);
        }

        [Fact]
        public void IncompleteTaskCount_ShouldReturnCorrectCount()
        {
            var oneOnOne = new OneOnOne();
            oneOnOne.Tasks.Add(new MeetingTask { Description = "Task 1", IsCompleted = false });
            oneOnOne.Tasks.Add(new MeetingTask { Description = "Task 2", IsCompleted = true });
            oneOnOne.Tasks.Add(new MeetingTask { Description = "Task 3", IsCompleted = false });

            oneOnOne.IncompleteTaskCount.Should().Be(2);
        }

        [Theory]
        [InlineData(MeetingStatusEnum.Scheduled)]
        [InlineData(MeetingStatusEnum.Completed)]
        [InlineData(MeetingStatusEnum.Canceled)]
        [InlineData(MeetingStatusEnum.Rescheduled)]
        public void Status_ShouldAcceptAllValidValues(MeetingStatusEnum status)
        {
            var oneOnOne = new OneOnOne { Status = status };
            oneOnOne.Status.Should().Be(status);
        }
    }
}
