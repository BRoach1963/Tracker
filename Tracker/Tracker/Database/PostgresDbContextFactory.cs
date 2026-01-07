using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.Logging;

namespace Tracker.Database
{
    /// <summary>
    /// Factory for creating PostgreSQL DbContexts with Row-Level Security (RLS) user context.
    /// Each context created by this factory will have the specified user ID set for RLS filtering.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// var factory = new PostgresDbContextFactory(settings, userId);
    /// using var context = factory.CreateContext();
    /// // All queries will be filtered by RLS policies for this user
    /// var teamMembers = await context.TeamMembers.ToListAsync();
    /// </code>
    /// </remarks>
    public class PostgresDbContextFactory : IDisposable
    {
        private readonly DatabaseSettings _settings;
        private readonly Guid _userId;
        private readonly ILogger _logger;
        private bool _disposed;

        /// <summary>
        /// Creates a new factory for PostgreSQL contexts with RLS.
        /// </summary>
        /// <param name="settings">PostgreSQL database settings.</param>
        /// <param name="userId">The user ID to use for RLS filtering.</param>
        public PostgresDbContextFactory(DatabaseSettings settings, Guid userId)
        {
            if (settings.Type != DatabaseType.PostgreSQL)
            {
                throw new ArgumentException("PostgresDbContextFactory requires PostgreSQL database settings.");
            }

            _settings = settings;
            _userId = userId;
            _logger = LoggingManager.GetComponentLogger("PostgresFactory");
            
            _logger.Debug("Created PostgresDbContextFactory for user: {0}", userId);
        }

        /// <summary>
        /// The user ID used for RLS filtering.
        /// </summary>
        public Guid UserId => _userId;

        /// <summary>
        /// Creates a new DbContext with RLS user context configured.
        /// </summary>
        /// <returns>A new TrackerDbContext with RLS interceptor for this user.</returns>
        public TrackerDbContext CreateContext()
        {
            ThrowIfDisposed();
            
            _logger.Debug("Creating context for user: {0}", _userId);
            return new TrackerDbContext(_settings, _userId);
        }

        /// <summary>
        /// Tests the database connection with RLS context.
        /// </summary>
        /// <returns>True if connection succeeds, false otherwise.</returns>
        public async Task<bool> TestConnectionAsync()
        {
            ThrowIfDisposed();
            
            try
            {
                using var context = CreateContext();
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Connection test failed for user: {0}", _userId);
                return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PostgresDbContextFactory));
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Factory for creating PostgreSQL DbContexts without RLS context.
    /// Used for authentication operations before user is known.
    /// </summary>
    public class PostgresAuthContextFactory
    {
        private readonly DatabaseSettings _settings;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a factory for authentication contexts (no RLS).
        /// </summary>
        /// <param name="settings">PostgreSQL database settings.</param>
        public PostgresAuthContextFactory(DatabaseSettings settings)
        {
            if (settings.Type != DatabaseType.PostgreSQL)
            {
                throw new ArgumentException("PostgresAuthContextFactory requires PostgreSQL database settings.");
            }

            _settings = settings;
            _logger = LoggingManager.GetComponentLogger("PostgresAuthFactory");
        }

        /// <summary>
        /// Creates a DbContext for authentication operations.
        /// This context does NOT have RLS filtering - use only for login/register.
        /// </summary>
        /// <returns>A TrackerDbContext without RLS interceptor.</returns>
        public TrackerDbContext CreateContext()
        {
            _logger.Debug("Creating auth context (no RLS)");
            return new TrackerDbContext(_settings);
        }

        /// <summary>
        /// Looks up a user by email for authentication.
        /// </summary>
        /// <param name="email">Email to search for.</param>
        /// <returns>User information if found, null otherwise.</returns>
        public async Task<(Guid? id, string? email, string? displayName, string? passwordHash)?> LookupUserByEmailAsync(string email)
        {
            _logger.Info("Looking up user by email: {0}", email);
            System.Diagnostics.Debug.WriteLine($"=== LookupUserByEmailAsync START: {email} ===");
            
            try
            {
                using var context = CreateContext();
                _logger.Info("Context created, querying Users table...");
                System.Diagnostics.Debug.WriteLine("=== LookupUserByEmailAsync: Context created ===");
                
                // Use projection to avoid loading DateTime columns (which cause InvalidCastException
                // with PostgreSQL timestamptz if EnableLegacyTimestampBehavior isn't set early enough)
                var result = await context.Users
                    .AsNoTracking()
                    .IgnoreQueryFilters() // Important: bypass RLS filters for auth lookup
                    .Where(u => u.Email.ToLower() == email.ToLowerInvariant())
                    .Select(u => new 
                    {
                        u.SupabaseUserId,
                        u.Email,
                        u.DisplayName,
                        u.PasswordHash
                    })
                    .FirstOrDefaultAsync();

                System.Diagnostics.Debug.WriteLine($"=== LookupUserByEmailAsync: Query complete, result is {(result == null ? "NULL" : "FOUND")} ===");

                if (result == null)
                {
                    _logger.Warn("User not found in database: {0}", email);
                    System.Diagnostics.Debug.WriteLine($"=== LookupUserByEmailAsync: User NOT FOUND ===");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"=== LookupUserByEmailAsync: Found Email={result.Email}, SupabaseId={result.SupabaseUserId}, HasHash={!string.IsNullOrEmpty(result.PasswordHash)} ===");
                _logger.Info("Found user: Email={0}, SupabaseUserId={1}, HasPassword={2}",
                    result.Email, result.SupabaseUserId, !string.IsNullOrEmpty(result.PasswordHash));

                // Return SupabaseUserId as the auth ID (used for RLS)
                return (
                    result.SupabaseUserId,
                    result.Email,
                    result.DisplayName,
                    result.PasswordHash
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== LookupUserByEmailAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                _logger.Error("EXCEPTION in LookupUserByEmailAsync: {0}: {1}", ex.GetType().Name, ex.Message);
                _logger.Exception(ex, "Failed to lookup user by email: {0}", email);
                throw; // Re-throw so we can see the actual error
            }
        }

        /// <summary>
        /// Creates a new user in the database.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="passwordHash">BCrypt-hashed password.</param>
        /// <param name="displayName">Optional display name.</param>
        /// <returns>The new user's ID if successful, null otherwise.</returns>
        public async Task<Guid?> CreateUserAsync(string email, string passwordHash, string? displayName)
        {
            try
            {
                using var context = CreateContext();
                
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO users (id, email, password_hash, display_name, created_at, updated_at)
                    VALUES (@id, @email, @passwordHash, @displayName, @now, @now)
                    ON CONFLICT (email) DO NOTHING
                    RETURNING id";
                
                var newId = Guid.NewGuid();
                
                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = newId;
                cmd.Parameters.Add(idParam);

                var emailParam = cmd.CreateParameter();
                emailParam.ParameterName = "@email";
                emailParam.Value = email.ToLowerInvariant();
                cmd.Parameters.Add(emailParam);

                var hashParam = cmd.CreateParameter();
                hashParam.ParameterName = "@passwordHash";
                hashParam.Value = passwordHash;
                cmd.Parameters.Add(hashParam);

                var nameParam = cmd.CreateParameter();
                nameParam.ParameterName = "@displayName";
                nameParam.Value = (object?)displayName ?? DBNull.Value;
                cmd.Parameters.Add(nameParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.UtcNow;
                cmd.Parameters.Add(nowParam);

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return (Guid)result;
                }

                // Conflict - email already exists
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to create user: {0}", email);
                return null;
            }
        }

        /// <summary>
        /// Gets a user by their ID.
        /// </summary>
        /// <param name="userId">The user ID to look up.</param>
        /// <returns>User information if found, null otherwise.</returns>
        public async Task<Services.Auth.AuthenticatedUser?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                using var context = CreateContext();
                
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, email, display_name, last_login_at 
                    FROM users 
                    WHERE id = @id";
                
                var param = cmd.CreateParameter();
                param.ParameterName = "@id";
                param.Value = userId;
                cmd.Parameters.Add(param);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Services.Auth.AuthenticatedUser
                    {
                        Id = reader.GetGuid(0),
                        Email = reader.GetString(1),
                        DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                        LastLoginAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to get user by ID: {0}", userId);
                return null;
            }
        }

        /// <summary>
        /// Updates the last login timestamp for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        public async Task UpdateLastLoginAsync(Guid userId)
        {
            try
            {
                using var context = CreateContext();
                
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE users 
                    SET last_login_at = @now, updated_at = @now
                    WHERE id = @id";
                
                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = userId;
                cmd.Parameters.Add(idParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.UtcNow;
                cmd.Parameters.Add(nowParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to update last login for user: {0}", userId);
            }
        }
    }
}
