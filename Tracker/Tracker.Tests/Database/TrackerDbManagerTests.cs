using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Database
{
    [Collection("Database")]
    public class TrackerDbManagerTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public TrackerDbManagerTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        #region TeamMember CRUD Tests

        [Fact]
        public async Task AddTeamMember_ShouldPersistToDatabase()
        {
            using var context = _fixture.CreateContext();
            var teamMember = TestDataBuilder.CreateTeamMember(firstName: "Test", lastName: "User");
            
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.TeamMembers.FindAsync(teamMember.Id);
            retrieved.Should().NotBeNull();
            retrieved!.FirstName.Should().Be("Test");
        }

        [Fact]
        public async Task UpdateTeamMember_ShouldPersistChanges()
        {
            using var context = _fixture.CreateContext();
            var teamMember = TestDataBuilder.CreateTeamMember(firstName: "Original");
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            teamMember.FirstName = "Updated";
            await context.SaveChangesAsync();

            using var verifyContext = _fixture.CreateContext();
            var retrieved = await verifyContext.TeamMembers.FindAsync(teamMember.Id);
            retrieved!.FirstName.Should().Be("Updated");
        }

        [Fact]
        public async Task DeleteTeamMember_ShouldSoftDelete()
        {
            using var context = _fixture.CreateContext();
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            teamMember.IsDeleted = true;
            await context.SaveChangesAsync();

            var retrieved = await context.TeamMembers.FindAsync(teamMember.Id);
            retrieved!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task GetTeamMembers_ShouldFilterByUserId()
        {
            using var context = _fixture.CreateContext();
            
            var member1 = TestDataBuilder.CreateTeamMember(firstName: "User1");
            context.TeamMembers.Add(member1);
            context.Entry(member1).Property("UserId").CurrentValue = _fixture.TestUserId;
            
            var member2 = TestDataBuilder.CreateTeamMember(firstName: "User2");
            context.TeamMembers.Add(member2);
            context.Entry(member2).Property("UserId").CurrentValue = 9999; // Different user
            
            await context.SaveChangesAsync();

            var results = await context.TeamMembers
                .Where(t => EF.Property<int>(t, "UserId") == _fixture.TestUserId)
                .ToListAsync();

            results.Should().HaveCount(1);
            results[0].FirstName.Should().Be("User1");
        }

        #endregion

        #region OneOnOne CRUD Tests

        [Fact]
        public async Task AddOneOnOne_ShouldPersistWithRelationships()
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

            var retrieved = await context.OneOnOnes
                .Include(o => o.TeamMember)
                .FirstOrDefaultAsync(o => o.Id == oneOnOne.Id);

            retrieved.Should().NotBeNull();
            retrieved!.TeamMember.Should().NotBeNull();
        }

        [Fact]
        public async Task AddOneOnOne_WithAgendaItems_ShouldPersist()
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
            
            context.OneOnOnes.Add(oneOnOne);
            context.Entry(oneOnOne).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.OneOnOnes
                .Include(o => o.AgendaItems)
                .FirstOrDefaultAsync(o => o.Id == oneOnOne.Id);

            retrieved!.AgendaItems.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddOneOnOne_WithTasks_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var oneOnOne = TestDataBuilder.CreateOneOnOne(teamMember);
            oneOnOne.Tasks.Add(new MeetingTask 
            { 
                Description = "Follow up on action item",
                DueDate = DateTime.Today.AddDays(7),
                IsCompleted = false
            });
            
            context.OneOnOnes.Add(oneOnOne);
            context.Entry(oneOnOne).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.OneOnOnes
                .Include(o => o.Tasks)
                .FirstOrDefaultAsync(o => o.Id == oneOnOne.Id);

            retrieved!.Tasks.Should().HaveCount(1);
        }

        #endregion

        #region Task CRUD Tests

        [Fact]
        public async Task AddTask_ShouldPersistWithOwner()
        {
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var task = TestDataBuilder.CreateTask(owner, description: "Important task");
            context.Tasks.Add(task);
            context.Entry(task).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.Tasks
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Description.Should().Be("Important task");
            retrieved.Owner.Should().NotBeNull();
        }

        [Fact]
        public async Task CompleteTask_ShouldUpdateIsCompleted()
        {
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var task = TestDataBuilder.CreateTask(owner, isCompleted: false);
            context.Tasks.Add(task);
            context.Entry(task).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            task.IsCompleted = true;
            await context.SaveChangesAsync();

            using var verifyContext = _fixture.CreateContext();
            var retrieved = await verifyContext.Tasks.FindAsync(task.Id);
            retrieved!.IsCompleted.Should().BeTrue();
        }

        #endregion

        #region Project CRUD Tests

        [Fact]
        public async Task AddProject_WithMilestones_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var project = TestDataBuilder.CreateProject(owner, name: "Big Project");
            project.Milestones.Add(new Milestone 
            { 
                Name = "Phase 1",
                TargetDate = DateTime.Today.AddMonths(1)
            });
            
            context.Projects.Add(project);
            context.Entry(project).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.Projects
                .Include(p => p.Milestones)
                .FirstOrDefaultAsync(p => p.Id == project.Id);

            retrieved!.Name.Should().Be("Big Project");
            retrieved.Milestones.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddProject_WithRisks_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var project = TestDataBuilder.CreateProject(owner);
            project.Risks.Add(new Risk 
            { 
                Name = "Resource shortage",
                Severity = RiskLevelEnum.High,
                MitigationStrategy = "Hire contractors"
            });
            
            context.Projects.Add(project);
            context.Entry(project).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.Projects
                .Include(p => p.Risks)
                .FirstOrDefaultAsync(p => p.Id == project.Id);

            retrieved!.Risks.Should().HaveCount(1);
            retrieved.Risks[0].Severity.Should().Be(RiskLevelEnum.High);
        }

        #endregion

        #region Feedback CRUD Tests

        [Fact]
        public async Task AddFeedback_ShouldPersistWithTeamMember()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var feedback = TestDataBuilder.CreateFeedback(teamMember, FeedbackType.Recognition);
            context.Feedbacks.Add(feedback);
            context.Entry(feedback).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.Feedbacks
                .Include(f => f.TeamMember)
                .FirstOrDefaultAsync(f => f.Id == feedback.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Type.Should().Be(FeedbackType.Recognition);
        }

        #endregion

        #region Goal CRUD Tests

        [Fact]
        public async Task AddGoal_WithMilestones_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var goal = TestDataBuilder.CreateGoal(teamMember);
            goal.Milestones.Add(new GoalMilestone 
            { 
                Description = "Complete training",
                IsCompleted = false,
                SortOrder = 1
            });
            
            context.IndividualGoals.Add(goal);
            context.Entry(goal).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.IndividualGoals
                .Include(g => g.Milestones)
                .FirstOrDefaultAsync(g => g.Id == goal.Id);

            retrieved!.Milestones.Should().HaveCount(1);
        }

        [Fact]
        public async Task UpdateGoalProgress_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var goal = TestDataBuilder.CreateGoal(teamMember);
            goal.ProgressPercent = 25;
            context.IndividualGoals.Add(goal);
            context.Entry(goal).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            goal.ProgressPercent = 75;
            await context.SaveChangesAsync();

            using var verifyContext = _fixture.CreateContext();
            var retrieved = await verifyContext.IndividualGoals.FindAsync(goal.Id);
            retrieved!.ProgressPercent.Should().Be(75);
        }

        #endregion

        #region OKR/KPI Tests

        [Fact]
        public async Task AddOkr_WithKeyResults_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var okr = TestDataBuilder.CreateOkr(owner, title: "Q1 Objectives");
            context.OKRs.Add(okr);
            context.Entry(okr).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var kpi = TestDataBuilder.CreateKpi(owner, name: "Revenue Growth");
            // KPIs no longer directly link to OKRs - they're linked via KeyResultMeasurable
            context.KPIs.Add(kpi);
            context.Entry(kpi).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.OKRs
                .Include(o => o.KeyResults)
                .FirstOrDefaultAsync(o => o.ID == okr.ID);

            retrieved!.Title.Should().Be("Q1 Objectives");
            retrieved.KeyResults.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddKpi_Standalone_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var owner = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var kpi = TestDataBuilder.CreateKpi(owner, name: "Customer Satisfaction");
            // KPIs are standalone by default - they link to OKRs via KeyResultMeasurable
            context.KPIs.Add(kpi);
            context.Entry(kpi).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.KPIs.FindAsync(kpi.KpiId);
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("Customer Satisfaction");
        }

        #endregion

        #region Reminder Tests

        [Fact]
        public async Task AddReminder_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var reminder = TestDataBuilder.CreateReminder("Test Reminder");
            context.Reminders.Add(reminder);
            context.Entry(reminder).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.Reminders.FindAsync(reminder.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be("Test Reminder");
        }

        [Fact]
        public async Task DismissReminder_ShouldUpdateStatus()
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

        #endregion

        #region QuickNote Tests

        [Fact]
        public async Task AddQuickNote_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var note = TestDataBuilder.CreateQuickNote("Remember this");
            context.QuickNotes.Add(note);
            context.Entry(note).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.QuickNotes.FindAsync(note.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Content.Should().Be("Remember this");
        }

        [Fact]
        public async Task PinQuickNote_ShouldUpdateIsPinned()
        {
            using var context = _fixture.CreateContext();
            
            var note = TestDataBuilder.CreateQuickNote();
            note.IsPinned = false;
            context.QuickNotes.Add(note);
            context.Entry(note).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            note.IsPinned = true;
            await context.SaveChangesAsync();

            using var verifyContext = _fixture.CreateContext();
            var retrieved = await verifyContext.QuickNotes.FindAsync(note.Id);
            retrieved!.IsPinned.Should().BeTrue();
        }

        #endregion

        #region MeetingTemplate Tests

        [Fact]
        public async Task AddMeetingTemplate_WithItems_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var template = TestDataBuilder.CreateMeetingTemplate("Weekly Standup");
            template.Items.Add(new MeetingTemplateItem 
            { 
                Description = "Review blockers",
                Category = AgendaItemCategory.Blocker,
                Priority = 1
            });
            
            context.MeetingTemplates.Add(template);
            context.Entry(template).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.MeetingTemplates
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == template.Id);

            retrieved!.Name.Should().Be("Weekly Standup");
            retrieved.Items.Should().HaveCount(1);
        }

        #endregion
    }
}

