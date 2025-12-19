using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Integration
{
    [Collection("Database")]
    public class OneOnOneIntegrationTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public OneOnOneIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task OneOnOne_CanHaveAgendaItemsAndTasks()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var oneOnOne = TestDataBuilder.CreateOneOnOne(teamMember);
            oneOnOne.AgendaItems.Add(new AgendaItem
            {
                Description = "Discuss project status",
                Category = AgendaItemCategory.Project
            });
            oneOnOne.AgendaItems.Add(new AgendaItem
            {
                Description = "Career development",
                Category = AgendaItemCategory.Career
            });
            oneOnOne.Tasks.Add(new MeetingTask
            {
                Description = "Update documentation",
                DueDate = DateTime.Today.AddDays(7),
                IsCompleted = false
            });
            
            context.OneOnOnes.Add(oneOnOne);
            context.Entry(oneOnOne).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.OneOnOnes
                .Include(o => o.AgendaItems)
                .Include(o => o.Tasks)
                .FirstOrDefaultAsync(o => o.Id == oneOnOne.Id);

            retrieved!.AgendaItems.Should().HaveCount(2);
            retrieved.Tasks.Should().HaveCount(1);
        }

        [Fact]
        public async Task OneOnOne_RolloverTasks_ShouldCopyIncompleteTasks()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Create previous meeting with tasks
            var previousMeeting = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today.AddDays(-7));
            previousMeeting.Status = MeetingStatusEnum.Completed;
            previousMeeting.Tasks.Add(new MeetingTask
            {
                Description = "Completed task",
                DueDate = DateTime.Today.AddDays(-3),
                IsCompleted = true
            });
            previousMeeting.Tasks.Add(new MeetingTask
            {
                Description = "Incomplete task",
                DueDate = DateTime.Today.AddDays(-1),
                IsCompleted = false
            });
            
            context.OneOnOnes.Add(previousMeeting);
            context.Entry(previousMeeting).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Create new meeting and rollover incomplete tasks
            var newMeeting = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today);
            var incompleteTasks = previousMeeting.Tasks.Where(t => !t.IsCompleted).ToList();
            foreach (var task in incompleteTasks)
            {
                newMeeting.Tasks.Add(new MeetingTask
                {
                    Description = task.Description,
                    DueDate = DateTime.Today.AddDays(7),
                    IsCompleted = false
                });
            }
            
            context.OneOnOnes.Add(newMeeting);
            context.Entry(newMeeting).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrievedNew = await context.OneOnOnes
                .Include(o => o.Tasks)
                .FirstOrDefaultAsync(o => o.Id == newMeeting.Id);

            retrievedNew!.Tasks.Should().HaveCount(1);
            retrievedNew.Tasks[0].Description.Should().Be("Incomplete task");
        }

        [Fact]
        public async Task OneOnOne_GetUpcoming_ShouldReturnFutureMeetings()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var pastMeeting = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today.AddDays(-7));
            pastMeeting.Status = MeetingStatusEnum.Completed;
            
            var futureMeeting1 = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today.AddDays(7));
            var futureMeeting2 = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today.AddDays(14));
            
            context.OneOnOnes.AddRange(pastMeeting, futureMeeting1, futureMeeting2);
            context.Entry(pastMeeting).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(futureMeeting1).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(futureMeeting2).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var upcomingMeetings = await context.OneOnOnes
                .Where(o => o.Date >= DateTime.Today && o.Status == MeetingStatusEnum.Scheduled)
                .OrderBy(o => o.Date)
                .ToListAsync();

            upcomingMeetings.Should().HaveCount(2);
        }

        [Fact]
        public async Task OneOnOne_GetHistory_ShouldReturnPastMeetings()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var pastMeeting1 = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today.AddDays(-14));
            pastMeeting1.Status = MeetingStatusEnum.Completed;
            
            var pastMeeting2 = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today.AddDays(-7));
            pastMeeting2.Status = MeetingStatusEnum.Completed;
            
            var futureMeeting = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today.AddDays(7));
            
            context.OneOnOnes.AddRange(pastMeeting1, pastMeeting2, futureMeeting);
            context.Entry(pastMeeting1).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(pastMeeting2).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(futureMeeting).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var meetingHistory = await context.OneOnOnes
                .Where(o => o.TeamMember.Id == teamMember.Id && o.Status == MeetingStatusEnum.Completed)
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            meetingHistory.Should().HaveCount(2);
            meetingHistory[0].Date.Should().Be(DateTime.Today.AddDays(-7));
        }

        [Fact]
        public async Task OneOnOne_CanBeCanceled()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var oneOnOne = TestDataBuilder.CreateOneOnOne(teamMember);
            context.OneOnOnes.Add(oneOnOne);
            context.Entry(oneOnOne).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            oneOnOne.Status = MeetingStatusEnum.Canceled;
            await context.SaveChangesAsync();

            using var verifyContext = _fixture.CreateContext();
            var retrieved = await verifyContext.OneOnOnes.FindAsync(oneOnOne.Id);
            retrieved!.Status.Should().Be(MeetingStatusEnum.Canceled);
        }
    }
}

