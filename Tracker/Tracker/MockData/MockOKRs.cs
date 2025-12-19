using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.MockData
{
    /// <summary>
    /// Mock OKR data with embedded KeyResults.
    /// This data matches the DatabaseSeeder's sample OKRs for consistency.
    /// 
    /// Status distribution: 2 On Track, 1 At Risk, 1 Off Track
    /// </summary>
    public static class MockOkRs
    {
        public static ObservableCollection<ObjectiveKeyResult> GetMockOkrData(List<TeamMember> teamMembers)
        { 
            if (teamMembers == null || teamMembers.Count == 0) return new ObservableCollection<ObjectiveKeyResult>();
            
            var today = DateTime.Today;
            var quarterStart = new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1);
            var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
            var currentQuarter = (today.Month - 1) / 3 + 1;
            var timePeriod = currentQuarter switch { 1 => TimePeriodEnum.Q1, 2 => TimePeriodEnum.Q2, 3 => TimePeriodEnum.Q3, _ => TimePeriodEnum.Q4 };

            var manager = teamMembers.Count > 0 ? teamMembers[0] : new TeamMember { FirstName = "Alex", LastName = "Rivera" };
            var jordan = teamMembers.Count > 1 ? teamMembers[1] : manager;
            var morgan = teamMembers.Count > 2 ? teamMembers[2] : manager;
            var riley = teamMembers.Count > 5 ? teamMembers[5] : manager;

            return new ObservableCollection<ObjectiveKeyResult>
            {
                // OKR 1: ON TRACK - Platform Performance (85% progress)
                new ObjectiveKeyResult
                {
                    ObjectiveId = 1,
                    Title = "Achieve World-Class Platform Performance",
                    Description = "Deliver sub-100ms response times and 99.9% uptime to provide best-in-class user experience",
                    Owner = jordan,
                    StartDate = quarterStart,
                    EndDate = quarterEnd,
                    TimePeriod = timePeriod,
                    Year = today.Year,
                    KeyResults = new List<KeyResult>
                    {
                        new KeyResult
                        {
                            Id = 1,
                            Title = "Reduce API p95 latency to 100ms",
                            Description = "Optimize database queries and implement caching",
                            CurrentValue = 120,
                            TargetValue = 100,
                            StartingValue = 200,
                            Unit = "ms",
                            TargetDirection = TargetDirectionEnum.LessOrEqual,
                            Weight = 1.5m,
                            SortOrder = 0
                        },
                        new KeyResult
                        {
                            Id = 2,
                            Title = "Maintain 99.9% system uptime",
                            Description = "Improve monitoring and incident response",
                            CurrentValue = 99.95m,
                            TargetValue = 99.9m,
                            StartingValue = 99.5m,
                            Unit = "%",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 1.0m,
                            SortOrder = 1
                        },
                        new KeyResult
                        {
                            Id = 3,
                            Title = "Reduce error rate to under 0.1%",
                            Description = "Fix bugs and improve error handling",
                            CurrentValue = 0.15m,
                            TargetValue = 0.1m,
                            StartingValue = 0.5m,
                            Unit = "%",
                            TargetDirection = TargetDirectionEnum.LessOrEqual,
                            Weight = 1.0m,
                            SortOrder = 2
                        }
                    }
                },

                // OKR 2: ON TRACK - API Modernization (78% progress)
                new ObjectiveKeyResult
                {
                    ObjectiveId = 2,
                    Title = "Complete API Modernization Initiative",
                    Description = "Migrate all REST endpoints to GraphQL and improve developer experience",
                    Owner = manager,
                    StartDate = quarterStart,
                    EndDate = quarterEnd,
                    TimePeriod = timePeriod,
                    Year = today.Year,
                    KeyResults = new List<KeyResult>
                    {
                        new KeyResult
                        {
                            Id = 4,
                            Title = "Migrate 100% of endpoints to GraphQL",
                            Description = "Complete migration of all 45 REST endpoints",
                            CurrentValue = 38,
                            TargetValue = 45,
                            StartingValue = 0,
                            Unit = "endpoints",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 2.0m,
                            SortOrder = 0
                        },
                        new KeyResult
                        {
                            Id = 5,
                            Title = "Achieve 90% test coverage on new APIs",
                            Description = "Comprehensive unit and integration tests",
                            CurrentValue = 87,
                            TargetValue = 90,
                            StartingValue = 60,
                            Unit = "%",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 1.0m,
                            SortOrder = 1
                        },
                        new KeyResult
                        {
                            Id = 6,
                            Title = "Complete API documentation",
                            Description = "Auto-generated docs from GraphQL schema",
                            CurrentValue = 75,
                            TargetValue = 100,
                            StartingValue = 20,
                            Unit = "%",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 0.5m,
                            SortOrder = 2
                        }
                    }
                },

                // OKR 3: AT RISK - Customer Dashboard (55% progress)
                new ObjectiveKeyResult
                {
                    ObjectiveId = 3,
                    Title = "Transform Customer Dashboard Experience",
                    Description = "Deliver modern, responsive dashboard with real-time insights",
                    Owner = morgan,
                    StartDate = quarterStart,
                    EndDate = quarterEnd,
                    TimePeriod = timePeriod,
                    Year = today.Year,
                    KeyResults = new List<KeyResult>
                    {
                        new KeyResult
                        {
                            Id = 7,
                            Title = "Launch new dashboard by quarter end",
                            Description = "Complete redesign with all planned features",
                            CurrentValue = 65,
                            TargetValue = 100,
                            StartingValue = 0,
                            Unit = "%",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 2.0m,
                            SortOrder = 0
                        },
                        new KeyResult
                        {
                            Id = 8,
                            Title = "Achieve 4.5 CSAT score",
                            Description = "User satisfaction with new dashboard",
                            CurrentValue = 4.0m,
                            TargetValue = 4.5m,
                            StartingValue = 3.8m,
                            Unit = "score",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 1.0m,
                            SortOrder = 1
                        },
                        new KeyResult
                        {
                            Id = 9,
                            Title = "Reduce page load time to 2s",
                            Description = "Optimize performance for all dashboard views",
                            CurrentValue = 2.8m,
                            TargetValue = 2.0m,
                            StartingValue = 4.5m,
                            Unit = "seconds",
                            TargetDirection = TargetDirectionEnum.LessOrEqual,
                            Weight = 1.0m,
                            SortOrder = 2
                        }
                    }
                },

                // OKR 4: OFF TRACK - Analytics Platform (40% progress)
                new ObjectiveKeyResult
                {
                    ObjectiveId = 4,
                    Title = "Build Real-Time Analytics Platform",
                    Description = "Enable real-time business insights and automated alerting",
                    Owner = riley,
                    StartDate = quarterStart,
                    EndDate = quarterEnd,
                    TimePeriod = timePeriod,
                    Year = today.Year,
                    StatusOverride = ObjectiveStatusEnum.OffTrack,
                    KeyResults = new List<KeyResult>
                    {
                        new KeyResult
                        {
                            Id = 10,
                            Title = "Process 10K events/second",
                            Description = "Scale streaming infrastructure",
                            CurrentValue = 4500,
                            TargetValue = 10000,
                            StartingValue = 1000,
                            Unit = "events/sec",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 1.5m,
                            SortOrder = 0
                        },
                        new KeyResult
                        {
                            Id = 11,
                            Title = "Deploy 15 real-time dashboards",
                            Description = "Customer-facing analytics widgets",
                            CurrentValue = 6,
                            TargetValue = 15,
                            StartingValue = 0,
                            Unit = "dashboards",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 1.0m,
                            SortOrder = 1
                        },
                        new KeyResult
                        {
                            Id = 12,
                            Title = "Implement 20 automated alerts",
                            Description = "Business metric threshold alerts",
                            CurrentValue = 5,
                            TargetValue = 20,
                            StartingValue = 0,
                            Unit = "alerts",
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Weight = 1.0m,
                            SortOrder = 2
                        }
                    }
                }
            };
        }
    }
}
