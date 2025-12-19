using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Services
{
    [Collection("Database")]
    public class ReminderServiceTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public ReminderServiceTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Reminder_ShouldBePending_WhenCreated()
        {
            using var context = _fixture.CreateContext();
            
            var reminder = TestDataBuilder.CreateReminder(dueDateTime: DateTime.Now.AddHours(1));
            context.Reminders.Add(reminder);
            context.Entry(reminder).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            reminder.Status.Should().Be(ReminderStatus.Pending);
        }

        [Fact]
        public async Task Reminder_CanBeTriggered()
        {
            using var context = _fixture.CreateContext();
            
            var reminder = TestDataBuilder.CreateReminder(dueDateTime: DateTime.Now.AddMinutes(-5));
            context.Reminders.Add(reminder);
            context.Entry(reminder).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            reminder.Status = ReminderStatus.Triggered;
            await context.SaveChangesAsync();

            using var verifyContext = _fixture.CreateContext();
            var retrieved = await verifyContext.Reminders.FindAsync(reminder.Id);
            retrieved!.Status.Should().Be(ReminderStatus.Triggered);
        }

        [Fact]
        public async Task Reminder_CanBeSnoozed()
        {
            using var context = _fixture.CreateContext();
            
            var reminder = TestDataBuilder.CreateReminder();
            context.Reminders.Add(reminder);
            context.Entry(reminder).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var snoozeUntil = DateTime.Now.AddMinutes(15);
            reminder.Status = ReminderStatus.Snoozed;
            reminder.SnoozedUntil = snoozeUntil;
            await context.SaveChangesAsync();

            using var verifyContext = _fixture.CreateContext();
            var retrieved = await verifyContext.Reminders.FindAsync(reminder.Id);
            retrieved!.Status.Should().Be(ReminderStatus.Snoozed);
            retrieved.SnoozedUntil.Should().BeCloseTo(snoozeUntil, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Reminder_CanBeDismissed()
        {
            using var context = _fixture.CreateContext();
            
            var reminder = TestDataBuilder.CreateReminder();
            context.Reminders.Add(reminder);
            context.Entry(reminder).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            reminder.Status = ReminderStatus.Dismissed;
            await context.SaveChangesAsync();

            using var verifyContext = _fixture.CreateContext();
            var retrieved = await verifyContext.Reminders.FindAsync(reminder.Id);
            retrieved!.Status.Should().Be(ReminderStatus.Dismissed);
        }

        [Fact]
        public async Task GetPendingReminders_ShouldFilterByStatus()
        {
            using var context = _fixture.CreateContext();
            
            var pending = TestDataBuilder.CreateReminder(title: "Pending");
            pending.Status = ReminderStatus.Pending;
            context.Reminders.Add(pending);
            context.Entry(pending).Property("UserId").CurrentValue = _fixture.TestUserId;

            var dismissed = TestDataBuilder.CreateReminder(title: "Dismissed");
            dismissed.Status = ReminderStatus.Dismissed;
            context.Reminders.Add(dismissed);
            context.Entry(dismissed).Property("UserId").CurrentValue = _fixture.TestUserId;
            
            await context.SaveChangesAsync();

            var pendingReminders = context.Reminders
                .Where(r => r.Status == ReminderStatus.Pending)
                .ToList();

            pendingReminders.Should().HaveCount(1);
            pendingReminders[0].Title.Should().Be("Pending");
        }

        [Fact]
        public async Task GetDueReminders_ShouldFilterByDueDateTime()
        {
            using var context = _fixture.CreateContext();
            
            var overdue = TestDataBuilder.CreateReminder(
                title: "Overdue",
                dueDateTime: DateTime.Now.AddHours(-1)
            );
            context.Reminders.Add(overdue);
            context.Entry(overdue).Property("UserId").CurrentValue = _fixture.TestUserId;

            var future = TestDataBuilder.CreateReminder(
                title: "Future",
                dueDateTime: DateTime.Now.AddHours(2)
            );
            context.Reminders.Add(future);
            context.Entry(future).Property("UserId").CurrentValue = _fixture.TestUserId;
            
            await context.SaveChangesAsync();

            var dueReminders = context.Reminders
                .Where(r => r.DueDateTime <= DateTime.Now && r.Status == ReminderStatus.Pending)
                .ToList();

            dueReminders.Should().HaveCount(1);
            dueReminders[0].Title.Should().Be("Overdue");
        }
    }
}

