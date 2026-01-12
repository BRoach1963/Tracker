// THIS FILE HAS BEEN INTENTIONALLY DISABLED.
// TrackerDbManager has been removed in favor of:
// - TrackerDbContextFactory (short-lived DbContexts)
// - Repository classes (MeetingRepository, GoalRepository, etc.)
//
// The entire legacy implementation is now wrapped in #if false so it
// does not compile. Any remaining references to TrackerDbManager will
// therefore fail to compile and must be migrated.

#if false

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
        /// Gets the database context for direct queries (use sparingly).
        /// </summary>
        /// <returns>The TrackerDbContext or null if not initialized.</returns>
        public TrackerDbContext? GetDbContext() => _context;

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

                // Note: Seed data is now managed via Supabase SQL scripts, not in-app seeding

                _isInitialized = true;
                
                // Determine actual SQLite path for logging
                var actualPath = settings.Type == DatabaseType.SQLite 
                    ? (!string.IsNullOrWhiteSpace(settings.CustomSqlitePath) 
                        ? settings.CustomSqlitePath 
                        : DatabaseSettings.GetSqlitePath())
                    : settings.Server;
                    
                _logger.Info("Database initialized: Type={0}, Path={1}", 
                    settings.Type, actualPath);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize database");
                throw;
            }
        }

        /// <summary>
        /// Clears all data from the database.
        /// Note: This is now a no-op. Data is managed via Supabase.
        /// </summary>
        public async Task<bool> ClearAllDataAsync()
        {
            _logger.Warn("ClearAllDataAsync called but is now a no-op - data is managed via Supabase");
            await Task.CompletedTask;
            return false;
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
        /// Note: This is now a no-op. Data is managed via Supabase SQL scripts.
        /// </summary>
        /// <param name="forceReseed">Ignored - seeding is now done via Supabase</param>
        public async Task<bool> SeedSampleDataAsync(bool forceReseed = false)
        {
            _logger.Warn("SeedSampleDataAsync called but is now a no-op - data is managed via Supabase SQL scripts");
            await Task.CompletedTask;
            return false;
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
        /// Gets or creates a User in the database based on the Supabase user ID.
        /// Used for Supabase authentication where we need to link local users to Supabase UUIDs.
        /// </summary>
        /// <param name="supabaseUserId">The Supabase user's UUID.</param>
        /// <param name="email">The user's email address.</param>
        /// <param name="displayName">The user's display name.</param>
        /// <returns>The User entity, or null if database is not initialized.</returns>
        public async Task<User?> GetOrCreateUserAsync(Guid supabaseUserId, string email, string displayName)
        {
            if (_context == null) return null;

            try
            {
                System.Diagnostics.Debug.WriteLine($"=== GetOrCreateUserAsync START: {email}, SupabaseId: {supabaseUserId} ===");
                
                // First, check if a user exists with this Supabase UUID (using projection to avoid DateTime issues)
                var existingUserInfo = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.SupabaseUserId == supabaseUserId)
                    .Select(u => new { u.Id, u.Email })
                    .FirstOrDefaultAsync();
                
                if (existingUserInfo != null)
                {
                    UserSettingsManager.Instance.CurrentUserId = existingUserInfo.Id;
                    
                    // Update context's CurrentUserId for EF query filters
                    _context.CurrentUserId = existingUserInfo.Id;
                    
                    // Store for context factory
                    _supabaseUserId = supabaseUserId;
                    _localUserId = existingUserInfo.Id;
                    
                    System.Diagnostics.Debug.WriteLine($"=== GetOrCreateUserAsync: Found existing user by SupabaseId, Id={existingUserInfo.Id} ===");
                    
                    // Return a minimal User object (don't reload full entity to avoid DateTime issues)
                    return new User 
                    { 
                        Id = existingUserInfo.Id, 
                        Email = existingUserInfo.Email,
                        SupabaseUserId = supabaseUserId,
                        DisplayName = displayName
                    };
                }

                // Check if a user exists with this email (migration scenario)
                var existingByEmailInfo = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.Email == email)
                    .Select(u => new { u.Id, u.Email, u.SupabaseUserId })
                    .FirstOrDefaultAsync();
                
                if (existingByEmailInfo != null)
                {
                    // Update existing user with Supabase UUID using raw SQL to avoid loading full entity
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE \"users\" SET \"supabaseuserid\" = {0}, \"displayname\" = {1} WHERE \"id\" = {2}",
                        supabaseUserId, displayName, existingByEmailInfo.Id);
                    
                    UserSettingsManager.Instance.CurrentUserId = existingByEmailInfo.Id;
                    _context.CurrentUserId = existingByEmailInfo.Id;
                    _supabaseUserId = supabaseUserId;
                    _localUserId = existingByEmailInfo.Id;
                    
                    System.Diagnostics.Debug.WriteLine($"=== GetOrCreateUserAsync: Updated existing user by email, Id={existingByEmailInfo.Id} ===");
                    _logger.Info("Updated existing User with Supabase ID: {0} (Local Id: {1})", displayName, existingByEmailInfo.Id);
                    
                    return new User 
                    { 
                        Id = existingByEmailInfo.Id, 
                        Email = email,
                        SupabaseUserId = supabaseUserId,
                        DisplayName = displayName
                    };
                }

                System.Diagnostics.Debug.WriteLine($"=== GetOrCreateUserAsync: Creating new user ===");
                
                // Create new User
                var newUser = new User
                {
                    SupabaseUserId = supabaseUserId,
                    Username = displayName,
                    Email = email,
                    DisplayName = displayName,
                    IsActive = true
                };
                
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                
                // Get the ID using projection instead of full entity reload
                var createdUserId = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.SupabaseUserId == supabaseUserId)
                    .Select(u => u.Id)
                    .FirstAsync();
                    
                UserSettingsManager.Instance.CurrentUserId = createdUserId;
                _context.CurrentUserId = createdUserId;
                _supabaseUserId = supabaseUserId;
                _localUserId = createdUserId;
                
                // Update the newUser with the assigned Id
                newUser.Id = createdUserId;
                
                System.Diagnostics.Debug.WriteLine($"=== GetOrCreateUserAsync: Created new user, Id={createdUserId} ===");
                _logger.Info("Created new User: {0} (Id: {1}, Supabase: {2})", displayName, createdUserId, supabaseUserId);
                return newUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetOrCreateUserAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                _logger.Exception(ex, "Failed to get or create User with Supabase ID: {0}", supabaseUserId);
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
                    var sqlitePath = !string.IsNullOrWhiteSpace(settings.CustomSqlitePath) 
                        ? settings.CustomSqlitePath 
                        : DatabaseSettings.GetSqlitePath();
                    result.DatabaseExists = File.Exists(sqlitePath);
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

        #region PostgreSQL Context Factory

        /// <summary>
        /// Sets up the PostgreSQL user context for the context factory pattern.
        /// Must be called after Supabase authentication to enable parallel queries.
        /// </summary>
        /// <param name="supabaseUserId">The authenticated Supabase user's UUID.</param>
        public async Task SetPostgresUserAsync(Guid supabaseUserId)
        {
            _supabaseUserId = supabaseUserId;
            _logger.Info("PostgreSQL user set: {0}", supabaseUserId);

            // Look up the local User.Id from the Supabase UUID
            if (_context != null && _settings?.Type == DatabaseType.PostgreSQL)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"=== SetPostgresUserAsync: Looking up local user ID for Supabase: {supabaseUserId} ===");
                    
                    // Use projection query to avoid loading DateTime columns that might cause InvalidCastException
                    // This is a workaround for PostgreSQL timestamp handling issues with EF Core
                    var userInfo = await _context.Users
                        .IgnoreQueryFilters()
                        .Where(u => u.SupabaseUserId == supabaseUserId)
                        .Select(u => new { u.Id, u.Email })
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

                    if (userInfo != null)
                    {
                        _localUserId = userInfo.Id;
                        
                        // Update UserSettingsManager for compatibility with existing code
                        UserSettingsManager.Instance.CurrentUserId = userInfo.Id;
                        
                        // Update the existing context's CurrentUserId for EF query filters
                        _context.CurrentUserId = userInfo.Id;
                        
                        System.Diagnostics.Debug.WriteLine($"=== SetPostgresUserAsync: Resolved local user ID = {userInfo.Id} for {userInfo.Email} ===");
                        _logger.Info("PostgreSQL local user ID resolved: {0} (Supabase: {1})", userInfo.Id, supabaseUserId);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"=== SetPostgresUserAsync: No user found for Supabase ID: {supabaseUserId} ===");
                        _logger.Warn("No user found for Supabase ID: {0}", supabaseUserId);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"=== SetPostgresUserAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                    _logger.Exception(ex, "Failed to look up local user ID for Supabase user {0}", supabaseUserId);
                }
            }
        }

        /// <summary>
        /// Creates a new DbContext for PostgreSQL operations.
        /// Each operation gets its own context to enable parallel queries.
        /// For SQLite/SQL Server, returns the singleton context.
        /// </summary>
        /// <returns>A DbContext instance - caller should dispose for PostgreSQL contexts.</returns>
        private TrackerDbContext CreateContext()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");
            }

            // For PostgreSQL, create a fresh context for each operation
            if (_settings.Type == DatabaseType.PostgreSQL && _supabaseUserId.HasValue)
            {
                return new TrackerDbContext(_settings, _supabaseUserId.Value, _localUserId);
            }

            // For SQLite/SQL Server, return the singleton context
            // (Caller should NOT dispose this)
            return _context ?? throw new InvalidOperationException("Database context not initialized.");
        }

        /// <summary>
        /// Gets whether PostgreSQL context factory is ready (user has been set).
        /// </summary>
        public bool IsPostgresFactoryReady => 
            _settings?.Type == DatabaseType.PostgreSQL && _supabaseUserId.HasValue;

        /// <summary>
        /// Gets whether we should use factory pattern (PostgreSQL) or singleton (SQLite/SQL Server).
        /// </summary>
        private bool ShouldUseContextFactory => 
            _settings?.Type == DatabaseType.PostgreSQL;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a context for read operations. For PostgreSQL, creates a fresh context.
        /// For SQLite/SQL Server, returns the singleton context.
        /// IMPORTANT: Caller must dispose the returned context if ShouldUseContextFactory is true!
        /// </summary>
        private TrackerDbContext? GetReadContext()
        {
            if (ShouldUseContextFactory && _settings != null && _supabaseUserId.HasValue)
            {
                return CreateContext();
            }
            return _context;
        }

        /// <summary>
        /// Disposes the context if it was created by the factory (PostgreSQL).
        /// Does nothing for singleton contexts (SQLite/SQL Server).
        /// </summary>
        private void DisposeIfFactory(TrackerDbContext? context)
        {
            if (ShouldUseContextFactory && context != null && context != _context)
            {
                context.Dispose();
            }
        }

        /// <summary>
        /// Gets the current UserId from UserSettingsManager.
        /// Returns null if not set (should not happen in normal operation).
        /// </summary>
        private int? GetCurrentUserId()
        {
            return UserSettingsManager.Instance.CurrentUserId;
        }

        /// <summary>
        /// Executes an async operation with context and user validation.
        /// For PostgreSQL, creates a fresh context per operation to enable parallel queries.
        /// For SQLite/SQL Server, uses the singleton context.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">The async operation to execute (receives context)</param>
        /// <param name="defaultValue">Value to return if validation fails</param>
        /// <param name="operationName">Name for logging</param>
        /// <param name="requireUserId">Whether to require CurrentUserId to be set</param>
        private async Task<T> ExecuteWithContextAsync<T>(
            Func<TrackerDbContext, Task<T>> operation,
            T defaultValue,
            string operationName,
            bool requireUserId = true)
        {
            if (_settings == null || (_context == null && !ShouldUseContextFactory))
            {
                _logger.Warn("{0} called but database not initialized", operationName);
                return defaultValue;
            }

            if (requireUserId && !GetCurrentUserId().HasValue)
            {
                _logger.Warn("{0} called but CurrentUserId is not set", operationName);
                return defaultValue;
            }

            try
            {
                if (ShouldUseContextFactory)
                {
                    // PostgreSQL: Create fresh context for this operation
                    using var context = CreateContext();
                    return await operation(context).ConfigureAwait(false);
                }
                else
                {
                    // SQLite/SQL Server: Use singleton context
                    return await operation(_context!).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // Log full exception details for PostgreSQL debugging
                System.Diagnostics.Debug.WriteLine($"=== DB ERROR in {operationName}: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                }
                _logger.Exception(ex, "Error in {0}", operationName);
                return defaultValue;
            }
        }

        /// <summary>
        /// Legacy overload for backwards compatibility.
        /// Wraps operations that reference _context directly.
        /// </summary>
        [Obsolete("Use the overload that accepts TrackerDbContext parameter for PostgreSQL support")]
        private async Task<T> ExecuteWithContextAsync<T>(
            Func<Task<T>> operation,
            T defaultValue,
            string operationName,
            bool requireUserId = true)
        {
            if (_context == null)
            {
                _logger.Warn("{0} called but context is null", operationName);
                return defaultValue;
            }

            if (requireUserId && !GetCurrentUserId().HasValue)
            {
                _logger.Warn("{0} called but CurrentUserId is not set", operationName);
                return defaultValue;
            }

            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error in {0}", operationName);
                return defaultValue;
            }
        }

        #endregion

        #region TeamMember Operations

        public async Task<List<TeamMember>> GetTeamMembersAsync()
        {
            return await ExecuteWithContextAsync(
                async (context) =>
                {
                    System.Diagnostics.Debug.WriteLine($"=== GetTeamMembersAsync: Starting query, CurrentUserId={context.CurrentUserId} ===");
                    
                    try
                    {
                        // Global query filters handle UserId and IsDeleted filtering automatically
                        var teamMembers = await context.TeamMembers
                            .AsNoTracking()
                            .OrderBy(tm => tm.Role)
                            .ThenBy(tm => tm.LastName)
                            .ThenBy(tm => tm.FirstName)
                            .ToListAsync()
                            .ConfigureAwait(false);

                        System.Diagnostics.Debug.WriteLine($"=== GetTeamMembersAsync: Query succeeded, got {teamMembers.Count} members ===");

                        // Populate runtime properties for display
                        await PopulateTeamMemberStatsAsync(teamMembers, GetCurrentUserId()!.Value).ConfigureAwait(false);

                        return teamMembers;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"=== GetTeamMembersAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                        }
                        throw; // Re-throw to let ExecuteWithContextAsync handle it
                    }
                },
                new List<TeamMember>(),
                nameof(GetTeamMembersAsync));
        }

        /// <summary>
        /// Populates runtime statistics for team members (last 1:1, next 1:1, task/goal counts).
        /// Uses global query filters for automatic UserId/IsDeleted filtering.
        /// For PostgreSQL, runs queries sequentially due to DbContext threading limitations.
        /// </summary>
        private async Task PopulateTeamMemberStatsAsync(List<TeamMember> teamMembers, int userId)
        {
            if (_context == null || teamMembers.Count == 0) return;

            try
            {
                var teamMemberIds = teamMembers.Select(t => t.Id).ToList();
                var today = DateTime.Now.Date;

                if (ShouldUseContextFactory)
                {
                    // PostgreSQL: Run queries sequentially with own context
                    // Each query gets its own context to avoid threading issues
                    await PopulateTeamMemberStatsSequentialAsync(teamMembers, teamMemberIds, today);
                }
                else
                {
                    // SQLite/SQL Server: Run queries in parallel (safe with singleton context)
                    await PopulateTeamMemberStatsParallelAsync(teamMembers, teamMemberIds, today);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error populating team member stats");
            }
        }

        /// <summary>
        /// Populates team member stats using parallel queries (for SQLite/SQL Server).
        /// </summary>
        private async Task PopulateTeamMemberStatsParallelAsync(List<TeamMember> teamMembers, List<Guid> teamMemberIds, DateTime today)
        {
            // Execute all stat queries in parallel for better performance
            // Global filters handle UserId/IsDeleted - we only add business logic filters
            var lastOneOnOnesTask = _context!.OneOnOnes
                .AsNoTracking()
                .Where(o => teamMemberIds.Contains(o.TeamMember.Id) && o.Date <= today)
                .GroupBy(o => o.TeamMember.Id)
                .Select(g => new { TeamMemberId = g.Key, LastDate = g.Max(o => o.Date) })
                .ToListAsync();

            var nextOneOnOnesTask = _context.OneOnOnes
                .AsNoTracking()
                .Where(o => teamMemberIds.Contains(o.TeamMember.Id) &&
                            o.Date >= today &&
                            o.Status == Common.Enums.MeetingStatusEnum.Scheduled)
                .GroupBy(o => o.TeamMember.Id)
                .Select(g => new { TeamMemberId = g.Key, NextDate = g.Min(o => o.Date), UpcomingCount = g.Count() })
                .ToListAsync();

            var taskCountsTask = _context.Tasks
                .AsNoTracking()
                .Include(t => t.Owner)
                .Where(t => t.Owner != null &&
                            teamMemberIds.Contains(t.Owner.Id) &&
                            !t.IsCompleted)
                .GroupBy(t => t.Owner.Id)
                .Select(g => new { TeamMemberId = g.Key, Count = g.Count() })
                .ToListAsync();

            var goalCountsTask = _context.DevelopmentGoals
                .AsNoTracking()
                .Where(g => teamMemberIds.Contains(g.TeamMemberId) &&
                            g.Status != DevelopmentGoalStatus.Completed &&
                            g.Status != DevelopmentGoalStatus.Cancelled)
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
            PopulateTeamMemberStatsFromResults(teamMembers, lastOneOnOnes, nextOneOnOnes, taskCounts, goalCounts);
        }

        /// <summary>
        /// Populates team member stats using sequential queries (for PostgreSQL).
        /// Uses context factory to create a fresh context per query for true parallelism.
        /// </summary>
        private async Task PopulateTeamMemberStatsSequentialAsync(List<TeamMember> teamMembers, List<Guid> teamMemberIds, DateTime today)
        {
            // For PostgreSQL, we can run TRUE parallel queries since each uses its own context
            var lastOneOnOnesTask = Task.Run(async () =>
            {
                using var context = CreateContext();
                return await context.OneOnOnes
                    .AsNoTracking()
                    .Where(o => teamMemberIds.Contains(o.TeamMember.Id) && o.Date <= today)
                    .GroupBy(o => o.TeamMember.Id)
                    .Select(g => new { TeamMemberId = g.Key, LastDate = g.Max(o => o.Date) })
                    .ToListAsync();
            });

            var nextOneOnOnesTask = Task.Run(async () =>
            {
                using var context = CreateContext();
                return await context.OneOnOnes
                    .AsNoTracking()
                    .Where(o => teamMemberIds.Contains(o.TeamMember.Id) &&
                                o.Date >= today &&
                                o.Status == Common.Enums.MeetingStatusEnum.Scheduled)
                    .GroupBy(o => o.TeamMember.Id)
                    .Select(g => new { TeamMemberId = g.Key, NextDate = g.Min(o => o.Date), UpcomingCount = g.Count() })
                    .ToListAsync();
            });

            var taskCountsTask = Task.Run(async () =>
            {
                using var context = CreateContext();
                return await context.Tasks
                    .AsNoTracking()
                    .Include(t => t.Owner)
                    .Where(t => t.Owner != null &&
                                teamMemberIds.Contains(t.Owner.Id) &&
                                !t.IsCompleted)
                    .GroupBy(t => t.Owner.Id)
                    .Select(g => new { TeamMemberId = g.Key, Count = g.Count() })
                    .ToListAsync();
            });

            var goalCountsTask = Task.Run(async () =>
            {
                using var context = CreateContext();
                return await context.DevelopmentGoals
                    .AsNoTracking()
                    .Where(g => teamMemberIds.Contains(g.TeamMemberId) &&
                                g.Status != DevelopmentGoalStatus.Completed &&
                                g.Status != DevelopmentGoalStatus.Cancelled)
                    .GroupBy(g => g.TeamMemberId)
                    .Select(g => new { TeamMemberId = g.Key, Count = g.Count() })
                    .ToListAsync();
            });

            // Wait for all parallel queries to complete
            await Task.WhenAll(lastOneOnOnesTask, nextOneOnOnesTask, taskCountsTask, goalCountsTask)
                .ConfigureAwait(false);

            var lastOneOnOnes = await lastOneOnOnesTask.ConfigureAwait(false);
            var nextOneOnOnes = await nextOneOnOnesTask.ConfigureAwait(false);
            var taskCounts = await taskCountsTask.ConfigureAwait(false);
            var goalCounts = await goalCountsTask.ConfigureAwait(false);

            // Populate the team members
            PopulateTeamMemberStatsFromResults(teamMembers, lastOneOnOnes, nextOneOnOnes, taskCounts, goalCounts);
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

        public async Task<TeamMember?> GetTeamMemberByIdAsync(Guid id)
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
                // Global query filters handle UserId and IsDeleted automatically
                return await _context.TeamMembers
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving team member with id {0}", id);
                return null;
            }
        }

        public async Task<Guid> AddTeamMemberAsync(TeamMember teamMember)
        {
            if (_context == null) return Guid.Empty;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddTeamMemberAsync called but CurrentUserId is not set");
                return Guid.Empty;
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
                return Guid.Empty;
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

        public async Task<bool> DeleteTeamMemberAsync(Guid id)
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

        /// <summary>
        /// Finds a team member by display name (case-insensitive).
        /// </summary>
        public async Task<TeamMember?> FindTeamMemberByNameAsync(string displayName)
        {
            if (_context == null || string.IsNullOrWhiteSpace(displayName)) return null;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("FindTeamMemberByNameAsync called but CurrentUserId is not set");
                return null;
            }

            try
            {
                // Try exact match first (case-insensitive)
                var member = await _context.TeamMembers
                    .Where(t => !t.IsDeleted && EF.Property<int>(t, "UserId") == currentUserId.Value)
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

        #endregion

        #region OneOnOne Operations

        public async Task<List<OneOnOne>> GetOneOnOnesAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetOneOnOnesAsync: Starting ===");
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetOneOnOnesAsync: No CurrentUserId ===");
                _logger.Warn("GetOneOnOnesAsync called but CurrentUserId is not set");
                return new List<OneOnOne>();
            }

            var context = GetReadContext();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetOneOnOnesAsync: No context ===");
                return new List<OneOnOne>();
            }

            try
            {
                var query = context.OneOnOnes
                    .Where(o => !o.IsDeleted && EF.Property<int>(o, "UserId") == currentUserId.Value)
                    .Include(o => o.TeamMember)
                    .Include(o => o.Tasks)
                    .Include(o => o.AgendaItems)
                    .AsQueryable();

                // Only include Phase 1 linked tables if they exist
                try
                {
                    query = query
                        .Include(o => o.LinkedTasks).ThenInclude(lt => lt.Task)
                        .Include(o => o.LinkedOkrs).ThenInclude(lo => lo.Okr)
                        .Include(o => o.LinkedKpis).ThenInclude(lk => lk.Kpi);
                }
                catch
                {
                    // Phase 1 tables don't exist yet - skip them
                }

                var results = await query
                    .OrderByDescending(o => o.Date)
                    .ToListAsync();
                
                // Filter deleted items in memory to avoid SQL translation issues
                foreach (var oneOnOne in results)
                {
                    if (oneOnOne.Tasks != null)
                        oneOnOne.Tasks = oneOnOne.Tasks.Where(t => !t.IsDeleted).ToList();
                    if (oneOnOne.AgendaItems != null)
                        oneOnOne.AgendaItems = oneOnOne.AgendaItems.Where(a => !a.IsDeleted).ToList();
                    if (oneOnOne.LinkedTasks != null)
                        oneOnOne.LinkedTasks = oneOnOne.LinkedTasks.Where(lt => !lt.IsDeleted).ToList();
                    if (oneOnOne.LinkedOkrs != null)
                        oneOnOne.LinkedOkrs = oneOnOne.LinkedOkrs.Where(lo => !lo.IsDeleted).ToList();
                    if (oneOnOne.LinkedKpis != null)
                        oneOnOne.LinkedKpis = oneOnOne.LinkedKpis.Where(lk => !lk.IsDeleted).ToList();
                }
                
                System.Diagnostics.Debug.WriteLine($"=== GetOneOnOnesAsync: Query succeeded, got {results.Count} meetings ===");
                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetOneOnOnesAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving one-on-ones from database");
                return new List<OneOnOne>();
            }
            finally
            {
                DisposeIfFactory(context);
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

        public async Task<int> AddOneOnOneAsync(OneOnOne oneOnOne, Guid? teamMemberId = null)
        {
            if (_context == null)
            {
                _logger.Error("AddOneOnOneAsync: _context is null");
                return 0;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddOneOnOneAsync called but CurrentUserId is not set");
                return 0;
            }
            
            _logger.Info("AddOneOnOneAsync: Starting with CurrentUserId={0}, TeamMemberId={1}", currentUserId.Value, teamMemberId);
            
            // Verify the user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == currentUserId.Value);
            _logger.Info("AddOneOnOneAsync: User existence check - UserId={0}, Exists={1}", currentUserId.Value, userExists);
            if (!userExists)
            {
                _logger.Error("AddOneOnOneAsync: UserId={0} does not exist in Users table", currentUserId.Value);
                return 0;
            }

            try
            {
                // Detach the TeamMember navigation property to prevent EF from tracking it
                // The navigation property is initialized to new TeamMember() which has no UserId
                // This causes FK constraint errors when EF tries to add it
                oneOnOne.TeamMember = null;
                
                _context.OneOnOnes.Add(oneOnOne);
                // Set UserId shadow property
                _context.Entry(oneOnOne).Property("UserId").CurrentValue = currentUserId.Value;
                
                // Set TeamMemberId shadow property
                // Priority: explicit parameter > TeamMember.Id > error
                if (teamMemberId.HasValue)
                {
                    // Verify the team member exists and belongs to this user
                    var teamMember = await _context.TeamMembers
                        .Where(tm => tm.Id == teamMemberId.Value && EF.Property<int>(tm, "UserId") == currentUserId.Value)
                        .FirstOrDefaultAsync();
                    
                    if (teamMember == null)
                    {
                        _logger.Error("AddOneOnOneAsync: TeamMemberId={0} not found or belongs to different user (UserId={1})", teamMemberId.Value, currentUserId.Value);
                        return 0;
                    }
                    
                    _context.Entry(oneOnOne).Property("TeamMemberId").CurrentValue = teamMemberId.Value;
                    _logger.Info("AddOneOnOneAsync: Setting TeamMemberId={0}, UserId={1}", teamMemberId.Value, currentUserId.Value);
                }
                else if (oneOnOne.TeamMember?.Id != Guid.Empty)
                {
                    _context.Entry(oneOnOne).Property("TeamMemberId").CurrentValue = oneOnOne.TeamMember.Id;
                    _logger.Info("AddOneOnOneAsync: Setting TeamMemberId={0} from navigation, UserId={1}", oneOnOne.TeamMember.Id, currentUserId.Value);
                }
                else
                {
                    _logger.Error("AddOneOnOneAsync: TeamMemberId not provided and TeamMember is null or has invalid Id");
                    return 0;
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Added one-on-one ID: {0}", oneOnOne.Id);
                return oneOnOne.Id;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "";
                _logger.Exception(ex, "Error adding one-on-one.{0}", innerMsg);
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
        public async Task<OneOnOne?> GetPreviousOneOnOneAsync(Guid teamMemberId, int? excludeOneOnOneId = null)
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
        public async Task<List<OneOnOne>> GetMeetingsForTeamMemberAsync(Guid teamMemberId)
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
                var results = await _context.OneOnOnes
                    .Where(o => !o.IsDeleted && EF.Property<int>(o, "UserId") == currentUserId.Value && o.TeamMember.Id == teamMemberId)
                    .Include(o => o.TeamMember)
                    .Include(o => o.Tasks)
                    .Include(o => o.AgendaItems)
                    .OrderByDescending(o => o.Date)
                    .ToListAsync();
                
                // Filter deleted items in memory to avoid SQL translation issues
                foreach (var oneOnOne in results)
                {
                    if (oneOnOne.Tasks != null)
                        oneOnOne.Tasks = oneOnOne.Tasks.Where(t => !t.IsDeleted).ToList();
                    if (oneOnOne.AgendaItems != null)
                        oneOnOne.AgendaItems = oneOnOne.AgendaItems.Where(a => !a.IsDeleted).ToList();
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meetings for team member {0}", teamMemberId);
                return new List<OneOnOne>();
            }
        }

        /// <summary>
        /// <summary>
        /// Gets all 1:1 meetings within a date range.
        /// </summary>
        public async Task<List<OneOnOne>> GetMeetingsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (_context == null) return new List<OneOnOne>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetMeetingsInRangeAsync called but CurrentUserId is not set");
                return new List<OneOnOne>();
            }

            try
            {
                var results = await _context.OneOnOnes
                    .Where(o => !o.IsDeleted && EF.Property<int>(o, "UserId") == currentUserId.Value &&
                                o.Date >= startDate && o.Date <= endDate)
                    .Include(o => o.TeamMember)
                    .Include(o => o.Tasks)
                    .Include(o => o.AgendaItems)
                    .OrderBy(o => o.Date)
                    .ToListAsync();
                
                // Filter deleted items in memory to avoid SQL translation issues
                foreach (var oneOnOne in results)
                {
                    if (oneOnOne.Tasks != null)
                        oneOnOne.Tasks = oneOnOne.Tasks.Where(t => !t.IsDeleted).ToList();
                    if (oneOnOne.AgendaItems != null)
                        oneOnOne.AgendaItems = oneOnOne.AgendaItems.Where(a => !a.IsDeleted).ToList();
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meetings in range {0} to {1}", startDate, endDate);
                return new List<OneOnOne>();
            }
        }

        /// <summary>
        /// Gets all uncompleted MeetingTasks for a specific team member from previous meetings.
        /// Used to rollover unfinished items into the next meeting.
        /// </summary>
        public async Task<List<OneOnOne>> GetCompletedMeetingsForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<OneOnOne>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new List<OneOnOne>();

            try
            {
                var results = await _context.OneOnOnes
                    .Where(o => !o.IsDeleted && 
                                EF.Property<int>(o, "UserId") == currentUserId.Value &&
                                o.TeamMember.Id == teamMemberId &&
                                o.Status == MeetingStatusEnum.Completed)
                    .Include(o => o.TeamMember)
                    .Include(o => o.Tasks)
                    .OrderByDescending(o => o.Date)
                    .ToListAsync();
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving completed meetings for team member {0}", teamMemberId);
                return new List<OneOnOne>();
            }
        }

        /// <summary>
        /// Saves calendar link data for a meeting.
        /// </summary>
        public async Task SaveCalendarLinkAsync(CalendarLink link)
        {
            if (_context == null || link == null) return;

            try
            {
                _context.CalendarLinks.Add(link);
                await _context.SaveChangesAsync();
                _logger.Info("Saved calendar link for meeting");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error saving calendar link");
            }
        }

        /// <summary>
        /// Deletes the calendar link for a meeting.
        /// </summary>
        public async Task DeleteCalendarLinkAsync(int meetingId, string provider)
        {
            if (_context == null) return;

            try
            {
                var link = await _context.CalendarLinks
                    .FirstOrDefaultAsync(l => l.OneOnOneId == meetingId && l.ProviderId == provider);
                
                if (link != null)
                {
                    _context.CalendarLinks.Remove(link);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted calendar link for meeting {0}", meetingId);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting calendar link for meeting {0}", meetingId);
            }
        }

        /// <summary>
        /// Finds a meeting by external calendar event ID.
        /// </summary>
        public async Task<OneOnOne?> FindMeetingByCalendarEventIdAsync(string provider, string externalEventId)
        {
            if (_context == null || string.IsNullOrEmpty(externalEventId)) return null;

            try
            {
                var link = await _context.CalendarLinks
                    .FirstOrDefaultAsync(l => l.ProviderId == provider && 
                                         l.ExternalEventId == externalEventId);
                
                if (link != null)
                {
                    return await _context.OneOnOnes.FirstOrDefaultAsync(o => o.Id == link.OneOnOneId);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error finding meeting by calendar event ID");
                return null;
            }
        }

        /// <summary>
        /// Updates meeting sync data from external calendar.
        /// </summary>
        public async Task UpdateMeetingSyncDataAsync(int meetingId, string? externalEventId, string? externalEtag, string? syncStatus)
        {
            if (_context == null) return;

            try
            {
                var meeting = await _context.OneOnOnes.FirstOrDefaultAsync(o => o.Id == meetingId);
                if (meeting != null)
                {
                    meeting.CalendarEventId = externalEventId;
                    meeting.CalendarEventEtag = externalEtag;
                    meeting.SyncStatus = syncStatus ?? meeting.SyncStatus;
                    meeting.LastSyncedAt = DateTime.UtcNow;
                    
                    _context.OneOnOnes.Update(meeting);
                    await _context.SaveChangesAsync();
                    _logger.Info("Updated sync data for meeting {0}", meetingId);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating meeting sync data for meeting {0}", meetingId);
            }
        }

        /// <summary>
        /// Gets all uncompleted MeetingTasks for a specific team member from previous meetings.
        /// Used to rollover unfinished items into the next meeting.
        /// </summary>
        public async Task<List<MeetingTask>> GetUncompletedMeetingTasksAsync(Guid teamMemberId)
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
        /// Gets the count of OneOnOne meetings for multiple tasks in a single query (batch operation).
        /// This prevents N+1 query problem when loading meeting counts for multiple tasks.
        /// </summary>
        public async Task<Dictionary<int, int>> GetTaskMeetingCountsAsync(List<int> taskIds)
        {
            if (_context == null || taskIds == null || taskIds.Count == 0)
                return new Dictionary<int, int>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return new Dictionary<int, int>();
            }

            try
            {
                var counts = await _context.OneOnOneLinkedTasks
                    .Where(link => !link.IsDeleted && taskIds.Contains(link.TaskId))
                    .Join(_context.OneOnOnes.Where(o => EF.Property<int>(o, "UserId") == currentUserId.Value),
                        link => link.OneOnOneId,
                        meeting => meeting.Id,
                        (link, meeting) => link)
                    .GroupBy(link => link.TaskId)
                    .Select(g => new { TaskId = g.Key, Count = g.Select(x => x.OneOnOneId).Distinct().Count() })
                    .ToDictionaryAsync(x => x.TaskId, x => x.Count);

                return counts;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for tasks");
                return new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// Gets the count of OneOnOne meetings for multiple KPIs in a single query (batch operation).
        /// This prevents N+1 query problem when loading meeting counts for multiple KPIs.
        /// </summary>
        public async Task<Dictionary<int, int>> GetKpiMeetingCountsAsync(List<int> kpiIds)
        {
            if (_context == null || kpiIds == null || kpiIds.Count == 0)
                return new Dictionary<int, int>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return new Dictionary<int, int>();
            }

            try
            {
                var counts = await _context.OneOnOneLinkedKpis
                    .Where(link => !link.IsDeleted && kpiIds.Contains(link.KpiId))
                    .Join(_context.OneOnOnes.Where(o => EF.Property<int>(o, "UserId") == currentUserId.Value),
                        link => link.OneOnOneId,
                        meeting => meeting.Id,
                        (link, meeting) => link)
                    .GroupBy(link => link.KpiId)
                    .Select(g => new { KpiId = g.Key, Count = g.Select(x => x.OneOnOneId).Distinct().Count() })
                    .ToDictionaryAsync(x => x.KpiId, x => x.Count);

                return counts;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for KPIs");
                return new Dictionary<int, int>();
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
            System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync: Starting ===");
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync: No CurrentUserId ===");
                _logger.Warn("GetProjectsAsync called but CurrentUserId is not set");
                return new List<Project>();
            }

            var context = GetReadContext();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync: No context ===");
                return new List<Project>();
            }

            try
            {
                var result = await context.Projects
                    .Where(p => !p.IsDeleted && EF.Property<int>(p, "UserId") == currentUserId.Value)
                    .Include(p => p.Owner)
                    .Include(p => p.TeamMembers.Where(tm => !tm.IsDeleted))
                    .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                    .Include(p => p.Risks.Where(r => !r.IsDeleted))
                    .Include(p => p.Dependencies.Where(d => !d.IsDeleted))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync: Query succeeded, got {result.Count} projects ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving projects from database");
                return new List<Project>();
            }
            finally
            {
                DisposeIfFactory(context);
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
            System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync: Starting ===");
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync: No CurrentUserId ===");
                _logger.Warn("GetTasksAsync called but CurrentUserId is not set");
                return new List<IndividualTask>();
            }

            var context = GetReadContext();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync: No context ===");
                return new List<IndividualTask>();
            }

            try
            {
                var result = await context.Tasks
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted && EF.Property<int>(t, "UserId") == currentUserId.Value)
                    .Include(t => t.Owner)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync: Query succeeded, got {result.Count} tasks ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving tasks from database");
                return new List<IndividualTask>();
            }
            finally
            {
                DisposeIfFactory(context);
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

        /// <summary>
        /// Deletes a task by ID.
        /// </summary>
        public async Task<bool> DeleteTaskAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task != null)
                {
                    _context.Tasks.Remove(task);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted Task ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting Task ID: {0}", id);
                return false;
            }
        }

        #endregion

        #region KeyResult Operations

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
            System.Diagnostics.Debug.WriteLine($"=== GetKPIsAsync: Starting ===");
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetKPIsAsync: No CurrentUserId ===");
                _logger.Warn("GetKPIsAsync called but CurrentUserId is not set");
                return new List<KeyPerformanceIndicator>();
            }

            var context = GetReadContext();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetKPIsAsync: No context ===");
                return new List<KeyPerformanceIndicator>();
            }

            try
            {
                var result = await context.KeyPerformanceIndicators
                    .AsNoTracking()
                    .Where(k => !k.IsDeleted && EF.Property<int>(k, "UserId") == currentUserId.Value)
                    .Include(k => k.Owner)
                    .OrderBy(k => k.Name)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetKPIsAsync: Query succeeded, got {result.Count} KPIs ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetKPIsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving KPIs from database");
                return new List<KeyPerformanceIndicator>();
            }
            finally
            {
                DisposeIfFactory(context);
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

        /// <summary>
        /// Deletes a KPI by ID.
        /// </summary>
        public async Task<bool> DeleteKPIAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var kpi = await _context.KeyPerformanceIndicators.FindAsync(id);
                if (kpi != null)
                {
                    _context.KeyPerformanceIndicators.Remove(kpi);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted KPI ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting KPI ID: {0}", id);
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
        public async Task<List<Feedback>> GetFeedbackForTeamMemberAsync(Guid teamMemberId)
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
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<Feedback>();

            var context = GetReadContext();
            if (context == null) return new List<Feedback>();

            try
            {
                return await context.Feedbacks
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
            finally
            {
                DisposeIfFactory(context);
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

        #region Development Goal Operations

        /// <summary>
        /// Gets all development goals for a specific team member.
        /// </summary>
        public async Task<List<DevelopmentGoal>> GetDevelopmentGoalsForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<DevelopmentGoal>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Warn("GetDevelopmentGoalsForTeamMemberAsync called but CurrentUserId is not set");
                return new List<DevelopmentGoal>();
            }

            try
            {
                return await _context.DevelopmentGoals
                    .Where(g => !g.IsDeleted && EF.Property<int>(g, "UserId") == currentUserId.Value && g.TeamMemberId == teamMemberId)
                    .Include(g => g.TeamMember)
                    .Include(g => g.Milestones.Where(m => !m.IsDeleted))
                    .OrderByDescending(g => g.Status == DevelopmentGoalStatus.Active)
                    .ThenBy(g => g.TargetDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving development goals for team member {0}", teamMemberId);
                return new List<DevelopmentGoal>();
            }
        }

        /// <summary>
        /// Gets all development goals (for reports/dashboard).
        /// </summary>
        public async Task<List<DevelopmentGoal>> GetAllDevelopmentGoalsAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<DevelopmentGoal>();

            var context = GetReadContext();
            if (context == null) return new List<DevelopmentGoal>();

            try
            {
                return await context.DevelopmentGoals
                    .Where(g => !g.IsDeleted && EF.Property<int>(g, "UserId") == currentUserId.Value)
                    .Include(g => g.TeamMember)
                    .Include(g => g.Milestones.Where(m => !m.IsDeleted))
                    .OrderByDescending(g => g.Status == DevelopmentGoalStatus.Active)
                    .ThenBy(g => g.TargetDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving all development goals");
                return new List<DevelopmentGoal>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Gets all Goals (organizational, team, and personal objectives).
        /// Goals consolidate the legacy OKR/KPI framework with type discrimination.
        /// </summary>
        public async Task<List<Goal>> GetGoalsAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<Goal>();

            var context = GetReadContext();
            if (context == null) return new List<Goal>();

            try
            {
                return await context.Goals
                    .Include(g => g.Targets)
                        .ThenInclude(t => t.Measurables)
                    .Include(g => g.CreatedByUser)
                    .Where(g => !g.IsDeleted)
                    .OrderByDescending(g => g.Type == GoalType.Organizational)
                    .ThenBy(g => g.EndDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving Goals");
                return new List<Goal>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Adds a new development goal.
        /// </summary>
        public async Task<Guid> AddDevelopmentGoalAsync(DevelopmentGoal goal)
        {
            if (_context == null) return Guid.Empty;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddDevelopmentGoalAsync called but CurrentUserId is not set");
                return Guid.Empty;
            }

            try
            {
                _context.DevelopmentGoals.Add(goal);
                _context.Entry(goal).Property("UserId").CurrentValue = currentUserId.Value;
                
                // Set UserId for milestones too
                foreach (var milestone in goal.Milestones)
                {
                    _context.Entry(milestone).Property("UserId").CurrentValue = currentUserId.Value;
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Added development goal ID: {0}", goal.Id);
                return goal.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding development goal");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing development goal.
        /// </summary>
        public async Task<bool> UpdateDevelopmentGoalAsync(DevelopmentGoal goal)
        {
            if (_context == null) return false;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return false;

            try
            {
                var existing = await _context.DevelopmentGoals.FindAsync(goal.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateDevelopmentGoalAsync: Goal ID {0} not found", goal.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(goal);
                
                // Handle milestones - set UserId for new ones
                foreach (var milestone in goal.Milestones.Where(m => m.Id == Guid.Empty))
                {
                    _context.Entry(milestone).Property("UserId").CurrentValue = currentUserId.Value;
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Updated development goal ID: {0}", goal.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating development goal ID: {0}", goal.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a development goal (soft delete).
        /// </summary>
        public async Task<bool> DeleteDevelopmentGoalAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var goal = await _context.DevelopmentGoals.FindAsync(id);
                if (goal != null)
                {
                    _context.DevelopmentGoals.Remove(goal);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted development goal ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting development goal ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Updates a development goal's progress percentage.
        /// </summary>
        public async Task<bool> UpdateDevelopmentGoalProgressAsync(Guid goalId, int progressPercent)
        {
            if (_context == null) return false;

            try
            {
                var goal = await _context.DevelopmentGoals.FindAsync(goalId);
                if (goal != null)
                {
                    goal.ProgressPercent = Math.Clamp(progressPercent, 0, 100);
                    if (goal.ProgressPercent == 100 && goal.Status != DevelopmentGoalStatus.Completed)
                    {
                        goal.Status = DevelopmentGoalStatus.Completed;
                    }
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating development goal progress");
                return false;
            }
        }

        /// <summary>
        /// Toggles a development goal milestone's completion status.
        /// </summary>
        public async Task<bool> ToggleDevelopmentGoalMilestoneAsync(Guid milestoneId)
        {
            if (_context == null) return false;

            try
            {
                var milestone = await _context.DevelopmentGoalMilestones.FindAsync(milestoneId);
                if (milestone != null)
                {
                    milestone.Status = milestone.Status == MilestoneStatus.Completed ? MilestoneStatus.NotStarted : MilestoneStatus.Completed;
                    milestone.CompletedAt = milestone.Status == MilestoneStatus.Completed ? DateTime.UtcNow : null;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error toggling development goal milestone");
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
                var userId = _context.PostgresUserId ?? throw new InvalidOperationException("PostgresUserId not set");
                return await _context.Reminders
                    .Where(r => !r.IsDeleted && 
                                r.UserId == userId &&
                                r.Status == ReminderStatus.Pending &&
                                r.RemindAt <= now &&
                                (r.SnoozedUntil == null || r.SnoozedUntil <= now))
                    .OrderBy(r => r.RemindAt)
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

            try
            {
                var userId = _context.PostgresUserId ?? throw new InvalidOperationException("PostgresUserId not set");
                return await _context.Reminders
                    .Where(r => !r.IsDeleted && r.UserId == userId)
                    .OrderBy(r => r.RemindAt)
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

            try
            {
                var userId = _context.PostgresUserId ?? throw new InvalidOperationException("PostgresUserId not set");
                return await _context.Reminders
                    .Where(r => !r.IsDeleted && 
                                r.UserId == userId &&
                                (r.Status == ReminderStatus.Pending || r.Status == ReminderStatus.Snoozed))
                    .OrderBy(r => r.RemindAt)
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
        public async Task<Guid> AddReminderAsync(Reminder reminder)
        {
            if (_context == null) return Guid.Empty;

            try
            {
                _context.Reminders.Add(reminder);
                
                // Set the Supabase user ID if available, otherwise use a placeholder
                if (_context.PostgresUserId.HasValue)
                {
                    reminder.UserId = _context.PostgresUserId.Value;
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Added reminder ID: {0}", reminder.Id);
                return reminder.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding reminder");
                return Guid.Empty;
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
        public async Task<bool> MarkReminderTriggeredAsync(Guid reminderId)
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
        public async Task<bool> SnoozeReminderAsync(Guid reminderId, int snoozeMinutes)
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
        public async Task<List<QuickNote>> GetQuickNotesForTeamMemberAsync(Guid teamMemberId)
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

        #region Pulse Survey Operations

        /// <summary>
        /// Gets all pulse surveys.
        /// </summary>
        public async Task<List<PulseSurvey>> GetPulseSurveysAsync()
        {
            if (_context == null) return new List<PulseSurvey>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<PulseSurvey>();

            try
            {
                return await _context.PulseSurveys
                    .Include(s => s.Questions.OrderBy(q => q.SortOrder))
                    .Include(s => s.Responses)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving pulse surveys");
                return new List<PulseSurvey>();
            }
        }

        /// <summary>
        /// Gets a pulse survey by ID with all related data.
        /// </summary>
        public async Task<PulseSurvey?> GetPulseSurveyAsync(int id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.PulseSurveys
                    .Include(s => s.Questions.OrderBy(q => q.SortOrder))
                    .Include(s => s.Responses)
                        .ThenInclude(r => r.Answers)
                    .Include(s => s.Responses)
                        .ThenInclude(r => r.TeamMember)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving pulse survey ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new pulse survey.
        /// </summary>
        public async Task<int> AddPulseSurveyAsync(PulseSurvey survey)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddPulseSurveyAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.PulseSurveys.Add(survey);
                _context.Entry(survey).Property("UserId").CurrentValue = currentUserId.Value;
                
                // Set UserId on questions
                foreach (var question in survey.Questions)
                {
                    _context.Entry(question).Property("UserId").CurrentValue = currentUserId.Value;
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Added pulse survey ID: {0}", survey.Id);
                return survey.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding pulse survey");
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing pulse survey.
        /// </summary>
        public async Task<bool> UpdatePulseSurveyAsync(PulseSurvey survey)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.PulseSurveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == survey.Id);
                
                if (existing == null)
                {
                    _logger.Error("UpdatePulseSurveyAsync: Survey ID {0} not found", survey.Id);
                    return false;
                }

                // Update basic properties
                existing.Title = survey.Title;
                existing.Description = survey.Description;
                existing.Status = survey.Status;
                existing.SentDate = survey.SentDate;
                existing.DueDate = survey.DueDate;
                existing.ClosedDate = survey.ClosedDate;
                existing.IsAnonymous = survey.IsAnonymous;

                await _context.SaveChangesAsync();
                _logger.Info("Updated pulse survey ID: {0}", survey.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating pulse survey ID: {0}", survey.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a pulse survey (soft delete).
        /// </summary>
        public async Task<bool> DeletePulseSurveyAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var survey = await _context.PulseSurveys.FindAsync(id);
                if (survey != null)
                {
                    _context.PulseSurveys.Remove(survey);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted pulse survey ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting pulse survey ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Adds a survey response.
        /// </summary>
        public async Task<int> AddSurveyResponseAsync(PulseSurveyResponse response)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddSurveyResponseAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.PulseSurveyResponses.Add(response);
                _context.Entry(response).Property("UserId").CurrentValue = currentUserId.Value;
                await _context.SaveChangesAsync();
                _logger.Info("Added survey response ID: {0}", response.Id);
                return response.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding survey response");
                return 0;
            }
        }

        /// <summary>
        /// Gets survey analytics for a specific survey.
        /// </summary>
        public async Task<Dictionary<int, (double AverageRating, int ResponseCount)>> GetSurveyAnalyticsAsync(int surveyId)
        {
            if (_context == null) return new Dictionary<int, (double, int)>();

            try
            {
                var responses = await _context.PulseSurveyResponses
                    .Where(r => r.PulseSurveyId == surveyId)
                    .Include(r => r.Answers)
                    .ToListAsync();

                var analytics = new Dictionary<int, (double AverageRating, int ResponseCount)>();
                
                var allAnswers = responses.SelectMany(r => r.Answers).ToList();
                var groupedByQuestion = allAnswers.GroupBy(a => a.PulseSurveyQuestionId);
                
                foreach (var group in groupedByQuestion)
                {
                    var ratingAnswers = group.Where(a => a.RatingValue.HasValue).ToList();
                    var avgRating = ratingAnswers.Any() ? ratingAnswers.Average(a => a.RatingValue!.Value) : 0;
                    analytics[group.Key] = (avgRating, group.Count());
                }
                
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting survey analytics for survey ID: {0}", surveyId);
                return new Dictionary<int, (double, int)>();
            }
        }

        #endregion

        #region Performance Review Template Operations

        /// <summary>
        /// Gets all review templates.
        /// </summary>
        public async Task<List<ReviewTemplate>> GetReviewTemplatesAsync()
        {
            if (_context == null) return new List<ReviewTemplate>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<ReviewTemplate>();

            try
            {
                return await _context.ReviewTemplates
                    .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                        .ThenInclude(s => s.Questions.OrderBy(q => q.SortOrder))
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving review templates");
                return new List<ReviewTemplate>();
            }
        }

        /// <summary>
        /// Gets a review template by ID.
        /// </summary>
        public async Task<ReviewTemplate?> GetReviewTemplateAsync(int id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.ReviewTemplates
                    .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                        .ThenInclude(s => s.Questions.OrderBy(q => q.SortOrder))
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving review template ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new review template.
        /// </summary>
        public async Task<int> AddReviewTemplateAsync(ReviewTemplate template)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddReviewTemplateAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.ReviewTemplates.Add(template);
                _context.Entry(template).Property("UserId").CurrentValue = currentUserId.Value;
                
                // Set UserId on sections and questions
                foreach (var section in template.Sections)
                {
                    _context.Entry(section).Property("UserId").CurrentValue = currentUserId.Value;
                    foreach (var question in section.Questions)
                    {
                        _context.Entry(question).Property("UserId").CurrentValue = currentUserId.Value;
                    }
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Added review template ID: {0}", template.Id);
                return template.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding review template");
                return 0;
            }
        }

        /// <summary>
        /// Updates a review template.
        /// </summary>
        public async Task<bool> UpdateReviewTemplateAsync(ReviewTemplate template)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.ReviewTemplates.FindAsync(template.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateReviewTemplateAsync: Template ID {0} not found", template.Id);
                    return false;
                }

                existing.Name = template.Name;
                existing.Description = template.Description;
                existing.ReviewType = template.ReviewType;
                existing.IsDefault = template.IsDefault;
                existing.IsActive = template.IsActive;

                await _context.SaveChangesAsync();
                _logger.Info("Updated review template ID: {0}", template.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating review template ID: {0}", template.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a review template (soft delete).
        /// </summary>
        public async Task<bool> DeleteReviewTemplateAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var template = await _context.ReviewTemplates.FindAsync(id);
                if (template != null)
                {
                    _context.ReviewTemplates.Remove(template);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted review template ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting review template ID: {0}", id);
                return false;
            }
        }

        #endregion

        #region Performance Review Cycle Operations

        /// <summary>
        /// Gets all review cycles.
        /// </summary>
        public async Task<List<PerformanceReviewCycle>> GetReviewCyclesAsync()
        {
            if (_context == null) return new List<PerformanceReviewCycle>();

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return new List<PerformanceReviewCycle>();

            try
            {
                return await _context.PerformanceReviewCycles
                    .Include(c => c.ReviewTemplate)
                    .Include(c => c.Reviews)
                        .ThenInclude(r => r.TeamMember)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving review cycles");
                return new List<PerformanceReviewCycle>();
            }
        }

        /// <summary>
        /// Gets a review cycle by ID with all related data.
        /// </summary>
        public async Task<PerformanceReviewCycle?> GetReviewCycleAsync(int id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.PerformanceReviewCycles
                    .Include(c => c.ReviewTemplate)
                        .ThenInclude(t => t.Sections)
                            .ThenInclude(s => s.Questions)
                    .Include(c => c.Reviews)
                        .ThenInclude(r => r.TeamMember)
                    .Include(c => c.Reviews)
                        .ThenInclude(r => r.Sections)
                            .ThenInclude(s => s.Answers)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving review cycle ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new review cycle.
        /// </summary>
        public async Task<int> AddReviewCycleAsync(PerformanceReviewCycle cycle)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.Error("AddReviewCycleAsync called but CurrentUserId is not set");
                return 0;
            }

            try
            {
                _context.PerformanceReviewCycles.Add(cycle);
                _context.Entry(cycle).Property("UserId").CurrentValue = currentUserId.Value;
                
                // Set UserId on reviews
                foreach (var review in cycle.Reviews)
                {
                    _context.Entry(review).Property("UserId").CurrentValue = currentUserId.Value;
                }
                
                await _context.SaveChangesAsync();
                _logger.Info("Added review cycle ID: {0}", cycle.Id);
                return cycle.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding review cycle");
                return 0;
            }
        }

        /// <summary>
        /// Updates a review cycle.
        /// </summary>
        public async Task<bool> UpdateReviewCycleAsync(PerformanceReviewCycle cycle)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.PerformanceReviewCycles.FindAsync(cycle.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateReviewCycleAsync: Cycle ID {0} not found", cycle.Id);
                    return false;
                }

                existing.Name = cycle.Name;
                existing.Description = cycle.Description;
                existing.Status = cycle.Status;
                existing.SelfReviewStartDate = cycle.SelfReviewStartDate;
                existing.SelfReviewDueDate = cycle.SelfReviewDueDate;
                existing.ManagerReviewStartDate = cycle.ManagerReviewStartDate;
                existing.ManagerReviewDueDate = cycle.ManagerReviewDueDate;
                existing.CalibrationDate = cycle.CalibrationDate;
                existing.ShareDate = cycle.ShareDate;

                await _context.SaveChangesAsync();
                _logger.Info("Updated review cycle ID: {0}", cycle.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating review cycle ID: {0}", cycle.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a review cycle (soft delete).
        /// </summary>
        public async Task<bool> DeleteReviewCycleAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var cycle = await _context.PerformanceReviewCycles.FindAsync(id);
                if (cycle != null)
                {
                    _context.PerformanceReviewCycles.Remove(cycle);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted review cycle ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting review cycle ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Creates reviews for all team members in a cycle.
        /// </summary>
        public async Task<int> CreateReviewsForCycleAsync(int cycleId)
        {
            if (_context == null) return 0;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return 0;

            try
            {
                var cycle = await _context.PerformanceReviewCycles
                    .Include(c => c.ReviewTemplate)
                        .ThenInclude(t => t.Sections)
                            .ThenInclude(s => s.Questions)
                    .FirstOrDefaultAsync(c => c.Id == cycleId);

                if (cycle == null)
                {
                    _logger.Error("CreateReviewsForCycleAsync: Cycle ID {0} not found", cycleId);
                    return 0;
                }

                var teamMembers = await _context.TeamMembers
                    .Where(tm => !tm.IsDeleted && EF.Property<int>(tm, "UserId") == currentUserId.Value)
                    .ToListAsync();

                var count = 0;
                foreach (var member in teamMembers)
                {
                    // Check if review already exists
                    var existingReview = await _context.PerformanceReviews
                        .AnyAsync(r => r.PerformanceReviewCycleId == cycleId && r.TeamMemberId == member.Id);

                    if (!existingReview)
                    {
                        var review = new PerformanceReview
                        {
                            PerformanceReviewCycleId = cycleId,
                            TeamMemberId = member.Id,
                            Status = Common.Enums.ReviewStatus.NotStarted
                        };

                        // Create sections based on template
                        foreach (var templateSection in cycle.ReviewTemplate.Sections.OrderBy(s => s.SortOrder))
                        {
                            var reviewSection = new PerformanceReviewSection
                            {
                                ReviewTemplateSectionId = templateSection.Id
                            };

                            // Create answers for each question in the section
                            foreach (var question in templateSection.Questions.OrderBy(q => q.SortOrder))
                            {
                                reviewSection.Answers.Add(new PerformanceReviewAnswer
                                {
                                    ReviewTemplateQuestionId = question.Id,
                                    IsSelfAssessment = true
                                });
                            }

                            review.Sections.Add(reviewSection);
                        }

                        _context.PerformanceReviews.Add(review);
                        _context.Entry(review).Property("UserId").CurrentValue = currentUserId.Value;
                        count++;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.Info("Created {0} reviews for cycle ID: {1}", count, cycleId);
                return count;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error creating reviews for cycle ID: {0}", cycleId);
                return 0;
            }
        }

        #endregion

        #region Individual Performance Review Operations

        /// <summary>
        /// Gets a performance review by ID.
        /// </summary>
        public async Task<PerformanceReview?> GetPerformanceReviewAsync(int id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.PerformanceReviews
                    .Include(r => r.TeamMember)
                    .Include(r => r.PerformanceReviewCycle)
                        .ThenInclude(c => c.ReviewTemplate)
                    .Include(r => r.Sections)
                        .ThenInclude(s => s.Answers)
                            .ThenInclude(a => a.ReviewTemplateQuestion)
                    .Include(r => r.Sections)
                        .ThenInclude(s => s.ReviewTemplateSection)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving performance review ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Gets all reviews for a team member.
        /// </summary>
        public async Task<List<PerformanceReview>> GetReviewsForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<PerformanceReview>();

            try
            {
                return await _context.PerformanceReviews
                    .Include(r => r.PerformanceReviewCycle)
                    .Where(r => r.TeamMemberId == teamMemberId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving reviews for team member ID: {0}", teamMemberId);
                return new List<PerformanceReview>();
            }
        }

        /// <summary>
        /// Updates a performance review.
        /// </summary>
        public async Task<bool> UpdatePerformanceReviewAsync(PerformanceReview review)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.PerformanceReviews
                    .Include(r => r.Sections)
                        .ThenInclude(s => s.Answers)
                    .FirstOrDefaultAsync(r => r.Id == review.Id);
                
                if (existing == null)
                {
                    _logger.Error("UpdatePerformanceReviewAsync: Review ID {0} not found", review.Id);
                    return false;
                }

                // Update review properties
                existing.Status = review.Status;
                existing.OverallRating = review.OverallRating;
                existing.ManagerSummary = review.ManagerSummary;
                existing.SelfAssessmentSummary = review.SelfAssessmentSummary;
                existing.SelfReviewSubmittedAt = review.SelfReviewSubmittedAt;
                existing.ManagerReviewSubmittedAt = review.ManagerReviewSubmittedAt;
                existing.SharedAt = review.SharedAt;
                existing.DiscussionDate = review.DiscussionDate;
                existing.OneOnOneId = review.OneOnOneId;

                // Update answers
                foreach (var section in review.Sections)
                {
                    var existingSection = existing.Sections.FirstOrDefault(s => s.Id == section.Id);
                    if (existingSection != null)
                    {
                        foreach (var answer in section.Answers)
                        {
                            var existingAnswer = existingSection.Answers.FirstOrDefault(a => a.Id == answer.Id);
                            if (existingAnswer != null)
                            {
                                existingAnswer.TextValue = answer.TextValue;
                                existingAnswer.RatingValue = answer.RatingValue;
                                existingAnswer.IsSelfAssessment = answer.IsSelfAssessment;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.Info("Updated performance review ID: {0}", review.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating performance review ID: {0}", review.Id);
                return false;
            }
        }

        /// <summary>
        /// Submits a self-assessment for a review.
        /// </summary>
        public async Task<bool> SubmitSelfAssessmentAsync(int reviewId)
        {
            if (_context == null) return false;

            try
            {
                var review = await _context.PerformanceReviews.FindAsync(reviewId);
                if (review == null) return false;

                review.Status = Common.Enums.ReviewStatus.SelfReviewComplete;
                review.SelfReviewSubmittedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.Info("Self-assessment submitted for review ID: {0}", reviewId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error submitting self-assessment for review ID: {0}", reviewId);
                return false;
            }
        }

        /// <summary>
        /// Submits a manager review.
        /// </summary>
        public async Task<bool> SubmitManagerReviewAsync(int reviewId)
        {
            if (_context == null) return false;

            try
            {
                var review = await _context.PerformanceReviews.FindAsync(reviewId);
                if (review == null) return false;

                review.Status = Common.Enums.ReviewStatus.ManagerReviewComplete;
                review.ManagerReviewSubmittedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.Info("Manager review submitted for review ID: {0}", reviewId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error submitting manager review for review ID: {0}", reviewId);
                return false;
            }
        }

        /// <summary>
        /// Shares a review with the employee.
        /// </summary>
        public async Task<bool> ShareReviewAsync(int reviewId)
        {
            if (_context == null) return false;

            try
            {
                var review = await _context.PerformanceReviews.FindAsync(reviewId);
                if (review == null) return false;

                review.Status = Common.Enums.ReviewStatus.Shared;
                review.SharedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.Info("Review shared for review ID: {0}", reviewId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sharing review ID: {0}", reviewId);
                return false;
            }
        }

        /// <summary>
        /// Marks a review as discussed.
        /// </summary>
        public async Task<bool> MarkReviewDiscussedAsync(int reviewId, int? oneOnOneId = null)
        {
            if (_context == null) return false;

            try
            {
                var review = await _context.PerformanceReviews.FindAsync(reviewId);
                if (review == null) return false;

                review.Status = Common.Enums.ReviewStatus.Discussed;
                review.DiscussionDate = DateTime.UtcNow;
                review.OneOnOneId = oneOnOneId;

                await _context.SaveChangesAsync();
                _logger.Info("Review marked as discussed for review ID: {0}", reviewId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error marking review as discussed ID: {0}", reviewId);
                return false;
            }
        }

        #endregion

        #region Kudos Operations

        /// <summary>
        /// Adds a new kudos to the database.
        /// </summary>
        public async Task<int> AddKudosAsync(Kudos kudos)
        {
            if (_context == null) return 0;

            try
            {
                kudos.UserId = GetCurrentUserId() ?? kudos.UserId;
                _context.Kudos.Add(kudos);
                await _context.SaveChangesAsync();
                _logger.Info("Added kudos ID: {0} for team member ID: {1}", kudos.Id, kudos.TeamMemberId);
                return kudos.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding kudos");
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing kudos.
        /// </summary>
        public async Task<bool> UpdateKudosAsync(Kudos kudos)
        {
            if (_context == null) return false;

            try
            {
                _context.Kudos.Update(kudos);
                await _context.SaveChangesAsync();
                _logger.Info("Updated kudos ID: {0}", kudos.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating kudos ID: {0}", kudos.Id);
                return false;
            }
        }

        /// <summary>
        /// Gets all kudos for the current user.
        /// </summary>
        public async Task<List<Kudos>> GetAllKudosAsync()
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                return await _context.Kudos
                    .AsNoTracking()
                    .Include(k => k.TeamMember)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting all kudos");
                return new List<Kudos>();
            }
        }

        /// <summary>
        /// Gets all kudos for a specific team member.
        /// </summary>
        public async Task<List<Kudos>> GetKudosForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                return await _context.Kudos
                    .AsNoTracking()
                    .Where(k => k.TeamMemberId == teamMemberId)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting kudos for team member ID: {0}", teamMemberId);
                return new List<Kudos>();
            }
        }

        /// <summary>
        /// Gets recent kudos that should be mentioned in meeting prep.
        /// </summary>
        public async Task<List<Kudos>> GetRecentKudosForMeetingPrepAsync(Guid teamMemberId, int daysSince = 30)
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-daysSince);
                return await _context.Kudos
                    .AsNoTracking()
                    .Where(k => k.TeamMemberId == teamMemberId &&
                                k.MentionInMeetingPrep &&
                                k.CreatedAt >= cutoff)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting kudos for meeting prep");
                return new List<Kudos>();
            }
        }

        #endregion
    }
}

#endif
