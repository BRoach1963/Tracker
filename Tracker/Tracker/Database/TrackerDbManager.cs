using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
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

        private static TrackerDbManager? _instance;
        private static readonly object SyncRoot = new();
        private bool _isInitialized;
        private TrackerDbContext? _context;
        private DatabaseSettings? _settings;

        private readonly LoggingManager.Logger _logger = new(nameof(TrackerDbManager), "DatabaseLog");

        #endregion

        #region Singleton Instance

        public static TrackerDbManager? Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        _instance ??= new TrackerDbManager();
                    }
                }
                return _instance;
            }
        }

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
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            InitializeAsync(new DatabaseSettings { Type = DatabaseType.SQLite }, true).Wait();
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

                await DatabaseSeeder.SeedSampleDataAsync(_context, forceReseed);
                _logger.Info("Sample data seeded to database (forceReseed={0})", forceReseed);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to seed sample data");
                return false;
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

        #region TeamMember Operations

        public async Task<List<TeamMember>> GetTeamMembersAsync()
        {
            if (_context == null) return new List<TeamMember>();

            try
            {
                return await _context.TeamMembers
                    .Where(t => !t.IsDeleted)
                    .OrderBy(tm => tm.Role)
                    .ThenBy(tm => tm.LastName)
                    .ThenBy(tm => tm.FirstName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving team members from database");
                return new List<TeamMember>();
            }
        }

        public async Task<TeamMember?> GetTeamMemberByIdAsync(int id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.TeamMembers
                    .Where(t => !t.IsDeleted)
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
                return 0;
            }
        }

        public async Task<bool> UpdateTeamMemberAsync(TeamMember teamMember)
        {
            if (_context == null) return false;

            try
            {
                _context.TeamMembers.Update(teamMember);
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

            try
            {
                var teamMember = await _context.TeamMembers.FindAsync(id);
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

            try
            {
                var query = _context.OneOnOnes
                    .Where(o => !o.IsDeleted)
                    .Include(o => o.TeamMember)
                    .Include(o => o.ActionItems.Where(a => !a.IsDeleted))
                    .Include(o => o.DiscussionPoints.Where(d => !d.IsDeleted))
                    .Include(o => o.Concerns.Where(c => !c.IsDeleted))
                    .Include(o => o.FollowUpItems.Where(f => !f.IsDeleted))
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

            try
            {
                return await _context.OneOnOnes
                    .Where(o => !o.IsDeleted)
                    .Include(o => o.TeamMember)
                    .Include(o => o.ActionItems.Where(a => !a.IsDeleted))
                    .Include(o => o.DiscussionPoints.Where(d => !d.IsDeleted))
                    .Include(o => o.Concerns.Where(c => !c.IsDeleted))
                    .Include(o => o.FollowUpItems.Where(f => !f.IsDeleted))
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

            try
            {
                _context.OneOnOnes.Add(oneOnOne);
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
                _context.OneOnOnes.Update(oneOnOne);
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

            try
            {
                var query = _context.OneOnOnes
                    .Where(o => !o.IsDeleted && o.TeamMember.Id == teamMemberId);

                if (excludeOneOnOneId.HasValue)
                {
                    query = query.Where(o => o.Id != excludeOneOnOneId.Value);
                }

                return await query
                    .Include(o => o.TeamMember)
                    .Include(o => o.ActionItems.Where(a => !a.IsDeleted))
                    .Include(o => o.FollowUpItems.Where(f => !f.IsDeleted))
                    .Include(o => o.DiscussionPoints.Where(d => !d.IsDeleted))
                    .Include(o => o.Concerns.Where(c => !c.IsDeleted))
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
        /// Gets all uncompleted ActionItems and FollowUpItems for a specific team member from previous meetings.
        /// Used to rollover unfinished items into the next meeting.
        /// </summary>
        public async Task<(List<ActionItem> ActionItems, List<FollowUpItem> FollowUpItems)> GetUncompletedItemsAsync(int teamMemberId)
        {
            if (_context == null) return (new List<ActionItem>(), new List<FollowUpItem>());

            try
            {
                var actionItems = await _context.ActionItems
                    .Where(a => !a.IsDeleted && !a.IsCompleted && a.Owner.Id == teamMemberId)
                    .Include(a => a.Owner)
                    .OrderByDescending(a => a.DueDate)
                    .ToListAsync();

                var followUpItems = await _context.FollowUpItems
                    .Where(f => !f.IsDeleted && !f.IsCompleted && f.Owner.Id == teamMemberId)
                    .Include(f => f.Owner)
                    .OrderByDescending(f => f.DueDate)
                    .ToListAsync();

                return (actionItems, followUpItems);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving uncompleted items for team member {0}", teamMemberId);
                return (new List<ActionItem>(), new List<FollowUpItem>());
            }
        }

        /// <summary>
        /// Gets the count of OneOnOne meetings where a specific task was discussed.
        /// </summary>
        public async Task<int> GetTaskMeetingCountAsync(int taskId)
        {
            if (_context == null) return 0;

            try
            {
                return await _context.OneOnOneLinkedTasks
                    .Where(link => !link.IsDeleted && link.TaskId == taskId)
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

            try
            {
                return await _context.OneOnOneLinkedOkrs
                    .Where(link => !link.IsDeleted && link.OkrId == okrId)
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

            try
            {
                return await _context.OneOnOneLinkedKpis
                    .Where(link => !link.IsDeleted && link.KpiId == kpiId)
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

            try
            {
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

            try
            {
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

            try
            {
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

            try
            {
                return await _context.Projects
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.Owner)
                    .Include(p => p.TeamMembers.Where(tm => !tm.IsDeleted))
                    .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                    .Include(p => p.Risks.Where(r => !r.IsDeleted))
                    .Include(p => p.Dependencies.Where(d => !d.IsDeleted))
                    .Include(p => p.OKRs.Where(o => !o.IsDeleted))
                        .ThenInclude(o => o.KeyResults.Where(k => !k.IsDeleted))
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

            try
            {
                _context.Projects.Add(project);
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
                _context.Projects.Update(project);
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

            try
            {
                var project = await _context.Projects.FindAsync(id);
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

            try
            {
                return await _context.Tasks
                    .Where(t => !t.IsDeleted)
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

            try
            {
                _context.Tasks.Add(task);
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
                _context.Tasks.Update(task);
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
            if (_context == null) return new List<ObjectiveKeyResult>();

            try
            {
                return await _context.ObjectiveKeyResults
                    .Where(o => !o.IsDeleted)
                    .Include(o => o.Owner)
                    .Include(o => o.KeyResults.Where(k => !k.IsDeleted))
                        .ThenInclude(k => k.Owner)
                    .OrderBy(o => o.EndDate)
                    .ToListAsync();
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

            try
            {
                _context.ObjectiveKeyResults.Add(okr);
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
                _context.ObjectiveKeyResults.Update(okr);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating OKR ID: {0}", okr.ObjectiveId);
                return false;
            }
        }

        #endregion

        #region KPI Operations

        public async Task<List<KeyPerformanceIndicator>> GetKPIsAsync()
        {
            if (_context == null) return new List<KeyPerformanceIndicator>();

            try
            {
                return await _context.KeyPerformanceIndicators
                    .Where(k => !k.IsDeleted)
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

            try
            {
                _context.KeyPerformanceIndicators.Add(kpi);
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
                _context.KeyPerformanceIndicators.Update(kpi);
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

        public async Task<int> AddDiscussionPointAsync(DiscussionPoint discussionPoint)
        {
            if (_context == null) return 0;

            try
            {
                _context.DiscussionPoints.Add(discussionPoint);
                await _context.SaveChangesAsync();
                _logger.Info("Added discussion point ID: {0}", discussionPoint.Id);
                return discussionPoint.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding discussion point");
                return 0;
            }
        }

        public async Task<int> AddConcernAsync(Concern concern)
        {
            if (_context == null) return 0;

            try
            {
                _context.Concerns.Add(concern);
                await _context.SaveChangesAsync();
                _logger.Info("Added concern ID: {0}", concern.Id);
                return concern.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding concern");
                return 0;
            }
        }

        public async Task<int> AddActionItemAsync(ActionItem actionItem)
        {
            if (_context == null) return 0;

            try
            {
                _context.ActionItems.Add(actionItem);
                await _context.SaveChangesAsync();
                _logger.Info("Added action item ID: {0}", actionItem.Id);
                return actionItem.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding action item");
                return 0;
            }
        }

        public async Task<int> AddFollowUpItemAsync(FollowUpItem followUpItem)
        {
            if (_context == null) return 0;

            try
            {
                _context.FollowUpItems.Add(followUpItem);
                await _context.SaveChangesAsync();
                _logger.Info("Added follow-up item ID: {0}", followUpItem.Id);
                return followUpItem.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding follow-up item");
                return 0;
            }
        }

        #endregion
    }
}
