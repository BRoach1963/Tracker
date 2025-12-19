using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.DataModels
{
    public class ReminderTests
    {
        [Fact]
        public void NewReminder_ShouldHaveDefaultValues()
        {
            var reminder = new Reminder();

            reminder.Id.Should().Be(0);
            reminder.Title.Should().Be(string.Empty);
            reminder.Message.Should().Be(string.Empty);
            reminder.Status.Should().Be(ReminderStatus.Pending);
            reminder.Type.Should().Be(ReminderType.Custom);
        }

        [Fact]
        public void TestDataBuilder_ShouldCreateValidReminder()
        {
            var dueDate = DateTime.Now.AddHours(2);
            var reminder = TestDataBuilder.CreateReminder("Meeting reminder", dueDate, ReminderType.Meeting);

            reminder.Title.Should().Be("Meeting reminder");
            reminder.DueDateTime.Should().BeCloseTo(dueDate, TimeSpan.FromSeconds(1));
            reminder.Type.Should().Be(ReminderType.Meeting);
        }

        [Theory]
        [InlineData(ReminderStatus.Pending)]
        [InlineData(ReminderStatus.Triggered)]
        [InlineData(ReminderStatus.Snoozed)]
        [InlineData(ReminderStatus.Dismissed)]
        public void Status_ShouldAcceptAllValidValues(ReminderStatus status)
        {
            var reminder = new Reminder { Status = status };
            reminder.Status.Should().Be(status);
        }

        [Theory]
        [InlineData(ReminderType.Meeting)]
        [InlineData(ReminderType.Task)]
        [InlineData(ReminderType.Goal)]
        [InlineData(ReminderType.Custom)]
        public void Type_ShouldAcceptAllValidValues(ReminderType type)
        {
            var reminder = new Reminder { Type = type };
            reminder.Type.Should().Be(type);
        }

        [Fact]
        public void SnoozedUntil_ShouldBeNullableAndSettable()
        {
            var reminder = new Reminder { SnoozedUntil = null };
            reminder.SnoozedUntil.Should().BeNull();

            var snoozeTime = DateTime.Now.AddMinutes(15);
            reminder.SnoozedUntil = snoozeTime;
            reminder.SnoozedUntil.Should().BeCloseTo(snoozeTime, TimeSpan.FromSeconds(1));
        }
    }
}

