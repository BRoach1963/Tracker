using Tracker.Database;

namespace Tracker.Classes
{
    /// <summary>
    /// Factory interface for creating TrackerDbContext instances with proper configuration.
    /// 
    /// This abstraction allows:
    /// - Different context configurations for different database providers
    /// - Proper RLS context setup for PostgreSQL
    /// - User/organization scoping
    /// - Testability via mock implementations
    /// </summary>
    public interface ITrackerDbContextFactory
    {
        /// <summary>
        /// Gets the current database settings.
        /// </summary>
        DatabaseSettings Settings { get; }

        /// <summary>
        /// Gets the database type.
        /// </summary>
        DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the current organization ID (if authenticated).
        /// </summary>
        Guid? OrganizationId { get; }

        /// <summary>
        /// Gets the current user ID (if authenticated).
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Creates a new DbContext instance with the current user/org context.
        /// </summary>
        TrackerDbContext CreateContext();

        /// <summary>
        /// Creates a new DbContext instance for a specific user.
        /// Used for background operations or impersonation.
        /// </summary>
        /// <param name="userId">The user ID to set for RLS filtering</param>
        TrackerDbContext CreateContextForUser(Guid userId);

        /// <summary>
        /// Creates a new DbContext instance with admin privileges (no RLS filtering).
        /// Use with caution - only for admin operations.
        /// </summary>
        TrackerDbContext CreateAdminContext();

        /// <summary>
        /// Sets the current user and organization context.
        /// Called after authentication.
        /// </summary>
        void SetUserContext(Guid userId, Guid organizationId, string role = "manager");

        /// <summary>
        /// Clears the current user context.
        /// Called on logout.
        /// </summary>
        void ClearUserContext();
    }

    /// <summary>
    /// Default implementation of ITrackerDbContextFactory.
    /// Manages database settings and user context for creating properly configured DbContext instances.
    /// </summary>
    public class TrackerDbContextFactory : ITrackerDbContextFactory
    {
        private static readonly Lazy<TrackerDbContextFactory> _instance =
            new(() => new TrackerDbContextFactory(new DatabaseSettings { Type = DatabaseType.SQLite }));

        /// <summary>
        /// Gets the singleton instance of the factory.
        /// </summary>
        public static TrackerDbContextFactory Instance => _instance.Value;

        private DatabaseSettings _settings;
        private Guid? _userId;
        private Guid? _organizationId;
        private string _role = "manager";
        private readonly object _lock = new();

        public TrackerDbContextFactory(DatabaseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Updates the database settings used for creating new contexts.
        /// </summary>
        public void UpdateSettings(DatabaseSettings settings)
        {
            lock (_lock)
            {
                _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            }
        }

        /// <inheritdoc />
        public DatabaseSettings Settings => _settings;

        /// <inheritdoc />
        public DatabaseType DatabaseType => _settings.Type;

        /// <inheritdoc />
        public Guid? OrganizationId => _organizationId;

        /// <inheritdoc />
        public Guid? UserId => _userId;

        /// <inheritdoc />
        public TrackerDbContext CreateContext()
        {
            if (_settings.Type == DatabaseType.PostgreSQL && _userId.HasValue)
            {
                // PostgreSQL with RLS requires user context
                return new TrackerDbContext(_settings, _userId.Value);
            }

            // SQLite/SQL Server use local user filtering
            return new TrackerDbContext(_settings);
        }

        /// <inheritdoc />
        public TrackerDbContext CreateContextForUser(Guid userId)
        {
            if (_settings.Type == DatabaseType.PostgreSQL)
            {
                return new TrackerDbContext(_settings, userId);
            }

            // For non-PostgreSQL, still create context but userId is informational
            var context = new TrackerDbContext(_settings);
            return context;
        }

        /// <inheritdoc />
        public TrackerDbContext CreateAdminContext()
        {
            // Create context without user filtering (for admin operations)
            // Warning: This bypasses RLS in PostgreSQL
            return new TrackerDbContext(_settings);
        }

        /// <inheritdoc />
        public void SetUserContext(Guid userId, Guid organizationId, string role = "manager")
        {
            _userId = userId;
            _organizationId = organizationId;
            _role = role;
        }

        /// <inheritdoc />
        public void ClearUserContext()
        {
            _userId = null;
            _organizationId = null;
            _role = "manager";
        }
    }
}
