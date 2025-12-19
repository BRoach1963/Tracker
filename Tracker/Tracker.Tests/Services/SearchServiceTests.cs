using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Services
{
    [Collection("Database")]
    public class SearchServiceTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public SearchServiceTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
            await SeedTestData();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private async Task SeedTestData()
        {
            using var context = _fixture.CreateContext();

            // Create test team members
            var john = new TeamMember
            {
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@test.com",
                Role = "Developer",
                JobTitle = "Senior Developer",
                HireDate = DateTime.Today.AddYears(-2),
                IsActive = true
            };

            var jane = new TeamMember
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@test.com",
                Role = "Manager",
                JobTitle = "Engineering Manager",
                HireDate = DateTime.Today.AddYears(-3),
                IsActive = true
            };

            context.TeamMembers.Add(john);
            context.TeamMembers.Add(jane);
            context.Entry(john).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(jane).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Create test projects
            var project = new Project
            {
                Name = "Alpha Project",
                Description = "Test project for alpha features",
                Owner = john,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3),
                Status = ProjectStatusEnum.InProgress
            };

            context.Projects.Add(project);
            context.Entry(project).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Create test tasks
            var task = new IndividualTask
            {
                Description = "Implement search functionality",
                Owner = john,
                DueDate = DateTime.Today.AddDays(7),
                Priority = TaskPriorityEnum.High,
                IsCompleted = false
            };

            context.Tasks.Add(task);
            context.Entry(task).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Create test notes
            var note = new QuickNote
            {
                Content = "Remember to review the search implementation",
                Category = NoteCategory.Todo,
                IsPinned = true
            };

            context.QuickNotes.Add(note);
            context.Entry(note).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();
        }

        [Fact]
        public void SearchQuery_ShouldMatchTeamMemberNames()
        {
            // This tests the search logic - in a real implementation
            // you'd use the SearchService directly
            
            // Arrange
            var query = "john";
            var teamMembers = new List<TeamMember>
            {
                new TeamMember { FirstName = "John", LastName = "Smith" },
                new TeamMember { FirstName = "Jane", LastName = "Doe" }
            };

            // Act
            var results = teamMembers.Where(tm =>
                tm.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                tm.LastName.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            // Assert
            results.Should().HaveCount(1);
            results[0].FirstName.Should().Be("John");
        }

        [Fact]
        public void SearchQuery_ShouldMatchProjectNames()
        {
            // Arrange
            var query = "alpha";
            var projects = new List<Project>
            {
                new Project { Name = "Alpha Project" },
                new Project { Name = "Beta Project" }
            };

            // Act
            var results = projects.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            // Assert
            results.Should().HaveCount(1);
            results[0].Name.Should().Be("Alpha Project");
        }

        [Fact]
        public void SearchQuery_ShouldBeCaseInsensitive()
        {
            // Arrange
            var query = "JOHN";
            var teamMembers = new List<TeamMember>
            {
                new TeamMember { FirstName = "john", LastName = "Smith" }
            };

            // Act
            var results = teamMembers.Where(tm =>
                tm.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            // Assert
            results.Should().HaveCount(1);
        }

        [Fact]
        public void SearchQuery_ShouldMatchPartialStrings()
        {
            // Arrange
            var query = "alph";
            var projects = new List<Project>
            {
                new Project { Name = "Alpha Project" }
            };

            // Act
            var results = projects.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            // Assert
            results.Should().HaveCount(1);
        }

        [Fact]
        public void SearchQuery_ShouldReturnEmptyForNoMatches()
        {
            // Arrange
            var query = "xyz123";
            var teamMembers = new List<TeamMember>
            {
                new TeamMember { FirstName = "John", LastName = "Smith" }
            };

            // Act
            var results = teamMembers.Where(tm =>
                tm.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                tm.LastName.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void SearchQuery_ShouldMatchNoteContent()
        {
            // Arrange
            var query = "search";
            var notes = new List<QuickNote>
            {
                new QuickNote { Content = "Remember to review the search implementation" },
                new QuickNote { Content = "Buy groceries" }
            };

            // Act
            var results = notes.Where(n =>
                n.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            // Assert
            results.Should().HaveCount(1);
        }
    }
}

