using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Tests.Infrastructure;
using Xunit;

namespace Tracker.Tests.Database
{
    /// <summary>
    /// Comprehensive CRUD tests for all major entities.
    /// Each test creates its own unique data to avoid conflicts.
    /// </summary>
    [Collection("Database")]
    public class EntityCrudTests
    {
        private readonly DatabaseFixture _fixture;

        public EntityCrudTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        #region Project Tests

        [Fact]
        public async Task CanCreate_Project()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"proj_create_{uid}");
            var owner = await CreateTeamMember(context, user, $"ProjectCreate_{uid}", "Owner");

            var project = new Project
            {
                Name = $"Test Project {uid}",
                Description = "A test project",
                StartDate = DateTime.Today,
                Status = "In Progress",
                Owner = owner
            };

            // Act
            context.Projects.Add(project);
            context.Entry(project).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Assert
            project.ID.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CanRetrieve_ProjectWithOwner()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"proj_retrieve_{uid}");
            var owner = await CreateTeamMember(context, user, $"ProjRetrieve_{uid}", "Owner");

            var project = new Project
            {
                Name = $"Retrieve Test {uid}",
                Owner = owner,
                StartDate = DateTime.Today,
                Status = "Planning"
            };
            context.Projects.Add(project);
            context.Entry(project).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();
            var projectId = project.ID;

            // Act
            await using var context2 = _fixture.CreateContext();
            var retrieved = await context2.Projects
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.ID == projectId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Contain("Retrieve Test");
            retrieved.Owner.Should().NotBeNull();
        }

        [Fact]
        public async Task CanUpdate_Project()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"proj_update_{uid}");
            var owner = await CreateTeamMember(context, user, $"ProjUpdate_{uid}", "Owner");

            var project = new Project
            {
                Name = $"Original Name {uid}",
                Owner = owner,
                StartDate = DateTime.Today,
                Status = "Planning"
            };
            context.Projects.Add(project);
            context.Entry(project).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();
            var projectId = project.ID;

            // Act
            await using var context2 = _fixture.CreateContext();
            var toUpdate = await context2.Projects.FindAsync(projectId);
            toUpdate!.Name = $"Updated Name {uid}";
            toUpdate.Status = "In Progress";
            await context2.SaveChangesAsync();

            // Assert
            await using var context3 = _fixture.CreateContext();
            var updated = await context3.Projects.FindAsync(projectId);
            updated!.Name.Should().Contain("Updated Name");
            updated.Status.Should().Be("In Progress");
        }

        [Fact]
        public async Task CanDelete_Project()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"proj_delete_{uid}");
            var owner = await CreateTeamMember(context, user, $"ProjDelete_{uid}", "Owner");

            var project = new Project
            {
                Name = $"To Delete {uid}",
                StartDate = DateTime.Today
            };
            project.Owner = null!; // Clear default navigation property
            context.Projects.Add(project);
            context.Entry(project).Property("UserId").CurrentValue = user.Id;
            context.Entry(project).Property("OwnerId").CurrentValue = owner.Id;
            await context.SaveChangesAsync();
            var projectId = project.ID;

            // Act
            await using var context2 = _fixture.CreateContext();
            var toDelete = await context2.Projects.FindAsync(projectId);
            context2.Projects.Remove(toDelete!);
            await context2.SaveChangesAsync();

            // Assert
            await using var context3 = _fixture.CreateContext();
            var deleted = await context3.Projects.FindAsync(projectId);
            deleted.Should().BeNull();
        }

        #endregion

        #region Task Tests

        [Fact]
        public async Task CanCreate_Task()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"task_create_{uid}");
            var owner = await CreateTeamMember(context, user, $"TaskCreate_{uid}", "Owner");

            var task = new IndividualTask
            {
                Description = $"Test Task {uid}",
                DueDate = DateTime.Today.AddDays(7),
                Owner = owner,
                IsCompleted = false
            };

            // Act
            context.Tasks.Add(task);
            context.Entry(task).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Assert
            task.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CanUpdate_TaskCompletion()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"task_complete_{uid}");
            var owner = await CreateTeamMember(context, user, $"TaskComplete_{uid}", "Owner");

            var task = new IndividualTask
            {
                Description = $"Complete Me {uid}",
                DueDate = DateTime.Today.AddDays(1),
                IsCompleted = false,
                Owner = owner
            };
            context.Tasks.Add(task);
            context.Entry(task).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();
            var taskId = task.Id;

            // Act
            await using var context2 = _fixture.CreateContext();
            var toUpdate = await context2.Tasks.FindAsync(taskId);
            toUpdate!.IsCompleted = true;
            await context2.SaveChangesAsync();

            // Assert
            await using var context3 = _fixture.CreateContext();
            var updated = await context3.Tasks.FindAsync(taskId);
            updated!.IsCompleted.Should().BeTrue();
            updated.Status.Should().Be("Completed");
        }

        #endregion

        #region OKR Tests

        [Fact]
        public async Task CanCreate_Okr()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"okr_create_{uid}");
            var owner = await CreateTeamMember(context, user, $"OKRCreate_{uid}", "Owner");

            var okr = new ObjectiveKeyResult
            {
                Title = $"Improve Code Quality {uid}",
                Description = "Reduce bugs by 50%",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3)
            };
            // Clear default navigation properties to avoid EF tracking them
            okr.Owner = null!;
            okr.KeyResults = null!;

            // Act - Set shadow properties for FK relationships
            context.ObjectiveKeyResults.Add(okr);
            context.Entry(okr).Property("UserId").CurrentValue = user.Id;
            context.Entry(okr).Property("OwnerId").CurrentValue = owner.Id;
            await context.SaveChangesAsync();

            // Assert
            okr.ObjectiveId.Should().BeGreaterThan(0);
        }

        #endregion

        #region KPI Tests

        [Fact]
        public async Task CanCreate_StandaloneKpi()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"kpi_standalone_{uid}");
            var owner = await CreateTeamMember(context, user, $"KPIStandalone_{uid}", "Owner");

            var kpi = new KeyPerformanceIndicator
            {
                Name = $"Customer Satisfaction {uid}",
                Description = "CSAT Score",
                TargetValue = 90,
                Value = 85,
                Owner = owner
            };

            // Act
            context.KeyPerformanceIndicators.Add(kpi);
            context.Entry(kpi).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Assert
            kpi.KpiId.Should().BeGreaterThan(0);
        }

        // Note: KPI-to-OKR direct linking was removed. 
        // KPIs are now linked to OKRs via KeyResultMeasurable entities.

        #endregion

        #region Feedback Tests

        [Fact]
        public async Task CanCreate_Feedback()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"feedback_create_{uid}");
            var teamMember = await CreateTeamMember(context, user, $"FeedbackRecipient_{uid}", "Member");

            var feedback = new Feedback
            {
                Title = $"Great work {uid}",
                Content = "The implementation was clean and well-tested",
                TeamMemberId = teamMember.Id, // Use FK directly, not navigation property
                Type = FeedbackType.Positive,
                Date = DateTime.Today
            };
            // Clear the default TeamMember navigation to avoid EF inserting it
            feedback.TeamMember = null!;

            // Act
            context.Feedbacks.Add(feedback);
            context.Entry(feedback).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Assert
            feedback.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CanRetrieve_FeedbackByType()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"feedback_type_{uid}");
            var teamMember = await CreateTeamMember(context, user, $"FeedbackType_{uid}", "Member");

            var positive = new Feedback
            {
                Title = $"Positive Feedback {uid}",
                Content = "Great job",
                TeamMemberId = teamMember.Id,
                Type = FeedbackType.Positive,
                Date = DateTime.Today
            };
            positive.TeamMember = null!; // Clear default navigation property
            
            var constructive = new Feedback
            {
                Title = $"Constructive Feedback {uid}",
                Content = "Could improve here",
                TeamMemberId = teamMember.Id,
                Type = FeedbackType.Constructive,
                Date = DateTime.Today
            };
            constructive.TeamMember = null!; // Clear default navigation property

            context.Feedbacks.AddRange(positive, constructive);
            context.Entry(positive).Property("UserId").CurrentValue = user.Id;
            context.Entry(constructive).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Act
            await using var context2 = _fixture.CreateContext();
            var positiveFeedback = await context2.Feedbacks
                .Where(f => f.Type == FeedbackType.Positive && f.Title.Contains(uid))
                .ToListAsync();

            // Assert
            positiveFeedback.Should().HaveCount(1);
        }

        #endregion

        #region Goal Tests

        [Fact]
        public async Task CanCreate_Goal()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"goal_create_{uid}");
            var teamMember = await CreateTeamMember(context, user, $"GoalCreate_{uid}", "Member");

            var goal = new DevelopmentGoal
            {
                Title = $"Learn Kubernetes {uid}",
                Description = "Complete K8s certification",
                TeamMemberId = teamMember.Id,
                Category = DevelopmentGoalCategory.Certification,
                Status = GoalStatus.InProgress,
                ProgressPercent = 25
            };
            goal.TeamMember = null!; // Clear default navigation property

            // Act
            context.DevelopmentGoals.Add(goal);
            context.Entry(goal).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Assert
            goal.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CanUpdate_GoalProgress()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"goal_progress_{uid}");
            var teamMember = await CreateTeamMember(context, user, $"GoalProgress_{uid}", "Member");

            var goal = new DevelopmentGoal
            {
                Title = $"Progress Test {uid}",
                TeamMemberId = teamMember.Id,
                Status = GoalStatus.InProgress,
                ProgressPercent = 0
            };
            goal.TeamMember = null!; // Clear default navigation property
            context.DevelopmentGoals.Add(goal);
            context.Entry(goal).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();
            var goalId = goal.Id;

            // Act
            await using var context2 = _fixture.CreateContext();
            var toUpdate = await context2.DevelopmentGoals.FindAsync(goalId);
            toUpdate!.ProgressPercent = 75;
            await context2.SaveChangesAsync();

            // Assert
            await using var context3 = _fixture.CreateContext();
            var updated = await context3.DevelopmentGoals.FindAsync(goalId);
            updated!.ProgressPercent.Should().Be(75);
        }

        #endregion

        #region MeetingTemplate Tests

        [Fact]
        public async Task CanCreate_MeetingTemplate()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"template_create_{uid}");

            var template = new MeetingTemplate
            {
                Name = $"Weekly Check-in {uid}",
                Description = "Standard weekly 1:1 template",
                SuggestedDurationMinutes = 30,
                IsSystemTemplate = false
            };

            // Act
            context.MeetingTemplates.Add(template);
            context.Entry(template).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Assert
            template.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CanCreate_MeetingTemplateWithItems()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"template_items_{uid}");

            // Create template without items first
            var template = new MeetingTemplate
            {
                Name = $"Performance Review {uid}",
                SuggestedDurationMinutes = 60,
                Items = new List<MeetingTemplateItem>() // Empty initially
            };

            context.MeetingTemplates.Add(template);
            context.Entry(template).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Now add items with proper FK and UserId
            var item1 = new MeetingTemplateItem
            {
                MeetingTemplateId = template.Id,
                Description = "Review recent accomplishments",
                Category = AgendaItemCategory.Update,
                Priority = Severity.High,
                SortOrder = 1
            };
            var item2 = new MeetingTemplateItem
            {
                MeetingTemplateId = template.Id,
                Description = "Discuss growth areas",
                Category = AgendaItemCategory.CareerDevelopment,
                Priority = Severity.Medium,
                SortOrder = 2
            };

            context.MeetingTemplateItems.AddRange(item1, item2);
            context.Entry(item1).Property("UserId").CurrentValue = user.Id;
            context.Entry(item2).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var retrieved = await context2.MeetingTemplates
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == template.Id);

            retrieved!.Items.Should().HaveCount(2);
        }

        #endregion

        #region Relationship Tests

        [Fact]
        public async Task CanRetrieve_OneOnOnes_ForTeamMember()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            var user = await CreateTestUser(context, $"meeting_rel_{uid}");
            var teamMember = await CreateTeamMember(context, user, $"MeetingRel_{uid}", "Member");

            var meeting1 = new OneOnOne
            {
                TeamMember = teamMember,
                Date = DateTime.Today.AddDays(-7),
                Duration = TimeSpan.FromMinutes(30),
                Status = MeetingStatusEnum.Completed
            };
            var meeting2 = new OneOnOne
            {
                TeamMember = teamMember,
                Date = DateTime.Today,
                Duration = TimeSpan.FromMinutes(30),
                Status = MeetingStatusEnum.Scheduled
            };

            context.OneOnOnes.AddRange(meeting1, meeting2);
            context.Entry(meeting1).Property("UserId").CurrentValue = user.Id;
            context.Entry(meeting2).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Act - Query meetings by team member
            await using var context2 = _fixture.CreateContext();
            var meetings = await context2.OneOnOnes
                .Include(o => o.TeamMember)
                .Where(o => o.TeamMember.Id == teamMember.Id)
                .ToListAsync();

            // Assert
            meetings.Should().HaveCount(2);
        }

        #endregion

        #region Helper Methods

        private async Task<User> CreateTestUser(TrackerDbContext context, string uniqueId)
        {
            var user = new User
            {
                Username = uniqueId,
                Email = $"{uniqueId}@test.com",
                DisplayName = "Test User",
                IsActive = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        private async Task<TeamMember> CreateTeamMember(TrackerDbContext context, User user, string firstName, string lastName)
        {
            var teamMember = new TeamMember
            {
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
                HireDate = DateTime.Today.AddYears(-1),
                Role = RoleEnum.Engineer,
                JobTitle = "Software Engineer",
                IsActive = true
            };

            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            return teamMember;
        }

        #endregion
    }
}
