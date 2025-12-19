using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Database
{
    /// <summary>
    /// Result of a connection test.
    /// </summary>
    public class ConnectionTestResult
    {
        public bool Success { get; set; }
        public bool DatabaseExists { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Manages database operations using Entity Framework Core.
    /// Supports both SQLite (local) and SQL Server (remote) providers.
    /// </summary>
    public class TrackerDbManager
    {
        #region Fields

        private bool _isInitialized;
        private TrackerDbContext? _context;
        private DatabaseSettings? _settings;

        private readonly LoggingManager.Logger _logger = new(nameof(TrackerDbManager), "DatabaseLog");

        #endregion

        #region Singleton Instance

        private static readonly Lazy<TrackerDbManager> _lazyInstance = 
            new(() => new TrackerDbManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of TrackerDbManager.
        /// Uses Lazy&lt;T&gt; for thread-safe initialization.
        /// </summary>
        public static TrackerDbManager Instance => _lazyInstance.Value;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the path to the SQLite database file (null if using SQL Server).
        /// </summary>
        public string? DatabasePath => _context?.DatabasePath;

        /// <summary>
        /// Gets whether the database is initialized and ready.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the current database settings.
        /// </summary>
        public DatabaseSettings? CurrentSettings => _settings;

        /// <summary>
        /// Gets whether we're connected to a local SQLite database.
        /// </summary>
        public bool IsLocalDatabase => _settings?.Type == DatabaseType.SQLite;

        /// <summary>
        /// Gets whether we're in offline mode (SQL Server configured but unavailable).
        /// </summary>
        public bool IsOfflineMode => _settings?.IsOfflineMode ?? false;

        #endregion

        #region Initialization

        /// <summary>
        /// Legacy initialization using default SQLite.
        /// Note: Prefer InitializeAsync for non-blocking initialization.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            // Use Task.Run to avoid deadlock in sync-over-async scenario
            Task.Run(async () => await InitializeAsync(new DatabaseSettings { Type = DatabaseType.SQLite }, true)
                .ConfigureAwait(false)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initialize the database with the specified settings.
        /// </summary>
        public async Task InitializeAsync(DatabaseSettings settings, bool createIfNotExists = true, bool seedSampleData = false)
        {
            if (_isInitialized && _settings?.GetConnectionString() == settings.GetConnectionString())
            {
                return; // Already initialized with same settings
            }

            try
            {
                _settings = settings;
                _context?.Dispose();
                _context = new TrackerDbContext(settings);

                if (createIfNotExists)
                {
                    await Task.Run(() => _context.EnsureCreated());
                }

                // Verify connection
                await _context.Database.CanConnectAsync();

                // Check if database schema is up to date (Phase 1 tables exist)
                // If Phase 1 tables are missing, recreate the database
                bool schemaOutdated = false;
                try
                {
                    // Try to query Phase 1 tables to see if they exist
                    _ = await _context.OneOnOneLinkedTasks.AnyAsync();
                    _ = await _context.OneOnOneLinkedOkrs.AnyAsync();
                    _ = await _context.OneOnOneLinkedKpis.AnyAsync();
                }
                catch
                {
                    // Phase 1 tables don't exist - schema is outdated
                    schemaOutdated = true;
                }

                if (schemaOutdated)
                {
                    _logger.Info("Database schema outdated - recreating database with Phase 1 tables");
                    await _context.Database.EnsureDeletedAsync();
                    await _context.Database.EnsureCreatedAsync();
                }
                else
                {
                    // Ensure all tables exist (creates missing tables if any)
                    await _context.Database.EnsureCreatedAsync();
                }

                // Seed sample data if requested
                if (seedSampleData)
                {
                    await DatabaseSeeder.SeedSampleDataAsync(_context);
                    _logger.Info("Sample data seeded to database");
                }

                _isInitialized = true;
                _logger.Info("Database initialized: Type={0}, Path={1}", 
                    settings.Type, 
                    settings.Type == DatabaseType.SQLite ? DatabaseSettings.GetSqlitePath() : settings.Server);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize database");
                throw;
            }
        }

        /// <summary>
        /// Clears all data from the database.
        /// </summary>
        public async Task<bool> ClearAllDataAsync()
        {
            if (_context == null) return false;

            try
            {
                await DatabaseSeeder.ClearAllDataAsync(_context);
                _logger.Info("All data cleared from database");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to clear database data");
                return false;
            }
        }

        /// <summary>
        /// Checks if the database contains any data.
        /// </summary>
        public async Task<bool> HasDataAsync()
        {
            if (_context == null) return false;

            try
            {
                // Clear change tracker to ensure fresh query
                _context.ChangeTracker.Clear();
                return await _context.TeamMembers.AnyAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Seeds sample data into the database.
        /// </summary>
        /// <param name="forceReseed">If true, clears existing data before seeding. If false, only seeds if database is empty.</param>
        public async Task<bool> SeedSampleDataAsync(bool forceReseed = false)
        {
            if (_context == null) return false;

            try
            {
                // Ensure schema is up to date before seeding (check for Phase 1 tables)
                bool schemaOutdated = false;
                try
                {
                    _ = await _context.OneOnOneLinkedTasks.AnyAsync();
                    _ = await _context.OneOnOneLinkedOkrs.AnyAsync();
                    _ = await _context.OneOnOneLinkedKpis.AnyAsync();
                }
                catch
                {
                    schemaOutdated = true;
                }

                if (schemaOutdated)
                {
                    _logger.Info("Database schema outdated - recreating database with Phase 1 tables before seeding");
                    await _context.Database.EnsureDeletedAsync();
                    await _context.Database.EnsureCreatedAsync();
                }
                else if (forceReseed)
                {
                    // Force reseed - recreate database to ensure schema matches current configuration
                    // This is necessary because foreign key configurations may have changed
                    _logger.Info("Force reseed requested - recreating database to ensure schema matches current configuration");
                    await _context.Database.EnsureDeletedAsync();
                    await _context.Database.EnsureCreatedAsync();
                }
                else
                {
                    // Ensure all tables exist (creates missing tables if any)
                    await _context.Database.EnsureCreatedAsync();
                }

                var seeded = await DatabaseSeeder.SeedSampleDataAsync(_context, forceReseed);
                if (seeded)
                {
                    _logger.Info("Sample data seeded to database (forceReseed={0})", forceReseed);
                    return true;
                }
                else
                {
                    _logger.Warn("Sample data seeding skipped - database already contains data and forceReseed=false");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to seed sample data: {0}", ex.Message);
                // Re-throw with more context
                throw;
            }
        }

        /// <summary>
        /// Gets or creates a User in the database based on the current username.
        /// Sets UserSettingsManager.CurrentUserId after successful retrieval/creation.
        /// </summary>
        /// <param name="username">The username to look up or create. If null, uses UserSettingsManager.CurrentUser.</param>
        /// <returns>The User entity, or null if database is not initialized.</returns>
        public async Task<User?> GetOrCreateUserAsync(string? username = null)
        {
            if (_context == null) return null;

            try
            {
                username ??= UserSettingsManager.Instance.CurrentUser;
                if (string.IsNullOrEmpty(username))
                {
                    username = Environment.UserName;
                }

                // Check if User already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                
                if (existingUser != null)
                {
                    UserSettingsManager.Instance.CurrentUserId = existingUser.Id;
                    return existingUser;
                }

                // Create new User
                var newUser = new User
                {
                    Username = username,
                    Email = $"{username}@company.com", // Default email
                    DisplayName = username,
                    IsActive = true
                };
                
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                
                // Reload to get the ID
                var createdUser = await _context.Users.FirstAsync(u => u.Username == username);
                UserSettingsManager.Instance.CurrentUserId = createdUser.Id;
                
                _logger.Info("Created new User: {0} (Id: {1})", username, createdUser.Id);
                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to get or create User: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Test a database connection without initializing.
        /// </summary>
        public async Task<ConnectionTestResult> TestConnectionAsync(DatabaseSettings settings)
        {
            var result = new ConnectionTestResult();

            try
            {
                if (settings.Type == DatabaseType.SQLite)
                {
                    // SQLite always succeeds - file will be created
                    result.Success = true;
                    result.DatabaseExists = File.Exists(DatabaseSettings.GetSqlitePath());
                    return result;
                }

                // SQL Server - test connection
                var connectionString = settings.GetConnectionString();
                
                // First try to connect to master to check if server is reachable
                var masterConnectionString = connectionString.Replace($"Database={settings.Database}", "Database=master");
                
                using var masterConnection = new SqlConnection(masterConnectionString);
                await masterConnection.OpenAsync();

                // Check if the specific database exists
                using var cmd = masterConnection.CreateCommand();
                cmd.CommandText = $"SELECT DB_ID('{settings.Database}')";
                var dbId = await cmd.ExecuteScalarAsync();
                
                result.Success = true;
                result.DatabaseExists = dbId != DBNull.Value && dbId != null;
                
                return result;
            }
            catch (SqlException ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Number switch
                {
                    -1 => "Could not connect to server. Check the server name and network connection.",
                    18456 => "Login failed. Check your username and password.",
                    4060 => "Cannot open database. Check the database name.",
                    _ => ex.Message
                };
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Switch to a different database connection.
        /// </summary>
        public async Task SwitchDatabaseAsync(DatabaseSettings newSettings, bool createIfNotExists = true)
        {
            _isInitialized = false;
            await InitializeAsync(newSettings, createIfNotExists);
        }

        public void Reset()
        {
            _isInitialized = false;
            _context?.Dispose();
            _context = null;
            _settings = null;
        }

        public void Shutdown()
        {
            _context?.Dispose();
            _context = null;
            _isInitialized = false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the current UserId from UserSettingsManager.
        /// Returns null if not set (should not happen in normal operation).
        /// </summary>
        private int? GetCurrentUserId()
        {
            return UserSettingsManager.Instance.CurrentUserId;
        }

        #endregion

        #region TeamMember Operations

        public async Task<List<TeamMember>> GetTeamMembersAsync()
        {
            if (_context == null) return new List<TeamMember>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetTeamMembersAsync called but CurrentUserId is not set");
                return new List<TeamMember>();
            }

            try
            {
                var teamMembers = await _context.TeamMembers
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted && EF.Property<int>(t, "UserId") == currentUserId.Value)
                    .OrderBy(tm => tm.Role)
                    .ThenBy(tm => tm.LastName)
                    .ThenBy(tm => tm.FirstName)
                    .ToListAsync()
                    .ConfigureAwait(false);

                // Populate runtime properties for display
                await PopulateTeamMemberStatsAsync(teamMembers, currentUserId.Value).ConfigureAwait(false);

                return teamMembers;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving team members from database");
                return new List<TeamMember>();
            }
        }

        /// <summary>
        /// Populates runtime statistics for team members (last 1:1, next 1:1, task/goal counts).
        /// </summary>
        private async Task PopulateTeamMemberStatsAsync(List<TeamMember> teamMembers, int userId)
        {
            if (_context == null || teamMembers.Count == 0) return;

            try
            {
                var teamMemberIds = teamMembers.Select(t => t.Id).ToList();
                var today = DateTime.Now.Date;

                // Execute all stat queries in parallel for better performance
                var lastOneOnOnesTask = _context.OneOnOnes
                    .AsNoTracking()
                    .Where(o => !o.IsDeleted &&
                                EF.Property<int>(o, "UserId") == userId &&
                                teamMemberIds.Contains(o.TeamMember.Id) &&
                                o.Date <= today)
                    .GroupBy(o => o.TeamMember.Id)
                    .Select(g => new { TeamMemberId = g.Key, LastDate = g.Max(o => o.Date) })
                    .ToListAsync();

                var nextOneOnOnesTask = _context.OneOnOnes
                    .AsNoTracking()
                    .Where(o => !o.IsDeleted &&
                                EF.Property<int>(o, "UserId") == userId &&
                                teamMemberIds.Contains(o.TeamMember.Id) &&
                                o.Date >= today &&
                                o.Status == Common.Enums.MeetingStatusEnum.Scheduled)
                    .GroupBy(o => o.TeamMember.Id)
                    .Select(g => new { TeamMemberId = g.Key, NextDate = g.Min(o => o.Date), UpcomingCount = g.Count() })
                    .ToListAsync();

                var taskCountsTask = _context.Tasks
                    .AsNoTracking()
                    .Include(t => t.Owner)
                    .Where(t => !t.IsDeleted &&
                                EF.Property<int>(t, "UserId") == userId &&
                                t.Owner != null &&
                                teamMemberIds.Contains(t.Owner.Id) &&
                                !t.IsCompleted)
                    .GroupBy(t => t.Owner.Id)
                    .Select(g => new { TeamMemberId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var goalCountsTask = _context.IndividualGoals
                    .AsNoTracking()
                    .Where(g => !g.IsDeleted &&
                                EF.Property<int>(g, "UserId") == userId &&
                                teamMemberIds.Contains(g.TeamMemberId) &&
                                g.Status != GoalStatus.Completed &&
                                g.Status != GoalStatus.Cancelled)
                    .GroupBy(g => g.TeamMemberId)
                    .Select(g => new { TeamMemberId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Wait for all queries to complete in parallel
                await Task.WhenAll(lastOneOnOnesTask, nextOneOnOnesTask, taskCountsTask, goalCountsTask)
                    .ConfigureAwait(false);

                var lastOneOnOnes = await lastOneOnOnesTask.ConfigureAwait(false);
                var nextOneOnOnes = await nextOneOnOnesTask.ConfigureAwait(false);
                var taskCounts = await taskCountsTask.ConfigureAwait(false);
                var goalCounts = await goalCountsTask.ConfigureAwait(false);

                // Populate the team members
                foreach (var tm in teamMembers)
                {
                    var lastMeeting = lastOneOnOnes.FirstOrDefault(x => x.TeamMemberId == tm.Id);
                    tm.LastOneOnOneDate = lastMeeting?.LastDate;

                    var nextMeeting = nextOneOnOnes.FirstOrDefault(x => x.TeamMemberId == tm.Id);
                    tm.NextOneOnOneDate = nextMeeting?.NextDate;
                    tm.UpcomingMeetingCount = nextMeeting?.UpcomingCount ?? 0;

                    var taskCount = taskCounts.FirstOrDefault(x => x.TeamMemberId == tm.Id);
                    tm.OpenTaskCount = taskCount?.Count ?? 0;

                    var goalCount = goalCounts.FirstOrDefault(x => x.TeamMemberId == tm.Id);
                    tm.ActiveGoalCount = goalCount?.Count ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error populating team member stats");
            }
        }

        public async Task<TeamMember?> GetTeamMemberByIdAsync(int id)
        {
            if (_context == null) return null;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetTeamMemberByIdAsync called but CurrentUserId is not set");
                return null;
            }

            try
            {
                return await _context.TeamMembers
                    .Where(t => !t.IsDeleted && EF.Property<int>(t, "UserId") == currentUserId.Value)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving team member with id {0}", id);
                return null;
            }
        }

        public async Task<int> AddTeamMemberAsync(TeamMember teamMember)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddTeamMemberAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.TeamMembers.Add(teamMember);
                // Set UserId shadow property
                _context.Entry(teamMember).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                _logger.Info("Added team member: {0} {1} (ID: {2})", teamMember.FirstName, teamMember.LastName, teamMember.Id);
                return teamMember.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding team member");
                return 0;
            }
        }

        public async Task<bool> UpdateTeamMemberAsync(TeamMember teamMember)
        {
            if (_context == null) return false;

            try
            {
                // Get the existing tracked entity
                var existing = await _context.TeamMembers.FindAsync(teamMember.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateTeamMemberAsync: Team member ID {0} not found", teamMember.Id);
                    return false;
                }

                // Copy values from the updated entity to the tracked entity
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

        public async Task<bool> DeleteTeamMemberAsync(int id)
        {
            if (_context == null) return false;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("DeleteTeamMemberAsync called but CurrentUserId is not set");
                return false;
            }

            try
            {
                var teamMember = await _context.TeamMembers
                    .Where(t => t.Id == id && EF.Property<int>(t, "UserId") == currentUserId.Value)
                    .FirstOrDefaultAsync();
                if (teamMember != null)
                {
                    _context.TeamMembers.Remove(teamMember); // Soft delete handled by SaveChanges
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

        #endregion

        #region OneOnOne Operations

        public async Task<List<OneOnOne>> GetOneOnOnesAsync()
        {
            if (_context == null) return new List<OneOnOne>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetOneOnOnesAsync called but CurrentUserId is not set");
                return new List<OneOnOne>();
            }

            try
            {
                var query = _context.OneOnOnes
                    .Where(o => !o.IsDeleted && EF.Property<int>(o, "UserId") == currentUserId.Value)
                    .Include(o => o.TeamMember)
                    .Include(o => o.Tasks.Where(t => !t.IsDeleted))
                    .Include(o => o.AgendaItems.Where(a => !a.IsDeleted))
                    .AsQueryable();

                // Only include Phase 1 linked tables if they exist
                try
                {
                    query = query
                        .Include(o => o.LinkedTasks.Where(lt => !lt.IsDeleted)).ThenInclude(lt => lt.Task)
                        .Include(o => o.LinkedOkrs.Where(lo => !lo.IsDeleted)).ThenInclude(lo => lo.Okr)
                        .Include(o => o.LinkedKpis.Where(lk => !lk.IsDeleted)).ThenInclude(lk => lk.Kpi);
                }
                catch
                {
                    // Phase 1 tables don't exist yet - skip them
                }

                return await query
                    .OrderByDescending(o => o.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving one-on-ones from database");
                return new List<OneOnOne>();
            }
        }

        public async Task<OneOnOne?> GetOneOnOneByIdAsync(int id)
        {
            if (_context == null) return null;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetOneOnOneByIdAsync called but CurrentUserId is not set");
                return null;
            }

            try
            {
                return await _context.OneOnOnes
                    .Where(o => !o.IsDeleted && EF.Property<int>(o, "UserId") == currentUserId.Value)
                    .Include(o => o.TeamMember)
                    .Include(o => o.Tasks.Where(t => !t.IsDeleted))
                    .Include(o => o.AgendaItems.Where(a => !a.IsDeleted))
                    .Include(o => o.LinkedTasks.Where(lt => !lt.IsDeleted)).ThenInclude(lt => lt.Task)
                    .Include(o => o.LinkedOkrs.Where(lo => !lo.IsDeleted)).ThenInclude(lo => lo.Okr)
                    .Include(o => o.LinkedKpis.Where(lk => !lk.IsDeleted)).ThenInclude(lk => lk.Kpi)
                    .FirstOrDefaultAsync(o => o.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving one-on-one with id {0}", id);
                return null;
            }
        }

        public async Task<int> AddOneOnOneAsync(OneOnOne oneOnOne)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddOneOnOneAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.OneOnOnes.Add(oneOnOne);
                // Set UserId shadow property
                _context.Entry(oneOnOne).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                _logger.Info("Added one-on-one ID: {0}", oneOnOne.Id);
                return oneOnOne.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding one-on-one");
                return 0;
            }
        }

        public async Task<bool> UpdateOneOnOneAsync(OneOnOne oneOnOne)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.OneOnOnes.FindAsync(oneOnOne.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateOneOnOneAsync: OneOnOne ID {0} not found", oneOnOne.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(oneOnOne);
                await _context.SaveChangesAsync();
                _logger.Info("Updated one-on-one ID: {0}", oneOnOne.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating one-on-one ID: {0}", oneOnOne.Id);
                return false;
            }
        }

        public async Task<bool> DeleteOneOnOneAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var oneOnOne = await _context.OneOnOnes.FindAsync(id);
                if (oneOnOne != null)
                {
                    _context.OneOnOnes.Remove(oneOnOne);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted one-on-one ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting one-on-one ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets the most recent OneOnOne meeting for a specific team member (excluding the current meeting if provided).
        /// Used to show previous meeting summary and rollover uncompleted items.
        /// </summary>
        public async Task<OneOnOne?> GetPreviousOneOnOneAsync(int teamMemberId, int? excludeOneOnOneId = null)
        {
            if (_context == null) return null;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetPreviousOneOnOneAsync called but CurrentUserId is not set");
                return null;
            }

            try
            {
                var query = _context.OneOnOnes
                    .Where(o => !o.IsDeleted && EF.Property<int>(o, "UserId") == currentUserId.Value && o.TeamMember.Id == teamMemberId);

                if (excludeOneOnOneId.HasValue)
                {
                    query = query.Where(o => o.Id != excludeOneOnOneId.Value);
                }

                return await query
                    .Include(o => o.TeamMember)
                    .Include(o => o.Tasks.Where(t => !t.IsDeleted))
                    .Include(o => o.AgendaItems.Where(a => !a.IsDeleted))
                    .OrderByDescending(o => o.Date)
                    .ThenByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving previous one-on-one for team member {0}", teamMemberId);
                return null;
            }
        }

        /// <summary>
        /// Gets all OneOnOne meetings for a specific team member.
        /// Used to show meeting history in the team member view.
        /// </summary>
        public async Task<List<OneOnOne>> GetMeetingsForTeamMemberAsync(int teamMemberId)
        {
            if (_context == null) return new List<OneOnOne>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetMeetingsForTeamMemberAsync called but CurrentUserId is not set");
                return new List<OneOnOne>();
            }

            try
            {
                return await _context.OneOnOnes
                    .Where(o => !o.IsDeleted && EF.Property<int>(o, "UserId") == currentUserId.Value && o.TeamMember.Id == teamMemberId)
                    .Include(o => o.TeamMember)
                    .Include(o => o.Tasks.Where(t => !t.IsDeleted))
                    .Include(o => o.AgendaItems.Where(a => !a.IsDeleted))
                    .OrderByDescending(o => o.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meetings for team member {0}", teamMemberId);
                return new List<OneOnOne>();
            }
        }

        /// <summary>
        /// Gets all uncompleted MeetingTasks for a specific team member from previous meetings.
        /// Used to rollover unfinished items into the next meeting.
        /// </summary>
        public async Task<List<MeetingTask>> GetUncompletedMeetingTasksAsync(int teamMemberId)
        {
            if (_context == null) return new List<MeetingTask>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetUncompletedMeetingTasksAsync called but CurrentUserId is not set");
                return new List<MeetingTask>();
            }

            try
            {
                return await _context.MeetingTasks
                    .Where(t => !t.IsDeleted && !t.IsCompleted && EF.Property<int>(t, "UserId") == currentUserId.Value && t.Owner.Id == teamMemberId)
                    .Include(t => t.Owner)
                    .OrderByDescending(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving uncompleted meeting tasks for team member {0}", teamMemberId);
                return new List<MeetingTask>();
            }
        }

        /// <summary>
        /// Gets the count of OneOnOne meetings where a specific task was discussed.
        /// </summary>
        public async Task<int> GetTaskMeetingCountAsync(int taskId)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return 0;
            }

            try
            {
                return await _context.OneOnOneLinkedTasks
                    .Where(link => !link.IsDeleted && link.TaskId == taskId)
                    .Join(_context.OneOnOnes.Where(o => EF.Property<int>(o, "UserId") == currentUserId.Value),
                        link => link.OneOnOneId,
                        meeting => meeting.Id,
                        (link, meeting) => link)
                    .Select(link => link.OneOnOneId)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for task {0}", taskId);
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of OneOnOne meetings where a specific OKR was discussed.
        /// </summary>
        public async Task<int> GetOkrMeetingCountAsync(int okrId)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return 0;
            }

            try
            {
                return await _context.OneOnOneLinkedOkrs
                    .Where(link => !link.IsDeleted && link.OkrId == okrId)
                    .Join(_context.OneOnOnes.Where(o => EF.Property<int>(o, "UserId") == currentUserId.Value),
                        link => link.OneOnOneId,
                        meeting => meeting.Id,
                        (link, meeting) => link)
                    .Select(link => link.OneOnOneId)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for OKR {0}", okrId);
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of OneOnOne meetings where a specific KPI was discussed.
        /// </summary>
        public async Task<int> GetKpiMeetingCountAsync(int kpiId)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return 0;
            }

            try
            {
                return await _context.OneOnOneLinkedKpis
                    .Where(link => !link.IsDeleted && link.KpiId == kpiId)
                    .Join(_context.OneOnOnes.Where(o => EF.Property<int>(o, "UserId") == currentUserId.Value),
                        link => link.OneOnOneId,
                        meeting => meeting.Id,
                        (link, meeting) => link)
                    .Select(link => link.OneOnOneId)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for KPI {0}", kpiId);
                return 0;
            }
        }

        /// <summary>
        /// Links an existing task to a OneOnOne meeting.
        /// </summary>
        public async Task<bool> LinkTaskToMeetingAsync(int oneOnOneId, int taskId, string? discussionNotes = null)
        {
            if (_context == null) return false;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("LinkTaskToMeetingAsync called but CurrentUserId is not set");
                return false;
            }

            try
            {
                // Verify OneOnOne belongs to current user
                var oneOnOne = await _context.OneOnOnes
                    .Where(o => o.Id == oneOnOneId && EF.Property<int>(o, "UserId") == currentUserId.Value)
                    .FirstOrDefaultAsync();
                
                if (oneOnOne == null)
                {
                    _logger.Warn("Cannot link task {0} to meeting {1} - meeting not found or doesn't belong to current user", taskId, oneOnOneId);
                    return false;
                }

                // Check if link already exists
                var existing = await _context.OneOnOneLinkedTasks
                    .FirstOrDefaultAsync(link => link.OneOnOneId == oneOnOneId && link.TaskId == taskId && !link.IsDeleted);

                if (existing != null)
                {
                    // Update existing link
                    existing.DiscussionNotes = discussionNotes ?? string.Empty;
                    _context.OneOnOneLinkedTasks.Update(existing);
                }
                else
                {
                    // Create new link
                    var link = new OneOnOneLinkedTask
                    {
                        OneOnOneId = oneOnOneId,
                        TaskId = taskId,
                        DiscussionNotes = discussionNotes ?? string.Empty
                    };
                    _context.OneOnOneLinkedTasks.Add(link);
                }

                await _context.SaveChangesAsync();
                _logger.Info("Linked task {0} to meeting {1}", taskId, oneOnOneId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error linking task {0} to meeting {1}", taskId, oneOnOneId);
                return false;
            }
        }

        /// <summary>
        /// Links an existing OKR to a OneOnOne meeting.
        /// </summary>
        public async Task<bool> LinkOkrToMeetingAsync(int oneOnOneId, int okrId, string? discussionNotes = null)
        {
            if (_context == null) return false;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("LinkOkrToMeetingAsync called but CurrentUserId is not set");
                return false;
            }

            try
            {
                // Verify OneOnOne belongs to current user
                var oneOnOne = await _context.OneOnOnes
                    .Where(o => o.Id == oneOnOneId && EF.Property<int>(o, "UserId") == currentUserId.Value)
                    .FirstOrDefaultAsync();
                
                if (oneOnOne == null)
                {
                    _logger.Warn("Cannot link OKR {0} to meeting {1} - meeting not found or doesn't belong to current user", okrId, oneOnOneId);
                    return false;
                }

                var existing = await _context.OneOnOneLinkedOkrs
                    .FirstOrDefaultAsync(link => link.OneOnOneId == oneOnOneId && link.OkrId == okrId && !link.IsDeleted);

                if (existing != null)
                {
                    existing.DiscussionNotes = discussionNotes ?? string.Empty;
                    _context.OneOnOneLinkedOkrs.Update(existing);
                }
                else
                {
                    var link = new OneOnOneLinkedOkr
                    {
                        OneOnOneId = oneOnOneId,
                        OkrId = okrId,
                        DiscussionNotes = discussionNotes ?? string.Empty
                    };
                    _context.OneOnOneLinkedOkrs.Add(link);
                }

                await _context.SaveChangesAsync();
                _logger.Info("Linked OKR {0} to meeting {1}", okrId, oneOnOneId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error linking OKR {0} to meeting {1}", okrId, oneOnOneId);
                return false;
            }
        }

        /// <summary>
        /// Links an existing KPI to a OneOnOne meeting.
        /// </summary>
        public async Task<bool> LinkKpiToMeetingAsync(int oneOnOneId, int kpiId, string? discussionNotes = null)
        {
            if (_context == null) return false;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("LinkKpiToMeetingAsync called but CurrentUserId is not set");
                return false;
            }

            try
            {
                // Verify OneOnOne belongs to current user
                var oneOnOne = await _context.OneOnOnes
                    .Where(o => o.Id == oneOnOneId && EF.Property<int>(o, "UserId") == currentUserId.Value)
                    .FirstOrDefaultAsync();
                
                if (oneOnOne == null)
                {
                    _logger.Warn("Cannot link KPI {0} to meeting {1} - meeting not found or doesn't belong to current user", kpiId, oneOnOneId);
                    return false;
                }

                var existing = await _context.OneOnOneLinkedKpis
                    .FirstOrDefaultAsync(link => link.OneOnOneId == oneOnOneId && link.KpiId == kpiId && !link.IsDeleted);

                if (existing != null)
                {
                    existing.DiscussionNotes = discussionNotes ?? string.Empty;
                    _context.OneOnOneLinkedKpis.Update(existing);
                }
                else
                {
                    var link = new OneOnOneLinkedKpi
                    {
                        OneOnOneId = oneOnOneId,
                        KpiId = kpiId,
                        DiscussionNotes = discussionNotes ?? string.Empty
                    };
                    _context.OneOnOneLinkedKpis.Add(link);
                }

                await _context.SaveChangesAsync();
                _logger.Info("Linked KPI {0} to meeting {1}", kpiId, oneOnOneId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error linking KPI {0} to meeting {1}", kpiId, oneOnOneId);
                return false;
            }
        }

        /// <summary>
        /// Unlinks a task from a OneOnOne meeting (soft delete).
        /// </summary>
        public async Task<bool> UnlinkTaskFromMeetingAsync(int oneOnOneId, int taskId)
        {
            if (_context == null) return false;

            try
            {
                var link = await _context.OneOnOneLinkedTasks
                    .FirstOrDefaultAsync(l => l.OneOnOneId == oneOnOneId && l.TaskId == taskId && !l.IsDeleted);

                if (link != null)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.Info("Unlinked task {0} from meeting {1}", taskId, oneOnOneId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error unlinking task {0} from meeting {1}", taskId, oneOnOneId);
                return false;
            }
        }

        /// <summary>
        /// Unlinks an OKR from a OneOnOne meeting (soft delete).
        /// </summary>
        public async Task<bool> UnlinkOkrFromMeetingAsync(int oneOnOneId, int okrId)
        {
            if (_context == null) return false;

            try
            {
                var link = await _context.OneOnOneLinkedOkrs
                    .FirstOrDefaultAsync(l => l.OneOnOneId == oneOnOneId && l.OkrId == okrId && !l.IsDeleted);

                if (link != null)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.Info("Unlinked OKR {0} from meeting {1}", okrId, oneOnOneId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error unlinking OKR {0} from meeting {1}", okrId, oneOnOneId);
                return false;
            }
        }

        /// <summary>
        /// Unlinks a KPI from a OneOnOne meeting (soft delete).
        /// </summary>
        public async Task<bool> UnlinkKpiFromMeetingAsync(int oneOnOneId, int kpiId)
        {
            if (_context == null) return false;

            try
            {
                var link = await _context.OneOnOneLinkedKpis
                    .FirstOrDefaultAsync(l => l.OneOnOneId == oneOnOneId && l.KpiId == kpiId && !l.IsDeleted);

                if (link != null)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.Info("Unlinked KPI {0} from meeting {1}", kpiId, oneOnOneId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error unlinking KPI {0} from meeting {1}", kpiId, oneOnOneId);
                return false;
            }
        }

        #endregion

        #region Project Operations

        public async Task<List<Project>> GetProjectsAsync()
        {
            if (_context == null) return new List<Project>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetProjectsAsync called but CurrentUserId is not set");
                return new List<Project>();
            }

            try
            {
                return await _context.Projects
                    .Where(p => !p.IsDeleted && EF.Property<int>(p, "UserId") == currentUserId.Value)
                    .Include(p => p.Owner)
                    .Include(p => p.TeamMembers.Where(tm => !tm.IsDeleted))
                    .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                    .Include(p => p.Risks.Where(r => !r.IsDeleted))
                    .Include(p => p.Dependencies.Where(d => !d.IsDeleted))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving projects from database");
                return new List<Project>();
            }
        }

        public async Task<int> AddProjectAsync(Project project)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddProjectAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.Projects.Add(project);
                // Set UserId shadow property
                _context.Entry(project).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                _logger.Info("Added project: {0} (ID: {1})", project.Name, project.ID);
                return project.ID;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding project");
                return 0;
            }
        }

        public async Task<bool> UpdateProjectAsync(Project project)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Projects.FindAsync(project.ID);
                if (existing == null)
                {
                    _logger.Error("UpdateProjectAsync: Project ID {0} not found", project.ID);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(project);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating project ID: {0}", project.ID);
                return false;
            }
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            if (_context == null) return false;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("DeleteProjectAsync called but CurrentUserId is not set");
                return false;
            }

            try
            {
                var project = await _context.Projects
                    .Where(p => p.ID == id && EF.Property<int>(p, "UserId") == currentUserId.Value)
                    .FirstOrDefaultAsync();
                if (project != null)
                {
                    _context.Projects.Remove(project);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting project ID: {0}", id);
                return false;
            }
        }

        #endregion

        #region Task Operations

        public async Task<List<IndividualTask>> GetTasksAsync()
        {
            if (_context == null) return new List<IndividualTask>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetTasksAsync called but CurrentUserId is not set");
                return new List<IndividualTask>();
            }

            try
            {
                return await _context.Tasks
                    .Where(t => !t.IsDeleted && EF.Property<int>(t, "UserId") == currentUserId.Value)
                    .Include(t => t.Owner)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving tasks from database");
                return new List<IndividualTask>();
            }
        }

        public async Task<int> AddTaskAsync(IndividualTask task)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddTaskAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.Tasks.Add(task);
                // Set UserId shadow property
                _context.Entry(task).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                return task.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding task");
                return 0;
            }
        }

        public async Task<bool> UpdateTaskAsync(IndividualTask task)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Tasks.FindAsync(task.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateTaskAsync: Task ID {0} not found", task.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(task);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating task");
                return false;
            }
        }

        #endregion

        #region OKR Operations

        public async Task<List<ObjectiveKeyResult>> GetOKRsAsync()
        {
            if (_context == null)
            {
                _logger.Warn("GetOKRsAsync: Context is null");
                return new List<ObjectiveKeyResult>();
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetOKRsAsync called but CurrentUserId is not set");
                return new List<ObjectiveKeyResult>();
            }

            _logger.Info("GetOKRsAsync: Querying OKRs for UserId = {0}", currentUserId.Value);

            try
            {
                // First, let's see all OKRs regardless of UserId for debugging
                var allOkrsCount = await _context.ObjectiveKeyResults.CountAsync();
                _logger.Info("GetOKRsAsync: Total OKRs in database (all users) = {0}", allOkrsCount);
                
                var result = await _context.ObjectiveKeyResults
                    .Where(o => !o.IsDeleted && EF.Property<int>(o, "UserId") == currentUserId.Value)
                    .Include(o => o.Owner)
                    .Include(o => o.KeyResults.Where(k => !k.IsDeleted))
                        .ThenInclude(k => k.Measurables.Where(m => !m.IsDeleted))
                    .OrderBy(o => o.EndDate)
                    .ToListAsync();
                    
                _logger.Info("GetOKRsAsync: Found {0} OKRs for UserId = {1}", result.Count, currentUserId.Value);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving OKRs from database");
                return new List<ObjectiveKeyResult>();
            }
        }

        // Alias for consistency
        public async Task<List<ObjectiveKeyResult>> GetOkrsAsync() => await GetOKRsAsync();

        public async Task<int> AddOKRAsync(ObjectiveKeyResult okr)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddOKRAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.ObjectiveKeyResults.Add(okr);
                // Set UserId shadow property
                _context.Entry(okr).Property("UserId").CurrentValue = currentUserId.Value;
                
                // Also set UserId for nested KPIs
                if (okr.KeyResults != null)
                {
                    foreach (var kpi in okr.KeyResults)
                    {
                        _context.Entry(kpi).Property("UserId").CurrentValue = currentUserId.Value;
                    }
                }
                
                await _context.SaveChangesAsync();
                return okr.ObjectiveId;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding OKR");
                return 0;
            }
        }

        public async Task<bool> UpdateOKRAsync(ObjectiveKeyResult okr)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.ObjectiveKeyResults.FindAsync(okr.ObjectiveId);
                if (existing == null)
                {
                    _logger.Error("UpdateOKRAsync: OKR ID {0} not found", okr.ObjectiveId);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(okr);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating OKR ID: {0}", okr.ObjectiveId);
                return false;
            }
        }

        /// <summary>
        /// Deletes an OKR by ID (soft delete).
        /// </summary>
        public async Task<bool> DeleteOKRAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var okr = await _context.ObjectiveKeyResults.FindAsync(id);
                if (okr != null)
                {
                    _context.ObjectiveKeyResults.Remove(okr);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted OKR ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting OKR ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Adds a Key Result to an OKR.
        /// </summary>
        public async Task<int> AddKeyResultAsync(KeyResult keyResult)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddKeyResultAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.KeyResults.Add(keyResult);
                _context.Entry(keyResult).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                _logger.Info("Added Key Result ID: {0} to OKR ID: {1}", keyResult.Id, keyResult.OkrId);
                return keyResult.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding Key Result");
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing Key Result.
        /// </summary>
        public async Task<bool> UpdateKeyResultAsync(KeyResult keyResult)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.KeyResults.FindAsync(keyResult.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateKeyResultAsync: KeyResult ID {0} not found", keyResult.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(keyResult);
                await _context.SaveChangesAsync();
                _logger.Info("Updated Key Result ID: {0}", keyResult.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating Key Result ID: {0}", keyResult.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a Key Result by ID (soft delete).
        /// </summary>
        public async Task<bool> DeleteKeyResultAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var kr = await _context.KeyResults.FindAsync(id);
                if (kr != null)
                {
                    _context.KeyResults.Remove(kr);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted Key Result ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting Key Result ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a KeyResultMeasurable link by ID.
        /// </summary>
        public async Task<bool> DeleteKeyResultMeasurableAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var measurable = await _context.KeyResultMeasurables.FindAsync(id);
                if (measurable != null)
                {
                    _context.KeyResultMeasurables.Remove(measurable);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted KeyResultMeasurable ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting KeyResultMeasurable ID: {0}", id);
                return false;
            }
        }

        #endregion

        #region TaskCollection Operations

        /// <summary>
        /// Gets all task collections for the current user.
        /// </summary>
        public async Task<List<TaskCollection>> GetTaskCollectionsAsync()
        {
            if (_context == null) return new List<TaskCollection>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetTaskCollectionsAsync called but CurrentUserId is not set");
                return new List<TaskCollection>();
            }

            try
            {
                return await _context.TaskCollections
                    .Where(tc => !tc.IsDeleted && EF.Property<int>(tc, "UserId") == currentUserId.Value)
                    .Include(tc => tc.Items)
                        .ThenInclude(i => i.Task)
                    .OrderBy(tc => tc.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving TaskCollections from database");
                return new List<TaskCollection>();
            }
        }

        #endregion

        #region KPI Operations

        public async Task<List<KeyPerformanceIndicator>> GetKPIsAsync()
        {
            if (_context == null) return new List<KeyPerformanceIndicator>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetKPIsAsync called but CurrentUserId is not set");
                return new List<KeyPerformanceIndicator>();
            }

            try
            {
                return await _context.KeyPerformanceIndicators
                    .Where(k => !k.IsDeleted && EF.Property<int>(k, "UserId") == currentUserId.Value)
                    .Include(k => k.Owner)
                    .OrderBy(k => k.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving KPIs from database");
                return new List<KeyPerformanceIndicator>();
            }
        }

        public async Task<int> AddKPIAsync(KeyPerformanceIndicator kpi)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddKPIAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.KeyPerformanceIndicators.Add(kpi);
                // Set UserId shadow property
                _context.Entry(kpi).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                return kpi.KpiId;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding KPI");
                return 0;
            }
        }

        public async Task<bool> UpdateKPIAsync(KeyPerformanceIndicator kpi)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.KeyPerformanceIndicators.FindAsync(kpi.KpiId);
                if (existing == null)
                {
                    _logger.Error("UpdateKPIAsync: KPI ID {0} not found", kpi.KpiId);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(kpi);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating KPI ID: {0}", kpi.KpiId);
                return false;
            }
        }

        #endregion

        #region Legacy Compatibility Methods

        /// <summary>
        /// Legacy method for backwards compatibility.
        /// </summary>
        public async Task<List<TeamMember>> GetTeamMembers() => await GetTeamMembersAsync();

        /// <summary>
        /// Legacy method - shows connection success notification.
        /// </summary>
        public Task CheckUserAsync()
        {
            if (_isInitialized)
            {
                var dbType = _settings?.Type == DatabaseType.SQLite ? "Local Database" : "SQL Server";
                NotificationManager.Instance.ShowSuccess("Database Ready", $"Connected to {dbType}");
            }
            return Task.CompletedTask;
        }

        #endregion

        #region OneOnOne Related Item Operations

        public async Task<int> AddAgendaItemAsync(AgendaItem agendaItem)
        {
            if (_context == null) return 0;

            try
            {
                _context.AgendaItems.Add(agendaItem);
                await _context.SaveChangesAsync();
                _logger.Info("Added agenda item ID: {0}", agendaItem.Id);
                return agendaItem.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding agenda item");
                return 0;
            }
        }

        public async Task<int> AddMeetingTaskAsync(MeetingTask meetingTask)
        {
            if (_context == null) return 0;

            try
            {
                _context.MeetingTasks.Add(meetingTask);
                await _context.SaveChangesAsync();
                _logger.Info("Added meeting task ID: {0}", meetingTask.Id);
                return meetingTask.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding meeting task");
                return 0;
            }
        }

        #endregion

        #region Feedback Operations

        /// <summary>
        /// Gets all feedback for a specific team member.
        /// </summary>
        public async Task<List<Feedback>> GetFeedbackForTeamMemberAsync(int teamMemberId)
        {
            if (_context == null) return new List<Feedback>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetFeedbackForTeamMemberAsync called but CurrentUserId is not set");
                return new List<Feedback>();
            }

            try
            {
                return await _context.Feedbacks
                    .Where(f => !f.IsDeleted && EF.Property<int>(f, "UserId") == currentUserId.Value && f.TeamMemberId == teamMemberId)
                    .Include(f => f.TeamMember)
                    .OrderByDescending(f => f.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving feedback for team member {0}", teamMemberId);
                return new List<Feedback>();
            }
        }

        /// <summary>
        /// Gets all feedback (for reports/dashboard).
        /// </summary>
        public async Task<List<Feedback>> GetAllFeedbackAsync()
        {
            if (_context == null) return new List<Feedback>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<Feedback>();

            try
            {
                return await _context.Feedbacks
                    .Where(f => !f.IsDeleted && EF.Property<int>(f, "UserId") == currentUserId.Value)
                    .Include(f => f.TeamMember)
                    .OrderByDescending(f => f.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving all feedback");
                return new List<Feedback>();
            }
        }

        /// <summary>
        /// Adds new feedback.
        /// </summary>
        public async Task<int> AddFeedbackAsync(Feedback feedback)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddFeedbackAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.Feedbacks.Add(feedback);
                _context.Entry(feedback).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                _logger.Info("Added feedback ID: {0}", feedback.Id);
                return feedback.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding feedback");
                return 0;
            }
        }

        /// <summary>
        /// Updates existing feedback.
        /// </summary>
        public async Task<bool> UpdateFeedbackAsync(Feedback feedback)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Feedbacks.FindAsync(feedback.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateFeedbackAsync: Feedback ID {0} not found", feedback.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(feedback);
                await _context.SaveChangesAsync();
                _logger.Info("Updated feedback ID: {0}", feedback.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating feedback ID: {0}", feedback.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes feedback (soft delete).
        /// </summary>
        public async Task<bool> DeleteFeedbackAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var feedback = await _context.Feedbacks.FindAsync(id);
                if (feedback != null)
                {
                    _context.Feedbacks.Remove(feedback);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted feedback ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting feedback ID: {0}", id);
                return false;
            }
        }

        #endregion

        #region Individual Goal Operations

        /// <summary>
        /// Gets all goals for a specific team member.
        /// </summary>
        public async Task<List<IndividualGoal>> GetGoalsForTeamMemberAsync(int teamMemberId)
        {
            if (_context == null) return new List<IndividualGoal>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetGoalsForTeamMemberAsync called but CurrentUserId is not set");
                return new List<IndividualGoal>();
            }

            try
            {
                return await _context.IndividualGoals
                    .Where(g => !g.IsDeleted && EF.Property<int>(g, "UserId") == currentUserId.Value && g.TeamMemberId == teamMemberId)
                    .Include(g => g.TeamMember)
                    .Include(g => g.Milestones.Where(m => !m.IsDeleted))
                    .OrderByDescending(g => g.Status == GoalStatus.InProgress)
                    .ThenBy(g => g.TargetDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving goals for team member {0}", teamMemberId);
                return new List<IndividualGoal>();
            }
        }

        /// <summary>
        /// Gets all goals (for reports/dashboard).
        /// </summary>
        public async Task<List<IndividualGoal>> GetAllGoalsAsync()
        {
            if (_context == null) return new List<IndividualGoal>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<IndividualGoal>();

            try
            {
                return await _context.IndividualGoals
                    .Where(g => !g.IsDeleted && EF.Property<int>(g, "UserId") == currentUserId.Value)
                    .Include(g => g.TeamMember)
                    .Include(g => g.Milestones.Where(m => !m.IsDeleted))
                    .OrderByDescending(g => g.Status == GoalStatus.InProgress)
                    .ThenBy(g => g.TargetDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving all goals");
                return new List<IndividualGoal>();
            }
        }

        /// <summary>
        /// Adds a new goal.
        /// </summary>
        public async Task<int> AddGoalAsync(IndividualGoal goal)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddGoalAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.IndividualGoals.Add(goal);
                _context.Entry(goal).Property("UserId").CurrentValue = currentUserId.Value;
                
                // Set UserId for milestones too
                foreach (var milestone in goal.Milestones)
                {
                    _context.Entry(milestone).Property("UserId").CurrentValue = currentUserId.Value;
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Added goal ID: {0}", goal.Id);
                return goal.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding goal");
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing goal.
        /// </summary>
        public async Task<bool> UpdateGoalAsync(IndividualGoal goal)
        {
            if (_context == null) return false;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return false;

            try
            {
                var existing = await _context.IndividualGoals.FindAsync(goal.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateGoalAsync: Goal ID {0} not found", goal.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(goal);
                
                // Handle milestones - set UserId for new ones
                foreach (var milestone in goal.Milestones.Where(m => m.Id == 0))
                {
                    _context.Entry(milestone).Property("UserId").CurrentValue = currentUserId.Value;
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Updated goal ID: {0}", goal.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating goal ID: {0}", goal.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a goal (soft delete).
        /// </summary>
        public async Task<bool> DeleteGoalAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var goal = await _context.IndividualGoals.FindAsync(id);
                if (goal != null)
                {
                    _context.IndividualGoals.Remove(goal);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted goal ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting goal ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Updates a goal's progress percentage.
        /// </summary>
        public async Task<bool> UpdateGoalProgressAsync(int goalId, int progressPercent)
        {
            if (_context == null) return false;

            try
            {
                var goal = await _context.IndividualGoals.FindAsync(goalId);
                if (goal != null)
                {
                    goal.ProgressPercent = Math.Clamp(progressPercent, 0, 100);
                    if (goal.ProgressPercent == 100 && goal.Status != GoalStatus.Completed)
                    {
                        goal.Status = GoalStatus.Completed;
                    }
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating goal progress");
                return false;
            }
        }

        /// <summary>
        /// Toggles a milestone's completion status.
        /// </summary>
        public async Task<bool> ToggleMilestoneAsync(int milestoneId)
        {
            if (_context == null) return false;

            try
            {
                var milestone = await _context.GoalMilestones.FindAsync(milestoneId);
                if (milestone != null)
                {
                    milestone.IsCompleted = !milestone.IsCompleted;
                    milestone.CompletedDate = milestone.IsCompleted ? DateTime.Now : null;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error toggling milestone");
                return false;
            }
        }

        #endregion

        #region Reminder Operations

        /// <summary>
        /// Gets all pending reminders that are due (for the reminder service).
        /// </summary>
        public async Task<List<Reminder>> GetDueRemindersAsync()
        {
            if (_context == null) return new List<Reminder>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<Reminder>();

            try
            {
                var now = DateTime.Now;
                return await _context.Reminders
                    .Where(r => !r.IsDeleted && 
                                EF.Property<int>(r, "UserId") == currentUserId.Value &&
                                r.Status == ReminderStatus.Pending &&
                                r.DueDateTime <= now &&
                                (r.SnoozedUntil == null || r.SnoozedUntil <= now))
                    .OrderBy(r => r.DueDateTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving due reminders");
                return new List<Reminder>();
            }
        }

        /// <summary>
        /// Gets all reminders for the current user.
        /// </summary>
        public async Task<List<Reminder>> GetAllRemindersAsync()
        {
            if (_context == null) return new List<Reminder>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<Reminder>();

            try
            {
                return await _context.Reminders
                    .Where(r => !r.IsDeleted && EF.Property<int>(r, "UserId") == currentUserId.Value)
                    .OrderBy(r => r.DueDateTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving all reminders");
                return new List<Reminder>();
            }
        }

        /// <summary>
        /// Gets pending reminders for display.
        /// </summary>
        public async Task<List<Reminder>> GetPendingRemindersAsync()
        {
            if (_context == null) return new List<Reminder>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<Reminder>();

            try
            {
                return await _context.Reminders
                    .Where(r => !r.IsDeleted && 
                                EF.Property<int>(r, "UserId") == currentUserId.Value &&
                                (r.Status == ReminderStatus.Pending || r.Status == ReminderStatus.Snoozed))
                    .OrderBy(r => r.DueDateTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving pending reminders");
                return new List<Reminder>();
            }
        }

        /// <summary>
        /// Adds a new reminder.
        /// </summary>
        public async Task<int> AddReminderAsync(Reminder reminder)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddReminderAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.Reminders.Add(reminder);
                _context.Entry(reminder).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                _logger.Info("Added reminder ID: {0}", reminder.Id);
                return reminder.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding reminder");
                return 0;
            }
        }

        /// <summary>
        /// Updates a reminder (e.g., after snooze or dismiss).
        /// </summary>
        public async Task<bool> UpdateReminderAsync(Reminder reminder)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Reminders.FindAsync(reminder.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateReminderAsync: Reminder ID {0} not found", reminder.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(reminder);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating reminder ID: {0}", reminder.Id);
                return false;
            }
        }

        /// <summary>
        /// Marks a reminder as triggered.
        /// </summary>
        public async Task<bool> MarkReminderTriggeredAsync(int reminderId)
        {
            if (_context == null) return false;

            try
            {
                var reminder = await _context.Reminders.FindAsync(reminderId);
                if (reminder != null)
                {
                    reminder.Status = ReminderStatus.Triggered;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error marking reminder triggered");
                return false;
            }
        }

        /// <summary>
        /// Snoozes a reminder.
        /// </summary>
        public async Task<bool> SnoozeReminderAsync(int reminderId, int snoozeMinutes)
        {
            if (_context == null) return false;

            try
            {
                var reminder = await _context.Reminders.FindAsync(reminderId);
                if (reminder != null)
                {
                    reminder.Status = ReminderStatus.Snoozed;
                    reminder.SnoozedUntil = DateTime.Now.AddMinutes(snoozeMinutes);
                    await _context.SaveChangesAsync();
                    _logger.Info("Snoozed reminder ID: {0} for {1} minutes", reminderId, snoozeMinutes);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error snoozing reminder");
                return false;
            }
        }

        /// <summary>
        /// Dismisses a reminder.
        /// </summary>
        public async Task<bool> DismissReminderAsync(int reminderId)
        {
            if (_context == null) return false;

            try
            {
                var reminder = await _context.Reminders.FindAsync(reminderId);
                if (reminder != null)
                {
                    reminder.Status = ReminderStatus.Dismissed;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error dismissing reminder");
                return false;
            }
        }

        /// <summary>
        /// Deletes a reminder.
        /// </summary>
        public async Task<bool> DeleteReminderAsync(int reminderId)
        {
            if (_context == null) return false;

            try
            {
                var reminder = await _context.Reminders.FindAsync(reminderId);
                if (reminder != null)
                {
                    _context.Reminders.Remove(reminder);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting reminder");
                return false;
            }
        }

        /// <summary>
        /// Gets team members who haven't had a 1:1 in the specified number of weeks.
        /// </summary>
        public async Task<List<TeamMember>> GetTeamMembersWithoutRecentOneOnOneAsync(int weeks)
        {
            if (_context == null) return new List<TeamMember>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<TeamMember>();

            try
            {
                var cutoffDate = DateTime.Now.AddDays(-weeks * 7);
                
                // Get all team members
                var teamMembers = await _context.TeamMembers
                    .Where(t => !t.IsDeleted && EF.Property<int>(t, "UserId") == currentUserId.Value)
                    .ToListAsync();

                // Get IDs of team members with recent 1:1s
                var recentOneOnOneTeamMemberIds = await _context.OneOnOnes
                    .Include(o => o.TeamMember)
                    .Where(o => !o.IsDeleted && 
                                EF.Property<int>(o, "UserId") == currentUserId.Value &&
                                o.Date >= cutoffDate)
                    .Select(o => o.TeamMember.Id)
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

        #endregion

        #region Meeting Template Operations

        /// <summary>
        /// Gets all meeting templates for the current user.
        /// </summary>
        public async Task<List<MeetingTemplate>> GetMeetingTemplatesAsync()
        {
            if (_context == null) return new List<MeetingTemplate>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<MeetingTemplate>();

            try
            {
                return await _context.MeetingTemplates
                    .Where(t => !t.IsDeleted && EF.Property<int>(t, "UserId") == currentUserId.Value)
                    .Include(t => t.Items.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder))
                    .OrderBy(t => t.SortOrder)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meeting templates");
                return new List<MeetingTemplate>();
            }
        }

        /// <summary>
        /// Gets a specific meeting template by ID.
        /// </summary>
        public async Task<MeetingTemplate?> GetMeetingTemplateByIdAsync(int id)
        {
            if (_context == null) return null;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return null;

            try
            {
                return await _context.MeetingTemplates
                    .Where(t => !t.IsDeleted && 
                                t.Id == id && 
                                EF.Property<int>(t, "UserId") == currentUserId.Value)
                    .Include(t => t.Items.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder))
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meeting template ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new meeting template.
        /// </summary>
        public async Task<int> AddMeetingTemplateAsync(MeetingTemplate template)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddMeetingTemplateAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.MeetingTemplates.Add(template);
                _context.Entry(template).Property("UserId").CurrentValue = currentUserId.Value;

                // Set UserId for items
                foreach (var item in template.Items)
                {
                    _context.Entry(item).Property("UserId").CurrentValue = currentUserId.Value;
                }

                await _context.SaveChangesAsync();
                _logger.Info("Added meeting template ID: {0}", template.Id);
                return template.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding meeting template");
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing meeting template.
        /// </summary>
        public async Task<bool> UpdateMeetingTemplateAsync(MeetingTemplate template)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.MeetingTemplates.FindAsync(template.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateMeetingTemplateAsync: Template ID {0} not found", template.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(template);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating meeting template ID: {0}", template.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a meeting template.
        /// </summary>
        public async Task<bool> DeleteMeetingTemplateAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var template = await _context.MeetingTemplates.FindAsync(id);
                if (template != null)
                {
                    _context.MeetingTemplates.Remove(template);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting meeting template ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Creates default system templates for a new user.
        /// </summary>
        public async Task CreateDefaultTemplatesAsync()
        {
            if (_context == null) return;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return;

            // Check if user already has templates
            var existingCount = await _context.MeetingTemplates
                .Where(t => !t.IsDeleted && EF.Property<int>(t, "UserId") == currentUserId.Value)
                .CountAsync();

            if (existingCount > 0) return;

            // Create default templates
            var templates = new List<MeetingTemplate>
            {
                new MeetingTemplate
                {
                    Name = "Weekly Check-in",
                    Description = "Regular weekly 1:1 to review progress and blockers",
                    SuggestedDurationMinutes = 30,
                    IsSystemTemplate = true,
                    SortOrder = 1,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "How are you feeling this week?", Category = AgendaItemCategory.Topic, Priority = Severity.Medium, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Progress on current tasks", Category = AgendaItemCategory.Update, Priority = Severity.High, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Any blockers or challenges?", Category = AgendaItemCategory.Blocker, Priority = Severity.High, SortOrder = 3 },
                        new MeetingTemplateItem { Description = "Priorities for next week", Category = AgendaItemCategory.Topic, Priority = Severity.Medium, SortOrder = 4 },
                        new MeetingTemplateItem { Description = "Any support needed from me?", Category = AgendaItemCategory.Question, Priority = Severity.Medium, SortOrder = 5 }
                    }
                },
                new MeetingTemplate
                {
                    Name = "Career Development",
                    Description = "Focus on career growth, skills, and long-term goals",
                    SuggestedDurationMinutes = 45,
                    IsSystemTemplate = true,
                    SortOrder = 2,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "Review progress on career goals", Category = AgendaItemCategory.CareerDevelopment, Priority = Severity.High, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Skills development opportunities", Category = AgendaItemCategory.CareerDevelopment, Priority = Severity.Medium, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Feedback on recent performance", Category = AgendaItemCategory.Feedback, Priority = Severity.High, SortOrder = 3 },
                        new MeetingTemplateItem { Description = "Upcoming growth opportunities", Category = AgendaItemCategory.CareerDevelopment, Priority = Severity.Medium, SortOrder = 4 },
                        new MeetingTemplateItem { Description = "Update goal milestones", Category = AgendaItemCategory.Decision, Priority = Severity.Medium, SortOrder = 5 }
                    }
                },
                new MeetingTemplate
                {
                    Name = "Performance Review",
                    Description = "Formal performance discussion (quarterly/annual)",
                    SuggestedDurationMinutes = 60,
                    IsSystemTemplate = true,
                    SortOrder = 3,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "Review accomplishments since last review", Category = AgendaItemCategory.Performance, Priority = Severity.High, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Discuss areas of strength", Category = AgendaItemCategory.Feedback, Priority = Severity.High, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Discuss areas for improvement", Category = AgendaItemCategory.Feedback, Priority = Severity.High, SortOrder = 3 },
                        new MeetingTemplateItem { Description = "Goal attainment review", Category = AgendaItemCategory.Performance, Priority = Severity.High, SortOrder = 4 },
                        new MeetingTemplateItem { Description = "Set goals for next period", Category = AgendaItemCategory.Decision, Priority = Severity.High, SortOrder = 5 },
                        new MeetingTemplateItem { Description = "Compensation/promotion discussion", Category = AgendaItemCategory.CareerDevelopment, Priority = Severity.Medium, SortOrder = 6 }
                    }
                },
                new MeetingTemplate
                {
                    Name = "Project Kickoff",
                    Description = "Initial discussion about a new project assignment",
                    SuggestedDurationMinutes = 45,
                    IsSystemTemplate = true,
                    SortOrder = 4,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "Project overview and context", Category = AgendaItemCategory.Topic, Priority = Severity.High, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Role and responsibilities", Category = AgendaItemCategory.Decision, Priority = Severity.High, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Key stakeholders", Category = AgendaItemCategory.Topic, Priority = Severity.Medium, SortOrder = 3 },
                        new MeetingTemplateItem { Description = "Timeline and milestones", Category = AgendaItemCategory.Update, Priority = Severity.High, SortOrder = 4 },
                        new MeetingTemplateItem { Description = "Resources and support needed", Category = AgendaItemCategory.Question, Priority = Severity.Medium, SortOrder = 5 }
                    }
                },
                new MeetingTemplate
                {
                    Name = "Quick Sync",
                    Description = "Brief 15-minute catch-up",
                    SuggestedDurationMinutes = 15,
                    IsSystemTemplate = true,
                    SortOrder = 5,
                    Items = new List<MeetingTemplateItem>
                    {
                        new MeetingTemplateItem { Description = "Quick status update", Category = AgendaItemCategory.Update, Priority = Severity.High, SortOrder = 1 },
                        new MeetingTemplateItem { Description = "Urgent items", Category = AgendaItemCategory.Blocker, Priority = Severity.High, SortOrder = 2 },
                        new MeetingTemplateItem { Description = "Next steps", Category = AgendaItemCategory.Decision, Priority = Severity.Medium, SortOrder = 3 }
                    }
                }
            };

            foreach (var template in templates)
            {
                _context.MeetingTemplates.Add(template);
                _context.Entry(template).Property("UserId").CurrentValue = currentUserId.Value;

                foreach (var item in template.Items)
                {
                    _context.Entry(item).Property("UserId").CurrentValue = currentUserId.Value;
                }
            }

            await _context.SaveChangesAsync();
            _logger.Info("Created {0} default meeting templates", templates.Count);
        }

        #endregion

        #region Quick Note Operations

        /// <summary>
        /// Gets all quick notes for the current user.
        /// </summary>
        public async Task<List<QuickNote>> GetQuickNotesAsync(bool includeArchived = false)
        {
            if (_context == null) return new List<QuickNote>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<QuickNote>();

            try
            {
                var baseQuery = _context.QuickNotes
                    .Where(n => !n.IsDeleted && EF.Property<int>(n, "UserId") == currentUserId.Value);

                if (!includeArchived)
                {
                    baseQuery = baseQuery.Where(n => !n.IsArchived);
                }

                return await baseQuery
                    .Include(n => n.TeamMember)
                    .OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving quick notes");
                return new List<QuickNote>();
            }
        }

        /// <summary>
        /// Gets quick notes for a specific team member.
        /// </summary>
        public async Task<List<QuickNote>> GetQuickNotesForTeamMemberAsync(int teamMemberId)
        {
            if (_context == null) return new List<QuickNote>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<QuickNote>();

            try
            {
                return await _context.QuickNotes
                    .Where(n => !n.IsDeleted && 
                                !n.IsArchived &&
                                EF.Property<int>(n, "UserId") == currentUserId.Value &&
                                n.TeamMemberId == teamMemberId)
                    .OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving quick notes for team member");
                return new List<QuickNote>();
            }
        }

        /// <summary>
        /// Adds a new quick note.
        /// </summary>
        public async Task<int> AddQuickNoteAsync(QuickNote note)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddQuickNoteAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.QuickNotes.Add(note);
                _context.Entry(note).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                _logger.Info("Added quick note ID: {0}", note.Id);
                return note.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding quick note");
                return 0;
            }
        }

        /// <summary>
        /// Updates a quick note.
        /// </summary>
        public async Task<bool> UpdateQuickNoteAsync(QuickNote note)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.QuickNotes.FindAsync(note.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateQuickNoteAsync: QuickNote ID {0} not found", note.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(note);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating quick note ID: {0}", note.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a quick note.
        /// </summary>
        public async Task<bool> DeleteQuickNoteAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var note = await _context.QuickNotes.FindAsync(id);
                if (note != null)
                {
                    _context.QuickNotes.Remove(note);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting quick note ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Toggles the pinned status of a note.
        /// </summary>
        public async Task<bool> ToggleNotePinnedAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var note = await _context.QuickNotes.FindAsync(id);
                if (note != null)
                {
                    note.IsPinned = !note.IsPinned;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error toggling note pinned status");
                return false;
            }
        }

        /// <summary>
        /// Archives a note.
        /// </summary>
        public async Task<bool> ArchiveNoteAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var note = await _context.QuickNotes.FindAsync(id);
                if (note != null)
                {
                    note.IsArchived = true;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error archiving note");
                return false;
            }
        }

        #endregion
    }
}
