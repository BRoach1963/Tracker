using FluentAssertions;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.DataModels
{
    public class TeamMemberTests
    {
        [Fact]
        public void FullName_ShouldCombineFirstAndLastName()
        {
            // Arrange
            var member = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe"
            };

            // Act & Assert
            member.FullName.Should().Be("John Doe");
        }

        [Fact]
        public void Initials_ShouldReturnFirstLettersOfNames()
        {
            // Arrange
            var member = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe"
            };

            // Act & Assert
            member.Initials.Should().Be("JD");
        }

        [Fact]
        public void Initials_ShouldHandleEmptyNames()
        {
            // Arrange
            var member = new TeamMember
            {
                FirstName = "",
                LastName = ""
            };

            // Act & Assert
            member.Initials.Should().Be("");
        }

        [Fact]
        public void Tenure_ShouldCalculateCorrectly()
        {
            // Arrange
            var member = new TeamMember
            {
                HireDate = DateTime.Today.AddYears(-2).AddMonths(-3)
            };

            // Act
            var tenure = member.Tenure;

            // Assert
            tenure.Should().Contain("2y");
        }

        [Fact]
        public void StatusDisplay_ShouldReturnActive_WhenIsActiveTrue()
        {
            // Arrange
            var member = new TeamMember { IsActive = true };

            // Act & Assert
            member.StatusDisplay.Should().Be("Active");
        }

        [Fact]
        public void StatusDisplay_ShouldReturnInactive_WhenIsActiveFalse()
        {
            // Arrange
            var member = new TeamMember { IsActive = false };

            // Act & Assert
            member.StatusDisplay.Should().Be("Inactive");
        }

        [Fact]
        public void NewTeamMember_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var member = new TeamMember();

            // Assert
            member.Id.Should().Be(0);
            member.FirstName.Should().Be(string.Empty);
            member.LastName.Should().Be(string.Empty);
            member.Email.Should().Be(string.Empty);
            member.IsActive.Should().BeTrue();
            member.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public void TestDataBuilder_ShouldCreateValidTeamMember()
        {
            // Arrange & Act
            var member = TestDataBuilder.CreateTeamMember(
                firstName: "Jane",
                lastName: "Smith",
                email: "jane@test.com",
                role: "Manager"
            );

            // Assert
            member.FirstName.Should().Be("Jane");
            member.LastName.Should().Be("Smith");
            member.Email.Should().Be("jane@test.com");
            member.Role.Should().Be("Manager");
            member.IsActive.Should().BeTrue();
        }
    }
}

