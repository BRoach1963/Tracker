using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Tests.ViewModels
{
    public class FeedbackViewModelTests
    {
        [Fact]
        public void Constructor_WithNewFeedback_ShouldBeInAddMode()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var feedback = new Feedback { TeamMember = teamMember };
            
            var vm = new FeedbackViewModel(feedback);

            vm.InEditMode.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithExistingFeedback_ShouldBeInEditMode()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var feedback = TestDataBuilder.CreateFeedback(teamMember);
            feedback.Id = 1;
            
            var vm = new FeedbackViewModel(feedback);

            vm.InEditMode.Should().BeTrue();
        }

        [Fact]
        public void Title_ShouldRaisePropertyChanged()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new FeedbackViewModel(new Feedback { TeamMember = teamMember });
            bool propertyChangedRaised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FeedbackViewModel.Title))
                    propertyChangedRaised = true;
            };

            vm.Title = "Great presentation";

            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void Content_ShouldRaisePropertyChanged()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new FeedbackViewModel(new Feedback { TeamMember = teamMember });
            bool propertyChangedRaised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FeedbackViewModel.Content))
                    propertyChangedRaised = true;
            };

            vm.Content = "Detailed feedback content";

            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void Type_ShouldDefaultToPositive()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new FeedbackViewModel(new Feedback { TeamMember = teamMember });

            vm.Type.Should().Be(FeedbackType.Positive);
        }

        [Fact]
        public void Date_ShouldDefaultToToday()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new FeedbackViewModel(new Feedback { TeamMember = teamMember });

            vm.Date.Should().Be(DateTime.Today);
        }

        [Fact]
        public void Context_ShouldBeSettable()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new FeedbackViewModel(new Feedback { TeamMember = teamMember });

            vm.Context = "Sprint review meeting";

            vm.Context.Should().Be("Sprint review meeting");
        }
    }
}

