using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Integration
{
    [Collection("Database")]
    public class TeamMemberIntegrationTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public TeamMemberIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task TeamMember_CanHaveMultipleOneOnOnes()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember(firstName: "John", lastName: "Doe");
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var oneOnOne1 = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today);
            var oneOnOne2 = TestDataBuilder.CreateOneOnOne(teamMember, date: DateTime.Today.AddDays(7));
            
            context.OneOnOnes.Add(oneOnOne1);
            context.OneOnOnes.Add(oneOnOne2);
            context.Entry(oneOnOne1).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(oneOnOne2).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var meetings = await context.OneOnOnes
                .Where(o => o.TeamMember.Id == teamMember.Id)
                .ToListAsync();

            meetings.Should().HaveCount(2);
        }

        [Fact]
        public async Task TeamMember_CanHaveMultipleTasks()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var task1 = TestDataBuilder.CreateTask(teamMember, description: "Task 1");
            var task2 = TestDataBuilder.CreateTask(teamMember, description: "Task 2");
            var task3 = TestDataBuilder.CreateTask(teamMember, description: "Task 3");
            
            context.Tasks.AddRange(task1, task2, task3);
            context.Entry(task1).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(task2).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(task3).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var tasks = await context.Tasks
                .Where(t => t.Owner.Id == teamMember.Id)
                .ToListAsync();

            tasks.Should().HaveCount(3);
        }

        [Fact]
        public async Task TeamMember_CanHaveFeedbackHistory()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var feedback1 = TestDataBuilder.CreateFeedback(teamMember, FeedbackType.Positive, "Great work");
            var feedback2 = TestDataBuilder.CreateFeedback(teamMember, FeedbackType.Constructive, "Improve communication");
            var feedback3 = TestDataBuilder.CreateFeedback(teamMember, FeedbackType.Recognition, "Above and beyond");
            
            context.Feedbacks.AddRange(feedback1, feedback2, feedback3);
            context.Entry(feedback1).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(feedback2).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(feedback3).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var feedbackHistory = await context.Feedbacks
                .Where(f => f.TeamMember.Id == teamMember.Id)
                .OrderByDescending(f => f.Date)
                .ToListAsync();

            feedbackHistory.Should().HaveCount(3);
        }

        [Fact]
        public async Task TeamMember_CanHaveGoals()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var goal1 = TestDataBuilder.CreateGoal(teamMember, title: "Learn Kubernetes", category: GoalCategory.Technical);
            var goal2 = TestDataBuilder.CreateGoal(teamMember, title: "Get AWS Certification", category: GoalCategory.Certification);
            
            context.IndividualGoals.AddRange(goal1, goal2);
            context.Entry(goal1).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(goal2).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var goals = await context.IndividualGoals
                .Where(g => g.TeamMember.Id == teamMember.Id)
                .ToListAsync();

            goals.Should().HaveCount(2);
        }

        [Fact]
        public async Task TeamMember_CanOwnProjects()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var project1 = TestDataBuilder.CreateProject(teamMember, name: "Project Alpha");
            var project2 = TestDataBuilder.CreateProject(teamMember, name: "Project Beta");
            
            context.Projects.AddRange(project1, project2);
            context.Entry(project1).Property("UserId").CurrentValue = _fixture.TestUserId;
            context.Entry(project2).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var projects = await context.Projects
                .Where(p => p.Owner.Id == teamMember.Id)
                .ToListAsync();

            projects.Should().HaveCount(2);
        }

        [Fact]
        public async Task TeamMember_SoftDelete_ShouldNotDeleteRelatedEntities()
        {
            using var context = _fixture.CreateContext();
            
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var feedback = TestDataBuilder.CreateFeedback(teamMember);
            context.Feedbacks.Add(feedback);
            context.Entry(feedback).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Soft delete team member
            teamMember.IsDeleted = true;
            await context.SaveChangesAsync();

            // Feedback should still exist
            var feedbackExists = await context.Feedbacks.AnyAsync(f => f.Id == feedback.Id);
            feedbackExists.Should().BeTrue();
        }
    }
}

