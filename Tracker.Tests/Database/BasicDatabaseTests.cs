using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;

namespace Tracker.Tests.Database
{
    [Collection("Database")]
    public class BasicDatabaseTests
    {
        private readonly DatabaseFixture _fixture;

        public BasicDatabaseTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void DatabaseFixture_ShouldInitialize()
        {
            _fixture.Should().NotBeNull();
            _fixture.Factory.Should().NotBeNull();
            _fixture.TestUser.Should().NotBeNull();
            _fixture.TestUserId.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateContext_ShouldReturnValidContext()
        {
            using var context = _fixture.CreateContext();
            
            context.Should().NotBeNull();
            context.Database.CanConnect().Should().BeTrue();
        }

        [Fact]
        public async Task CanCreateAndRetrieveUser()
        {
            using var context = _fixture.CreateContext();
            
            var user = new User
            {
                Username = "testuser2",
                DisplayName = "Test User 2",
                Email = "test2@example.com",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var retrieved = await context.Users.FindAsync(user.Id);
            
            retrieved.Should().NotBeNull();
            retrieved!.Username.Should().Be("testuser2");
        }

        [Fact]
        public async Task CanCreateAndRetrieveTeamMember()
        {
            using var context = _fixture.CreateContext();

            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Role = RoleEnum.Engineer,
                JobTitle = "Senior Developer",
                HireDate = DateTime.Today.AddYears(-2),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.TeamMembers.FindAsync(teamMember.Id);
            
            retrieved.Should().NotBeNull();
            retrieved!.FirstName.Should().Be("John");
            retrieved.LastName.Should().Be("Doe");
        }

        [Fact]
        public async Task CanCreateOneOnOneWithTeamMember()
        {
            using var context = _fixture.CreateContext();

            // Create team member
            var teamMember = new TeamMember
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Role = RoleEnum.Manager,
                JobTitle = "Engineering Manager",
                HireDate = DateTime.Today.AddYears(-3),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Create 1:1
            var oneOnOne = new OneOnOne
            {
                TeamMember = teamMember,
                Date = DateTime.Today.AddDays(7),
                Duration = TimeSpan.FromMinutes(30),
                Description = "Weekly sync",
                Status = MeetingStatusEnum.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            context.OneOnOnes.Add(oneOnOne);
            context.Entry(oneOnOne).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.OneOnOnes
                .Include(o => o.TeamMember)
                .FirstOrDefaultAsync(o => o.Id == oneOnOne.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Description.Should().Be("Weekly sync");
            retrieved.TeamMember.Should().NotBeNull();
        }

        [Fact]
        public async Task CanCreateQuickNote()
        {
            using var context = _fixture.CreateContext();

            var note = new QuickNote
            {
                Content = "Important note",
                Category = NoteCategory.Meeting,
                IsPinned = true,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow
            };

            context.QuickNotes.Add(note);
            context.Entry(note).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.QuickNotes.FindAsync(note.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Content.Should().Be("Important note");
            retrieved.IsPinned.Should().BeTrue();
        }

        [Fact]
        public async Task CanCreateReminder()
        {
            using var context = _fixture.CreateContext();

            var reminder = new Reminder
            {
                Title = "Meeting reminder",
                Message = "Don't forget the meeting",
                Type = ReminderType.Meeting,
                Status = ReminderStatus.Pending,
                DueDateTime = DateTime.Now.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            context.Reminders.Add(reminder);
            context.Entry(reminder).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.Reminders.FindAsync(reminder.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be("Meeting reminder");
            retrieved.Status.Should().Be(ReminderStatus.Pending);
        }

        [Fact]
        public async Task SoftDelete_ShouldSetIsDeletedFlag()
        {
            using var context = _fixture.CreateContext();

            var teamMember = new TeamMember
            {
                FirstName = "ToDelete",
                LastName = "User",
                Email = "todelete@example.com",
                Role = RoleEnum.Engineer,
                HireDate = DateTime.Today,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            teamMember.IsDeleted = true;
            await context.SaveChangesAsync();

            var retrieved = await context.TeamMembers.FindAsync(teamMember.Id);
            retrieved!.IsDeleted.Should().BeTrue();
        }
    }
}
