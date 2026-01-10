using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Helpers;
using Xunit;
using FluentAssertions;

namespace Tracker.Tests.Helpers
{
    public class ValidationHelperTests
    {
        #region TeamMember Validation Tests

        [Fact]
        public void ValidateTeamMember_WithValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                HireDate = DateTime.Today.AddYears(-1),
                Role = RoleEnum.Engineer,
                JobTitle = "Software Engineer",
                IsActive = true
            };

            // Act
            var results = ValidationHelper.Validate(teamMember);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void ValidateTeamMember_WithEmptyFirstName_ShouldReturnError()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "",
                LastName = "Doe",
                Email = "john.doe@company.com",
                HireDate = DateTime.Today.AddYears(-1)
            };

            // Act
            var results = ValidationHelper.Validate(teamMember);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("First name"));
        }

        [Fact]
        public void ValidateTeamMember_WithEmptyLastName_ShouldReturnError()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "",
                Email = "john.doe@company.com",
                HireDate = DateTime.Today.AddYears(-1)
            };

            // Act
            var results = ValidationHelper.Validate(teamMember);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Last name"));
        }

        [Fact]
        public void ValidateTeamMember_WithEmptyEmail_ShouldReturnError()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "",
                HireDate = DateTime.Today.AddYears(-1)
            };

            // Act
            var results = ValidationHelper.Validate(teamMember);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Email"));
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("missing@")]
        [InlineData("@nodomain.com")]
        [InlineData("spaces in@email.com")]
        public void ValidateTeamMember_WithInvalidEmail_ShouldReturnError(string invalidEmail)
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe",
                Email = invalidEmail,
                HireDate = DateTime.Today.AddYears(-1)
            };

            // Act
            var results = ValidationHelper.Validate(teamMember);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("email", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ValidateTeamMember_WithFutureHireDate_ShouldReturnError()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                HireDate = DateTime.Today.AddDays(30)
            };

            // Act
            var results = ValidationHelper.Validate(teamMember);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Hire date"));
        }

        [Fact]
        public void IsValid_WithValidTeamMember_ShouldReturnTrue()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                HireDate = DateTime.Today.AddYears(-1)
            };

            // Act
            var isValid = ValidationHelper.IsValid(teamMember);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithInvalidTeamMember_ShouldReturnFalse()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "",
                LastName = "",
                Email = "",
                HireDate = DateTime.Today
            };

            // Act
            var isValid = ValidationHelper.IsValid(teamMember);

            // Assert
            isValid.Should().BeFalse();
        }

        #endregion

        #region OneOnOne Validation Tests

        [Fact]
        public void ValidateOneOnOne_WithValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                HireDate = DateTime.Today.AddYears(-1)
            };

            var oneOnOne = new OneOnOne
            {
                TeamMember = teamMember,
                Date = DateTime.Today,
                Duration = TimeSpan.FromMinutes(30),
                Status = MeetingStatusEnum.Scheduled
            };

            // Act
            var results = ValidationHelper.Validate(oneOnOne);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void ValidateOneOnOne_WithoutTeamMember_ShouldReturnError()
        {
            // Arrange
            var oneOnOne = new OneOnOne
            {
                TeamMember = null!,
                Date = DateTime.Today,
                Duration = TimeSpan.FromMinutes(30)
            };

            // Act
            var results = ValidationHelper.Validate(oneOnOne);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Team member"));
        }

        [Fact]
        public void ValidateOneOnOne_WithZeroDuration_ShouldReturnError()
        {
            // Arrange
            var teamMember = new TeamMember { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "test@test.com" };
            var oneOnOne = new OneOnOne
            {
                TeamMember = teamMember,
                Date = DateTime.Today,
                Duration = TimeSpan.Zero
            };

            // Act
            var results = ValidationHelper.Validate(oneOnOne);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Duration"));
        }

        [Fact]
        public void ValidateOneOnOne_WithExcessiveDuration_ShouldReturnError()
        {
            // Arrange
            var teamMember = new TeamMember { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "test@test.com" };
            var oneOnOne = new OneOnOne
            {
                TeamMember = teamMember,
                Date = DateTime.Today,
                Duration = TimeSpan.FromHours(5) // 5 hours
            };

            // Act
            var results = ValidationHelper.Validate(oneOnOne);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("unreasonably long", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Task Validation Tests

        [Fact]
        public void ValidateTask_WithValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var task = new IndividualTask
            {
                Description = "Complete the feature",
                DueDate = DateTime.Today.AddDays(7),
                CreatedAt = DateTime.Today,
                IsCompleted = false
            };

            // Act
            var results = ValidationHelper.Validate(task);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void ValidateTask_WithEmptyDescription_ShouldReturnError()
        {
            // Arrange
            var task = new IndividualTask
            {
                Description = "",
                DueDate = DateTime.Today.AddDays(7),
                CreatedAt = DateTime.Today
            };

            // Act
            var results = ValidationHelper.Validate(task);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Description"));
        }

        [Fact]
        public void ValidateTask_WithPastDueDateAndNotCompleted_ShouldReturnError()
        {
            // Arrange
            var task = new IndividualTask
            {
                Description = "Overdue task",
                DueDate = DateTime.Today.AddDays(-7),
                CreatedAt = DateTime.Today.AddDays(-10),
                IsCompleted = false
            };

            // Act
            var results = ValidationHelper.Validate(task);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("past"));
        }

        [Fact]
        public void ValidateTask_WithPastDueDateButCompleted_ShouldNotReturnDueDateError()
        {
            // Arrange
            var task = new IndividualTask
            {
                Description = "Completed task",
                DueDate = DateTime.Today.AddDays(-7),
                CreatedAt = DateTime.Today.AddDays(-10),
                IsCompleted = true
            };

            // Act
            var results = ValidationHelper.Validate(task);

            // Assert
            results.Should().NotContain(r => r.ErrorMessage!.Contains("past"));
        }

        #endregion

        #region Project Validation Tests

        [Fact]
        public void ValidateProject_WithValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var project = new Project
            {
                Name = "New Product Launch",
                Description = "Launch the new product",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3),
                Status = "In Progress"
            };

            // Act
            var results = ValidationHelper.Validate(project);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void ValidateProject_WithEmptyName_ShouldReturnError()
        {
            // Arrange
            var project = new Project
            {
                Name = "",
                StartDate = DateTime.Today
            };

            // Act
            var results = ValidationHelper.Validate(project);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("name", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ValidateProject_WithEndDateBeforeStartDate_ShouldReturnError()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(-10)
            };

            // Act
            var results = ValidationHelper.Validate(project);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("End date"));
        }

        [Fact]
        public void ValidateProject_WithNoEndDate_ShouldReturnNoDateError()
        {
            // Arrange
            var project = new Project
            {
                Name = "Ongoing Project",
                StartDate = DateTime.Today,
                EndDate = null
            };

            // Act
            var results = ValidationHelper.Validate(project);

            // Assert
            results.Should().NotContain(r => r.ErrorMessage!.Contains("End date"));
        }

        #endregion

        #region Feedback Validation Tests

        [Fact]
        public void ValidateFeedback_WithValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var feedback = new Feedback
            {
                Title = "Great work on the presentation",
                Content = "The presentation was clear and well-organized.",
                TeamMemberId = Guid.NewGuid(),
                Date = DateTime.Today,
                Type = FeedbackType.Positive
            };

            // Act
            var results = ValidationHelper.Validate(feedback);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void ValidateFeedback_WithEmptyTitle_ShouldReturnError()
        {
            // Arrange
            var feedback = new Feedback
            {
                Title = "",
                Content = "Some content",
                TeamMemberId = Guid.NewGuid()
            };

            // Act
            var results = ValidationHelper.Validate(feedback);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Title"));
        }

        [Fact]
        public void ValidateFeedback_WithEmptyContent_ShouldReturnError()
        {
            // Arrange
            var feedback = new Feedback
            {
                Title = "Feedback Title",
                Content = "",
                TeamMemberId = Guid.NewGuid()
            };

            // Act
            var results = ValidationHelper.Validate(feedback);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Content"));
        }

        [Fact]
        public void ValidateFeedback_WithInvalidTeamMemberId_ShouldReturnError()
        {
            // Arrange
            var feedback = new Feedback
            {
                Title = "Feedback Title",
                Content = "Some content",
                TeamMemberId = Guid.Empty
            };

            // Act
            var results = ValidationHelper.Validate(feedback);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Team member"));
        }

        #endregion

        #region Goal Validation Tests

        [Fact]
        public void ValidateGoal_WithValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var goal = new DevelopmentGoal
            {
                Title = "Learn Kubernetes",
                Description = "Complete K8s certification",
                TeamMemberId = Guid.NewGuid(),
                ProgressPercent = 50,
                Category = DevelopmentGoalCategory.Certification,
                Status = GoalStatus.InProgress
            };

            // Act
            var results = ValidationHelper.Validate(goal);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void ValidateGoal_WithEmptyTitle_ShouldReturnError()
        {
            // Arrange
            var goal = new DevelopmentGoal
            {
                Title = "",
                TeamMemberId = Guid.NewGuid()
            };

            // Act
            var results = ValidationHelper.Validate(goal);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Title"));
        }

        [Fact]
        public void ValidateGoal_WithInvalidTeamMemberId_ShouldReturnError()
        {
            // Arrange
            var goal = new DevelopmentGoal
            {
                Title = "Learn something",
                TeamMemberId = Guid.Empty
            };

            // Act
            var results = ValidationHelper.Validate(goal);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Team member"));
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(150)]
        [InlineData(-1)]
        [InlineData(101)]
        public void ValidateGoal_WithInvalidProgressPercent_ShouldReturnError(int invalidProgress)
        {
            // Arrange
            var goal = new DevelopmentGoal
            {
                Title = "Learn something",
                TeamMemberId = Guid.NewGuid(),
                ProgressPercent = invalidProgress
            };

            // Act
            var results = ValidationHelper.Validate(goal);

            // Assert
            results.Should().Contain(r => r.ErrorMessage!.Contains("Progress"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public void ValidateGoal_WithValidProgressPercent_ShouldNotReturnProgressError(int validProgress)
        {
            // Arrange
            var goal = new DevelopmentGoal
            {
                Title = "Learn something",
                TeamMemberId = Guid.NewGuid(),
                ProgressPercent = validProgress
            };

            // Act
            var results = ValidationHelper.Validate(goal);

            // Assert
            results.Should().NotContain(r => r.ErrorMessage!.Contains("Progress"));
        }

        #endregion

        #region ValidateAndThrow Tests

        [Fact]
        public void ValidateAndThrow_WithValidEntity_ShouldNotThrow()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                HireDate = DateTime.Today.AddYears(-1)
            };

            // Act & Assert
            var action = () => ValidationHelper.ValidateAndThrow(teamMember);
            action.Should().NotThrow();
        }

        [Fact]
        public void ValidateAndThrow_WithInvalidEntity_ShouldThrowValidationException()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                FirstName = "",
                LastName = "",
                Email = "",
                HireDate = DateTime.Today
            };

            // Act & Assert
            var action = () => ValidationHelper.ValidateAndThrow(teamMember);
            action.Should().Throw<ValidationException>()
                  .WithMessage("*Validation failed*");
        }

        [Fact]
        public void Validate_WithNullEntity_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => ValidationHelper.Validate<TeamMember>(null!);
            action.Should().Throw<ArgumentNullException>();
        }

        #endregion
    }
}

