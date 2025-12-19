using FluentAssertions;
using Tracker.Help.Models;

namespace Tracker.Tests.Help
{
    public class HelpServiceTests
    {
        [Fact]
        public void HelpTopic_ShouldHaveDefaultValues()
        {
            var topic = new HelpTopic();

            topic.Id.Should().Be(string.Empty);
            topic.Title.Should().Be(string.Empty);
            topic.Content.Should().Be(string.Empty);
            topic.Sections.Should().BeEmpty();
        }

        [Fact]
        public void HelpTopic_ShouldBeSettable()
        {
            var topic = new HelpTopic
            {
                Id = "getting-started/overview",
                Title = "Overview",
                Content = "# Overview\n\nThis is the overview.",
                Category = "Getting Started"
            };

            topic.Id.Should().Be("getting-started/overview");
            topic.Title.Should().Be("Overview");
            topic.Content.Should().Contain("# Overview");
        }

        [Fact]
        public void HelpTocEntry_ShouldSupportHierarchy()
        {
            var parent = new HelpTocEntry
            {
                Id = "features",
                Title = "Features",
                TopicId = "features/overview",
                Children = new List<HelpTocEntry>
                {
                    new HelpTocEntry { Id = "features-dashboard", Title = "Dashboard", TopicId = "features/dashboard" },
                    new HelpTocEntry { Id = "features-team", Title = "Team Members", TopicId = "features/team-members" }
                }
            };

            parent.Children.Should().HaveCount(2);
            parent.Children[0].Title.Should().Be("Dashboard");
        }

        [Fact]
        public void HelpSearchResult_ShouldHaveDefaultValues()
        {
            var result = new HelpSearchResult();

            result.TopicId.Should().Be(string.Empty);
            result.Title.Should().Be(string.Empty);
            result.Snippet.Should().Be(string.Empty);
        }

        [Fact]
        public void HelpSearchResult_ShouldBeSettable()
        {
            var result = new HelpSearchResult
            {
                TopicId = "features/dashboard",
                Title = "Dashboard",
                Snippet = "...overview of your team's status..."
            };

            result.TopicId.Should().Be("features/dashboard");
            result.Title.Should().Be("Dashboard");
            result.Snippet.Should().Contain("overview");
        }

        [Fact]
        public void HelpContext_ShouldStoreTopicAndSection()
        {
            var context = new HelpContext
            {
                TopicId = "dialogs/add-team-member",
                Section = "email-field"
            };

            context.TopicId.Should().Be("dialogs/add-team-member");
            context.Section.Should().Be("email-field");
        }

        [Fact]
        public void HelpSection_ShouldHaveDefaultValues()
        {
            var section = new HelpSection();

            section.Id.Should().Be(string.Empty);
            section.Title.Should().Be(string.Empty);
            section.Level.Should().Be(0);
        }

        [Fact]
        public void HelpSection_ShouldBeSettable()
        {
            var section = new HelpSection
            {
                Id = "getting-started",
                Title = "Getting Started",
                Level = 2
            };

            section.Id.Should().Be("getting-started");
            section.Title.Should().Be("Getting Started");
            section.Level.Should().Be(2);
        }
    }
}
