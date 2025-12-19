using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.DataModels
{
    public class FeedbackTests
    {
        [Fact]
        public void NewFeedback_ShouldHaveDefaultValues()
        {
            var feedback = new Feedback();

            feedback.Id.Should().Be(0);
            feedback.Title.Should().Be(string.Empty);
            feedback.Content.Should().Be(string.Empty);
            feedback.Type.Should().Be(FeedbackType.Positive);
        }

        [Fact]
        public void TestDataBuilder_ShouldCreateValidFeedback()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            var feedback = TestDataBuilder.CreateFeedback(teamMember, FeedbackType.Recognition, "Great work");

            feedback.TeamMember.Should().Be(teamMember);
            feedback.Type.Should().Be(FeedbackType.Recognition);
            feedback.Title.Should().Be("Great work");
        }

        [Theory]
        [InlineData(FeedbackType.Positive)]
        [InlineData(FeedbackType.Constructive)]
        [InlineData(FeedbackType.Recognition)]
        [InlineData(FeedbackType.Coaching)]
        [InlineData(FeedbackType.PerformanceReview)]
        public void Type_ShouldAcceptAllValidValues(FeedbackType type)
        {
            var feedback = new Feedback { Type = type };
            feedback.Type.Should().Be(type);
        }

        [Fact]
        public void Context_ShouldBeOptional()
        {
            var feedback = new Feedback
            {
                Title = "Test",
                Content = "Test content",
                Context = null
            };

            feedback.Context.Should().BeNull();
        }
    }
}

