using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.Database;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Database
{
    [Collection("Database")]
    public class DatabaseSeederTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public DatabaseSeederTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task SeedSampleData_ShouldCreateTeamMembers()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var teamMembers = await context.TeamMembers.ToListAsync();
            teamMembers.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldCreateOneOnOnes()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var oneOnOnes = await context.OneOnOnes.ToListAsync();
            oneOnOnes.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldCreateProjects()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var projects = await context.Projects.ToListAsync();
            projects.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldCreateTasks()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var tasks = await context.Tasks.ToListAsync();
            tasks.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldCreateOKRs()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var okrs = await context.OKRs.ToListAsync();
            okrs.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldCreateKPIs()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var kpis = await context.KPIs.ToListAsync();
            kpis.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldCreateFeedback()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var feedbacks = await context.Feedbacks.ToListAsync();
            feedbacks.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldCreateGoals()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var goals = await context.IndividualGoals.ToListAsync();
            goals.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldCreateMeetingTemplates()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert
            var templates = await context.MeetingTemplates.ToListAsync();
            templates.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SeedSampleData_ShouldBeIdempotent()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act - Seed twice
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);
            var countAfterFirst = await context.TeamMembers.CountAsync();
            
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);
            var countAfterSecond = await context.TeamMembers.CountAsync();

            // Assert - Counts should be the same (not doubled)
            countAfterSecond.Should().Be(countAfterFirst);
        }

        [Fact]
        public async Task ClearAllData_ShouldRemoveAllRecords()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);
            
            // First seed some data
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);
            var teamMembersBeforeClear = await context.TeamMembers.CountAsync();
            teamMembersBeforeClear.Should().BeGreaterThan(0);

            // Act
            await seeder.ClearAllDataAsync();

            // Assert
            var teamMembersAfterClear = await context.TeamMembers.CountAsync();
            teamMembersAfterClear.Should().Be(0);
        }

        [Fact]
        public async Task SeedSampleData_AllEntitiesShouldHaveUserId()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var seeder = new DatabaseSeeder(context);

            // Act
            await seeder.SeedSampleDataAsync(_fixture.TestUserId);

            // Assert - Check UserId shadow property is set
            var teamMembers = await context.TeamMembers.ToListAsync();
            foreach (var tm in teamMembers)
            {
                var userId = context.Entry(tm).Property("UserId").CurrentValue;
                userId.Should().Be(_fixture.TestUserId);
            }
        }
    }
}

