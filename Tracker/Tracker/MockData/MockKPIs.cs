using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.MockData
{
    /// <summary>
    /// Mock KPI data - standalone KPIs that can be linked to Key Results.
    /// This data matches the DatabaseSeeder's sample KPIs for consistency.
    /// 
    /// Status distribution: 4 On Target, 3 Close, 3 Off Target
    /// </summary>
    public static class MockKpIs
    {
        public static ObservableCollection<KeyPerformanceIndicator> GetMockKpiData(List<TeamMember> teamMembers)
        { 
            if (teamMembers == null || teamMembers.Count == 0) return new ObservableCollection<KeyPerformanceIndicator>();
            
            var today = DateTime.Today;
            var manager = teamMembers.Count > 0 ? teamMembers[0] : new TeamMember { FirstName = "Alex", LastName = "Rivera" };
            var jordan = teamMembers.Count > 1 ? teamMembers[1] : manager;
            var morgan = teamMembers.Count > 2 ? teamMembers[2] : manager;
            var taylor = teamMembers.Count > 3 ? teamMembers[3] : manager;
            var casey = teamMembers.Count > 4 ? teamMembers[4] : manager;

            return new ObservableCollection<KeyPerformanceIndicator>
            {
                // ON TARGET (Green) - 4 KPIs
                new KeyPerformanceIndicator
                {
                    KpiId = 1,
                    Name = "System Uptime",
                    Description = "Percentage of time production systems are available",
                    Value = 99.95,
                    TargetValue = 99.9,
                    Unit = "%",
                    Category = "Reliability",
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = jordan,
                    LastUpdated = today.AddDays(-1),
                    Frequency = KpiFrequencyEnum.Daily
                },
                new KeyPerformanceIndicator
                {
                    KpiId = 2,
                    Name = "Unit Test Coverage",
                    Description = "Percentage of code covered by unit tests",
                    Value = 87,
                    TargetValue = 80,
                    Unit = "%",
                    Category = "Quality",
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = manager,
                    LastUpdated = today.AddDays(-2),
                    Frequency = KpiFrequencyEnum.Weekly
                },
                new KeyPerformanceIndicator
                {
                    KpiId = 3,
                    Name = "Deployment Success Rate",
                    Description = "Percentage of deployments without rollbacks",
                    Value = 98,
                    TargetValue = 95,
                    Unit = "%",
                    Category = "Delivery",
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = taylor,
                    LastUpdated = today.AddDays(-1),
                    Frequency = KpiFrequencyEnum.Weekly
                },
                new KeyPerformanceIndicator
                {
                    KpiId = 4,
                    Name = "Sprint Velocity",
                    Description = "Story points completed per sprint",
                    Value = 52,
                    TargetValue = 45,
                    Unit = "points",
                    Category = "Delivery",
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = manager,
                    LastUpdated = today.AddDays(-3),
                    Frequency = KpiFrequencyEnum.BiWeekly
                },

                // CLOSE TO TARGET (Amber) - 3 KPIs
                new KeyPerformanceIndicator
                {
                    KpiId = 5,
                    Name = "API Response Time (p95)",
                    Description = "95th percentile API response latency",
                    Value = 145,
                    TargetValue = 100,
                    Unit = "ms",
                    Category = "Performance",
                    TargetDirection = TargetDirectionEnum.LessOrEqual,
                    Owner = jordan,
                    LastUpdated = today,
                    Frequency = KpiFrequencyEnum.Daily
                },
                new KeyPerformanceIndicator
                {
                    KpiId = 6,
                    Name = "Code Review Turnaround",
                    Description = "Average hours from PR open to first review",
                    Value = 6.5,
                    TargetValue = 4,
                    Unit = "hours",
                    Category = "Efficiency",
                    TargetDirection = TargetDirectionEnum.LessOrEqual,
                    Owner = morgan,
                    LastUpdated = today.AddDays(-1),
                    Frequency = KpiFrequencyEnum.Weekly
                },
                new KeyPerformanceIndicator
                {
                    KpiId = 7,
                    Name = "Customer Satisfaction (CSAT)",
                    Description = "Customer satisfaction score from surveys",
                    Value = 4.2,
                    TargetValue = 4.5,
                    Unit = "score",
                    Category = "Customer",
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = manager,
                    LastUpdated = today.AddDays(-5),
                    Frequency = KpiFrequencyEnum.Monthly
                },

                // OFF TARGET (Red) - 3 KPIs
                new KeyPerformanceIndicator
                {
                    KpiId = 8,
                    Name = "Bug Escape Rate",
                    Description = "Percentage of bugs found in production",
                    Value = 15,
                    TargetValue = 5,
                    Unit = "%",
                    Category = "Quality",
                    TargetDirection = TargetDirectionEnum.LessOrEqual,
                    Owner = casey,
                    LastUpdated = today.AddDays(-2),
                    Frequency = KpiFrequencyEnum.Weekly
                },
                new KeyPerformanceIndicator
                {
                    KpiId = 9,
                    Name = "Open Security Vulnerabilities",
                    Description = "Count of unresolved security issues",
                    Value = 7,
                    TargetValue = 0,
                    Unit = "issues",
                    Category = "Security",
                    TargetDirection = TargetDirectionEnum.LessOrEqual,
                    Owner = jordan,
                    LastUpdated = today,
                    Frequency = KpiFrequencyEnum.Weekly
                },
                new KeyPerformanceIndicator
                {
                    KpiId = 10,
                    Name = "Documentation Coverage",
                    Description = "Percentage of APIs with complete docs",
                    Value = 62,
                    TargetValue = 90,
                    Unit = "%",
                    Category = "Quality",
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = taylor,
                    LastUpdated = today.AddDays(-4),
                    Frequency = KpiFrequencyEnum.Monthly
                }
            };
        }
    }
}
