using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Database
{
    [Collection("Database")]
    public class TrackerDbContextTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public TrackerDbContextTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CanCreateAndRetrieveTeamMember()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var teamMember = TestDataBuilder.CreateTeamMember(
                firstName: "John",
                lastName: "Doe"
            );

            // Set the UserId shadow property
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.TeamMembers.FindAsync(teamMember.Id);
            retrieved.Should().NotBeNull();
            retrieved!.FirstName.Should().Be("John");
            retrieved.LastName.Should().Be("Doe");
        }

        [Fact]
        public async Task CanCreateOneOnOneWithTeamMember()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var oneOnOne = TestDataBuilder.CreateOneOnOne(teamMember);
            context.OneOnOnes.Add(oneOnOne);
            context.Entry(oneOnOne).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.OneOnOnes
                .Include(o => o.TeamMember)
                .FirstOrDefaultAsync(o => o.Id == oneOnOne.Id);
            
            retrieved.Should().NotBeNull();
            retrieved!.TeamMember.Should().NotBeNull();
            retrieved.TeamMember.Id.Should().Be(teamMember.Id);
        }

        [Fact]
        public async Task CanCreateTaskWithOwner()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var task = TestDataBuilder.CreateTask(owner);
            context.Tasks.Add(task);
            context.Entry(task).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.Tasks
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == task.Id);
            
            retrieved.Should().NotBeNull();
            retrieved!.Owner.Should().NotBeNull();
        }

        [Fact]
        public async Task CanCreateProjectWithOwnerAndRelatedEntities()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var project = TestDataBuilder.CreateProject(owner);
            project.Milestones.Add(new Milestone
            {
                Name = "Phase 1",
                Description = "Initial phase",
                TargetDate = DateTime.Today.AddMonths(1)
            });
            
            context.Projects.Add(project);
            context.Entry(project).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Milestones)
                .FirstOrDefaultAsync(p => p.Id == project.Id);
            
            retrieved.Should().NotBeNull();
            retrieved!.Owner.Should().NotBeNull();
            retrieved.Milestones.Should().HaveCount(1);
        }

        [Fact]
        public async Task CanCreateOkrWithKeyResults()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var okr = TestDataBuilder.CreateOkr(owner);
            context.OKRs.Add(okr);
            context.Entry(okr).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var kpi = TestDataBuilder.CreateKpi(owner);
            // KPIs link to OKRs via KeyResultMeasurable, not directly
            context.KPIs.Add(kpi);
            context.Entry(kpi).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert - KPI should be persisted
            var retrievedKpi = await context.KPIs.FindAsync(kpi.KpiId);
            retrievedKpi.Should().NotBeNull();
            retrievedKpi!.Name.Should().Be(kpi.Name);
        }

        [Fact]
        public async Task CanCreateFeedbackForTeamMember()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var feedback = TestDataBuilder.CreateFeedback(teamMember, FeedbackType.Positive);
            context.Feedbacks.Add(feedback);
            context.Entry(feedback).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.Feedbacks
                .Include(f => f.TeamMember)
                .FirstOrDefaultAsync(f => f.Id == feedback.Id);
            
            retrieved.Should().NotBeNull();
            retrieved!.Type.Should().Be(FeedbackType.Positive);
            retrieved.TeamMember.Id.Should().Be(teamMember.Id);
        }

        [Fact]
        public async Task CanCreateGoalWithMilestones()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var goal = TestDataBuilder.CreateGoal(teamMember);
            goal.Milestones.Add(new GoalMilestone
            {
                Description = "Complete certification",
                IsCompleted = false,
                SortOrder = 1
            });
            
            context.IndividualGoals.Add(goal);
            context.Entry(goal).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.IndividualGoals
                .Include(g => g.Milestones)
                .FirstOrDefaultAsync(g => g.Id == goal.Id);
            
            retrieved.Should().NotBeNull();
            retrieved!.Milestones.Should().HaveCount(1);
        }

        [Fact]
        public async Task CanCreateReminder()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var reminder = TestDataBuilder.CreateReminder(
                title: "Test Reminder",
                dueDateTime: DateTime.Now.AddHours(1)
            );
            
            context.Reminders.Add(reminder);
            context.Entry(reminder).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.Reminders.FindAsync(reminder.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be("Test Reminder");
            retrieved.Status.Should().Be(ReminderStatus.Pending);
        }

        [Fact]
        public async Task CanCreateQuickNote()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var note = TestDataBuilder.CreateQuickNote(
                content: "Important note",
                category: NoteCategory.Meeting
            );
            
            context.QuickNotes.Add(note);
            context.Entry(note).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.QuickNotes.FindAsync(note.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Content.Should().Be("Important note");
            retrieved.Category.Should().Be(NoteCategory.Meeting);
        }

        [Fact]
        public async Task CanCreateMeetingTemplate()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var template = TestDataBuilder.CreateMeetingTemplate(
                name: "Weekly Sync",
                duration: 30
            );
            template.Items.Add(new MeetingTemplateItem
            {
                Description = "Review previous action items",
                Category = AgendaItemCategory.Review,
                Priority = 1
            });
            
            context.MeetingTemplates.Add(template);
            context.Entry(template).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.MeetingTemplates
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == template.Id);
            
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("Weekly Sync");
            retrieved.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task SoftDelete_ShouldSetIsDeletedFlag()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Act
            teamMember.IsDeleted = true;
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.TeamMembers.FindAsync(teamMember.Id);
            retrieved.Should().NotBeNull();
            retrieved!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task AuditableEntity_ShouldTrackCreatedAt()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var beforeCreate = DateTime.UtcNow.AddSeconds(-1);
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;

            // Act
            await context.SaveChangesAsync();

            // Assert
            teamMember.CreatedAt.Should().BeAfter(beforeCreate);
        }
    }
}

