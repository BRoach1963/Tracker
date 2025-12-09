using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using System.Linq;

namespace Tracker.Database
{
    /// <summary>
    /// Seeds the database with sample data for demonstration purposes.
    /// </summary>
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with sample team members and related data.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="forceReseed">If true, clears existing data before seeding. If false, only seeds if database is empty.</param>
        public static async Task SeedSampleDataAsync(TrackerDbContext context, bool forceReseed = false)
        {
            // Clear existing data if force reseed is requested
            if (forceReseed && context.TeamMembers.Any())
            {
                await ClearAllDataAsync(context);
            }
            
            // Only seed if database is empty
            if (context.TeamMembers.Any())
            {
                return;
            }

            var teamMembers = GetSampleTeamMembers();
            
            // Add team members
            context.TeamMembers.AddRange(teamMembers);
            await context.SaveChangesAsync();

            // Add sample tasks first (needed for linking)
            var tasks = GetSampleTasks(teamMembers);
            context.Tasks.AddRange(tasks);
            await context.SaveChangesAsync();

            // Add sample projects FIRST (needed for OKR ProjectId foreign key)
            var projects = GetSampleProjects(teamMembers, new List<ObjectiveKeyResult>()); // Empty OKRs list initially
            context.Projects.AddRange(projects);
            await context.SaveChangesAsync();

            // Reload projects to get their IDs
            var savedProjects = await context.Projects.ToListAsync();

            // Add sample OKRs with KPIs (needed for linking) - now projects exist
            var okrs = GetSampleOKRs(teamMembers, savedProjects);
            context.ObjectiveKeyResults.AddRange(okrs);
            await context.SaveChangesAsync();

            // Add standalone KPIs (not tied to OKRs)
            var standaloneKpis = GetSampleStandaloneKPIs(teamMembers);
            context.KeyPerformanceIndicators.AddRange(standaloneKpis);
            await context.SaveChangesAsync();

            // Update projects to link OKRs (many-to-many relationship)
            if (savedProjects.Count > 0 && okrs.Count > 0)
            {
                savedProjects[0].OKRs = okrs.Take(4).ToList(); // Link first 4 OKRs to first project
                if (savedProjects.Count > 1 && okrs.Count > 4)
                {
                    savedProjects[1].OKRs = okrs.Skip(4).ToList(); // Link remaining OKRs to second project
                }
                context.Projects.UpdateRange(savedProjects);
                await context.SaveChangesAsync();
            }

            // Add sample 1:1s (can now link to tasks/OKRs/KPIs)
            var oneOnOnes = GetSampleOneOnOnes(teamMembers, tasks, okrs);
            context.OneOnOnes.AddRange(oneOnOnes);
            await context.SaveChangesAsync();

            // Reload entities to get their IDs after save (EF Core populates IDs, but reload ensures we have all data)
            var savedTasks = context.Tasks.Include(t => t.Owner).ToList();
            var savedOkrs = context.ObjectiveKeyResults.Include(o => o.KeyResults).Include(o => o.Owner).ToList();
            var savedOneOnOnes = context.OneOnOnes.Include(o => o.TeamMember).Where(o => o.Status == MeetingStatusEnum.Completed).ToList();

            // Link some tasks, OKRs, and KPIs to meetings (Phase 1 feature)
            await LinkItemsToMeetingsAsync(context, savedOneOnOnes, savedTasks, savedOkrs);
        }

        /// <summary>
        /// Clears all data from the database.
        /// </summary>
        public static async Task ClearAllDataAsync(TrackerDbContext context)
        {
            try
            {
                // Check if database exists and has the correct schema
                // If tables are missing (old schema), recreate the database
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect)
                {
                    // Try to query a table that should exist - if it fails, schema is outdated
                    try
                    {
                        _ = await context.TeamMembers.AnyAsync();
                    }
                    catch
                    {
                        // Schema is outdated, recreate database
                        await context.Database.EnsureDeletedAsync();
                        await context.Database.EnsureCreatedAsync();
                        return;
                    }
                }
                
                // Clear in reverse order of dependencies
                try { context.ChangeTrackingEntries.RemoveRange(context.ChangeTrackingEntries); } catch { }
                try { context.OneOnOneLinkedTasks.RemoveRange(context.OneOnOneLinkedTasks); } catch { }
                try { context.OneOnOneLinkedOkrs.RemoveRange(context.OneOnOneLinkedOkrs); } catch { }
                try { context.OneOnOneLinkedKpis.RemoveRange(context.OneOnOneLinkedKpis); } catch { }
                try { context.Concerns.RemoveRange(context.Concerns); } catch { }
                try { context.DiscussionPoints.RemoveRange(context.DiscussionPoints); } catch { }
                try { context.FollowUpItems.RemoveRange(context.FollowUpItems); } catch { }
                try { context.ActionItems.RemoveRange(context.ActionItems); } catch { }
                try { context.Tasks.RemoveRange(context.Tasks); } catch { }
                try { context.KeyPerformanceIndicators.RemoveRange(context.KeyPerformanceIndicators); } catch { }
                try { context.ObjectiveKeyResults.RemoveRange(context.ObjectiveKeyResults); } catch { }
                try { context.Milestones.RemoveRange(context.Milestones); } catch { }
                try { context.Risks.RemoveRange(context.Risks); } catch { }
                try { context.ProjectDependencies.RemoveRange(context.ProjectDependencies); } catch { }
                try { context.Projects.RemoveRange(context.Projects); } catch { }
                try { context.OneOnOnes.RemoveRange(context.OneOnOnes); } catch { }
                try { context.TeamMembers.RemoveRange(context.TeamMembers); } catch { }
                
                await context.SaveChangesAsync();
                
                // Ensure database schema is up to date (creates missing tables if needed)
                await context.Database.EnsureCreatedAsync();
            }
            catch (Exception)
            {
                // If clearing fails, try to recreate the database
                try
                {
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.EnsureCreatedAsync();
                }
                catch
                {
                    // If that fails, rethrow the original exception
                    throw;
                }
            }
        }

        #region Sample Data Generators

        private static List<TeamMember> GetSampleTeamMembers()
        {
            return new List<TeamMember>
            {
                new TeamMember
                {
                    FirstName = "Ben",
                    LastName = "Roethlisberger",
                    NickName = "Big Ben",
                    Email = "ben.roethlisberger@company.com",
                    CellPhone = "412-555-0007",
                    JobTitle = "Engineering Manager",
                    BirthDay = new DateTime(1982, 3, 2),
                    HireDate = new DateTime(2018, 1, 15),
                    IsActive = true,
                    ManagerId = 0,
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Manager,
                    LinkedInProfile = "https://www.linkedin.com/in/ben-roethlisberger",
                    XProfile = "https://twitter.com/benroethlisberger"
                },
                new TeamMember
                {
                    FirstName = "Troy",
                    LastName = "Polamalu",
                    Email = "troy.polamalu@company.com",
                    CellPhone = "412-555-0001",
                    JobTitle = "Senior Software Engineer",
                    BirthDay = new DateTime(1981, 4, 19),
                    HireDate = new DateTime(2019, 3, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.Backend,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer,
                    LinkedInProfile = "https://www.linkedin.com/in/troy-polamalu",
                    XProfile = "https://twitter.com/tpolamalu"
                },
                new TeamMember
                {
                    FirstName = "James",
                    LastName = "Farrior",
                    Email = "james.farrior@company.com",
                    CellPhone = "412-555-0002",
                    JobTitle = "Software Engineer",
                    BirthDay = new DateTime(1975, 1, 6),
                    HireDate = new DateTime(2020, 6, 15),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.WebUI,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer,
                    LinkedInProfile = "https://www.linkedin.com/in/james-farrior",
                    XProfile = "https://twitter.com/jamesfarrior"
                },
                new TeamMember
                {
                    FirstName = "Hines",
                    LastName = "Ward",
                    Email = "hines.ward@company.com",
                    CellPhone = "412-555-0003",
                    JobTitle = "Senior Software Engineer",
                    BirthDay = new DateTime(1976, 3, 8),
                    HireDate = new DateTime(2019, 8, 20),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer,
                    LinkedInProfile = "https://www.linkedin.com/in/hines-ward",
                    XProfile = "https://twitter.com/hinesward"
                },
                new TeamMember
                {
                    FirstName = "James",
                    LastName = "Harrison",
                    Email = "james.harrison@company.com",
                    CellPhone = "412-555-0004",
                    JobTitle = "Software Engineer",
                    BirthDay = new DateTime(1978, 5, 4),
                    HireDate = new DateTime(2021, 2, 10),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.Backend,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer,
                    LinkedInProfile = "https://www.linkedin.com/in/james-harrison",
                    XProfile = "https://twitter.com/jharrison92"
                },
                new TeamMember
                {
                    FirstName = "Cam",
                    LastName = "Heyward",
                    Email = "cam.heyward@company.com",
                    CellPhone = "412-555-0005",
                    JobTitle = "Senior Software Engineer",
                    BirthDay = new DateTime(1989, 5, 6),
                    HireDate = new DateTime(2020, 4, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.DataScience,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer,
                    LinkedInProfile = "https://www.linkedin.com/in/cam-heyward",
                    XProfile = "https://twitter.com/camheyward"
                },
                new TeamMember
                {
                    FirstName = "T.J.",
                    LastName = "Watt",
                    Email = "tj.watt@company.com",
                    CellPhone = "412-555-0006",
                    JobTitle = "Software Engineer",
                    BirthDay = new DateTime(1994, 10, 11),
                    HireDate = new DateTime(2022, 7, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.WebUI,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer,
                    LinkedInProfile = "https://www.linkedin.com/in/tj-watt",
                    XProfile = "https://twitter.com/tjwatt"
                }
            };
        }

        private static List<OneOnOne> GetSampleOneOnOnes(List<TeamMember> teamMembers, List<IndividualTask> tasks, List<ObjectiveKeyResult> okrs)
        {
            var oneOnOnes = new List<OneOnOne>();
            var random = new Random(42); // Fixed seed for reproducibility
            var manager = teamMembers[0];

            // Create multiple 1:1s for each team member (past and future)
            foreach (var member in teamMembers.Skip(1)) // Skip the manager
            {
                // Historical meetings - going back 3 months
                for (int i = 0; i < 12; i++) // 12 weeks = 3 months
                {
                    var meetingDate = DateTime.Today.AddDays(-(i * 7 + random.Next(0, 3))); // Weekly meetings with slight variation
                    var isRecent = i < 3; // Last 3 meetings are more detailed
                    
                    oneOnOnes.Add(new OneOnOne
                    {
                        Description = $"Weekly 1:1 with {member.FirstName}",
                        Date = meetingDate,
                        StartTime = new TimeSpan(10, 0, 0),
                        EndTime = new TimeSpan(10, 30, 0),
                        Duration = TimeSpan.FromMinutes(30),
                        Agenda = isRecent ? "Sprint retrospective, career goals, technical challenges" : "Weekly check-in and project updates",
                        Notes = isRecent ? $"Reviewed {member.FirstName}'s progress on current assignments. Discussed opportunities for growth." : $"Weekly check-in with {member.FirstName}. Discussed current work and any blockers.",
                        Feedback = isRecent ? "Excellent work on recent deliverables. Continue focusing on code quality." : "Good progress this week.",
                        IsRecurring = true,
                        Status = meetingDate < DateTime.Today ? MeetingStatusEnum.Completed : MeetingStatusEnum.Scheduled,
                        TeamMember = member,
                        ActionItems = isRecent && i == 0 ? new List<ActionItem>
                        {
                            new ActionItem
                            {
                                Description = $"Complete code review training for {member.FirstName}",
                                DueDate = DateTime.Today.AddDays(-7),
                                IsCompleted = true,
                                Notes = "Training completed successfully",
                                Owner = member
                            },
                            new ActionItem
                            {
                                Description = $"Follow up on {member.FirstName}'s certification plan",
                                DueDate = DateTime.Today.AddDays(7),
                                IsCompleted = false,
                                Owner = member
                            }
                        } : (isRecent && i == 1 ? new List<ActionItem>
                        {
                            new ActionItem
                            {
                                Description = $"Update project documentation for {member.FirstName}'s current work",
                                DueDate = DateTime.Today.AddDays(3),
                                IsCompleted = false,
                                Owner = member
                            }
                        } : new List<ActionItem>()),
                        FollowUpItems = isRecent && i == 0 ? new List<FollowUpItem>
                        {
                            new FollowUpItem
                            {
                                Description = $"Schedule architecture review session with {member.FirstName}",
                                DueDate = DateTime.Today.AddDays(14),
                                IsCompleted = false,
                                Owner = manager
                            }
                        } : new List<FollowUpItem>(),
                        DiscussionPoints = isRecent && i == 0 ? new List<DiscussionPoint>
                        {
                            new DiscussionPoint
                            {
                                Description = "Sprint velocity and capacity planning",
                                Type = DiscussionType.Project,
                                DateRaised = meetingDate,
                                PriorityLevel = Severity.Medium
                            },
                            new DiscussionPoint
                            {
                                Description = "Technical debt reduction strategy",
                                Type = DiscussionType.Process,
                                DateRaised = meetingDate,
                                PriorityLevel = Severity.High
                            }
                        } : (isRecent && i == 1 ? new List<DiscussionPoint>
                        {
                            new DiscussionPoint
                            {
                                Description = "Cross-team collaboration improvements",
                                Type = DiscussionType.TeamDynamics,
                                DateRaised = meetingDate,
                                PriorityLevel = Severity.Medium
                            }
                        } : new List<DiscussionPoint>()),
                        Concerns = isRecent && i == 0 ? new List<Concern>
                        {
                            new Concern
                            {
                                Description = "Workload balance concerns",
                                SeverityLevel = Severity.Low,
                                DateRaised = meetingDate,
                                Details = "Feeling stretched across multiple projects"
                            }
                        } : new List<Concern>()
                    });
                }
            }

            return oneOnOnes;
        }

        private static List<IndividualTask> GetSampleTasks(List<TeamMember> teamMembers)
        {
            var tasks = new List<IndividualTask>
            {
                // Tasks for Troy Polamalu (Sr Dev - Backend)
                new IndividualTask
                {
                    Description = "Refactor authentication service for better performance",
                    DueDate = DateTime.Today.AddDays(10),
                    IsCompleted = false,
                    Notes = "Focus on reducing latency and improving error handling",
                    Owner = teamMembers[1] // Troy
                },
                new IndividualTask
                {
                    Description = "Design new microservices architecture",
                    DueDate = DateTime.Today.AddDays(14),
                    IsCompleted = false,
                    Notes = "Create technical design document for team review",
                    Owner = teamMembers[1] // Troy
                },
                new IndividualTask
                {
                    Description = "Complete API documentation for payment service",
                    DueDate = DateTime.Today.AddDays(5),
                    IsCompleted = false,
                    Notes = "Need to document all REST endpoints and request/response schemas",
                    Owner = teamMembers[1] // Troy
                },
                
                // Tasks for James Farrior (Dev - WebUI)
                new IndividualTask
                {
                    Description = "Implement new dashboard UI components",
                    DueDate = DateTime.Today.AddDays(7),
                    IsCompleted = false,
                    Notes = "Using React and TypeScript, follow design system guidelines",
                    Owner = teamMembers[2] // James F
                },
                new IndividualTask
                {
                    Description = "Fix accessibility issues in user profile page",
                    DueDate = DateTime.Today.AddDays(3),
                    IsCompleted = true,
                    Notes = "Added ARIA labels and keyboard navigation support",
                    Owner = teamMembers[2] // James F
                },
                
                // Tasks for Hines Ward (Sr Dev - FullStack)
                new IndividualTask
                {
                    Description = "Review and merge pending pull requests",
                    DueDate = DateTime.Today.AddDays(1),
                    IsCompleted = false,
                    Notes = "5 PRs pending review - prioritize critical bug fixes",
                    Owner = teamMembers[3] // Hines
                },
                new IndividualTask
                {
                    Description = "Optimize database queries for reporting module",
                    DueDate = DateTime.Today.AddDays(12),
                    IsCompleted = false,
                    Notes = "Current queries are too slow, need to add indexes",
                    Owner = teamMembers[3] // Hines
                },
                
                // Tasks for James Harrison (Dev - Backend)
                new IndividualTask
                {
                    Description = "Write integration tests for order processing service",
                    DueDate = DateTime.Today.AddDays(8),
                    IsCompleted = false,
                    Notes = "Cover all edge cases and error scenarios",
                    Owner = teamMembers[4] // James H
                },
                new IndividualTask
                {
                    Description = "Deploy new version to staging environment",
                    DueDate = DateTime.Today.AddDays(2),
                    IsCompleted = false,
                    Notes = "Waiting for QA sign-off before deployment",
                    Owner = teamMembers[4] // James H
                },
                
                // Tasks for Cam Heyward (Sr Dev - DataScience)
                new IndividualTask
                {
                    Description = "Build predictive analytics model for user behavior",
                    DueDate = DateTime.Today.AddDays(15),
                    IsCompleted = false,
                    Notes = "Using machine learning to predict churn and engagement",
                    Owner = teamMembers[5] // Cam
                },
                new IndividualTask
                {
                    Description = "Create data pipeline for real-time analytics",
                    DueDate = DateTime.Today.AddDays(20),
                    IsCompleted = false,
                    Notes = "Stream processing with Kafka and Spark",
                    Owner = teamMembers[5] // Cam
                },
                
                // Tasks for T.J. Watt (Dev - WebUI)
                new IndividualTask
                {
                    Description = "Implement responsive design for mobile app",
                    DueDate = DateTime.Today.AddDays(6),
                    IsCompleted = false,
                    Notes = "Ensure all components work well on mobile devices",
                    Owner = teamMembers[6] // T.J.
                },
                new IndividualTask
                {
                    Description = "Update component library documentation",
                    DueDate = DateTime.Today.AddDays(4),
                    IsCompleted = true,
                    Notes = "Added examples and usage guidelines for all components",
                    Owner = teamMembers[6] // T.J.
                }
            };

            return tasks;
        }

        private static List<ObjectiveKeyResult> GetSampleOKRs(List<TeamMember> teamMembers, List<Project> projects)
        {
            return new List<ObjectiveKeyResult>
            {
                new ObjectiveKeyResult
                {
                    Title = "Improve System Performance",
                    Description = "Optimize application performance to handle increased load and improve user experience",
                    Owner = teamMembers[1], // Troy - Sr Backend Dev
                    StartDate = DateTime.Today.AddMonths(-1),
                    EndDate = DateTime.Today.AddMonths(2),
                    ProjectId = projects.Count > 0 ? projects[0].ID : 0,
                    KeyResults = new List<KeyPerformanceIndicator>
                    {
                        new KeyPerformanceIndicator
                        {
                            Name = "API Response Time",
                            Description = "Average API response time in milliseconds",
                            Value = 150,
                            TargetValue = 100,
                            TargetDirection = TargetDirectionEnum.LessOrEqual,
                            Owner = teamMembers[1], // Troy
                            LastUpdated = DateTime.Today.AddDays(-2)
                        },
                        new KeyPerformanceIndicator
                        {
                            Name = "Error Rate",
                            Description = "Percentage of requests resulting in errors",
                            Value = 0.5,
                            TargetValue = 0.1,
                            TargetDirection = TargetDirectionEnum.LessOrEqual,
                            Owner = teamMembers[1], // Troy
                            LastUpdated = DateTime.Today.AddDays(-1)
                        },
                        new KeyPerformanceIndicator
                        {
                            Name = "Database Query Time",
                            Description = "Average database query execution time",
                            Value = 250,
                            TargetValue = 150,
                            TargetDirection = TargetDirectionEnum.LessOrEqual,
                            Owner = teamMembers[3], // Hines
                            LastUpdated = DateTime.Today
                        }
                    }
                },
                new ObjectiveKeyResult
                {
                    Title = "Increase Test Coverage",
                    Description = "Improve code quality through comprehensive testing and reduce production bugs",
                    Owner = teamMembers[0], // Ben - Manager
                    StartDate = DateTime.Today.AddMonths(-2),
                    EndDate = DateTime.Today.AddMonths(1),
                    ProjectId = projects.Count > 0 ? projects[0].ID : 0,
                    KeyResults = new List<KeyPerformanceIndicator>
                    {
                        new KeyPerformanceIndicator
                        {
                            Name = "Unit Test Coverage",
                            Description = "Percentage of code covered by unit tests",
                            Value = 72,
                            TargetValue = 80,
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Owner = teamMembers[2], // James Farrior
                            LastUpdated = DateTime.Today.AddDays(-3)
                        },
                        new KeyPerformanceIndicator
                        {
                            Name = "Integration Tests",
                            Description = "Number of integration tests",
                            Value = 45,
                            TargetValue = 60,
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Owner = teamMembers[4], // James Harrison
                            LastUpdated = DateTime.Today.AddDays(-5)
                        }
                    }
                },
                new ObjectiveKeyResult
                {
                    Title = "Enhance User Experience",
                    Description = "Improve frontend performance and user interface responsiveness",
                    Owner = teamMembers[3], // Hines - Sr FullStack
                    StartDate = DateTime.Today.AddMonths(-1),
                    EndDate = DateTime.Today.AddMonths(3),
                    ProjectId = projects.Count > 0 ? projects[0].ID : 0,
                    KeyResults = new List<KeyPerformanceIndicator>
                    {
                        new KeyPerformanceIndicator
                        {
                            Name = "Page Load Time",
                            Description = "Average page load time in seconds",
                            Value = 3.2,
                            TargetValue = 2.0,
                            TargetDirection = TargetDirectionEnum.LessOrEqual,
                            Owner = teamMembers[6], // T.J. Watt
                            LastUpdated = DateTime.Today.AddDays(-1)
                        },
                        new KeyPerformanceIndicator
                        {
                            Name = "User Satisfaction Score",
                            Description = "Average user satisfaction rating (1-5 scale)",
                            Value = 3.8,
                            TargetValue = 4.5,
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Owner = teamMembers[2], // James Farrior
                            LastUpdated = DateTime.Today.AddDays(-7)
                        }
                    }
                },
                new ObjectiveKeyResult
                {
                    Title = "Data Analytics Platform",
                    Description = "Build robust data analytics platform for business intelligence",
                    Owner = teamMembers[5], // Cam - Sr DataScience
                    StartDate = DateTime.Today.AddMonths(-2),
                    EndDate = DateTime.Today.AddMonths(4),
                    ProjectId = projects.Count > 0 ? projects[0].ID : 0,
                    KeyResults = new List<KeyPerformanceIndicator>
                    {
                        new KeyPerformanceIndicator
                        {
                            Name = "Data Processing Speed",
                            Description = "Records processed per second",
                            Value = 5000,
                            TargetValue = 10000,
                            TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                            Owner = teamMembers[5], // Cam
                            LastUpdated = DateTime.Today.AddDays(-2)
                        },
                        new KeyPerformanceIndicator
                        {
                            Name = "Report Generation Time",
                            Description = "Average time to generate analytics reports (seconds)",
                            Value = 45,
                            TargetValue = 20,
                            TargetDirection = TargetDirectionEnum.LessOrEqual,
                            Owner = teamMembers[5], // Cam
                            LastUpdated = DateTime.Today
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Generates standalone KPIs that are not part of OKRs.
        /// </summary>
        private static List<KeyPerformanceIndicator> GetSampleStandaloneKPIs(List<TeamMember> teamMembers)
        {
            return new List<KeyPerformanceIndicator>
            {
                new KeyPerformanceIndicator
                {
                    Name = "Code Review Turnaround Time",
                    Description = "Average time from PR submission to review completion (hours)",
                    Value = 8.5,
                    TargetValue = 4.0,
                    TargetDirection = TargetDirectionEnum.LessOrEqual,
                    Owner = teamMembers[0], // Ben - Manager
                    LastUpdated = DateTime.Today.AddDays(-1)
                },
                new KeyPerformanceIndicator
                {
                    Name = "Deployment Frequency",
                    Description = "Number of deployments per week",
                    Value = 3,
                    TargetValue = 5,
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = teamMembers[1], // Troy
                    LastUpdated = DateTime.Today.AddDays(-2)
                },
                new KeyPerformanceIndicator
                {
                    Name = "Bug Escape Rate",
                    Description = "Percentage of bugs found in production vs total bugs",
                    Value = 12.5,
                    TargetValue = 5.0,
                    TargetDirection = TargetDirectionEnum.LessOrEqual,
                    Owner = teamMembers[2], // James Farrior
                    LastUpdated = DateTime.Today.AddDays(-3)
                },
                new KeyPerformanceIndicator
                {
                    Name = "Customer Satisfaction Score",
                    Description = "Average customer satisfaction rating (1-10 scale)",
                    Value = 7.2,
                    TargetValue = 8.5,
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = teamMembers[3], // Hines
                    LastUpdated = DateTime.Today.AddDays(-5)
                },
                new KeyPerformanceIndicator
                {
                    Name = "System Uptime",
                    Description = "Percentage of time system is available",
                    Value = 99.2,
                    TargetValue = 99.9,
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = teamMembers[4], // James Harrison
                    LastUpdated = DateTime.Today.AddDays(-1)
                },
                new KeyPerformanceIndicator
                {
                    Name = "Data Processing Accuracy",
                    Description = "Percentage of data processed without errors",
                    Value = 98.5,
                    TargetValue = 99.5,
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = teamMembers[5], // Cam
                    LastUpdated = DateTime.Today.AddDays(-2)
                },
                new KeyPerformanceIndicator
                {
                    Name = "Mobile App Crash Rate",
                    Description = "Percentage of app sessions that end in a crash",
                    Value = 0.8,
                    TargetValue = 0.1,
                    TargetDirection = TargetDirectionEnum.LessOrEqual,
                    Owner = teamMembers[6], // T.J.
                    LastUpdated = DateTime.Today.AddDays(-1)
                },
                new KeyPerformanceIndicator
                {
                    Name = "Team Velocity",
                    Description = "Story points completed per sprint",
                    Value = 42,
                    TargetValue = 50,
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = teamMembers[0], // Ben - Manager
                    LastUpdated = DateTime.Today.AddDays(-7)
                },
                new KeyPerformanceIndicator
                {
                    Name = "Security Vulnerability Count",
                    Description = "Number of open security vulnerabilities",
                    Value = 3,
                    TargetValue = 0,
                    TargetDirection = TargetDirectionEnum.LessOrEqual,
                    Owner = teamMembers[1], // Troy
                    LastUpdated = DateTime.Today
                },
                new KeyPerformanceIndicator
                {
                    Name = "Documentation Coverage",
                    Description = "Percentage of APIs with complete documentation",
                    Value = 75,
                    TargetValue = 95,
                    TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                    Owner = teamMembers[2], // James Farrior
                    LastUpdated = DateTime.Today.AddDays(-4)
                }
            };
        }

        private static List<Project> GetSampleProjects(List<TeamMember> teamMembers, List<ObjectiveKeyResult>? okrs = null)
        {
            return new List<Project>
            {
                new Project
                {
                    Name = "Platform Modernization",
                    Description = "Modernize the legacy platform with new technologies and improved architecture",
                    StartDate = DateTime.Today.AddMonths(-3),
                    EndDate = DateTime.Today.AddMonths(6),
                    Status = "In Progress",
                    Owner = teamMembers[0],
                    TeamMembers = teamMembers.Take(4).ToList(),
                    Budget = 150000m,
                    OKRs = okrs ?? new List<ObjectiveKeyResult>(),
                    Milestones = new List<Milestone>
                    {
                        new Milestone
                        {
                            Name = "Phase 1: Assessment",
                            Description = "Complete technical assessment and planning",
                            TargetDate = DateTime.Today.AddMonths(-2),
                            IsAchieved = true
                        },
                        new Milestone
                        {
                            Name = "Phase 2: Core Migration",
                            Description = "Migrate core services to new architecture",
                            TargetDate = DateTime.Today.AddMonths(2),
                            IsAchieved = false
                        },
                        new Milestone
                        {
                            Name = "Phase 3: Launch",
                            Description = "Production deployment and go-live",
                            TargetDate = DateTime.Today.AddMonths(6),
                            IsAchieved = false
                        }
                    },
                    Risks = new List<Risk>
                    {
                        new Risk
                        {
                            Name = "Resource Constraints",
                            Description = "Team capacity may be impacted by other priorities",
                            RiskLevel = RiskLevelEnum.Medium,
                            Likelihood = RiskLevelEnum.Medium,
                            Impact = RiskLevelEnum.High,
                            MitigationStrategy = "Early identification of resource conflicts and cross-training",
                            IdentifiedDate = DateTime.Today.AddMonths(-2),
                            IsMitigated = false
                        }
                    }
                },
                new Project
                {
                    Name = "Mobile App Development",
                    Description = "Develop native mobile applications for iOS and Android",
                    StartDate = DateTime.Today.AddMonths(-1),
                    EndDate = DateTime.Today.AddMonths(4),
                    Status = "Planning",
                    Owner = teamMembers[3],
                    TeamMembers = new List<TeamMember> { teamMembers[3], teamMembers[4] },
                    Budget = 80000m,
                    Milestones = new List<Milestone>
                    {
                        new Milestone
                        {
                            Name = "Design Complete",
                            Description = "Complete UI/UX design and prototypes",
                            TargetDate = DateTime.Today.AddMonths(1),
                            IsAchieved = false
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Links existing tasks, OKRs, and KPIs to 1:1 meetings (Phase 1 feature).
        /// </summary>
        private static async Task LinkItemsToMeetingsAsync(
            TrackerDbContext context,
            List<OneOnOne> oneOnOnes,
            List<IndividualTask> tasks,
            List<ObjectiveKeyResult> okrs)
        {
            var random = new Random(42);
            var allKpis = okrs.SelectMany(o => o.KeyResults).ToList();
            
            // Get standalone KPIs
            var standaloneKpis = await context.KeyPerformanceIndicators
                .Where(k => !okrs.Any(o => o.KeyResults.Any(kr => kr.KpiId == k.KpiId)))
                .ToListAsync();
            allKpis.AddRange(standaloneKpis);

            foreach (var meeting in oneOnOnes.Where(m => m.Status == MeetingStatusEnum.Completed))
            {
                // Link 1-3 tasks to each completed meeting (more variety)
                var availableTasks = tasks.Where(t => t.Owner.Id == meeting.TeamMember.Id).ToList();
                if (availableTasks.Any())
                {
                    var tasksToLink = availableTasks.OrderBy(x => random.Next()).Take(random.Next(1, Math.Min(4, availableTasks.Count + 1))).ToList();
                    foreach (var task in tasksToLink)
                    {
                        context.OneOnOneLinkedTasks.Add(new OneOnOneLinkedTask
                        {
                            OneOnOneId = meeting.Id,
                            TaskId = task.Id,
                            DiscussionNotes = $"Discussed progress on {task.Description} during the meeting."
                        });
                    }
                }

                // Link 0-1 OKR to some meetings (increased chance for recent meetings)
                var isRecentMeeting = meeting.Date >= DateTime.Today.AddDays(-14);
                var okrChance = isRecentMeeting ? 60 : 30; // 60% for recent, 30% for older
                if (random.Next(100) < okrChance)
                {
                    var availableOkrs = okrs.Where(o => o.Owner.Id == meeting.TeamMember.Id || 
                                                         o.KeyResults.Any(k => k.Owner.Id == meeting.TeamMember.Id)).ToList();
                    if (availableOkrs.Any())
                    {
                        var okrToLink = availableOkrs.OrderBy(x => random.Next()).First();
                        context.OneOnOneLinkedOkrs.Add(new OneOnOneLinkedOkr
                        {
                            OneOnOneId = meeting.Id,
                            OkrId = okrToLink.ObjectiveId,
                            DiscussionNotes = $"Reviewed progress on {okrToLink.Title} objective."
                        });
                    }
                }

                // Link 0-2 KPIs to some meetings (increased chance and count)
                var kpiChance = isRecentMeeting ? 50 : 25; // 50% for recent, 25% for older
                if (random.Next(100) < kpiChance)
                {
                    var availableKpis = allKpis.Where(k => k.Owner.Id == meeting.TeamMember.Id).ToList();
                    if (availableKpis.Any())
                    {
                        var kpisToLink = availableKpis.OrderBy(x => random.Next()).Take(random.Next(1, Math.Min(3, availableKpis.Count + 1))).ToList();
                        foreach (var kpi in kpisToLink)
                        {
                            context.OneOnOneLinkedKpis.Add(new OneOnOneLinkedKpi
                            {
                                OneOnOneId = meeting.Id,
                                KpiId = kpi.KpiId,
                                DiscussionNotes = $"Discussed {kpi.Name} metrics and current performance."
                            });
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        #endregion
    }
}

