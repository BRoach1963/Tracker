using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.DataModels
{
    public class ProjectTests
    {
        [Fact]
        public void NewProject_ShouldHaveDefaultValues()
        {
            var project = new Project();

            project.Id.Should().Be(0);
            project.Name.Should().Be(string.Empty);
            project.Description.Should().Be(string.Empty);
            project.Status.Should().Be(ProjectStatusEnum.NotStarted);
            project.Milestones.Should().BeEmpty();
            project.Risks.Should().BeEmpty();
            project.Dependencies.Should().BeEmpty();
        }

        [Fact]
        public void TestDataBuilder_ShouldCreateValidProject()
        {
            var owner = TestDataBuilder.CreateTeamMember();
            var project = TestDataBuilder.CreateProject(owner, name: "Test Project");

            project.Name.Should().Be("Test Project");
            project.Owner.Should().Be(owner);
            project.Status.Should().Be(ProjectStatusEnum.InProgress);
        }

        [Theory]
        [InlineData(ProjectStatusEnum.NotStarted)]
        [InlineData(ProjectStatusEnum.InProgress)]
        [InlineData(ProjectStatusEnum.OnHold)]
        [InlineData(ProjectStatusEnum.Completed)]
        [InlineData(ProjectStatusEnum.Cancelled)]
        public void Status_ShouldAcceptAllValidValues(ProjectStatusEnum status)
        {
            var project = new Project { Status = status };
            project.Status.Should().Be(status);
        }

        [Fact]
        public void Milestones_ShouldBeAddable()
        {
            var project = new Project();
            var milestone = new Milestone { Name = "Phase 1", TargetDate = DateTime.Today.AddMonths(1) };
            
            project.Milestones.Add(milestone);

            project.Milestones.Should().HaveCount(1);
            project.Milestones[0].Name.Should().Be("Phase 1");
        }

        [Fact]
        public void Risks_ShouldBeAddable()
        {
            var project = new Project();
            var risk = new Risk { Name = "Resource Risk", Severity = RiskLevelEnum.High };
            
            project.Risks.Add(risk);

            project.Risks.Should().HaveCount(1);
            project.Risks[0].Severity.Should().Be(RiskLevelEnum.High);
        }
    }
}

