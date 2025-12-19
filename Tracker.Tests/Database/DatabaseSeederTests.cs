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
    /// Tests for database seeding functionality patterns.
    /// Each test creates unique data to avoid conflicts.
    /// </summary>
    [Collection("Database")]
    public class DatabaseSeederTests
    {
        private readonly DatabaseFixture _fixture;

        public DatabaseSeederTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CanSeed_UserAndTeamMembers()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_tm_{uid}");

            // Act - Seed team members
            var teamMembers = new[]
            {
                new TeamMember
                {
                    FirstName = $"Alice_{uid}",
                    LastName = "Smith",
                    Email = $"alice_{uid}@test.com",
                    HireDate = DateTime.Today.AddYears(-2),
                    Role = RoleEnum.Engineer,
                    JobTitle = "Senior Developer",
                    IsActive = true
                },
                new TeamMember
                {
                    FirstName = $"Bob_{uid}",
                    LastName = "Jones",
                    Email = $"bob_{uid}@test.com",
                    HireDate = DateTime.Today.AddYears(-1),
                    Role = RoleEnum.Engineer,
                    JobTitle = "Developer",
                    IsActive = true
                }
            };

            context.TeamMembers.AddRange(teamMembers);
            foreach (var tm in teamMembers)
            {
                context.Entry(tm).Property("UserId").CurrentValue = user.Id;
            }
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededMembers = await context2.TeamMembers
                .Where(t => t.FirstName.Contains(uid))
                .ToListAsync();
            seededMembers.Should().HaveCount(2);
        }

        [Fact]
        public async Task CanSeed_ProjectsWithOwners()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_proj_{uid}");

            var owner = new TeamMember
            {
                FirstName = $"Project_{uid}",
                LastName = "Owner",
                Email = $"owner_{uid}@test.com",
                HireDate = DateTime.Today.AddYears(-1),
                Role = RoleEnum.Manager,
                IsActive = true
            };
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Act - Seed projects
            var projects = new[]
            {
                new Project
                {
                    Name = $"Mobile App {uid}",
                    Description = "New mobile application",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(6),
                    Status = "In Progress",
                    Owner = owner
                },
                new Project
                {
                    Name = $"API Redesign {uid}",
                    Description = "REST API overhaul",
                    StartDate = DateTime.Today.AddMonths(-1),
                    Status = "Planning",
                    Owner = owner
                }
            };

            context.Projects.AddRange(projects);
            foreach (var p in projects)
            {
                context.Entry(p).Property("UserId").CurrentValue = user.Id;
            }
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededProjects = await context2.Projects
                .Include(p => p.Owner)
                .Where(p => p.Name.Contains(uid))
                .ToListAsync();
            
            seededProjects.Should().HaveCount(2);
            seededProjects.Should().OnlyContain(p => p.Owner != null);
        }

        [Fact]
        public async Task CanSeed_OneOnOnesWithTeamMembers()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_mtg_{uid}");

            var teamMember = new TeamMember
            {
                FirstName = $"Meeting_{uid}",
                LastName = "Member",
                Email = $"member_{uid}@test.com",
                HireDate = DateTime.Today.AddYears(-1),
                Role = RoleEnum.Engineer,
                IsActive = true
            };
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Act - Seed 1:1 meetings
            var meetings = new[]
            {
                new OneOnOne
                {
                    TeamMember = teamMember,
                    Date = DateTime.Today.AddDays(-14),
                    Duration = TimeSpan.FromMinutes(30),
                    Status = MeetingStatusEnum.Completed,
                    Description = $"Bi-weekly sync {uid}"
                },
                new OneOnOne
                {
                    TeamMember = teamMember,
                    Date = DateTime.Today,
                    Duration = TimeSpan.FromMinutes(30),
                    Status = MeetingStatusEnum.Scheduled,
                    Description = $"Weekly check-in {uid}"
                },
                new OneOnOne
                {
                    TeamMember = teamMember,
                    Date = DateTime.Today.AddDays(7),
                    Duration = TimeSpan.FromMinutes(30),
                    Status = MeetingStatusEnum.Scheduled,
                    Description = $"Next week sync {uid}"
                }
            };

            context.OneOnOnes.AddRange(meetings);
            foreach (var m in meetings)
            {
                context.Entry(m).Property("UserId").CurrentValue = user.Id;
            }
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededMeetings = await context2.OneOnOnes
                .Include(o => o.TeamMember)
                .Where(o => o.Description.Contains(uid))
                .ToListAsync();
            
            seededMeetings.Should().HaveCount(3);
            seededMeetings.Should().OnlyContain(m => m.TeamMember != null);
        }

        [Fact]
        public async Task CanSeed_OkrsWithKeyResults()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_okr_{uid}");

            var owner = new TeamMember
            {
                FirstName = $"OKR_{uid}",
                LastName = "Owner",
                Email = $"okrowner_{uid}@test.com",
                HireDate = DateTime.Today.AddYears(-1),
                Role = RoleEnum.Manager,
                IsActive = true
            };
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Act - Seed OKR with linked KPIs
            var okr = new ObjectiveKeyResult
            {
                Title = $"Customer Satisfaction {uid}",
                Description = "Improve overall customer experience",
                Owner = owner,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3)
            };
            context.ObjectiveKeyResults.Add(okr);
            context.Entry(okr).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            var kpis = new[]
            {
                new KeyPerformanceIndicator
                {
                    Name = $"NPS Score {uid}",
                    Description = "Net Promoter Score",
                    TargetValue = 50,
                    Value = 35,
                    Owner = owner
                },
                new KeyPerformanceIndicator
                {
                    Name = $"Response Time {uid}",
                    Description = "Average support response time",
                    TargetValue = 2,
                    Value = 4,
                    Owner = owner
                }
            };

            context.KeyPerformanceIndicators.AddRange(kpis);
            foreach (var kpi in kpis)
            {
                context.Entry(kpi).Property("UserId").CurrentValue = user.Id;
            }
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededOkr = await context2.ObjectiveKeyResults
                .Include(o => o.KeyResults)
                .FirstOrDefaultAsync(o => o.Title.Contains(uid));
            
            seededOkr.Should().NotBeNull();
            seededOkr!.KeyResults.Should().HaveCount(2);
        }

        [Fact]
        public async Task CanSeed_Tasks()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_task_{uid}");

            var owner = new TeamMember
            {
                FirstName = $"Task_{uid}",
                LastName = "Owner",
                Email = $"taskowner_{uid}@test.com",
                HireDate = DateTime.Today.AddYears(-1),
                Role = RoleEnum.Engineer,
                IsActive = true
            };
            context.TeamMembers.Add(owner);
            context.Entry(owner).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Act - Seed tasks
            var tasks = new[]
            {
                new IndividualTask
                {
                    Description = $"Design database schema {uid}",
                    DueDate = DateTime.Today.AddDays(7),
                    Owner = owner,
                    IsCompleted = false
                },
                new IndividualTask
                {
                    Description = $"Implement API endpoints {uid}",
                    DueDate = DateTime.Today.AddDays(14),
                    Owner = owner,
                    IsCompleted = false
                },
                new IndividualTask
                {
                    Description = $"Write unit tests {uid}",
                    DueDate = DateTime.Today.AddDays(21),
                    Owner = owner,
                    IsCompleted = false
                }
            };

            context.Tasks.AddRange(tasks);
            foreach (var t in tasks)
            {
                context.Entry(t).Property("UserId").CurrentValue = user.Id;
            }
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededTasks = await context2.Tasks
                .Include(t => t.Owner)
                .Where(t => t.Description.Contains(uid))
                .ToListAsync();
            
            seededTasks.Should().HaveCount(3);
            seededTasks.Should().OnlyContain(t => t.Owner != null);
        }

        [Fact]
        public async Task CanSeed_FeedbackForTeamMembers()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_fb_{uid}");

            var teamMember = new TeamMember
            {
                FirstName = $"Feedback_{uid}",
                LastName = "Recipient",
                Email = $"recipient_{uid}@test.com",
                HireDate = DateTime.Today.AddYears(-1),
                Role = RoleEnum.Engineer,
                IsActive = true
            };
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Act - Seed feedback
            var feedbacks = new[]
            {
                new Feedback
                {
                    Title = $"Excellent presentation {uid}",
                    Content = "The quarterly review presentation was outstanding",
                    TeamMemberId = teamMember.Id,
                    Type = FeedbackType.Positive,
                    Date = DateTime.Today.AddDays(-30)
                },
                new Feedback
                {
                    Title = $"Communication improvement {uid}",
                    Content = "Could benefit from more proactive status updates",
                    TeamMemberId = teamMember.Id,
                    Type = FeedbackType.Constructive,
                    Date = DateTime.Today.AddDays(-14)
                },
                new Feedback
                {
                    Title = $"Team collaboration award {uid}",
                    Content = "Recognized for helping onboard new team members",
                    TeamMemberId = teamMember.Id,
                    Type = FeedbackType.Recognition,
                    Date = DateTime.Today
                }
            };

            context.Feedbacks.AddRange(feedbacks);
            foreach (var f in feedbacks)
            {
                context.Entry(f).Property("UserId").CurrentValue = user.Id;
            }
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededFeedback = await context2.Feedbacks
                .Where(f => f.Title.Contains(uid))
                .ToListAsync();
            
            seededFeedback.Should().HaveCount(3);
        }

        [Fact]
        public async Task CanSeed_GoalsWithMilestones()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_goal_{uid}");

            var teamMember = new TeamMember
            {
                FirstName = $"Goal_{uid}",
                LastName = "Setter",
                Email = $"setter_{uid}@test.com",
                HireDate = DateTime.Today.AddYears(-1),
                Role = RoleEnum.Engineer,
                IsActive = true
            };
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Act - Seed goal with milestones
            var goal = new IndividualGoal
            {
                Title = $"AWS Certification {uid}",
                Description = "Obtain AWS Solutions Architect certification",
                TeamMemberId = teamMember.Id,
                Category = GoalCategory.Certification,
                Status = GoalStatus.InProgress,
                ProgressPercent = 40,
                TargetDate = DateTime.Today.AddMonths(2)
            };
            context.IndividualGoals.Add(goal);
            context.Entry(goal).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            var milestones = new[]
            {
                new GoalMilestone
                {
                    GoalId = goal.Id,
                    Description = "Complete Cloud Practitioner",
                    IsCompleted = true,
                    CompletedDate = DateTime.Today.AddMonths(-1),
                    SortOrder = 1
                },
                new GoalMilestone
                {
                    GoalId = goal.Id,
                    Description = "Complete SAA-C03 course",
                    IsCompleted = true,
                    CompletedDate = DateTime.Today.AddDays(-7),
                    SortOrder = 2
                },
                new GoalMilestone
                {
                    GoalId = goal.Id,
                    Description = "Take practice exams",
                    IsCompleted = false,
                    SortOrder = 3
                },
                new GoalMilestone
                {
                    GoalId = goal.Id,
                    Description = "Schedule and pass exam",
                    IsCompleted = false,
                    SortOrder = 4
                }
            };

            context.GoalMilestones.AddRange(milestones);
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededGoal = await context2.IndividualGoals
                .Include(g => g.Milestones)
                .FirstOrDefaultAsync(g => g.Title.Contains(uid));
            
            seededGoal.Should().NotBeNull();
            seededGoal!.Milestones.Should().HaveCount(4);
            seededGoal.Milestones.Count(m => m.IsCompleted).Should().Be(2);
        }

        [Fact]
        public async Task CanSeed_Reminders()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_reminder_{uid}");

            // Act - Seed reminders
            var reminders = new[]
            {
                new Reminder
                {
                    Title = $"Weekly Team Meeting {uid}",
                    Message = "Prepare agenda for weekly sync",
                    Type = ReminderType.Meeting,
                    Status = ReminderStatus.Pending,
                    DueDateTime = DateTime.Today.AddDays(1).AddHours(9)
                },
                new Reminder
                {
                    Title = $"Project Deadline {uid}",
                    Message = "Phase 1 deliverable due",
                    Type = ReminderType.Task,
                    Status = ReminderStatus.Pending,
                    DueDateTime = DateTime.Today.AddDays(7)
                }
            };

            context.Reminders.AddRange(reminders);
            foreach (var r in reminders)
            {
                context.Entry(r).Property("UserId").CurrentValue = user.Id;
            }
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededReminders = await context2.Reminders
                .Where(r => r.Title.Contains(uid))
                .ToListAsync();
            
            seededReminders.Should().HaveCount(2);
        }

        [Fact]
        public async Task CanSeed_QuickNotes()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_note_{uid}");

            // Act - Seed quick notes
            var notes = new[]
            {
                new QuickNote
                {
                    Content = $"Research new testing frameworks {uid}",
                    Category = NoteCategory.Idea,
                    IsPinned = true,
                    IsArchived = false,
                    Tags = "testing,research"
                },
                new QuickNote
                {
                    Content = $"Meeting notes from architecture review {uid}",
                    Category = NoteCategory.Meeting,
                    IsPinned = false,
                    IsArchived = false
                },
                new QuickNote
                {
                    Content = $"Decision: Use microservices {uid}",
                    Category = NoteCategory.Decision,
                    IsPinned = false,
                    IsArchived = false
                }
            };

            context.QuickNotes.AddRange(notes);
            foreach (var n in notes)
            {
                context.Entry(n).Property("UserId").CurrentValue = user.Id;
            }
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededNotes = await context2.QuickNotes
                .Where(n => n.Content.Contains(uid))
                .ToListAsync();
            
            seededNotes.Should().HaveCount(3);
            seededNotes.Count(n => n.IsPinned).Should().Be(1);
        }

        [Fact]
        public async Task CanSeed_MeetingTemplates()
        {
            // Arrange
            var uid = Guid.NewGuid().ToString("N")[..8];
            await using var context = _fixture.CreateContext();
            
            var user = await CreateTestUser(context, $"seed_template_{uid}");

            // Act - Seed meeting templates
            var template = new MeetingTemplate
            {
                Name = $"Weekly 1:1 {uid}",
                Description = "Standard weekly check-in template",
                SuggestedDurationMinutes = 30,
                IsSystemTemplate = true,
                Items = new List<MeetingTemplateItem>
                {
                    new() { Description = "How are you doing?", Category = AgendaItemCategory.Topic, Priority = Severity.Medium, SortOrder = 1 },
                    new() { Description = "Work updates", Category = AgendaItemCategory.Update, Priority = Severity.High, SortOrder = 2 },
                    new() { Description = "Blockers", Category = AgendaItemCategory.Blocker, Priority = Severity.High, SortOrder = 3 }
                }
            };

            context.MeetingTemplates.Add(template);
            context.Entry(template).Property("UserId").CurrentValue = user.Id;
            await context.SaveChangesAsync();

            // Assert
            await using var context2 = _fixture.CreateContext();
            var seededTemplate = await context2.MeetingTemplates
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Name.Contains(uid));
            
            seededTemplate.Should().NotBeNull();
            seededTemplate!.Items.Should().HaveCount(3);
        }

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

        #endregion
    }
}
