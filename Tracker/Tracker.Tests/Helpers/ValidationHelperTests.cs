using FluentAssertions;
using Tracker.DataModels;
using Tracker.Helpers;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Helpers
{
    public class ValidationHelperTests
    {
        [Theory]
        [InlineData("test@example.com", true)]
        [InlineData("user.name@domain.org", true)]
        [InlineData("user+tag@example.com", true)]
        [InlineData("invalid", false)]
        [InlineData("@nodomain.com", false)]
        [InlineData("noemail@", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        public void IsValidEmail_ShouldValidateCorrectly(string? email, bool expected)
        {
            // Act
            var result = ValidationHelper.IsValidEmail(email!);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void IsValidTeamMember_ShouldReturnTrue_ForValidTeamMember()
        {
            // Arrange
            var teamMember = TestDataBuilder.CreateTeamMember(
                firstName: "John",
                lastName: "Doe",
                email: "john@test.com"
            );

            // Act
            var result = ValidationHelper.IsValidTeamMember(teamMember);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidTeamMember_ShouldReturnFalse_ForNullTeamMember()
        {
            // Act
            var result = ValidationHelper.IsValidTeamMember(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidTeamMember_ShouldReturnFalse_ForEmptyFirstName()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "",
                LastName = "Doe",
                Email = "test@test.com",
                HireDate = DateTime.Today
            };

            // Act
            var result = ValidationHelper.IsValidTeamMember(teamMember);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidTeamMember_ShouldReturnFalse_ForInvalidEmail()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "invalid-email",
                HireDate = DateTime.Today
            };

            // Act
            var result = ValidationHelper.IsValidTeamMember(teamMember);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidOneOnOne_ShouldReturnTrue_ForValidOneOnOne()
        {
            // Arrange
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1; // Simulate saved entity
            var oneOnOne = TestDataBuilder.CreateOneOnOne(teamMember);

            // Act
            var result = ValidationHelper.IsValidOneOnOne(oneOnOne);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidOneOnOne_ShouldReturnFalse_ForNullOneOnOne()
        {
            // Act
            var result = ValidationHelper.IsValidOneOnOne(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidOneOnOne_ShouldReturnFalse_ForMissingTeamMember()
        {
            // Arrange
            var oneOnOne = new OneOnOne
            {
                Date = DateTime.Today,
                Description = "Test meeting"
            };

            // Act
            var result = ValidationHelper.IsValidOneOnOne(oneOnOne);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidTask_ShouldReturnTrue_ForValidTask()
        {
            // Arrange
            var owner = TestDataBuilder.CreateTeamMember();
            owner.Id = 1;
            var task = TestDataBuilder.CreateTask(owner);

            // Act
            var result = ValidationHelper.IsValidTask(task);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidTask_ShouldReturnFalse_ForEmptyDescription()
        {
            // Arrange
            var owner = TestDataBuilder.CreateTeamMember();
            owner.Id = 1;
            var task = new IndividualTask
            {
                Description = "",
                Owner = owner,
                DueDate = DateTime.Today
            };

            // Act
            var result = ValidationHelper.IsValidTask(task);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidProject_ShouldReturnTrue_ForValidProject()
        {
            // Arrange
            var owner = TestDataBuilder.CreateTeamMember();
            owner.Id = 1;
            var project = TestDataBuilder.CreateProject(owner);

            // Act
            var result = ValidationHelper.IsValidProject(project);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidProject_ShouldReturnFalse_ForEmptyName()
        {
            // Arrange
            var owner = TestDataBuilder.CreateTeamMember();
            owner.Id = 1;
            var project = new Project
            {
                Name = "",
                Owner = owner,
                StartDate = DateTime.Today
            };

            // Act
            var result = ValidationHelper.IsValidProject(project);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidFeedback_ShouldReturnTrue_ForValidFeedback()
        {
            // Arrange
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var feedback = TestDataBuilder.CreateFeedback(teamMember);

            // Act
            var result = ValidationHelper.IsValidFeedback(feedback);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidGoal_ShouldReturnTrue_ForValidGoal()
        {
            // Arrange
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var goal = TestDataBuilder.CreateGoal(teamMember);

            // Act
            var result = ValidationHelper.IsValidGoal(goal);

            // Assert
            result.Should().BeTrue();
        }
    }
}

