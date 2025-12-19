using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.MockData
{
    /// <summary>
    /// Mock Project data - projects implement IMeasurable and IKpiSource.
    /// This data matches the DatabaseSeeder's sample projects for consistency.
    /// 
    /// Status distribution: 3 In Progress (various completion %), 1 Planning
    /// </summary>
    public static class MockProjects
    {
        public static ObservableCollection<Project> GetMockProjects(List<TeamMember> teamMembers)
        { 
            if (teamMembers == null || teamMembers.Count == 0) return new ObservableCollection<Project>();

            var today = DateTime.Today;
            var manager = teamMembers.Count > 0 ? teamMembers[0] : new TeamMember { FirstName = "Alex", LastName = "Rivera" };
            var jordan = teamMembers.Count > 1 ? teamMembers[1] : manager;
            var morgan = teamMembers.Count > 2 ? teamMembers[2] : manager;
            var taylor = teamMembers.Count > 3 ? teamMembers[3] : manager;
            var casey = teamMembers.Count > 4 ? teamMembers[4] : manager;
            var riley = teamMembers.Count > 5 ? teamMembers[5] : manager;
            var jamie = teamMembers.Count > 6 ? teamMembers[6] : manager;

            return new ObservableCollection<Project>
            {
                // Project 1: Platform API Modernization - 75% complete (On Track)
                new Project
                {
                    ID = 1,
                    Name = "Platform API Modernization",
                    Description = "Migrate legacy REST APIs to modern GraphQL architecture with improved performance and developer experience.",
                    StartDate = today.AddMonths(-2),
                    EndDate = today.AddMonths(1),
                    Status = "In Progress",
                    Owner = jordan,
                    TeamMembers = new List<TeamMember> { jordan, taylor, casey },
                    Budget = 75000m,
                    Tasks = new List<IndividualTask>
                    {
                        new IndividualTask { Id = 1, Description = "Implement GraphQL user queries", IsCompleted = true, Owner = jordan },
                        new IndividualTask { Id = 2, Description = "Migrate order endpoints to GraphQL", IsCompleted = true, Owner = jordan },
                        new IndividualTask { Id = 3, Description = "Implement caching layer for GraphQL", IsCompleted = true, Owner = jordan },
                        new IndividualTask { Id = 4, Description = "Update SDK documentation", IsCompleted = false, Owner = taylor },
                        new IndividualTask { Id = 5, Description = "Performance testing and optimization", IsCompleted = false, Owner = casey }
                    },
                    Milestones = new List<Milestone>
                    {
                        new Milestone { ID = 1, Name = "API Design Complete", TargetDate = today.AddMonths(-1), IsAchieved = true },
                        new Milestone { ID = 2, Name = "Core Endpoints Migrated", TargetDate = today.AddDays(-7), IsAchieved = true },
                        new Milestone { ID = 3, Name = "Full Migration", TargetDate = today.AddDays(21), IsAchieved = false },
                        new Milestone { ID = 4, Name = "Legacy Deprecation", TargetDate = today.AddMonths(1), IsAchieved = false }
                    },
                    Risks = new List<Risk>
                    {
                        new Risk { ID = 1, Name = "Third-party Integration Delays", Description = "External partners may need time to update", Severity = RiskLevelEnum.Medium, MitigationStrategy = "Early communication and parallel support" }
                    }
                },

                // Project 2: Customer Dashboard Redesign - 60% complete (At Risk - deadline pressure)
                new Project
                {
                    ID = 2,
                    Name = "Customer Dashboard Redesign",
                    Description = "Complete overhaul of customer-facing dashboard with new data visualizations and mobile responsiveness.",
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddDays(14),
                    Status = "In Progress",
                    Owner = morgan,
                    TeamMembers = new List<TeamMember> { morgan, jamie, taylor },
                    Budget = 45000m,
                    Tasks = new List<IndividualTask>
                    {
                        new IndividualTask { Id = 6, Description = "Create chart component library", IsCompleted = true, Owner = morgan },
                        new IndividualTask { Id = 7, Description = "Implement responsive grid system", IsCompleted = true, Owner = morgan },
                        new IndividualTask { Id = 8, Description = "Build KPI widget components", IsCompleted = true, Owner = jamie },
                        new IndividualTask { Id = 9, Description = "Implement dark mode theme", IsCompleted = false, Owner = jamie },
                        new IndividualTask { Id = 10, Description = "Mobile responsive testing", IsCompleted = false, Owner = morgan }
                    },
                    Milestones = new List<Milestone>
                    {
                        new Milestone { ID = 5, Name = "Design System Complete", TargetDate = today.AddMonths(-2), IsAchieved = true },
                        new Milestone { ID = 6, Name = "Core Components Built", TargetDate = today.AddMonths(-1), IsAchieved = true },
                        new Milestone { ID = 7, Name = "Mobile Responsive", TargetDate = today.AddDays(7), IsAchieved = false },
                        new Milestone { ID = 8, Name = "Launch", TargetDate = today.AddDays(14), IsAchieved = false }
                    },
                    Risks = new List<Risk>
                    {
                        new Risk { ID = 2, Name = "Tight Timeline", Description = "Aggressive deadline with scope changes", Severity = RiskLevelEnum.High, MitigationStrategy = "Scope prioritization and overtime budget" }
                    }
                },

                // Project 3: Analytics Pipeline - 90% complete (On Track - almost done)
                new Project
                {
                    ID = 3,
                    Name = "Real-time Analytics Pipeline",
                    Description = "Build streaming data pipeline for real-time business metrics and alerting.",
                    StartDate = today.AddMonths(-4),
                    EndDate = today.AddDays(7),
                    Status = "In Progress",
                    Owner = riley,
                    TeamMembers = new List<TeamMember> { riley, jordan, casey },
                    Budget = 60000m,
                    Tasks = new List<IndividualTask>
                    {
                        new IndividualTask { Id = 11, Description = "Configure Kafka topics", IsCompleted = true, Owner = riley },
                        new IndividualTask { Id = 12, Description = "Build Spark aggregation jobs", IsCompleted = true, Owner = riley },
                        new IndividualTask { Id = 13, Description = "Create analytics API endpoints", IsCompleted = true, Owner = casey },
                        new IndividualTask { Id = 14, Description = "Dashboard widget integration", IsCompleted = true, Owner = casey },
                        new IndividualTask { Id = 15, Description = "Implement alerting rules engine", IsCompleted = false, Owner = riley }
                    },
                    Milestones = new List<Milestone>
                    {
                        new Milestone { ID = 9, Name = "Infrastructure Setup", TargetDate = today.AddMonths(-3), IsAchieved = true },
                        new Milestone { ID = 10, Name = "Data Ingestion", TargetDate = today.AddMonths(-2), IsAchieved = true },
                        new Milestone { ID = 11, Name = "Dashboard Integration", TargetDate = today.AddMonths(-1), IsAchieved = true },
                        new Milestone { ID = 12, Name = "Alerting System", TargetDate = today.AddDays(7), IsAchieved = false }
                    }
                },

                // Project 4: Mobile App v2 - Planning stage
                new Project
                {
                    ID = 4,
                    Name = "Mobile App v2.0",
                    Description = "Major mobile app update with offline support, biometric auth, and performance improvements.",
                    StartDate = today.AddDays(14),
                    EndDate = today.AddMonths(4),
                    Status = "Planning",
                    Owner = manager,
                    TeamMembers = new List<TeamMember> { morgan, taylor, jamie },
                    Budget = 120000m,
                    Tasks = new List<IndividualTask>(),
                    Milestones = new List<Milestone>
                    {
                        new Milestone { ID = 13, Name = "Requirements Finalized", TargetDate = today.AddDays(14), IsAchieved = false },
                        new Milestone { ID = 14, Name = "Architecture Design", TargetDate = today.AddMonths(1), IsAchieved = false },
                        new Milestone { ID = 15, Name = "Beta Release", TargetDate = today.AddMonths(3), IsAchieved = false },
                        new Milestone { ID = 16, Name = "Public Launch", TargetDate = today.AddMonths(4), IsAchieved = false }
                    }
                }
            };
        }
    }
}
