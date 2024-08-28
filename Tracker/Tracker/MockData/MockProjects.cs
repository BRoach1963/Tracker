using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.MockData
{
    public static class MockProjects
    {
        public static ObservableCollection<Project> GetMockProjects()
        {
            var teamMembers = MockTeamMemberData.GetMockTeamMemberData().ToList();
            var okrs = MockOkRs.GetMockOkrData();

            var projects = new ObservableCollection<Project>()
            {
                new Project
                {
                    ID = 1,
                    Name = "Revamp E-commerce Platform",
                    Owner = teamMembers[new Random().Next(teamMembers.Count)],
                    Description = "A comprehensive project to overhaul the existing e-commerce platform, including redesign, new features, and improved performance.",
                    StartDate = new DateTime(2024, 1, 15),
                    EndDate = new DateTime(2024, 12, 15),
                    TeamMembers = teamMembers.OrderBy(_ => Guid.NewGuid()).Take(3).ToList(),
                    OKRs = okrs.Where(okr => okr.ProjectId == 1).ToList(), 
                    Milestones =
                    [
                        new Milestone
                            { ID = 1, Name = "Design Phase Complete", TargetDate = new DateTime(2024, 3, 31) },
                        new Milestone
                            { ID = 2, Name = "Development Phase Complete", TargetDate = new DateTime(2024, 6, 30) },
                        new Milestone
                            { ID = 3, Name = "Testing Phase Complete", TargetDate = new DateTime(2024, 9, 30) },
                        new Milestone { ID = 4, Name = "Launch", TargetDate = new DateTime(2024, 12, 15) }
                    ],
                    Dependencies =
                    [
                        new ProjectDependency()
                        {
                            ID = 1, Name = "Backend API Development",
                            Description = "Complete API development before frontend integration.", ProjectId = 1
                        },
                        new ProjectDependency
                        {
                            ID = 2, Name = "UI/UX Design Approval",
                            Description = "Design needs approval before starting development.", ProjectId = 1
                        }
                    ],
                    Risks =
                    [
                        new Risk
                        {
                            ID = 1, Name = "Delayed Design Approval",
                            Description = "Risk of delays if the design is not approved on time.",
                            Likelihood = RiskLevelEnum.High, Impact = RiskLevelEnum.Medium
                        },
                        new Risk
                        {
                            ID = 2, Name = "Integration Issues",
                            Description = "Potential issues with integrating new features with existing systems.",
                            Likelihood = RiskLevelEnum.Medium, Impact = RiskLevelEnum.High
                        }
                    ]
                },
                new Project
                {
                    ID = 2,
                    Name = "AI-Powered Customer Support",
                    Owner = teamMembers[new Random().Next(teamMembers.Count)],
                    Description = "Develop and deploy an AI-powered customer support system to improve response times and customer satisfaction.",
                    StartDate = new DateTime(2024, 2, 1),
                    EndDate = new DateTime(2024, 11, 30),
                    TeamMembers = teamMembers.OrderBy(_ => Guid.NewGuid()).Take(3).ToList(),
                    OKRs = okrs.Where(okr => okr.ProjectId == 2).ToList(), 
                    Milestones =
                    [
                        new Milestone
                            { ID = 1, Name = "Research Phase Complete", TargetDate = new DateTime(2024, 4, 30) },
                        new Milestone
                            { ID = 2, Name = "Prototype Development", TargetDate = new DateTime(2024, 7, 31) },
                        new Milestone { ID = 3, Name = "User Testing", TargetDate = new DateTime(2024, 10, 15) },
                        new Milestone { ID = 4, Name = "Full Deployment", TargetDate = new DateTime(2024, 11, 30) }
                    ],
                    Dependencies =
                    [
                        new ProjectDependency
                        {
                            ID = 3, Name = "Data Collection",
                            Description = "AI model development dependent on data collection.", ProjectId = 2
                        },
                        new ProjectDependency
                        {
                            ID = 4, Name = "Server Infrastructure Setup",
                            Description = "Requires server setup before deploying the prototype.", ProjectId = 2
                        }
                    ],
                    Risks =
                    [
                        new Risk
                        {
                            ID = 1, Name = "Data Privacy Concerns",
                            Description = "Risk of privacy issues with collected customer data.",
                            Likelihood = RiskLevelEnum.Medium, Impact = RiskLevelEnum.High
                        },
                        new Risk
                        {
                            ID = 2, Name = "AI Model Accuracy",
                            Description = "Potential for low accuracy in AI responses.",
                            Likelihood = RiskLevelEnum.High, Impact = RiskLevelEnum.Medium
                        }
                    ]
                },
                new Project
                {
                    ID = 3,
                    Name = "Mobile App Redesign",
                    Owner = teamMembers[new Random().Next(teamMembers.Count)],
                    Description = "Redesign and enhance the existing mobile application to improve user experience and add new functionalities.",
                    StartDate = new DateTime(2024, 3, 1),
                    EndDate = new DateTime(2024, 10, 31),
                    TeamMembers = teamMembers.OrderBy(_ => Guid.NewGuid()).Take(3).ToList(),
                    OKRs = okrs.Where(okr => okr.ProjectId == 3).ToList(), 
                    Milestones =
                    [
                        new Milestone
                            { ID = 1, Name = "Wireframe Designs Complete", TargetDate = new DateTime(2024, 5, 31) },
                        new Milestone
                            { ID = 2, Name = "Development Sprint 1 Complete", TargetDate = new DateTime(2024, 7, 31) },
                        new Milestone { ID = 3, Name = "Beta Release", TargetDate = new DateTime(2024, 9, 15) },
                        new Milestone { ID = 4, Name = "Official Release", TargetDate = new DateTime(2024, 10, 31) }
                    ],
                    Dependencies =
                    [
                        new ProjectDependency
                        {
                            ID = 5, Name = "UI/UX Design Approval",
                            Description = "Requires approval of new design before development.", ProjectId = 3
                        },
                        new ProjectDependency
                        {
                            ID = 6, Name = "API Integration",
                            Description = "Dependent on new API endpoints for the app functionalities.", ProjectId = 3
                        }
                    ],
                    Risks =
                    [
                        new Risk
                        {
                            ID = 1, Name = "Delayed Design Feedback",
                            Description = "Risk of delays if design feedback is delayed.",
                            Likelihood = RiskLevelEnum.High, Impact = RiskLevelEnum.Medium
                        },
                        new Risk
                        {
                            ID = 2, Name = "Compatibility Issues",
                            Description = "Potential compatibility issues with older mobile devices.",
                            Likelihood = RiskLevelEnum.Medium, Impact = RiskLevelEnum.High
                        }
                    ]
                },
                new Project
                {
                    ID = 4,
                    Name = "Cloud Migration",
                    Owner = teamMembers[new Random().Next(teamMembers.Count)],
                    Description = "Migrate on-premise infrastructure and applications to a cloud environment for better scalability and cost-efficiency.",
                    StartDate = new DateTime(2024, 4, 1),
                    EndDate = new DateTime(2024, 12, 31),
                    TeamMembers = teamMembers.OrderBy(_ => Guid.NewGuid()).Take(3).ToList(),
                    OKRs = okrs.Where(okr => okr.ProjectId == 4).ToList(), 
                    Milestones =
                    [
                        new Milestone
                        {
                            ID = 1, Name = "Infrastructure Assessment Complete", TargetDate = new DateTime(2024, 6, 30)
                        },
                        new Milestone
                            { ID = 2, Name = "Cloud Environment Setup", TargetDate = new DateTime(2024, 8, 31) },
                        new Milestone
                            { ID = 3, Name = "Data Migration Complete", TargetDate = new DateTime(2024, 10, 31) },
                        new Milestone { ID = 4, Name = "Full Transition", TargetDate = new DateTime(2024, 12, 31) }
                    ],
                    Dependencies =
                    [
                        new ProjectDependency
                        {
                            ID = 7, Name = "Data Backup", Description = "Complete data backup before migration.",
                            ProjectId = 4
                        },
                        new ProjectDependency
                        {
                            ID = 8, Name = "Cloud Service Provider Selection",
                            Description = "Selection of cloud provider must be completed before setup.", ProjectId = 4
                        }
                    ],
                    Risks =
                    [
                        new Risk
                        {
                            ID = 1, Name = "Data Loss During Migration",
                            Description = "Risk of data loss during the migration process.",
                            Likelihood = RiskLevelEnum.Medium, Impact = RiskLevelEnum.High
                        },
                        new Risk
                        {
                            ID = 2, Name = "Increased Costs",
                            Description = "Potential for increased costs due to unplanned resource usage.",
                            Likelihood = RiskLevelEnum.Low, Impact = RiskLevelEnum.Medium
                        }
                    ]
                },
            };
 
            return projects;
        }
    }
}
