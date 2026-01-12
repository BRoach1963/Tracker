using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for TeamMember data access operations.
    /// Handles all operations including CRUD, search, and filtering by meeting recency.
    /// </summary>
    public class TeamMemberRepository : ITeamMemberRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory; // For PostgreSQL parallel operations
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of TeamMemberRepository.
        /// </summary>
        /// <param name="context">The database context (for SQLite/SQL Server).</param>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="contextFactory">Optional factory for creating contexts (for PostgreSQL).</param>
        public TeamMemberRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(TeamMemberRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all team members for the current user.
        /// Includes runtime statistics (last/next 1:1, open tasks, active goals).
        /// </summary>
        public async Task<List<TeamMember>> GetTeamMembersAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetTeamMembersAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTeamMembersAsync: No context ===");
                return new List<TeamMember>();
            }

            try
            {
                var teamMembers = await context.TeamMembers
                    .AsNoTracking()
                    .OrderBy(tm => tm.Role)
                    .ThenBy(tm => tm.LastName)
                    .ThenBy(tm => tm.FirstName)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"=== GetTeamMembersAsync: Query succeeded, got {teamMembers.Count} members ===");

                // Populate runtime properties for display
                await PopulateTeamMemberStatsAsync(teamMembers);

                return teamMembers;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTeamMembersAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving team members from database");
                return new List<TeamMember>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Gets a specific team member by ID.
        /// </summary>
        public async Task<TeamMember?> GetTeamMemberByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.TeamMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving team member with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new team member.
        /// </summary>
        public async Task<Guid> AddTeamMemberAsync(TeamMember teamMember)
        {
            if (_context == null)
            {
                _logger.Error("AddTeamMemberAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.TeamMembers.Add(teamMember);
                await _context.SaveChangesAsync();
                _logger.Info("Added team member: {0} {1} (ID: {2})", teamMember.FirstName, teamMember.LastName, teamMember.Id);
                return teamMember.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding team member");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing team member.
        /// </summary>
        public async Task<bool> UpdateTeamMemberAsync(TeamMember teamMember)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.TeamMembers.FindAsync(teamMember.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateTeamMemberAsync: Team member ID {0} not found", teamMember.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(teamMember);
                await _context.SaveChangesAsync();
                _logger.Info("Updated team member ID: {0}", teamMember.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating team member ID: {0}", teamMember.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a team member by ID.
        /// </summary>
        public async Task<bool> DeleteTeamMemberAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var teamMember = await _context.TeamMembers.FindAsync(id);
                if (teamMember != null)
                {
                    _context.TeamMembers.Remove(teamMember);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted team member ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting team member ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Finds a team member by display name (case-insensitive).
        /// Matches on full name, first name, or last name.
        /// </summary>
        public async Task<TeamMember?> FindTeamMemberByNameAsync(string displayName)
        {
            if (_context == null || string.IsNullOrWhiteSpace(displayName)) return null;

            try
            {
                // Try exact match first (case-insensitive)
                var member = await _context.TeamMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        (t.FirstName + " " + t.LastName).ToLower() == displayName.ToLower() ||
                        t.FirstName.ToLower() == displayName.ToLower() ||
                        t.LastName.ToLower() == displayName.ToLower());

                return member;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error finding team member by name: {0}", displayName);
                return null;
            }
        }

        /// <summary>
        /// Gets team members who haven't had a 1:1 meeting in the specified number of weeks.
        /// </summary>
        public async Task<List<TeamMember>> GetTeamMembersWithoutRecentOneOnOneAsync(int weeks)
        {
            if (_context == null) return new List<TeamMember>();

            try
            {
                var cutoffDate = DateTime.Now.AddDays(-weeks * 7);

                // Get all team members
                var teamMembers = await _context.TeamMembers
                    .AsNoTracking()
                    .ToListAsync();

                // Get IDs of team members with recent 1:1s (using ReportTeamMemberId for the report in 1:1)
                var recentOneOnOneTeamMemberIds = await _context.Meetings
                    .AsNoTracking()
                    .Where(m => !m.IsDeleted && 
                                m.Type == Common.Enums.MeetingType.OneOnOne &&
                                m.ScheduledAt >= cutoffDate &&
                                m.ReportTeamMemberId.HasValue)
                    .Select(m => m.ReportTeamMemberId!.Value)
                    .Distinct()
                    .ToListAsync();

                // Return team members without recent 1:1s
                return teamMembers.Where(t => !recentOneOnOneTeamMemberIds.Contains(t.Id)).ToList();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting team members without recent 1:1");
                return new List<TeamMember>();
            }
        }

        /// <summary>
        /// Populates runtime statistics for team members (last 1:1, next 1:1, task/goal counts).
        /// Uses factory pattern for PostgreSQL to enable parallel queries.
        /// </summary>
        private async Task PopulateTeamMemberStatsAsync(List<TeamMember> teamMembers)
        {
            if (_context == null || teamMembers.Count == 0) return;

            try
            {
                var teamMemberIds = teamMembers.Select(t => t.Id).ToList();
                var today = DateTime.Now.Date;

                // Run queries in parallel - 1:1 meetings use ReportTeamMemberId
                var lastOneOnOnesTask = _context.Meetings
                    .AsNoTracking()
                    .Where(m => m.Type == Common.Enums.MeetingType.OneOnOne &&
                                m.ReportTeamMemberId.HasValue &&
                                teamMemberIds.Contains(m.ReportTeamMemberId.Value) && 
                                m.ScheduledAt <= today)
                    .GroupBy(m => m.ReportTeamMemberId!.Value)
                    .Select(g => new { TeamMemberId = g.Key, LastDate = g.Max(m => m.ScheduledAt) })
                    .ToListAsync();

                var nextOneOnOnesTask = _context.Meetings
                    .AsNoTracking()
                    .Where(m => m.Type == Common.Enums.MeetingType.OneOnOne &&
                                m.ReportTeamMemberId.HasValue &&
                                teamMemberIds.Contains(m.ReportTeamMemberId.Value) &&
                                m.ScheduledAt >= today &&
                                m.Status == Common.Enums.MeetingStatus.Scheduled)
                    .GroupBy(m => m.ReportTeamMemberId!.Value)
                    .Select(g => new { TeamMemberId = g.Key, NextDate = g.Min(m => m.ScheduledAt), UpcomingCount = g.Count() })
                    .ToListAsync();

                var taskCountsTask = _context.TrackerTasks
                    .AsNoTracking()
                    .Where(t => t.OwnerTeamMemberId.HasValue && 
                                teamMemberIds.Contains(t.OwnerTeamMemberId.Value) && 
                                !t.IsCompleted)
                    .GroupBy(t => t.OwnerTeamMemberId!.Value)
                    .Select(g => new { TeamMemberId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var goalCountsTask = _context.DevelopmentGoals
                    .AsNoTracking()
                    .Where(g => teamMemberIds.Contains(g.TeamMemberId) &&
                                g.Status != Common.Enums.DevelopmentGoalStatus.Completed &&
                                g.Status != Common.Enums.DevelopmentGoalStatus.Cancelled)
                    .GroupBy(g => g.TeamMemberId)
                    .Select(g => new { TeamMemberId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Wait for all queries to complete
                await Task.WhenAll(lastOneOnOnesTask, nextOneOnOnesTask, taskCountsTask, goalCountsTask)
                    .ConfigureAwait(false);

                var lastOneOnOnes = await lastOneOnOnesTask.ConfigureAwait(false);
                var nextOneOnOnes = await nextOneOnOnesTask.ConfigureAwait(false);
                var taskCounts = await taskCountsTask.ConfigureAwait(false);
                var goalCounts = await goalCountsTask.ConfigureAwait(false);

                // Populate the team members
                PopulateTeamMemberStatsFromResults(teamMembers, lastOneOnOnes, nextOneOnOnes, taskCounts, goalCounts);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error populating team member stats");
            }
        }

        /// <summary>
        /// Populates team member properties from query results.
        /// </summary>
        private void PopulateTeamMemberStatsFromResults<TLast, TNext, TTask, TGoal>(
            List<TeamMember> teamMembers,
            List<TLast> lastOneOnOnes,
            List<TNext> nextOneOnOnes,
            List<TTask> taskCounts,
            List<TGoal> goalCounts)
            where TLast : class
            where TNext : class
            where TTask : class
            where TGoal : class
        {
            foreach (var tm in teamMembers)
            {
                // Use dynamic to access anonymous type properties
                dynamic? lastMeeting = lastOneOnOnes.FirstOrDefault(x => ((dynamic)x!).TeamMemberId == tm.Id);
                tm.LastOneOnOneDate = lastMeeting?.LastDate;

                dynamic? nextMeeting = nextOneOnOnes.FirstOrDefault(x => ((dynamic)x!).TeamMemberId == tm.Id);
                tm.NextOneOnOneDate = nextMeeting?.NextDate;
                tm.UpcomingMeetingCount = nextMeeting?.UpcomingCount ?? 0;

                dynamic? taskCount = taskCounts.FirstOrDefault(x => ((dynamic)x!).TeamMemberId == tm.Id);
                tm.OpenTaskCount = taskCount?.Count ?? 0;

                dynamic? goalCount = goalCounts.FirstOrDefault(x => ((dynamic)x!).TeamMemberId == tm.Id);
                tm.ActiveGoalCount = goalCount?.Count ?? 0;
            }
        }

        /// <summary>
        /// Disposes the context if it was created by the factory.
        /// </summary>
        private void DisposeIfFactory(TrackerDbContext context)
        {
            // Only dispose if it came from the factory and not the primary context
            if (context != _context && context is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}
