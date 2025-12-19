using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Tests.ViewModels
{
    public class OneOnOneViewModelTests
    {
        [Fact]
        public void Constructor_WithNewOneOnOne_ShouldBeInAddMode()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var oneOnOne = new OneOnOne { TeamMember = teamMember };
            
            var vm = new OneOnOneViewModel(oneOnOne);

            vm.InEditMode.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithExistingOneOnOne_ShouldBeInEditMode()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var oneOnOne = TestDataBuilder.CreateOneOnOne(teamMember);
            oneOnOne.Id = 1;
            
            var vm = new OneOnOneViewModel(oneOnOne);

            vm.InEditMode.Should().BeTrue();
        }

        [Fact]
        public void Date_ShouldRaisePropertyChanged()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new OneOnOneViewModel(new OneOnOne { TeamMember = teamMember });
            bool propertyChangedRaised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(OneOnOneViewModel.Date))
                    propertyChangedRaised = true;
            };

            vm.Date = DateTime.Today.AddDays(7);

            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void Description_ShouldRaisePropertyChanged()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new OneOnOneViewModel(new OneOnOne { TeamMember = teamMember });
            bool propertyChangedRaised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(OneOnOneViewModel.Description))
                    propertyChangedRaised = true;
            };

            vm.Description = "Weekly sync";

            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void Status_ShouldDefaultToScheduled()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new OneOnOneViewModel(new OneOnOne { TeamMember = teamMember });

            vm.Status.Should().Be(MeetingStatusEnum.Scheduled);
        }

        [Fact]
        public void Duration_ShouldDefaultTo30()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new OneOnOneViewModel(new OneOnOne { TeamMember = teamMember });

            vm.Duration.Should().Be(30);
        }

        [Fact]
        public void AgendaItems_ShouldBeEmpty_ForNewOneOnOne()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new OneOnOneViewModel(new OneOnOne { TeamMember = teamMember });

            vm.AgendaItems.Should().BeEmpty();
        }

        [Fact]
        public void Tasks_ShouldBeEmpty_ForNewOneOnOne()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new OneOnOneViewModel(new OneOnOne { TeamMember = teamMember });

            vm.Tasks.Should().BeEmpty();
        }

        [Fact]
        public void CanSave_ShouldBeFalse_WhenDescriptionEmpty()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new OneOnOneViewModel(new OneOnOne { TeamMember = teamMember });
            vm.Description = "";

            vm.CanSave.Should().BeFalse();
        }

        [Fact]
        public void CanSave_ShouldBeTrue_WhenValid()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new OneOnOneViewModel(new OneOnOne { TeamMember = teamMember });
            vm.Description = "Valid description";
            vm.Date = DateTime.Today;

            vm.CanSave.Should().BeTrue();
        }
    }
}

