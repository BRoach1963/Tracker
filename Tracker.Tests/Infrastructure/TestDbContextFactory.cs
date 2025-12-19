using System.IO;
using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.Database;

namespace Tracker.Tests.Infrastructure
{
    /// <summary>
    /// Factory for creating test database contexts.
    /// Uses a file-based SQLite database in the temp folder.
    /// </summary>
    public class TestDbContextFactory : IDbContextFactory<TrackerDbContext>, IDisposable
    {
        private readonly string _tempDbPath;
        private bool _initialized;

        public TestDbContextFactory()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"TrackerTest_{Guid.NewGuid():N}.db");
        }

        public TrackerDbContext CreateDbContext()
        {
            return new TestTrackerDbContext(_tempDbPath);
        }

        public async Task InitializeAsync()
        {
            if (!_initialized)
            {
                using var context = CreateDbContext();
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                _initialized = true;
            }
        }

        public async Task ResetAsync()
        {
            using var context = CreateDbContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempDbPath))
                {
                    File.Delete(_tempDbPath);
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Test-specific DbContext that uses a custom SQLite path.
    /// </summary>
    public class TestTrackerDbContext : TrackerDbContext
    {
        private readonly string _dbPath;

        public TestTrackerDbContext(string dbPath) : base()
        {
            _dbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Enable foreign keys in SQLite connection string
            optionsBuilder.UseSqlite($"Data Source={_dbPath};Foreign Keys=True");
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}
