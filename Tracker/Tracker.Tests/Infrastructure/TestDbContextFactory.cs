using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.Database;

namespace Tracker.Tests.Infrastructure
{
    /// <summary>
    /// Factory for creating in-memory SQLite test database contexts.
    /// </summary>
    public class TestDbContextFactory : IDbContextFactory<TrackerDbContext>, IDisposable
    {
        private readonly string _connectionString;
        private readonly DatabaseSettings _settings;
        private Microsoft.Data.Sqlite.SqliteConnection? _connection;

        public TestDbContextFactory(string? databaseName = null)
        {
            var dbName = databaseName ?? $"TestDb_{Guid.NewGuid():N}";
            _connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
            _settings = new DatabaseSettings
            {
                Type = DatabaseType.SQLite,
                SQLiteFileName = dbName
            };
        }

        /// <summary>
        /// Creates a new DbContext instance. For in-memory SQLite, we need to keep
        /// at least one connection open to preserve the database.
        /// </summary>
        public TrackerDbContext CreateDbContext()
        {
            // Keep a connection open to preserve in-memory database
            if (_connection == null)
            {
                _connection = new Microsoft.Data.Sqlite.SqliteConnection(_connectionString);
                _connection.Open();
            }

            var options = new DbContextOptionsBuilder<TrackerDbContext>()
                .UseSqlite(_connectionString)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new TrackerDbContext(_settings, options);
            return context;
        }

        /// <summary>
        /// Creates the database schema.
        /// </summary>
        public async Task InitializeAsync()
        {
            using var context = CreateDbContext();
            await context.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// Resets the database to a clean state.
        /// </summary>
        public async Task ResetAsync()
        {
            using var context = CreateDbContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }
    }
}

