using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Managers;

namespace Tracker.MockData
{
    public static class MockKpIs
    {
        public static ObservableCollection<KeyPerformanceIndicator> GetMockKpiData(List<TeamMember> teamMembers)
        { 
            if (teamMembers.Count == 0) return new ObservableCollection<KeyPerformanceIndicator>();
            var random = new Random();
            DateTime today = DateTime.Today;

            return
                new ObservableCollection<KeyPerformanceIndicator>
                {
                    new KeyPerformanceIndicator
                    {
                        KpiId = 1,
                        Name = "Code Coverage",
                        Description = "Percentage of code covered by automated tests.",
                        Value = 85.0,
                        TargetValue = 90.0,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                        OnTargetThresholdPercentage = 5.0,
                        OffTargetThresholdPercentage = 10.0,
                        OnTargetThresholdAbsolute = 2.0,
                        OffTargetThresholdAbsolute = 5.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 1 // Associated with Objective 1
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 2,
                        Name = "Bug Fix Rate",
                        Description = "Average number of bugs fixed per sprint.",
                        Value = 10.5,
                        TargetValue = 12.0,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                        OnTargetThresholdPercentage = 5.0,
                        OffTargetThresholdPercentage = 10.0,
                        OnTargetThresholdAbsolute = 1.0,
                        OffTargetThresholdAbsolute = 2.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 1 // Associated with Objective 1
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 3,
                        Name = "Feature Delivery",
                        Description = "Number of features delivered in the last sprint.",
                        Value = 7.0,
                        TargetValue = 8.0,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                        OnTargetThresholdPercentage = 10.0,
                        OffTargetThresholdPercentage = 15.0,
                        OnTargetThresholdAbsolute = 1.0,
                        OffTargetThresholdAbsolute = 2.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 2 // Associated with Objective 2
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 4,
                        Name = "Pull Request Review Time",
                        Description = "Average time taken to review pull requests (in hours).",
                        Value = 4.2,
                        TargetValue = 3.5,
                        TargetDirection = TargetDirectionEnum.LessOrEqual,
                        OnTargetThresholdPercentage = 5.0,
                        OffTargetThresholdPercentage = 10.0,
                        OnTargetThresholdAbsolute = 0.5,
                        OffTargetThresholdAbsolute = 1.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 2 // Associated with Objective 2
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 5,
                        Name = "Deployment Frequency",
                        Description = "Number of deployments to production per week.",
                        Value = 2.0,
                        TargetValue = 3.0,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                        OnTargetThresholdPercentage = 10.0,
                        OffTargetThresholdPercentage = 20.0,
                        OnTargetThresholdAbsolute = 0.5,
                        OffTargetThresholdAbsolute = 1.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 3 // Associated with Objective 3
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 6,
                        Name = "Lead Time for Changes",
                        Description =
                            "Average time from code committed to code successfully running in production (in days).",
                        Value = 5.0,
                        TargetValue = 4.0,
                        TargetDirection = TargetDirectionEnum.LessOrEqual,
                        OnTargetThresholdPercentage = 5.0,
                        OffTargetThresholdPercentage = 15.0,
                        OnTargetThresholdAbsolute = 0.5,
                        OffTargetThresholdAbsolute = 1.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 3 // Associated with Objective 3
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 7,
                        Name = "System Uptime",
                        Description = "Percentage of time the system is up and running.",
                        Value = 99.8,
                        TargetValue = 99.9,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                        OnTargetThresholdPercentage = 0.2,
                        OffTargetThresholdPercentage = 1.0,
                        OnTargetThresholdAbsolute = 0.1,
                        OffTargetThresholdAbsolute = 0.5,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 4 // Associated with Objective 4
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 8,
                        Name = "Customer Satisfaction",
                        Description = "Average customer satisfaction rating out of 5.",
                        Value = 4.3,
                        TargetValue = 4.5,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                        OnTargetThresholdPercentage = 5.0,
                        OffTargetThresholdPercentage = 10.0,
                        OnTargetThresholdAbsolute = 0.2,
                        OffTargetThresholdAbsolute = 0.5,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 4 // Associated with Objective 4
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 9,
                        Name = "Security Vulnerabilities",
                        Description = "Number of security vulnerabilities identified.",
                        Value = 2.0,
                        TargetValue = 0.0,
                        TargetDirection = TargetDirectionEnum.LessOrEqual,
                        OnTargetThresholdPercentage = 0.0,
                        OffTargetThresholdPercentage = 0.0,
                        OnTargetThresholdAbsolute = 0.0,
                        OffTargetThresholdAbsolute = 0.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 5 // Associated with Objective 5
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 10,
                        Name = "Code Quality",
                        Description = "Average code quality score (out of 10).",
                        Value = 8.2,
                        TargetValue = 9.0,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                        OnTargetThresholdPercentage = 10.0,
                        OffTargetThresholdPercentage = 20.0,
                        OnTargetThresholdAbsolute = 1.0,
                        OffTargetThresholdAbsolute = 2.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 5 // Associated with Objective 5
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 11,
                        Name = "Story Completion Rate",
                        Description = "Percentage of user stories completed in a sprint.",
                        Value = 92.0,
                        TargetValue = 95.0,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                        OnTargetThresholdPercentage = 5.0,
                        OffTargetThresholdPercentage = 10.0,
                        OnTargetThresholdAbsolute = 2.0,
                        OffTargetThresholdAbsolute = 5.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 6 // Associated with Objective 6
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 12,
                        Name = "Incident Resolution Time",
                        Description = "Average time taken to resolve incidents (in hours).",
                        Value = 6.5,
                        TargetValue = 5.0,
                        TargetDirection = TargetDirectionEnum.LessOrEqual,
                        OnTargetThresholdPercentage = 10.0,
                        OffTargetThresholdPercentage = 20.0,
                        OnTargetThresholdAbsolute = 1.0,
                        OffTargetThresholdAbsolute = 2.0,
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 6 // Associated with Objective 6
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 13,
                        Name = "Technical Debt",
                        Description = "Estimated amount of technical debt in the codebase.",
                        Value = 30.0,
                        TargetValue = 20.0,
                        TargetDirection = TargetDirectionEnum.LessOrEqual, // Lower technical debt is better
                        OnTargetThresholdPercentage = 10.0, // On target if within 10% of target
                        OffTargetThresholdPercentage = 20.0, // Off target if more than 20% above target
                        OnTargetThresholdAbsolute = 5.0, // On target if within 5 points
                        OffTargetThresholdAbsolute = 10.0, // Off target if more than 10 points above target
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 7 // Associated with Objective 7
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 14,
                        Name = "Automation Coverage",
                        Description = "Percentage of test cases automated.",
                        Value = 75.0,
                        TargetValue = 85.0,
                        TargetDirection = TargetDirectionEnum.GreaterOrEqual, // Higher automation coverage is better
                        OnTargetThresholdPercentage = 5.0, // On target if within 5% of target
                        OffTargetThresholdPercentage = 10.0, // Off target if more than 10% below target
                        OnTargetThresholdAbsolute = 3.0, // On target if within 3 percentage points
                        OffTargetThresholdAbsolute = 5.0, // Off target if more than 5 percentage points below target
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 7 // Associated with Objective 7
                    },
                    new KeyPerformanceIndicator
                    {
                        KpiId = 15,
                        Name = "Server Response Time",
                        Description = "Average server response time (in milliseconds).",
                        Value = 250.0,
                        TargetValue = 200.0,
                        TargetDirection = TargetDirectionEnum.LessOrEqual, // Faster response time is better
                        OnTargetThresholdPercentage = 10.0, // On target if within 10% of target
                        OffTargetThresholdPercentage = 20.0, // Off target if more than 20% above target
                        OnTargetThresholdAbsolute = 10.0, // On target if within 10ms
                        OffTargetThresholdAbsolute = 15.0, // Off target if more than 15ms above target
                        Owner = teamMembers[random.Next(teamMembers.Count)],
                        LastUpdated = today.AddDays(-random.Next(7)),
                        OkrId = 8
                    }
                };
        }
    }
}
