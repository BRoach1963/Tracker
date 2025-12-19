using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Tests.ViewModels
{
    public class GoalViewModelTests
    {
        [Fact]
        public void Constructor_WithNewGoal_ShouldBeInAddMode()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var goal = new IndividualGoal { TeamMember = teamMember };
            
            var vm = new GoalViewModel(goal);

            vm.InEditMode.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithExistingGoal_ShouldBeInEditMode()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var goal = TestDataBuilder.CreateGoal(teamMember);
            goal.Id = 1;
            
            var vm = new GoalViewModel(goal);

            vm.InEditMode.Should().BeTrue();
        }

        [Fact]
        public void Title_ShouldRaisePropertyChanged()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new GoalViewModel(new IndividualGoal { TeamMember = teamMember });
            bool propertyChangedRaised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GoalViewModel.Title))
                    propertyChangedRaised = true;
            };

            vm.Title = "Learn AWS";

            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void ProgressPercent_ShouldClampTo0And100()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new GoalViewModel(new IndividualGoal { TeamMember = teamMember });

            vm.ProgressPercent = 150; // Over 100
            vm.ProgressPercent.Should().BeLessOrEqualTo(100);

            vm.ProgressPercent = -50; // Under 0
            vm.ProgressPercent.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void Status_ShouldDefaultToNotStarted()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new GoalViewModel(new IndividualGoal { TeamMember = teamMember });

            vm.Status.Should().Be(GoalStatus.NotStarted);
        }

        [Fact]
        public void Category_ShouldDefaultToSkillDevelopment()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var goal = new IndividualGoal { TeamMember = teamMember };
            
            var vm = new GoalViewModel(goal);

            vm.Category.Should().Be(GoalCategory.SkillDevelopment);
        }

        [Fact]
        public void Milestones_ShouldBeEmpty_ForNewGoal()
        {
            var teamMember = TestDataBuilder.CreateTeamMember();
            teamMember.Id = 1;
            var vm = new GoalViewModel(new IndividualGoal { TeamMember = teamMember });

            vm.Milestones.Should().BeEmpty();
        }
    }
}

