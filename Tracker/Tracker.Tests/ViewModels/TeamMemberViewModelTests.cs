using FluentAssertions;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Tests.ViewModels
{
    public class TeamMemberViewModelTests
    {
        [Fact]
        public void Constructor_WithNewTeamMember_ShouldBeInAddMode()
        {
            var vm = new TeamMemberViewModel(new TeamMember());

            vm.InEditMode.Should().BeFalse();
            vm.DialogTitle.Should().Contain("Add");
        }

        [Fact]
        public void Constructor_WithExistingTeamMember_ShouldBeInEditMode()
        {
            var teamMember = TestDataBuilder.CreateTeamMember(firstName: "John", lastName: "Doe");
            teamMember.Id = 1; // Simulate saved entity
            
            var vm = new TeamMemberViewModel(teamMember);

            vm.InEditMode.Should().BeTrue();
            vm.DialogTitle.Should().Contain("Edit");
        }

        [Fact]
        public void FirstName_ShouldRaisePropertyChanged()
        {
            var vm = new TeamMemberViewModel(new TeamMember());
            bool propertyChangedRaised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TeamMemberViewModel.FirstName))
                    propertyChangedRaised = true;
            };

            vm.FirstName = "Jane";

            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void LastName_ShouldRaisePropertyChanged()
        {
            var vm = new TeamMemberViewModel(new TeamMember());
            bool propertyChangedRaised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TeamMemberViewModel.LastName))
                    propertyChangedRaised = true;
            };

            vm.LastName = "Smith";

            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void Email_ShouldRaisePropertyChanged()
        {
            var vm = new TeamMemberViewModel(new TeamMember());
            bool propertyChangedRaised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TeamMemberViewModel.Email))
                    propertyChangedRaised = true;
            };

            vm.Email = "test@example.com";

            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void IsActive_ShouldDefaultToTrue()
        {
            var vm = new TeamMemberViewModel(new TeamMember());

            vm.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Role_ShouldBeSettable()
        {
            var vm = new TeamMemberViewModel(new TeamMember());

            vm.Role = "Developer";

            vm.Role.Should().Be("Developer");
        }

        [Fact]
        public void JobTitle_ShouldBeSettable()
        {
            var vm = new TeamMemberViewModel(new TeamMember());

            vm.JobTitle = "Senior Engineer";

            vm.JobTitle.Should().Be("Senior Engineer");
        }

        [Fact]
        public void HireDate_ShouldBeSettable()
        {
            var vm = new TeamMemberViewModel(new TeamMember());
            var hireDate = DateTime.Today.AddYears(-1);

            vm.HireDate = hireDate;

            vm.HireDate.Should().Be(hireDate);
        }
    }
}

