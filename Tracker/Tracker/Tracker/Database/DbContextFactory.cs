using Microsoft.EntityFrameworkCore;
using Tracker.Classes;

namespace Tracker.Database
{
    /// <summary>
    /// Factory for creating TrackerDbContext instances.
    /// Implements the proper pattern for EF Core contexts - short-lived, unit-of-work scoped.
    /// </summary>
    public interface ITrackerDbContextFactory
    {
        /// <summary>
        /// Creates a new DbContext instance configured with current settings.
        /// </summary>
        TrackerDbContext CreateContext();

        /// <summary>
        /// Updates the database settings used for creating new contexts.
        /// </summary>
        void UpdateSettings(DatabaseSettings settings);
    }

    /// <summary>
    /// Default implementation of the DbContext factory.
    /// </summary>
    public class TrackerDbContextFactory : ITrackerDbContextFactory
    {
        private static readonly Lazy<TrackerDbContextFactory> _instance = 
            new(() => new TrackerDbContextFactory());
        
        private DatabaseSettings _settings;
        private readonly object _lock = new();

        public static TrackerDbContextFactory Instance => _instance.Value;

        private TrackerDbContextFactory()
        {
            _settings = new DatabaseSettings { Type = DatabaseType.SQLite };
        }

        public TrackerDbContext CreateContext()
        {
            lock (_lock)
            {
                return new TrackerDbContext(_settings);
            }
        }

        public void UpdateSettings(DatabaseSettings settings)
        {
            lock (_lock)
            {
                _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            }
        }
    }
}

