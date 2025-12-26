using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Interfaces;
using System.Linq;
using System.Data.Common;

namespace Tracker.Database
{
    /// <summary>
    /// Seeds the database with sample data for demonstration purposes.
    /// 
    /// Sample Data Story: "Q1 2025 Engineering Team"
    /// - A manager overseeing 6 engineers with diverse specialties
    /// - Quarterly OKRs with clear progress showing app value
    /// - KPIs linked to Key Results (demonstrating IMeasurable)
    /// - Projects with tasks feeding Key Results
    /// - Task Collections for grouped tracking
    /// - Optimized status distribution for impactful dashboard visualization
    /// </summary>
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with sample team members and related data.
        /// </summary>
        public static async Task<bool> SeedSampleDataAsync(TrackerDbContext context, bool forceReseed = false)
        {
            // Clear existing data if force reseed is requested
            if (forceReseed)
            {
                var hasData = await context.TeamMembers.AnyAsync() ||
                              await context.Projects.AnyAsync() ||
                              await context.Tasks.AnyAsync() ||
                              await context.ObjectiveKeyResults.AnyAsync() ||
                              await context.KeyPerformanceIndicators.AnyAsync() ||
                              await context.OneOnOnes.AnyAsync();
                
                if (hasData)
                {
                    await ClearAllDataAsync(context);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                }
            }
            
            context.ChangeTracker.Clear();
            
            // Check if database is empty using raw SQL
            var connection = context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM TeamMembers";
                var count = await command.ExecuteScalarAsync();
                var hasTeamMembers = Convert.ToInt32(count) > 0;
                
                if (hasTeamMembers && !forceReseed)
                    return false;
                
                if (hasTeamMembers && forceReseed)
                {
                    await ClearAllDataAsync(context);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                    
                    command.CommandText = "SELECT COUNT(*) FROM TeamMembers";
                    count = await command.ExecuteScalarAsync();
                    if (Convert.ToInt32(count) > 0)
                        return false;
                }
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }

            try
            {
                var logger = Tracker.Logging.LoggingManager.GetComponentLogger("DatabaseSeeder");
                
                // Disable query filters during seeding (we're setting UserId manually)
                context.CurrentUserId = null;
                
                // Use a transaction for atomicity - all or nothing
                using var transaction = await context.Database.BeginTransactionAsync();
                
                try
                {
                    // STEP 1: Create or get the current User (manager)
                    var username = Tracker.Managers.UserSettingsManager.Instance.CurrentUser;
                    logger.Info("Seeder: CurrentUser from UserSettingsManager = '{0}'", username ?? "(null)");
                    
                    if (string.IsNullOrEmpty(username))
                        username = Environment.UserName;
                    
                    logger.Info("Seeder: Using username = '{0}'", username);
                    
                    var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
                    User currentUser;
                    
                    if (existingUser == null)
                    {
                        logger.Info("Seeder: No existing user found, creating new user");
                        currentUser = new User
                        {
                            Username = username,
                            Email = $"{username}@techcorp.com",
                            DisplayName = username,
                            IsActive = true
                        };
                        context.Users.Add(currentUser);
                        await context.SaveChangesAsync();
                        currentUser = await context.Users.FirstAsync(u => u.Username == username);
                        logger.Info("Seeder: Created new user with Id = {0}", currentUser.Id);
                    }
                    else
                    {
                        currentUser = existingUser;
                        logger.Info("Seeder: Found existing user with Id = {0}", currentUser.Id);
                    }
                    
                    Tracker.Managers.UserSettingsManager.Instance.CurrentUserId = currentUser.Id;
                    logger.Info("Seeder: Set CurrentUserId to {0}", currentUser.Id);
                    
                    // STEP 2: Create team members with consistent, meaningful assignments
                    var teamMembers = GetSampleTeamMembers();
                    context.TeamMembers.AddRange(teamMembers);
                    foreach (var teamMember in teamMembers)
                    {
                        context.Entry(teamMember).Property("UserId").CurrentValue = currentUser.Id;
                    }
                    await context.SaveChangesAsync();
                    var savedTeamMembers = await context.TeamMembers.IgnoreQueryFilters().ToListAsync();

                    // STEP 3: Create Projects FIRST (needed for task/OKR links)
                    var projects = GetSampleProjects(savedTeamMembers);
                    context.Projects.AddRange(projects);
                    foreach (var project in projects)
                    {
                        context.Entry(project).Property("UserId").CurrentValue = currentUser.Id;
                        context.Entry(project).Property("OwnerId").CurrentValue = project.Owner.Id;
                        foreach (var milestone in project.Milestones ?? new List<Milestone>())
                            context.Entry(milestone).Property("UserId").CurrentValue = currentUser.Id;
                        foreach (var risk in project.Risks ?? new List<Risk>())
                            context.Entry(risk).Property("UserId").CurrentValue = currentUser.Id;
                    }
                    await context.SaveChangesAsync();
                    var savedProjects = await context.Projects.IgnoreQueryFilters().Include(p => p.Tasks).ToListAsync();

                    // STEP 4: Create Tasks linked to Projects
                    var tasks = GetSampleTasks(savedTeamMembers, savedProjects);
                    context.Tasks.AddRange(tasks);
                    foreach (var task in tasks)
                    {
                        context.Entry(task).Property("UserId").CurrentValue = currentUser.Id;
                        context.Entry(task).Property("OwnerId").CurrentValue = task.Owner.Id;
                    }
                    await context.SaveChangesAsync();
                    var savedTasks = await context.Tasks.IgnoreQueryFilters().Include(t => t.Owner).ToListAsync();

                    // Reload projects with tasks
                    savedProjects = await context.Projects.IgnoreQueryFilters().Include(p => p.Tasks).ToListAsync();

                    // STEP 5: Create standalone KPIs (these will be linked to Key Results)
                    var kpis = GetSampleKPIs(savedTeamMembers);
                    context.KeyPerformanceIndicators.AddRange(kpis);
                    foreach (var kpi in kpis)
                    {
                        context.Entry(kpi).Property("UserId").CurrentValue = currentUser.Id;
                        context.Entry(kpi).Property("OwnerId").CurrentValue = kpi.Owner.Id;
                    }
                    await context.SaveChangesAsync();
                    var savedKpis = await context.KeyPerformanceIndicators.IgnoreQueryFilters().ToListAsync();

                    // STEP 6: Create Task Collections (for grouped task tracking)
                    var taskCollections = GetSampleTaskCollections(savedTasks);
                    context.TaskCollections.AddRange(taskCollections);
                    foreach (var tc in taskCollections)
                    {
                        context.Entry(tc).Property("UserId").CurrentValue = currentUser.Id;
                        foreach (var item in tc.Items)
                            context.Entry(item).Property("UserId").CurrentValue = currentUser.Id;
                    }
                    await context.SaveChangesAsync();
                    var savedTaskCollections = await context.TaskCollections.IgnoreQueryFilters().Include(tc => tc.Items).ThenInclude(i => i.Task).ToListAsync();

                    // STEP 7: Create OKRs with Key Results
                    var okrs = GetSampleOKRs(savedTeamMembers);
                    context.ObjectiveKeyResults.AddRange(okrs);
                    foreach (var okr in okrs)
                    {
                        context.Entry(okr).Property("UserId").CurrentValue = currentUser.Id;
                        context.Entry(okr).Property("OwnerId").CurrentValue = okr.Owner.Id;
                        if (okr.KeyResults != null)
                        {
                            foreach (var kr in okr.KeyResults)
                                context.Entry(kr).Property("UserId").CurrentValue = currentUser.Id;
                        }
                    }
                    await context.SaveChangesAsync();
                    var savedOkrs = await context.ObjectiveKeyResults.IgnoreQueryFilters().Include(o => o.KeyResults).ToListAsync();

                    // STEP 8: Link Key Results to Measurables (KPIs, Projects, TaskCollections)
                    await LinkKeyResultsToMeasurablesAsync(context, savedOkrs, savedKpis, savedProjects, savedTaskCollections, currentUser);

                    // STEP 9: Create 1:1s with linked items
                    var oneOnOnes = GetSampleOneOnOnes(savedTeamMembers);
                    context.OneOnOnes.AddRange(oneOnOnes);
                    foreach (var oneOnOne in oneOnOnes)
                    {
                        context.Entry(oneOnOne).Property("UserId").CurrentValue = currentUser.Id;
                        context.Entry(oneOnOne).Property("TeamMemberId").CurrentValue = oneOnOne.TeamMember.Id;
                        foreach (var agendaItem in oneOnOne.AgendaItems ?? new List<AgendaItem>())
                            context.Entry(agendaItem).Property("UserId").CurrentValue = currentUser.Id;
                        foreach (var task in oneOnOne.Tasks ?? new List<MeetingTask>())
                        {
                            context.Entry(task).Property("UserId").CurrentValue = currentUser.Id;
                            context.Entry(task).Property("OwnerId").CurrentValue = task.Owner.Id;
                        }
                    }
                    await context.SaveChangesAsync();
                    var savedOneOnOnes = await context.OneOnOnes.IgnoreQueryFilters().Include(o => o.TeamMember).ToListAsync();

                    // STEP 10: Link items to meetings
                    await LinkItemsToMeetingsAsync(context, savedOneOnOnes, savedTasks, savedOkrs, savedKpis);
                    
                    // STEP 11: Generate feedback and goals
                    await GenerateFeedbackAndGoalsAsync(context, savedTeamMembers, currentUser);
                    
                    // STEP 12: Create Quick Notes
                    await GenerateQuickNotesAsync(context, savedTeamMembers, savedOkrs, savedKpis, savedProjects, currentUser);
                    
                    // STEP 13: Create Meeting Templates
                    await GenerateMeetingTemplatesAsync(context, currentUser);
                    
                    // STEP 14: Create Reminders
                    await GenerateRemindersAsync(context, savedTeamMembers, savedOneOnOnes, currentUser);
                    
                    // Commit the transaction - all changes are now permanent
                    await transaction.CommitAsync();
                    logger.Info("Seeder: Transaction committed successfully");
                    
                    return true;
                }
                catch
                {
                    // Rollback on any error - database stays clean
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                var errorDetails = ex.Message;
                if (!errorDetails.Contains("Entities being added"))
                {
                    var innerEx = ex.InnerException ?? ex;
                    errorDetails = $"Error seeding database: {ex.Message}";
                    if (innerEx != ex)
                        errorDetails += $"\nInner: {innerEx.Message}";
                }
                throw new Exception(errorDetails, ex);
            }
        }

        /// <summary>
        /// Clears all data from the database.
        /// </summary>
        public static async Task ClearAllDataAsync(TrackerDbContext context)
        {
            try
            {
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect)
                {
                    try { _ = await context.TeamMembers.AnyAsync(); }
                    catch
                    {
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
                try { context.AgendaItems.RemoveRange(context.AgendaItems); } catch { }
                try { context.MeetingTasks.RemoveRange(context.MeetingTasks); } catch { }
                try { context.Reminders.RemoveRange(context.Reminders); } catch { }
                try { context.QuickNotes.RemoveRange(context.QuickNotes); } catch { }
                try { context.MeetingTemplateItems.RemoveRange(context.MeetingTemplateItems); } catch { }
                try { context.MeetingTemplates.RemoveRange(context.MeetingTemplates); } catch { }
                try { context.GoalMilestones.RemoveRange(context.GoalMilestones); } catch { }
                try { context.IndividualGoals.RemoveRange(context.IndividualGoals); } catch { }
                try { context.Feedbacks.RemoveRange(context.Feedbacks); } catch { }
                try { context.TaskCollectionItems.RemoveRange(context.TaskCollectionItems); } catch { }
                try { context.TaskCollections.RemoveRange(context.TaskCollections); } catch { }
                try { context.Tasks.RemoveRange(context.Tasks); } catch { }
                try { context.KpiDataSources.RemoveRange(context.KpiDataSources); } catch { }
                try { context.KeyResultMeasurables.RemoveRange(context.KeyResultMeasurables); } catch { }
                try { context.KeyResults.RemoveRange(context.KeyResults); } catch { }
                try { context.KeyPerformanceIndicators.RemoveRange(context.KeyPerformanceIndicators); } catch { }
                try { context.ObjectiveKeyResults.RemoveRange(context.ObjectiveKeyResults); } catch { }
                try { context.Milestones.RemoveRange(context.Milestones); } catch { }
                try { context.Risks.RemoveRange(context.Risks); } catch { }
                try { context.ProjectDependencies.RemoveRange(context.ProjectDependencies); } catch { }
                try { context.Projects.RemoveRange(context.Projects); } catch { }
                try { context.OneOnOnes.RemoveRange(context.OneOnOnes); } catch { }
                try { context.TeamMembers.RemoveRange(context.TeamMembers); } catch { }
                
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
                await context.Database.EnsureCreatedAsync();
            }
            catch
            {
                try
                {
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.EnsureCreatedAsync();
                }
                catch { throw; }
            }
        }

        #region Sample Data Generators

        /// <summary>
        /// Creates a cohesive team with clear roles and specialties.
        /// Manager + 6 engineers with diverse backgrounds.
        /// </summary>
        private static List<TeamMember> GetSampleTeamMembers()
        {
            return new List<TeamMember>
            {
                // Manager/Tech Lead
                new TeamMember
                {
                    FirstName = "Alex",
                    LastName = "Rivera",
                    NickName = "Alex",
                    Email = "alex.rivera@techcorp.com",
                    CellPhone = "555-100-0001",
                    JobTitle = "Engineering Manager",
                    BirthDay = new DateTime(1985, 6, 15),
                    HireDate = new DateTime(2020, 3, 1),
                    IsActive = true,
                    ManagerId = 0,
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Principle,
                    Role = RoleEnum.Manager
                },
                // Senior Backend Engineer - API & Performance focus
                new TeamMember
                {
                    FirstName = "Jordan",
                    LastName = "Chen",
                    Email = "jordan.chen@techcorp.com",
                    CellPhone = "555-100-0002",
                    JobTitle = "Senior Backend Engineer",
                    BirthDay = new DateTime(1988, 11, 22),
                    HireDate = new DateTime(2021, 6, 15),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.Backend,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                // Senior Frontend Engineer - UX & Performance focus
                new TeamMember
                {
                    FirstName = "Morgan",
                    LastName = "Patel",
                    Email = "morgan.patel@techcorp.com",
                    CellPhone = "555-100-0003",
                    JobTitle = "Senior Frontend Engineer",
                    BirthDay = new DateTime(1990, 3, 8),
                    HireDate = new DateTime(2022, 1, 10),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.WebUI,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                // Mid-level Full Stack - Feature development focus
                new TeamMember
                {
                    FirstName = "Taylor",
                    LastName = "Kim",
                    Email = "taylor.kim@techcorp.com",
                    CellPhone = "555-100-0004",
                    JobTitle = "Software Engineer",
                    BirthDay = new DateTime(1993, 7, 14),
                    HireDate = new DateTime(2023, 2, 20),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.FullStack,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer
                },
                // Mid-level Backend - Data & Integration focus
                new TeamMember
                {
                    FirstName = "Casey",
                    LastName = "Okonkwo",
                    Email = "casey.okonkwo@techcorp.com",
                    CellPhone = "555-100-0005",
                    JobTitle = "Software Engineer",
                    BirthDay = new DateTime(1994, 12, 3),
                    HireDate = new DateTime(2023, 8, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.Backend,
                    SkillLevel = SkillLevelEnum.Mid,
                    Role = RoleEnum.Engineer
                },
                // Senior Data/ML Engineer - Analytics focus
                new TeamMember
                {
                    FirstName = "Riley",
                    LastName = "Nakamura",
                    Email = "riley.nakamura@techcorp.com",
                    CellPhone = "555-100-0006",
                    JobTitle = "Senior Data Engineer",
                    BirthDay = new DateTime(1987, 9, 28),
                    HireDate = new DateTime(2021, 11, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.DataScience,
                    SkillLevel = SkillLevelEnum.Senior,
                    Role = RoleEnum.Engineer
                },
                // Junior Frontend - Learning & growth focus
                new TeamMember
                {
                    FirstName = "Jamie",
                    LastName = "Santos",
                    Email = "jamie.santos@techcorp.com",
                    CellPhone = "555-100-0007",
                    JobTitle = "Junior Software Engineer",
                    BirthDay = new DateTime(1998, 4, 19),
                    HireDate = new DateTime(2024, 6, 1),
                    IsActive = true,
                    ManagerId = 1,
                    Specialty = EngineeringSpecialtyEnum.WebUI,
                    SkillLevel = SkillLevelEnum.Junior,
                    Role = RoleEnum.Engineer
                }
            };
        }

        /// <summary>
        /// Creates projects with clear timelines and progress.
        /// Mix of statuses to show dashboard value.
        /// </summary>
        private static List<Project> GetSampleProjects(List<TeamMember> teamMembers)
        {
            var today = DateTime.Today;
            var manager = teamMembers[0]; // Alex
            var jordan = teamMembers[1];  // Backend
            var morgan = teamMembers[2];  // Frontend
            var taylor = teamMembers[3];  // FullStack
            var riley = teamMembers[5];   // Data

            return new List<Project>
            {
                // Project 1: Platform API Modernization - 75% complete (On Track)
                new Project
                {
                    Name = "Platform API Modernization",
                    Description = "Migrate legacy REST APIs to modern GraphQL architecture with improved performance and developer experience.",
                    StartDate = today.AddMonths(-2),
                    EndDate = today.AddMonths(1),
                    Status = "In Progress",
                    Owner = jordan,
                    TeamMembers = new List<TeamMember> { jordan, taylor, teamMembers[4] },
                    Budget = 75000m,
                    Milestones = new List<Milestone>
                    {
                        new Milestone { Name = "API Design Complete", Description = "GraphQL schema finalized", TargetDate = today.AddMonths(-1), IsAchieved = true },
                        new Milestone { Name = "Core Endpoints Migrated", Description = "Primary 20 endpoints live", TargetDate = today.AddDays(-7), IsAchieved = true },
                        new Milestone { Name = "Full Migration", Description = "All endpoints migrated", TargetDate = today.AddDays(21), IsAchieved = false },
                        new Milestone { Name = "Legacy Deprecation", Description = "Old APIs deprecated", TargetDate = today.AddMonths(1), IsAchieved = false }
                    },
                    Risks = new List<Risk>
                    {
                        new Risk { Name = "Third-party Integration Delays", Description = "External partners may need time to update", Severity = RiskLevelEnum.Medium, MitigationStrategy = "Early communication and parallel support" }
                    }
                },
                // Project 2: Customer Dashboard Redesign - 60% complete (At Risk - deadline pressure)
                new Project
                {
                    Name = "Customer Dashboard Redesign",
                    Description = "Complete overhaul of customer-facing dashboard with new data visualizations and mobile responsiveness.",
                    StartDate = today.AddMonths(-3),
                    EndDate = today.AddDays(14),
                    Status = "In Progress",
                    Owner = morgan,
                    TeamMembers = new List<TeamMember> { morgan, teamMembers[6], taylor },
                    Budget = 45000m,
                    Milestones = new List<Milestone>
                    {
                        new Milestone { Name = "Design System Complete", Description = "Component library finalized", TargetDate = today.AddMonths(-2), IsAchieved = true },
                        new Milestone { Name = "Core Components Built", Description = "Main dashboard widgets", TargetDate = today.AddMonths(-1), IsAchieved = true },
                        new Milestone { Name = "Mobile Responsive", Description = "All breakpoints working", TargetDate = today.AddDays(7), IsAchieved = false },
                        new Milestone { Name = "Launch", Description = "Production deployment", TargetDate = today.AddDays(14), IsAchieved = false }
                    },
                    Risks = new List<Risk>
                    {
                        new Risk { Name = "Tight Timeline", Description = "Aggressive deadline with scope changes", Severity = RiskLevelEnum.High, MitigationStrategy = "Scope prioritization and overtime budget" }
                    }
                },
                // Project 3: Analytics Pipeline - 90% complete (On Track - almost done)
                new Project
                {
                    Name = "Real-time Analytics Pipeline",
                    Description = "Build streaming data pipeline for real-time business metrics and alerting.",
                    StartDate = today.AddMonths(-4),
                    EndDate = today.AddDays(7),
                    Status = "In Progress",
                    Owner = riley,
                    TeamMembers = new List<TeamMember> { riley, jordan, teamMembers[4] },
                    Budget = 60000m,
                    Milestones = new List<Milestone>
                    {
                        new Milestone { Name = "Infrastructure Setup", Description = "Kafka cluster and Spark jobs", TargetDate = today.AddMonths(-3), IsAchieved = true },
                        new Milestone { Name = "Data Ingestion", Description = "All data sources connected", TargetDate = today.AddMonths(-2), IsAchieved = true },
                        new Milestone { Name = "Dashboard Integration", Description = "Real-time widgets live", TargetDate = today.AddMonths(-1), IsAchieved = true },
                        new Milestone { Name = "Alerting System", Description = "Automated alerts", TargetDate = today.AddDays(7), IsAchieved = false }
                    }
                },
                // Project 4: Mobile App v2 - Planning stage
                new Project
                {
                    Name = "Mobile App v2.0",
                    Description = "Major mobile app update with offline support, biometric auth, and performance improvements.",
                    StartDate = today.AddDays(14),
                    EndDate = today.AddMonths(4),
                    Status = "Planning",
                    Owner = manager,
                    TeamMembers = new List<TeamMember> { morgan, taylor, teamMembers[6] },
                    Budget = 120000m,
                    Milestones = new List<Milestone>
                    {
                        new Milestone { Name = "Requirements Finalized", Description = "PRD approved", TargetDate = today.AddDays(14), IsAchieved = false },
                        new Milestone { Name = "Architecture Design", Description = "Technical design doc", TargetDate = today.AddMonths(1), IsAchieved = false },
                        new Milestone { Name = "Beta Release", Description = "Internal testing", TargetDate = today.AddMonths(3), IsAchieved = false },
                        new Milestone { Name = "Public Launch", Description = "App store release", TargetDate = today.AddMonths(4), IsAchieved = false }
                    }
                }
            };
        }

        /// <summary>
        /// Creates tasks linked to projects and team members.
        /// Mix of completed and in-progress for good visualization.
        /// </summary>
        private static List<IndividualTask> GetSampleTasks(List<TeamMember> teamMembers, List<Project> projects)
        {
            var today = DateTime.Today;
            var jordan = teamMembers[1];
            var morgan = teamMembers[2];
            var taylor = teamMembers[3];
            var casey = teamMembers[4];
            var riley = teamMembers[5];
            var jamie = teamMembers[6];

            // Get project IDs (they should be saved by now)
            var apiProject = projects.FirstOrDefault(p => p.Name.Contains("API"));
            var dashboardProject = projects.FirstOrDefault(p => p.Name.Contains("Dashboard"));
            var analyticsProject = projects.FirstOrDefault(p => p.Name.Contains("Analytics"));

            return new List<IndividualTask>
            {
                // API Modernization Tasks (Project 1)
                new IndividualTask { Description = "Implement GraphQL user queries", DueDate = today.AddDays(-14), IsCompleted = true, Notes = "Completed with pagination support", Owner = jordan, ProjectId = apiProject?.ID },
                new IndividualTask { Description = "Migrate order endpoints to GraphQL", DueDate = today.AddDays(-7), IsCompleted = true, Notes = "Including subscriptions for real-time updates", Owner = jordan, ProjectId = apiProject?.ID },
                new IndividualTask { Description = "Implement caching layer for GraphQL", DueDate = today.AddDays(3), IsCompleted = false, Notes = "Using DataLoader pattern", Owner = jordan, ProjectId = apiProject?.ID },
                new IndividualTask { Description = "Update SDK documentation", DueDate = today.AddDays(7), IsCompleted = false, Notes = "Auto-generate from schema", Owner = taylor, ProjectId = apiProject?.ID },
                new IndividualTask { Description = "Performance testing and optimization", DueDate = today.AddDays(14), IsCompleted = false, Notes = "Target: <100ms p95", Owner = casey, ProjectId = apiProject?.ID },

                // Dashboard Redesign Tasks (Project 2)
                new IndividualTask { Description = "Create chart component library", DueDate = today.AddDays(-21), IsCompleted = true, Notes = "D3.js based with accessibility", Owner = morgan, ProjectId = dashboardProject?.ID },
                new IndividualTask { Description = "Implement responsive grid system", DueDate = today.AddDays(-14), IsCompleted = true, Notes = "CSS Grid with breakpoints", Owner = morgan, ProjectId = dashboardProject?.ID },
                new IndividualTask { Description = "Build KPI widget components", DueDate = today.AddDays(-7), IsCompleted = true, Notes = "Reusable metric cards", Owner = jamie, ProjectId = dashboardProject?.ID },
                new IndividualTask { Description = "Implement dark mode theme", DueDate = today.AddDays(3), IsCompleted = false, Notes = "CSS variables approach", Owner = jamie, ProjectId = dashboardProject?.ID },
                new IndividualTask { Description = "Mobile responsive testing", DueDate = today.AddDays(7), IsCompleted = false, Notes = "All devices and orientations", Owner = morgan, ProjectId = dashboardProject?.ID },
                new IndividualTask { Description = "Performance optimization pass", DueDate = today.AddDays(10), IsCompleted = false, Notes = "Lazy loading and code splitting", Owner = taylor, ProjectId = dashboardProject?.ID },

                // Analytics Pipeline Tasks (Project 3)
                new IndividualTask { Description = "Configure Kafka topics", DueDate = today.AddDays(-45), IsCompleted = true, Notes = "Event streaming setup", Owner = riley, ProjectId = analyticsProject?.ID },
                new IndividualTask { Description = "Build Spark aggregation jobs", DueDate = today.AddDays(-30), IsCompleted = true, Notes = "Real-time and batch", Owner = riley, ProjectId = analyticsProject?.ID },
                new IndividualTask { Description = "Create analytics API endpoints", DueDate = today.AddDays(-14), IsCompleted = true, Notes = "REST API for dashboard", Owner = casey, ProjectId = analyticsProject?.ID },
                new IndividualTask { Description = "Implement alerting rules engine", DueDate = today.AddDays(3), IsCompleted = false, Notes = "Configurable thresholds", Owner = riley, ProjectId = analyticsProject?.ID },
                new IndividualTask { Description = "Dashboard widget integration", DueDate = today.AddDays(7), IsCompleted = false, Notes = "Real-time data connection", Owner = casey, ProjectId = analyticsProject?.ID },

                // Standalone tasks (not in projects)
                new IndividualTask { Description = "Quarterly security review", DueDate = today.AddDays(5), IsCompleted = false, Notes = "OWASP checklist", Owner = jordan },
                new IndividualTask { Description = "Update CI/CD pipeline", DueDate = today.AddDays(-5), IsCompleted = true, Notes = "Added parallel test execution", Owner = taylor },
                new IndividualTask { Description = "Onboard new team member", DueDate = today.AddDays(-10), IsCompleted = true, Notes = "Jamie Santos onboarding complete", Owner = morgan },
                new IndividualTask { Description = "Code review backlog cleanup", DueDate = today.AddDays(2), IsCompleted = false, Notes = "12 PRs pending review", Owner = jordan },
                new IndividualTask { Description = "Write technical blog post", DueDate = today.AddDays(14), IsCompleted = false, Notes = "GraphQL migration learnings", Owner = taylor }
            };
        }

        /// <summary>
        /// Creates KPIs that will be linked to Key Results.
        /// Optimized distribution: 4 On Target, 3 Close, 3 Off Target
        /// </summary>
        private static List<KeyPerformanceIndicator> GetSampleKPIs(List<TeamMember> teamMembers)
        {
            var today = DateTime.Today;
            var manager = teamMembers[0];
            var jordan = teamMembers[1];
            var morgan = teamMembers[2];
            var taylor = teamMembers[3];
            var casey = teamMembers[4];
            var riley = teamMembers[5];

            return new List<KeyPerformanceIndicator>
            {
                // ON TARGET (Green) - 4 KPIs
                new KeyPerformanceIndicator
                {
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

        /// <summary>
        /// Creates Task Collections for grouped task tracking.
        /// </summary>
        private static List<TaskCollection> GetSampleTaskCollections(List<IndividualTask> tasks)
        {
            var apiTasks = tasks.Where(t => t.Description.Contains("GraphQL") || t.Description.Contains("API")).Take(3).ToList();
            var dashboardTasks = tasks.Where(t => t.Description.Contains("chart") || t.Description.Contains("widget") || t.Description.Contains("responsive")).Take(3).ToList();
            var securityTasks = tasks.Where(t => t.Description.Contains("security") || t.Description.Contains("review")).Take(2).ToList();

            var collections = new List<TaskCollection>
            {
                new TaskCollection
                {
                    Name = "Q1 API Migration Tasks",
                    Description = "All tasks related to GraphQL API migration initiative",
                    Items = apiTasks.Select((t, i) => new TaskCollectionItem { TaskId = t.Id, Task = t, SortOrder = i }).ToList()
                },
                new TaskCollection
                {
                    Name = "Dashboard Launch Checklist",
                    Description = "Critical tasks for dashboard redesign launch",
                    Items = dashboardTasks.Select((t, i) => new TaskCollectionItem { TaskId = t.Id, Task = t, SortOrder = i }).ToList()
                }
            };

            // Only add security collection if we have tasks
            if (securityTasks.Any())
            {
                collections.Add(new TaskCollection
                {
                    Name = "Security Review Items",
                    Description = "Security-related tasks requiring attention",
                    Items = securityTasks.Select((t, i) => new TaskCollectionItem { TaskId = t.Id, Task = t, SortOrder = i }).ToList()
                });
            }

            return collections;
        }

        /// <summary>
        /// Creates OKRs with clear status distribution.
        /// 2 On Track, 1 At Risk, 1 Off Track for dashboard impact.
        /// </summary>
        private static List<ObjectiveKeyResult> GetSampleOKRs(List<TeamMember> teamMembers)
        {
            var today = DateTime.Today;
            var quarterStart = new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1);
            var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
            var currentQuarter = (today.Month - 1) / 3 + 1;
            var timePeriod = currentQuarter switch { 1 => TimePeriodEnum.Q1, 2 => TimePeriodEnum.Q2, 3 => TimePeriodEnum.Q3, _ => TimePeriodEnum.Q4 };

            var manager = teamMembers[0];
            var jordan = teamMembers[1];
            var morgan = teamMembers[2];
            var riley = teamMembers[5];

            return new List<ObjectiveKeyResult>
            {
                // OKR 1: ON TRACK - Platform Performance (85% progress)
                new ObjectiveKeyResult
                {
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

                // OKR 2: ON TRACK - Modernize Tech Stack (78% progress)
                new ObjectiveKeyResult
                {
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

                // OKR 3: AT RISK - Customer Experience (55% progress, deadline pressure)
                new ObjectiveKeyResult
                {
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

                // OKR 4: OFF TRACK - Data Platform (40% progress, blocked on resources)
                new ObjectiveKeyResult
                {
                    Title = "Build Real-Time Analytics Platform",
                    Description = "Enable real-time business insights and automated alerting",
                    Owner = riley,
                    StartDate = quarterStart,
                    EndDate = quarterEnd,
                    TimePeriod = timePeriod,
                    Year = today.Year,
                    StatusOverride = ObjectiveStatusEnum.OffTrack, // Manual override due to resource constraints
                    KeyResults = new List<KeyResult>
                    {
                        new KeyResult
                        {
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

        /// <summary>
        /// Links Key Results to their Measurable sources (KPIs, Projects, TaskCollections).
        /// Demonstrates the IMeasurable relationship.
        /// </summary>
        private static async Task LinkKeyResultsToMeasurablesAsync(
            TrackerDbContext context,
            List<ObjectiveKeyResult> okrs,
            List<KeyPerformanceIndicator> kpis,
            List<Project> projects,
            List<TaskCollection> taskCollections,
            User currentUser)
        {
            // Index measurables by name keywords for flexible matching
            var kpisByKeyword = new Dictionary<string, KeyPerformanceIndicator>(StringComparer.OrdinalIgnoreCase);
            foreach (var kpi in kpis)
            {
                if (kpi.Name.Contains("Uptime")) kpisByKeyword["uptime"] = kpi;
                if (kpi.Name.Contains("Response Time")) { kpisByKeyword["latency"] = kpi; kpisByKeyword["response"] = kpi; }
                if (kpi.Name.Contains("Test Coverage")) kpisByKeyword["test"] = kpi;
                if (kpi.Name.Contains("CSAT")) kpisByKeyword["csat"] = kpi;
                if (kpi.Name.Contains("NPS")) kpisByKeyword["nps"] = kpi;
                if (kpi.Name.Contains("Sprint Velocity")) kpisByKeyword["velocity"] = kpi;
                if (kpi.Name.Contains("Error Rate")) kpisByKeyword["error"] = kpi;
                if (kpi.Name.Contains("Bug")) kpisByKeyword["bug"] = kpi;
                if (kpi.Name.Contains("Documentation")) kpisByKeyword["documentation"] = kpi;
            }

            var projectsByKeyword = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);
            foreach (var project in projects)
            {
                if (project.Name.Contains("API")) { projectsByKeyword["api"] = project; projectsByKeyword["endpoint"] = project; projectsByKeyword["graphql"] = project; }
                if (project.Name.Contains("Dashboard")) { projectsByKeyword["dashboard"] = project; projectsByKeyword["widget"] = project; }
                if (project.Name.Contains("Analytics")) projectsByKeyword["analytics"] = project;
                if (project.Name.Contains("Security")) projectsByKeyword["security"] = project;
                if (project.Name.Contains("Performance")) projectsByKeyword["performance"] = project;
            }

            var taskCollectionsByKeyword = new Dictionary<string, TaskCollection>(StringComparer.OrdinalIgnoreCase);
            foreach (var tc in taskCollections)
            {
                if (tc.Name.Contains("API")) { taskCollectionsByKeyword["api"] = tc; taskCollectionsByKeyword["migration"] = tc; }
                if (tc.Name.Contains("Dashboard")) taskCollectionsByKeyword["dashboard"] = tc;
                if (tc.Name.Contains("Security")) taskCollectionsByKeyword["security"] = tc;
            }

            int krIndex = 0;
            foreach (var okr in okrs)
            {
                foreach (var kr in okr.KeyResults ?? new List<KeyResult>())
                {
                    var sortOrder = 0;
                    var measurablesLinked = 0;
                    var krTitleLower = kr.Title.ToLower();

                    // Strategy: Link based on content matching, but ensure variety
                    // Some KRs get multiple measurables to showcase aggregation

                    // Link KPIs based on title keywords
                    foreach (var (keyword, kpi) in kpisByKeyword)
                    {
                        if (krTitleLower.Contains(keyword))
                        {
                            var link = new KeyResultMeasurable
                            {
                                KeyResultId = kr.Id,
                                MeasurableType = MeasurableType.Kpi,
                                MeasurableId = kpi.KpiId,
                                AggregationType = AggregationTypeEnum.Latest,
                                Weight = 1.0m,
                                SortOrder = sortOrder++
                            };
                            context.KeyResultMeasurables.Add(link);
                            context.Entry(link).Property("UserId").CurrentValue = currentUser.Id;
                            measurablesLinked++;
                            break; // One KPI match per KR to avoid duplicates
                        }
                    }

                    // Link Projects based on title keywords
                    foreach (var (keyword, project) in projectsByKeyword)
                    {
                        if (krTitleLower.Contains(keyword))
                        {
                            var link = new KeyResultMeasurable
                            {
                                KeyResultId = kr.Id,
                                MeasurableType = MeasurableType.Project,
                                MeasurableId = project.ID,
                                AggregationType = AggregationTypeEnum.Latest,
                                Weight = 0.8m,
                                SortOrder = sortOrder++
                            };
                            context.KeyResultMeasurables.Add(link);
                            context.Entry(link).Property("UserId").CurrentValue = currentUser.Id;
                            measurablesLinked++;
                            break; // One project match per KR
                        }
                    }

                    // Link Task Collections based on title keywords
                    foreach (var (keyword, tc) in taskCollectionsByKeyword)
                    {
                        if (krTitleLower.Contains(keyword))
                        {
                            var link = new KeyResultMeasurable
                            {
                                KeyResultId = kr.Id,
                                MeasurableType = MeasurableType.TaskCollection,
                                MeasurableId = tc.Id,
                                AggregationType = AggregationTypeEnum.Average,
                                Weight = 0.5m,
                                SortOrder = sortOrder++
                            };
                            context.KeyResultMeasurables.Add(link);
                            context.Entry(link).Property("UserId").CurrentValue = currentUser.Id;
                            measurablesLinked++;
                            break; // One task collection match per KR
                        }
                    }

                    // If no natural matches, assign based on rotation to ensure coverage
                    // This ensures every KR has at least one measurable for demo purposes
                    if (measurablesLinked == 0)
                    {
                        var rotationType = krIndex % 3;
                        
                        if (rotationType == 0 && kpis.Any())
                        {
                            // Link to a KPI
                            var kpi = kpis[krIndex % kpis.Count];
                            var link = new KeyResultMeasurable
                            {
                                KeyResultId = kr.Id,
                                MeasurableType = MeasurableType.Kpi,
                                MeasurableId = kpi.KpiId,
                                AggregationType = AggregationTypeEnum.Latest,
                                Weight = 1.0m,
                                SortOrder = 0
                            };
                            context.KeyResultMeasurables.Add(link);
                            context.Entry(link).Property("UserId").CurrentValue = currentUser.Id;
                        }
                        else if (rotationType == 1 && projects.Any())
                        {
                            // Link to a Project
                            var project = projects[krIndex % projects.Count];
                            var link = new KeyResultMeasurable
                            {
                                KeyResultId = kr.Id,
                                MeasurableType = MeasurableType.Project,
                                MeasurableId = project.ID,
                                AggregationType = AggregationTypeEnum.Latest,
                                Weight = 1.0m,
                                SortOrder = 0
                            };
                            context.KeyResultMeasurables.Add(link);
                            context.Entry(link).Property("UserId").CurrentValue = currentUser.Id;
                        }
                        else if (taskCollections.Any())
                        {
                            // Link to a Task Collection
                            var tc = taskCollections[krIndex % taskCollections.Count];
                            var link = new KeyResultMeasurable
                            {
                                KeyResultId = kr.Id,
                                MeasurableType = MeasurableType.TaskCollection,
                                MeasurableId = tc.Id,
                                AggregationType = AggregationTypeEnum.Average,
                                Weight = 1.0m,
                                SortOrder = 0
                            };
                            context.KeyResultMeasurables.Add(link);
                            context.Entry(link).Property("UserId").CurrentValue = currentUser.Id;
                        }
                    }

                    // For demonstration: Add multiple measurables to some KRs
                    // Every 3rd KR gets additional measurables to show aggregation
                    if (krIndex % 3 == 0 && measurablesLinked > 0)
                    {
                        // Add a secondary measurable of different type
                        if (projects.Any() && !projectsByKeyword.Values.Any(p => 
                            context.ChangeTracker.Entries<KeyResultMeasurable>()
                                .Any(e => e.Entity.KeyResultId == kr.Id && 
                                          e.Entity.MeasurableType == MeasurableType.Project && 
                                          e.Entity.MeasurableId == p.ID)))
                        {
                            var project = projects[(krIndex + 1) % projects.Count];
                            var link = new KeyResultMeasurable
                            {
                                KeyResultId = kr.Id,
                                MeasurableType = MeasurableType.Project,
                                MeasurableId = project.ID,
                                AggregationType = AggregationTypeEnum.WeightedAverage,
                                Weight = 0.3m,
                                SortOrder = sortOrder++
                            };
                            context.KeyResultMeasurables.Add(link);
                            context.Entry(link).Property("UserId").CurrentValue = currentUser.Id;
                        }
                    }

                    // Every 4th KR gets a task collection for additional variety
                    if (krIndex % 4 == 0 && taskCollections.Any() && measurablesLinked > 0)
                    {
                        var tc = taskCollections[krIndex % taskCollections.Count];
                        var existingTcLink = context.ChangeTracker.Entries<KeyResultMeasurable>()
                            .Any(e => e.Entity.KeyResultId == kr.Id && 
                                      e.Entity.MeasurableType == MeasurableType.TaskCollection);
                        
                        if (!existingTcLink)
                        {
                            var link = new KeyResultMeasurable
                            {
                                KeyResultId = kr.Id,
                                MeasurableType = MeasurableType.TaskCollection,
                                MeasurableId = tc.Id,
                                AggregationType = AggregationTypeEnum.Sum,
                                Weight = 0.2m,
                                SortOrder = sortOrder++
                            };
                            context.KeyResultMeasurables.Add(link);
                            context.Entry(link).Property("UserId").CurrentValue = currentUser.Id;
                        }
                    }

                    krIndex++;
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates 1:1 meetings with realistic cadence and content.
        /// Some members overdue to demonstrate tracking value.
        /// </summary>
        private static List<OneOnOne> GetSampleOneOnOnes(List<TeamMember> teamMembers)
        {
            var oneOnOnes = new List<OneOnOne>();
            var today = DateTime.Today;
            var manager = teamMembers[0];

            // Meeting cadence varies by team member
            var meetingSchedule = new Dictionary<int, (int daysBetween, int lastMeetingDaysAgo)>
            {
                { 1, (14, 5) },   // Jordan - bi-weekly, recent
                { 2, (14, 3) },   // Morgan - bi-weekly, very recent
                { 3, (7, 6) },    // Taylor - weekly (new-ish), slight gap
                { 4, (14, 18) },  // Casey - bi-weekly, OVERDUE
                { 5, (14, 8) },   // Riley - bi-weekly, recent
                { 6, (7, 12) },   // Jamie - weekly (junior), OVERDUE
            };

            foreach (var member in teamMembers.Skip(1)) // Skip manager
            {
                var memberIndex = teamMembers.IndexOf(member);
                var (daysBetween, lastMeetingDaysAgo) = meetingSchedule.GetValueOrDefault(memberIndex, (14, 7));

                // Create historical meetings (last 3 months)
                for (int weeksAgo = 0; weeksAgo < 12; weeksAgo++)
                {
                    var meetingDate = today.AddDays(-(lastMeetingDaysAgo + (weeksAgo * daysBetween)));
                    if (meetingDate < today.AddMonths(-3)) break;

                    var isRecent = weeksAgo < 2;
                    var meeting = new OneOnOne
                    {
                        Description = $"Weekly 1:1 with {member.FirstName}",
                        Date = meetingDate,
                        StartTime = new TimeSpan(10, 0, 0),
                        EndTime = new TimeSpan(10, 30, 0),
                        Duration = TimeSpan.FromMinutes(30),
                        Agenda = isRecent ? "Project updates, blockers, career development" : "Weekly sync and project updates",
                        Notes = isRecent ? $"Discussed {member.FirstName}'s current workload and upcoming priorities." : $"Routine check-in with {member.FirstName}.",
                        Feedback = isRecent ? "Great progress this sprint. Continue focus on quality." : "",
                        IsRecurring = true,
                        Status = MeetingStatusEnum.Completed,
                        TeamMember = member,
                        Tasks = isRecent && weeksAgo == 0 ? new List<MeetingTask>
                        {
                            new MeetingTask
                            {
                                Description = $"Follow up on {member.FirstName}'s technical design doc",
                                DueDate = today.AddDays(7),
                                IsCompleted = false,
                                Owner = member
                            },
                            new MeetingTask
                            {
                                Description = $"Schedule {member.FirstName} for architecture review",
                                DueDate = today.AddDays(14),
                                IsCompleted = false,
                                Owner = manager
                            }
                        } : new List<MeetingTask>(),
                        AgendaItems = isRecent && weeksAgo == 0 ? new List<AgendaItem>
                        {
                            new AgendaItem { Description = "Sprint progress review", Category = AgendaItemCategory.Topic, Priority = Severity.Medium },
                            new AgendaItem { Description = "Career development goals", Category = AgendaItemCategory.Process, Priority = Severity.Low }
                        } : new List<AgendaItem>()
                    };

                    // Add concerns for specific team members
                    if (isRecent && weeksAgo == 0)
                    {
                        if (memberIndex == 4) // Casey - resource concerns
                        {
                            meeting.AgendaItems.Add(new AgendaItem
                            {
                                Description = "Feeling stretched across too many projects",
                                Category = AgendaItemCategory.Concern,
                                Priority = Severity.High
                            });
                        }
                        if (memberIndex == 6) // Jamie - junior learning
                        {
                            meeting.AgendaItems.Add(new AgendaItem
                            {
                                Description = "Need more code review feedback",
                                Category = AgendaItemCategory.Concern,
                                Priority = Severity.Medium,
                                Resolution = "Paired Jamie with Morgan for mentorship"
                            });
                        }
                    }

                    oneOnOnes.Add(meeting);
                }

                // Add upcoming scheduled meeting
                var nextMeetingDate = today.AddDays(daysBetween - lastMeetingDaysAgo);
                if (nextMeetingDate > today && nextMeetingDate < today.AddDays(14))
                {
                    oneOnOnes.Add(new OneOnOne
                    {
                        Description = $"Weekly 1:1 with {member.FirstName}",
                        Date = nextMeetingDate,
                        StartTime = new TimeSpan(10, 0, 0),
                        EndTime = new TimeSpan(10, 30, 0),
                        Duration = TimeSpan.FromMinutes(30),
                        Agenda = "Sprint review, blockers, upcoming priorities",
                        IsRecurring = true,
                        Status = MeetingStatusEnum.Scheduled,
                        TeamMember = member
                    });
                }
            }

            return oneOnOnes;
        }

        /// <summary>
        /// Links existing tasks, OKRs, and KPIs to completed meetings.
        /// </summary>
        private static async Task LinkItemsToMeetingsAsync(
            TrackerDbContext context,
            List<OneOnOne> oneOnOnes,
            List<IndividualTask> tasks,
            List<ObjectiveKeyResult> okrs,
            List<KeyPerformanceIndicator> kpis)
        {
            var completedMeetings = oneOnOnes.Where(m => m.Status == MeetingStatusEnum.Completed && m.Id > 0).ToList();

            foreach (var meeting in completedMeetings.OrderByDescending(m => m.Date).Take(20))
            {
                // Link tasks owned by the team member
                var memberTasks = tasks.Where(t => t.Owner?.Id == meeting.TeamMember?.Id && t.Id > 0).Take(2).ToList();
                foreach (var task in memberTasks)
                {
                    if (!await context.OneOnOneLinkedTasks.AnyAsync(l => l.OneOnOneId == meeting.Id && l.TaskId == task.Id))
                    {
                        context.OneOnOneLinkedTasks.Add(new OneOnOneLinkedTask
                        {
                            OneOnOneId = meeting.Id,
                            TaskId = task.Id,
                            DiscussionNotes = $"Discussed progress on: {task.Description}"
                        });
                    }
                }

                // Link OKRs owned by the team member
                var memberOkrs = okrs.Where(o => o.Owner?.Id == meeting.TeamMember?.Id && o.ObjectiveId > 0).Take(1).ToList();
                foreach (var okr in memberOkrs)
                {
                    if (!await context.OneOnOneLinkedOkrs.AnyAsync(l => l.OneOnOneId == meeting.Id && l.OkrId == okr.ObjectiveId))
                    {
                        context.OneOnOneLinkedOkrs.Add(new OneOnOneLinkedOkr
                        {
                            OneOnOneId = meeting.Id,
                            OkrId = okr.ObjectiveId,
                            DiscussionNotes = $"Reviewed OKR progress: {okr.Title}"
                        });
                    }
                }

                // Link KPIs owned by the team member
                var memberKpis = kpis.Where(k => k.Owner?.Id == meeting.TeamMember?.Id && k.KpiId > 0).Take(1).ToList();
                foreach (var kpi in memberKpis)
                {
                    if (!await context.OneOnOneLinkedKpis.AnyAsync(l => l.OneOnOneId == meeting.Id && l.KpiId == kpi.KpiId))
                    {
                        context.OneOnOneLinkedKpis.Add(new OneOnOneLinkedKpi
                        {
                            OneOnOneId = meeting.Id,
                            KpiId = kpi.KpiId,
                            DiscussionNotes = $"Reviewed KPI: {kpi.Name}"
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Generates comprehensive feedback and goals for team members.
        /// Creates varied data to populate Feedback and Goals pages.
        /// </summary>
        private static async Task GenerateFeedbackAndGoalsAsync(TrackerDbContext context, List<TeamMember> teamMembers, User currentUser)
        {
            var today = DateTime.Today;
            var random = new Random(42); // Fixed seed for consistent sample data

            // Feedback templates for variety
            var positiveFeedback = new[]
            {
                ("Outstanding code review contributions", "Provided thorough, constructive code reviews that helped improve team code quality.", "Code Review"),
                ("Great team collaboration", "Excellent job helping onboard the new team member and sharing knowledge.", "Team Meeting"),
                ("Exceeded sprint goals", "Completed all sprint tasks early and helped teammates with blockers.", "Sprint Review"),
                ("Excellent documentation", "Created comprehensive documentation that will help the entire team.", "Project Milestone"),
                ("Strong technical leadership", "Led the technical discussion and helped the team reach a solid architectural decision.", "Architecture Review")
            };
            
            var constructiveFeedback = new[]
            {
                ("Communication timing", "Consider sharing blockers earlier in the sprint to allow more time for resolution.", "Sprint Retrospective"),
                ("Testing coverage", "Would benefit from additional unit test coverage on complex methods.", "Code Review"),
                ("Meeting participation", "Encourage more active participation in team discussions.", "1:1 Meeting")
            };
            
            var recognitionFeedback = new[]
            {
                ("Q4 MVP Award", "Recognized for exceptional contributions to the Q4 product release.", "All Hands Meeting"),
                ("Peer Recognition", "Multiple team members highlighted their helpful attitude and knowledge sharing.", "Team Survey"),
                ("Customer Impact Award", "Directly contributed to improved customer satisfaction scores.", "Customer Review")
            };
            
            var coachingFeedback = new[]
            {
                ("Career path discussion", "Discussed long-term career goals and created development plan.", "1:1 Meeting"),
                ("Presentation skills", "Worked on improving presentation skills for stakeholder meetings.", "Training Session"),
                ("Leadership development", "Identified opportunities to take on more leadership responsibilities.", "Career Review")
            };

            // Goal templates for variety
            var skillGoals = new[]
            {
                ("Master React Testing Library", "Achieve proficiency in testing React components using RTL and Jest", GoalCategory.SkillDevelopment),
                ("Learn Kubernetes", "Complete K8s certification and deploy a production service", GoalCategory.Technical),
                ("Improve SQL performance tuning", "Optimize database queries and learn advanced indexing strategies", GoalCategory.Technical),
                ("Master TypeScript advanced features", "Learn generics, decorators, and advanced type patterns", GoalCategory.SkillDevelopment)
            };
            
            var careerGoals = new[]
            {
                ("Prepare for senior promotion", "Meet all senior engineer criteria and build promotion case", GoalCategory.Career),
                ("Become team tech lead", "Take on technical leadership responsibilities for a project", GoalCategory.Leadership),
                ("Expand cross-team influence", "Lead initiatives that impact multiple teams", GoalCategory.Leadership),
                ("Develop mentoring skills", "Successfully mentor a junior developer through onboarding", GoalCategory.Leadership)
            };
            
            var certificationGoals = new[]
            {
                ("AWS Solutions Architect certification", "Pass the AWS SA Professional exam", GoalCategory.Certification),
                ("Scrum Master certification", "Complete CSM certification and apply to team processes", GoalCategory.Certification),
                ("Azure DevOps certification", "Obtain AZ-400 certification", GoalCategory.Certification)
            };

            int memberIndex = 0;
            foreach (var member in teamMembers.Skip(1)) // Skip manager
            {
                // Generate 3-5 feedback entries per member with varied types and dates
                var feedbackCount = random.Next(3, 6);
                var feedbackEntries = new List<Feedback>();
                
                // Always add at least one positive feedback
                var (posTitle, posContent, posContext) = positiveFeedback[memberIndex % positiveFeedback.Length];
                feedbackEntries.Add(new Feedback
                {
                    TeamMemberId = member.Id,
                    TeamMember = member,
                    Date = today.AddDays(-random.Next(5, 30)),
                    Type = FeedbackType.Positive,
                    Title = posTitle,
                    Content = $"{member.FirstName} {posContent}",
                    Context = posContext
                });
                
                // Add recognition for senior members
                if (member.SkillLevel == SkillLevelEnum.Senior || member.SkillLevel == SkillLevelEnum.Principle)
                {
                    var (recTitle, recContent, recContext) = recognitionFeedback[memberIndex % recognitionFeedback.Length];
                    feedbackEntries.Add(new Feedback
                    {
                        TeamMemberId = member.Id,
                        TeamMember = member,
                        Date = today.AddDays(-random.Next(30, 60)),
                        Type = FeedbackType.Recognition,
                        Title = recTitle,
                        Content = $"{member.FirstName}: {recContent}",
                        Context = recContext
                    });
                }
                
                // Add coaching feedback
                var (coachTitle, coachContent, coachContext) = coachingFeedback[memberIndex % coachingFeedback.Length];
                feedbackEntries.Add(new Feedback
                {
                    TeamMemberId = member.Id,
                    TeamMember = member,
                    Date = today.AddDays(-random.Next(14, 45)),
                    Type = FeedbackType.Coaching,
                    Title = coachTitle,
                    Content = $"With {member.FirstName}: {coachContent}",
                    Context = coachContext
                });
                
                // Maybe add constructive feedback
                if (random.Next(100) < 40) // 40% chance
                {
                    var (constTitle, constContent, constContext) = constructiveFeedback[memberIndex % constructiveFeedback.Length];
                    feedbackEntries.Add(new Feedback
                    {
                        TeamMemberId = member.Id,
                        TeamMember = member,
                        Date = today.AddDays(-random.Next(20, 50)),
                        Type = FeedbackType.Constructive,
                        Title = constTitle,
                        Content = $"For {member.FirstName}: {constContent}",
                        Context = constContext
                    });
                }
                
                // Add another positive feedback from recent week
                var (pos2Title, pos2Content, pos2Context) = positiveFeedback[(memberIndex + 2) % positiveFeedback.Length];
                feedbackEntries.Add(new Feedback
                {
                    TeamMemberId = member.Id,
                    TeamMember = member,
                    Date = today.AddDays(-random.Next(1, 7)),
                    Type = FeedbackType.Positive,
                    Title = pos2Title,
                    Content = $"{member.FirstName} {pos2Content}",
                    Context = pos2Context
                });

                foreach (var feedback in feedbackEntries)
                {
                    context.Feedbacks.Add(feedback);
                    context.Entry(feedback).Property("UserId").CurrentValue = currentUser.Id;
                }

                // Generate 2-3 goals per member with varied statuses
                var goals = new List<IndividualGoal>();
                
                // Skill development goal (In Progress)
                var (skillTitle, skillDesc, skillCat) = skillGoals[memberIndex % skillGoals.Length];
                var skillProgress = random.Next(30, 70);
                goals.Add(new IndividualGoal
                {
                    TeamMemberId = member.Id,
                    TeamMember = member,
                    Title = skillTitle,
                    Description = skillDesc,
                    Category = skillCat,
                    Status = GoalStatus.InProgress,
                    TargetDate = today.AddMonths(random.Next(1, 4)),
                    ProgressPercent = skillProgress,
                    Notes = "Making steady progress on this goal.",
                    Milestones = new List<GoalMilestone>
                    {
                        new GoalMilestone { Description = "Research and planning", SortOrder = 0, IsCompleted = true, CompletedDate = today.AddDays(-45) },
                        new GoalMilestone { Description = "Complete initial learning", SortOrder = 1, IsCompleted = skillProgress >= 50, CompletedDate = skillProgress >= 50 ? today.AddDays(-14) : null },
                        new GoalMilestone { Description = "Apply to real project", SortOrder = 2, IsCompleted = skillProgress >= 80, CompletedDate = skillProgress >= 80 ? today.AddDays(-3) : null },
                        new GoalMilestone { Description = "Document learnings", SortOrder = 3, IsCompleted = false }
                    }
                });
                
                // Career goal (varied status based on seniority)
                var (careerTitle, careerDesc, careerCat) = careerGoals[memberIndex % careerGoals.Length];
                var careerStatus = member.SkillLevel == SkillLevelEnum.Senior ? GoalStatus.InProgress : 
                                   member.SkillLevel == SkillLevelEnum.Junior ? GoalStatus.NotStarted : GoalStatus.InProgress;
                var careerProgress = careerStatus == GoalStatus.NotStarted ? 0 : random.Next(20, 60);
                goals.Add(new IndividualGoal
                {
                    TeamMemberId = member.Id,
                    TeamMember = member,
                    Title = careerTitle,
                    Description = careerDesc,
                    Category = careerCat,
                    Status = careerStatus,
                    TargetDate = today.AddMonths(random.Next(3, 6)),
                    ProgressPercent = careerProgress,
                    Notes = careerStatus == GoalStatus.NotStarted ? "Planning to start next quarter." : "Working towards this goal.",
                    Milestones = new List<GoalMilestone>
                    {
                        new GoalMilestone { Description = "Define success criteria", SortOrder = 0, IsCompleted = careerProgress >= 20, CompletedDate = careerProgress >= 20 ? today.AddDays(-30) : null },
                        new GoalMilestone { Description = "Create action plan", SortOrder = 1, IsCompleted = careerProgress >= 40, CompletedDate = careerProgress >= 40 ? today.AddDays(-14) : null },
                        new GoalMilestone { Description = "Execute plan", SortOrder = 2, IsCompleted = false },
                        new GoalMilestone { Description = "Review and adjust", SortOrder = 3, IsCompleted = false }
                    }
                });
                
                // For some members, add a completed goal
                if (random.Next(100) < 50 || member.SkillLevel == SkillLevelEnum.Senior)
                {
                    var (certTitle, certDesc, certCat) = certificationGoals[memberIndex % certificationGoals.Length];
                    goals.Add(new IndividualGoal
                    {
                        TeamMemberId = member.Id,
                        TeamMember = member,
                        Title = certTitle,
                        Description = certDesc,
                        Category = certCat,
                        Status = GoalStatus.Completed,
                        TargetDate = today.AddDays(-random.Next(10, 30)),
                        ProgressPercent = 100,
                        Notes = "Successfully completed!",
                        Milestones = new List<GoalMilestone>
                        {
                            new GoalMilestone { Description = "Register for exam", SortOrder = 0, IsCompleted = true, CompletedDate = today.AddDays(-60) },
                            new GoalMilestone { Description = "Complete study materials", SortOrder = 1, IsCompleted = true, CompletedDate = today.AddDays(-30) },
                            new GoalMilestone { Description = "Pass exam", SortOrder = 2, IsCompleted = true, CompletedDate = today.AddDays(-15) }
                        }
                    });
                }

                foreach (var goal in goals)
                {
                    context.IndividualGoals.Add(goal);
                    context.Entry(goal).Property("UserId").CurrentValue = currentUser.Id;
                    foreach (var milestone in goal.Milestones)
                    {
                        context.Entry(milestone).Property("UserId").CurrentValue = currentUser.Id;
                    }
                }
                
                memberIndex++;
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Generates Quick Notes to populate the Quick Notes screen.
        /// Uses polymorphic linking to connect notes to various entity types.
        /// </summary>
        private static async Task GenerateQuickNotesAsync(
            TrackerDbContext context,
            List<TeamMember> teamMembers,
            List<ObjectiveKeyResult> okrs,
            List<KeyPerformanceIndicator> kpis,
            List<Project> projects,
            User currentUser)
        {
            var today = DateTime.Today;
            var jordan = teamMembers.FirstOrDefault(t => t.FirstName == "Jordan");
            var morgan = teamMembers.FirstOrDefault(t => t.FirstName == "Morgan");
            var taylor = teamMembers.FirstOrDefault(t => t.FirstName == "Taylor");
            var casey = teamMembers.FirstOrDefault(t => t.FirstName == "Casey");
            var riley = teamMembers.FirstOrDefault(t => t.FirstName == "Riley");
            
            var dashboardProject = projects.FirstOrDefault(p => p.Name.Contains("Dashboard"));
            var apiProject = projects.FirstOrDefault(p => p.Name.Contains("API"));
            var analyticsProject = projects.FirstOrDefault(p => p.Name.Contains("Analytics"));
            
            var customerSatKpi = kpis.FirstOrDefault(k => k.Name.Contains("Customer Satisfaction"));
            var revenueKpi = kpis.FirstOrDefault(k => k.Name.Contains("Revenue"));
            var deliveryKpi = kpis.FirstOrDefault(k => k.Name.Contains("Delivery"));
            
            var firstOkr = okrs.FirstOrDefault();
            var secondOkr = okrs.Skip(1).FirstOrDefault();

            var quickNotes = new List<QuickNote>
            {
                // Team Member linked notes
                new QuickNote
                {
                    Title = "Jordan - API Migration Concerns",
                    Content = "Jordan mentioned concerns about the GraphQL migration timeline - need to discuss in next 1:1. Possible blocker with external API dependencies. They suggested breaking the migration into phases which could reduce risk.",
                    LinkedEntityType = NoteLinkedEntityType.TeamMember,
                    LinkedEntityId = jordan?.Id,
                    TeamMemberId = jordan?.Id,
                    CreatedAt = today.AddDays(-2),
                    IsPinned = true,
                    Category = NoteCategory.Meeting,
                    Tags = "follow-up,api,blocker"
                },
                new QuickNote
                {
                    Title = "Morgan Leadership Interest",
                    Content = "Morgan is interested in leading the mobile app initiative next quarter. They've shown strong technical leadership and good mentoring of juniors. Keep in mind for capacity planning and potential promotion track.",
                    LinkedEntityType = NoteLinkedEntityType.TeamMember,
                    LinkedEntityId = morgan?.Id,
                    TeamMemberId = morgan?.Id,
                    CreatedAt = today.AddDays(-5),
                    IsPinned = false,
                    Category = NoteCategory.Observation,
                    Tags = "career,leadership,promotion"
                },
                new QuickNote
                {
                    Title = "Taylor's Feature Flag Suggestion",
                    Content = "Taylor suggested implementing feature flags for the dashboard rollout. Good idea - add to next sprint planning. They can take the lead on the implementation since they have experience from previous company.",
                    LinkedEntityType = NoteLinkedEntityType.TeamMember,
                    LinkedEntityId = taylor?.Id,
                    TeamMemberId = taylor?.Id,
                    CreatedAt = today.AddDays(-7),
                    IsPinned = false,
                    Category = NoteCategory.Idea,
                    Tags = "ideas,sprint,dashboard"
                },
                
                // Project linked notes
                new QuickNote
                {
                    Title = "Dashboard Beta Feedback",
                    Content = "Dashboard redesign feedback from beta users: love the new charts, want more customization options. Consider adding widget drag-and-drop and color theme selection. Priority for next iteration.",
                    LinkedEntityType = NoteLinkedEntityType.Project,
                    LinkedEntityId = dashboardProject?.ID,
                    ProjectId = dashboardProject?.ID,
                    CreatedAt = today.AddDays(-1),
                    IsPinned = false,
                    Category = NoteCategory.Meeting,
                    Tags = "feedback,dashboard,beta,ux"
                },
                new QuickNote
                {
                    Title = "API Performance Wins",
                    Content = "API performance metrics showing improvement - latency down 15% after caching changes. Document wins for all-hands. Jordan's optimization work is paying off.",
                    LinkedEntityType = NoteLinkedEntityType.Project,
                    LinkedEntityId = apiProject?.ID,
                    ProjectId = apiProject?.ID,
                    CreatedAt = today.AddDays(-3),
                    IsPinned = false,
                    Category = NoteCategory.Observation,
                    Tags = "wins,performance,api"
                },
                new QuickNote
                {
                    Title = "Analytics Pipeline Scaling",
                    Content = "Analytics pipeline handling 2x expected load without issues. Good architecture decisions upfront. Consider writing up as tech blog post.",
                    LinkedEntityType = NoteLinkedEntityType.Project,
                    LinkedEntityId = analyticsProject?.ID,
                    ProjectId = analyticsProject?.ID,
                    CreatedAt = today.AddDays(-8),
                    IsPinned = false,
                    Category = NoteCategory.Observation,
                    Tags = "architecture,scaling,wins"
                },
                
                // KPI linked notes
                new QuickNote
                {
                    Title = "Customer Satisfaction Trending Up",
                    Content = "CSAT scores improving after support team changes. New ticket routing and faster response times are having impact. Continue monitoring weekly.",
                    LinkedEntityType = NoteLinkedEntityType.KPI,
                    LinkedEntityId = customerSatKpi?.KpiId,
                    CreatedAt = today.AddDays(-4),
                    IsPinned = false,
                    Category = NoteCategory.Observation,
                    Tags = "metrics,customer,support"
                },
                new QuickNote
                {
                    Title = "Revenue KPI Discussion",
                    Content = "Revenue target may need adjustment for Q3. Market conditions shifting. Schedule meeting with finance to review projections and potentially revise targets.",
                    LinkedEntityType = NoteLinkedEntityType.KPI,
                    LinkedEntityId = revenueKpi?.KpiId,
                    CreatedAt = today.AddDays(-6),
                    IsPinned = true,
                    Category = NoteCategory.Decision,
                    Tags = "finance,planning,targets"
                },
                new QuickNote
                {
                    Title = "Delivery Velocity Concern",
                    Content = "Sprint delivery velocity dropped last 2 sprints. Need to investigate - could be scope creep or estimation issues. Add to retro agenda.",
                    LinkedEntityType = NoteLinkedEntityType.KPI,
                    LinkedEntityId = deliveryKpi?.KpiId,
                    CreatedAt = today.AddDays(-2),
                    IsPinned = false,
                    Category = NoteCategory.Observation,
                    Tags = "agile,velocity,process"
                },
                
                // OKR linked notes
                new QuickNote
                {
                    Title = "OKR Progress Review",
                    Content = "Q1 OKR progress looking good overall. Platform reliability objective on track. Need to push on customer acquisition key results - currently at 65% of target.",
                    LinkedEntityType = NoteLinkedEntityType.OKR,
                    LinkedEntityId = firstOkr?.ObjectiveId,
                    CreatedAt = today.AddDays(-3),
                    IsPinned = true,
                    Category = NoteCategory.Meeting,
                    Tags = "okr,quarterly,review"
                },
                new QuickNote
                {
                    Title = "OKR Alignment Discussion",
                    Content = "Team expressed concern about OKR alignment with company goals. Schedule town hall to clarify strategy and how our objectives connect to broader mission.",
                    LinkedEntityType = NoteLinkedEntityType.OKR,
                    LinkedEntityId = secondOkr?.ObjectiveId,
                    CreatedAt = today.AddDays(-9),
                    IsPinned = false,
                    Category = NoteCategory.Meeting,
                    Tags = "strategy,alignment,communication"
                },
                
                // Standalone notes (no linked entity)
                new QuickNote
                {
                    Title = "Q2 Planning",
                    Content = "Q2 planning: Consider dedicating 20% capacity to tech debt. Team has been pushing hard on features. Need balance to maintain code quality and developer satisfaction.",
                    LinkedEntityType = NoteLinkedEntityType.None,
                    CreatedAt = today.AddDays(-10),
                    IsPinned = true,
                    Category = NoteCategory.Decision,
                    Tags = "planning,tech-debt,capacity"
                },
                new QuickNote
                {
                    Title = "Team Offsite Planning",
                    Content = "Need to schedule offsite for team bonding before end of quarter. Budget approved for 15 people. Looking at venues - considering escape room + dinner combo.",
                    LinkedEntityType = NoteLinkedEntityType.None,
                    CreatedAt = today.AddDays(-14),
                    IsPinned = false,
                    Category = NoteCategory.Todo,
                    Tags = "team,offsite,culture"
                },
                new QuickNote
                {
                    Title = "Performance Reviews",
                    Content = "Reminder: Performance review calibration meeting next Thursday. Prepare ratings for all directs. Need to finalize promotion recommendations for Jordan and possibly Morgan.",
                    LinkedEntityType = NoteLinkedEntityType.None,
                    CreatedAt = today.AddDays(-4),
                    IsPinned = true,
                    Category = NoteCategory.Reminder,
                    Tags = "admin,reviews,hr"
                },
                new QuickNote
                {
                    Title = "Team Standup Reflection",
                    Content = "Great standup today - team energy is high. Jordan and Morgan collaborating well on API/UI integration. This cross-functional pairing is working well.",
                    LinkedEntityType = NoteLinkedEntityType.None,
                    CreatedAt = today.AddDays(-1),
                    IsPinned = false,
                    Category = NoteCategory.Reflection,
                    Tags = "team,collaboration,wins"
                },
                new QuickNote
                {
                    Title = "Casey On-Call Concern",
                    Content = "Casey raised concern about on-call rotation fairness. Need to review schedule and possibly adjust. Some team members carrying more weekend load than others.",
                    LinkedEntityType = NoteLinkedEntityType.TeamMember,
                    LinkedEntityId = casey?.Id,
                    TeamMemberId = casey?.Id,
                    CreatedAt = today.AddDays(-6),
                    IsPinned = false,
                    Category = NoteCategory.Observation,
                    Tags = "on-call,concern"
                }
            };

            foreach (var note in quickNotes)
            {
                context.QuickNotes.Add(note);
                context.Entry(note).Property("UserId").CurrentValue = currentUser.Id;
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Generates Meeting Templates for the 1:1s feature.
        /// </summary>
        private static async Task GenerateMeetingTemplatesAsync(TrackerDbContext context, User currentUser)
        {
            var templates = new List<MeetingTemplate>
            {
                new MeetingTemplate
                {
                    Name = "Weekly 1:1",
                    Description = "Standard weekly check-in template",
                    IsSystemTemplate = true,
                    SuggestedDurationMinutes = 30,
                    SortOrder = 0,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "How are you doing? Personal check-in", Category = AgendaItemCategory.Topic, Priority = Severity.High, SortOrder = 0 },
                        new MeetingTemplateItem { Description = "What's on your mind? Open floor for anything pressing", Category = AgendaItemCategory.Topic, Priority = Severity.High, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Progress on goals - Review current objectives", Category = AgendaItemCategory.Update, Priority = Severity.Medium, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Blockers or concerns - Anything preventing you from doing your best work?", Category = AgendaItemCategory.Blocker, Priority = Severity.High, SortOrder = 3 },
                        new MeetingTemplateItem { Description = "Action items from last week - Review and update", Category = AgendaItemCategory.Decision, Priority = Severity.Medium, SortOrder = 4 }
                    }
                },
                new MeetingTemplate
                {
                    Name = "Career Development",
                    Description = "Focused career growth discussion",
                    IsSystemTemplate = true,
                    SuggestedDurationMinutes = 45,
                    SortOrder = 1,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "Career aspirations - Where do you see yourself in 1-2 years?", Category = AgendaItemCategory.CareerDevelopment, Priority = Severity.High, SortOrder = 0 },
                        new MeetingTemplateItem { Description = "Skill development - What skills do you want to build?", Category = AgendaItemCategory.CareerDevelopment, Priority = Severity.High, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Growth opportunities - What projects interest you?", Category = AgendaItemCategory.CareerDevelopment, Priority = Severity.High, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Feedback on support - How can I better support your growth?", Category = AgendaItemCategory.Feedback, Priority = Severity.Medium, SortOrder = 3 },
                        new MeetingTemplateItem { Description = "Next steps - Concrete action items for development", Category = AgendaItemCategory.Decision, Priority = Severity.High, SortOrder = 4 }
                    }
                },
                new MeetingTemplate
                {
                    Name = "Performance Check-in",
                    Description = "Mid-cycle performance discussion",
                    IsSystemTemplate = true,
                    SuggestedDurationMinutes = 45,
                    SortOrder = 2,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "Wins this period - Celebrate accomplishments", Category = AgendaItemCategory.Performance, Priority = Severity.High, SortOrder = 0 },
                        new MeetingTemplateItem { Description = "Goal progress review - How are you tracking?", Category = AgendaItemCategory.Performance, Priority = Severity.High, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Areas for improvement - What could be going better?", Category = AgendaItemCategory.Topic, Priority = Severity.High, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Feedback from peers - Any feedback to share?", Category = AgendaItemCategory.Feedback, Priority = Severity.Medium, SortOrder = 3 },
                        new MeetingTemplateItem { Description = "Expectations alignment - Are expectations clear?", Category = AgendaItemCategory.Topic, Priority = Severity.High, SortOrder = 4 }
                    }
                },
                new MeetingTemplate
                {
                    Name = "Project Review",
                    Description = "Deep dive on specific project",
                    IsSystemTemplate = false,
                    SuggestedDurationMinutes = 60,
                    SortOrder = 3,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "Project status - Current state and progress", Category = AgendaItemCategory.Update, Priority = Severity.High, SortOrder = 0 },
                        new MeetingTemplateItem { Description = "Timeline and milestones - Are we on track?", Category = AgendaItemCategory.Update, Priority = Severity.High, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Risks and blockers - What could derail us?", Category = AgendaItemCategory.Blocker, Priority = Severity.High, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Resource needs - Do you have what you need?", Category = AgendaItemCategory.Question, Priority = Severity.Medium, SortOrder = 3 },
                        new MeetingTemplateItem { Description = "Stakeholder updates - Any communication needed?", Category = AgendaItemCategory.Decision, Priority = Severity.Medium, SortOrder = 4 }
                    }
                }
            };

            foreach (var template in templates)
            {
                context.MeetingTemplates.Add(template);
                context.Entry(template).Property("UserId").CurrentValue = currentUser.Id;
                foreach (var item in template.Items)
                {
                    context.Entry(item).Property("UserId").CurrentValue = currentUser.Id;
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Generates Reminders for upcoming items.
        /// </summary>
        private static async Task GenerateRemindersAsync(
            TrackerDbContext context,
            List<TeamMember> teamMembers,
            List<OneOnOne> oneOnOnes,
            User currentUser)
        {
            var now = DateTime.Now;
            var jordan = teamMembers.FirstOrDefault(t => t.FirstName == "Jordan");
            var morgan = teamMembers.FirstOrDefault(t => t.FirstName == "Morgan");
            var casey = teamMembers.FirstOrDefault(t => t.FirstName == "Casey");
            var upcomingMeeting = oneOnOnes.FirstOrDefault(o => o.Date >= DateTime.Today);

            var reminders = new List<Reminder>
            {
                new Reminder
                {
                    Title = "Follow up with Jordan on API migration blockers",
                    Message = "Check on external API dependency issue that was raised in last 1:1",
                    Type = ReminderType.Custom,
                    Status = ReminderStatus.Pending,
                    DueDateTime = now.AddDays(2).Date.AddHours(9),
                    TeamMemberId = jordan?.Id
                },
                new Reminder
                {
                    Title = "Prepare Q2 capacity planning",
                    Message = "Review project pipeline and headcount needs for next quarter",
                    Type = ReminderType.Custom,
                    Status = ReminderStatus.Pending,
                    DueDateTime = now.AddDays(7).Date.AddHours(10)
                },
                new Reminder
                {
                    Title = "Schedule Morgan's career development meeting",
                    Message = "Discuss mobile app leadership opportunity and growth path",
                    Type = ReminderType.Custom,
                    Status = ReminderStatus.Pending,
                    DueDateTime = now.AddDays(5).Date.AddHours(14),
                    TeamMemberId = morgan?.Id
                },
                new Reminder
                {
                    Title = "Review Casey's promotion case",
                    Message = "Gather peer feedback and prepare documentation for promotion committee",
                    Type = ReminderType.Custom,
                    Status = ReminderStatus.Pending,
                    DueDateTime = now.AddDays(10).Date.AddHours(11),
                    TeamMemberId = casey?.Id
                },
                new Reminder
                {
                    Title = "Team offsite planning",
                    Message = "Book venue and send calendar invites for Q2 team bonding event",
                    Type = ReminderType.Custom,
                    Status = ReminderStatus.Pending,
                    DueDateTime = now.AddDays(14).Date.AddHours(9)
                },
                new Reminder
                {
                    Title = "Submit expense reports",
                    Message = "Monthly expense report deadline - don't forget receipts",
                    Type = ReminderType.Custom,
                    Status = ReminderStatus.Dismissed, // Already handled
                    DueDateTime = now.AddDays(-2).Date.AddHours(9)
                },
                new Reminder
                {
                    Title = "Upcoming 1:1 meeting",
                    Message = upcomingMeeting != null ? $"1:1 with {upcomingMeeting.TeamMember?.FirstName}" : "Check schedule for upcoming meetings",
                    Type = ReminderType.Meeting,
                    Status = ReminderStatus.Pending,
                    DueDateTime = upcomingMeeting?.Date ?? now.AddDays(3),
                    OneOnOneId = upcomingMeeting?.Id,
                    TeamMemberId = upcomingMeeting?.TeamMember?.Id
                },
                new Reminder
                {
                    Title = "Team engagement check",
                    Message = "Review engagement metrics and check in with anyone who hasn't had a recent 1:1",
                    Type = ReminderType.Engagement,
                    Status = ReminderStatus.Pending,
                    DueDateTime = now.AddDays(7).Date.AddHours(10),
                    IsRecurring = true,
                    RecurrenceIntervalDays = 14
                }
            };

            foreach (var reminder in reminders)
            {
                context.Reminders.Add(reminder);
                context.Entry(reminder).Property("UserId").CurrentValue = currentUser.Id;
            }

            await context.SaveChangesAsync();
        }

        #endregion
    }
}

