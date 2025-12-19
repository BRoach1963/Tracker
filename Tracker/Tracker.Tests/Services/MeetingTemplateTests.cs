using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.Services
{
    [Collection("Database")]
    public class MeetingTemplateTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public MeetingTemplateTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CreateTemplate_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var template = TestDataBuilder.CreateMeetingTemplate("Weekly Standup", 15);
            context.MeetingTemplates.Add(template);
            context.Entry(template).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.MeetingTemplates.FindAsync(template.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("Weekly Standup");
            retrieved.Duration.Should().Be(15);
        }

        [Fact]
        public async Task CreateTemplate_WithItems_ShouldPersist()
        {
            using var context = _fixture.CreateContext();
            
            var template = TestDataBuilder.CreateMeetingTemplate("Sprint Retro");
            template.Items.Add(new MeetingTemplateItem
            {
                Description = "What went well?",
                Category = AgendaItemCategory.Feedback,
                Priority = 1
            });
            template.Items.Add(new MeetingTemplateItem
            {
                Description = "What could be improved?",
                Category = AgendaItemCategory.Feedback,
                Priority = 2
            });
            template.Items.Add(new MeetingTemplateItem
            {
                Description = "Action items",
                Category = AgendaItemCategory.ActionItem,
                Priority = 3
            });
            
            context.MeetingTemplates.Add(template);
            context.Entry(template).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.MeetingTemplates
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == template.Id);

            retrieved!.Items.Should().HaveCount(3);
            retrieved.Items.OrderBy(i => i.Priority).First().Description.Should().Be("What went well?");
        }

        [Fact]
        public async Task SetDefaultTemplate_ShouldWork()
        {
            using var context = _fixture.CreateContext();
            
            var template1 = TestDataBuilder.CreateMeetingTemplate("Template 1");
            template1.IsDefault = true;
            context.MeetingTemplates.Add(template1);
            context.Entry(template1).Property("UserId").CurrentValue = _fixture.TestUserId;

            var template2 = TestDataBuilder.CreateMeetingTemplate("Template 2");
            template2.IsDefault = false;
            context.MeetingTemplates.Add(template2);
            context.Entry(template2).Property("UserId").CurrentValue = _fixture.TestUserId;
            
            await context.SaveChangesAsync();

            var defaultTemplate = await context.MeetingTemplates
                .FirstOrDefaultAsync(t => t.IsDefault);

            defaultTemplate.Should().NotBeNull();
            defaultTemplate!.Name.Should().Be("Template 1");
        }

        [Fact]
        public async Task ApplyTemplate_ShouldCopyItemsToOneOnOne()
        {
            using var context = _fixture.CreateContext();
            
            // Create template with items
            var template = TestDataBuilder.CreateMeetingTemplate("Standard Meeting");
            template.Items.Add(new MeetingTemplateItem
            {
                Description = "Review previous actions",
                Category = AgendaItemCategory.Review,
                Priority = 1
            });
            template.Items.Add(new MeetingTemplateItem
            {
                Description = "Current projects update",
                Category = AgendaItemCategory.Project,
                Priority = 2
            });
            
            context.MeetingTemplates.Add(template);
            context.Entry(template).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            // Create team member and one-on-one
            var teamMember = TestDataBuilder.CreateTeamMember();
            context.TeamMembers.Add(teamMember);
            context.Entry(teamMember).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var oneOnOne = TestDataBuilder.CreateOneOnOne(teamMember);
            
            // Apply template items
            foreach (var templateItem in template.Items)
            {
                oneOnOne.AgendaItems.Add(new AgendaItem
                {
                    Description = templateItem.Description,
                    Category = templateItem.Category
                });
            }
            
            context.OneOnOnes.Add(oneOnOne);
            context.Entry(oneOnOne).Property("UserId").CurrentValue = _fixture.TestUserId;
            await context.SaveChangesAsync();

            var retrieved = await context.OneOnOnes
                .Include(o => o.AgendaItems)
                .FirstOrDefaultAsync(o => o.Id == oneOnOne.Id);

            retrieved!.AgendaItems.Should().HaveCount(2);
        }
    }
}

