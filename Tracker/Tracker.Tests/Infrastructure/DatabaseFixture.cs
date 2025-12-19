using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.Database;
using Tracker.DataModels;

namespace Tracker.Tests.Infrastructure
{
    /// <summary>
    /// Shared database fixture for integration tests.
    /// Creates a single in-memory database that persists across all tests in a collection.
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        public TestDbContextFactory Factory { get; private set; }
        public User TestUser { get; private set; }
        public int TestUserId => TestUser.Id;

        public DatabaseFixture()
        {
            Factory = new TestDbContextFactory();
            Factory.InitializeAsync().GetAwaiter().GetResult();

            // Create a test user
            using var context = Factory.CreateDbContext();
            TestUser = new User
            {
                Username = "testuser",
                DisplayName = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(TestUser);
            context.SaveChanges();
        }

        public void Dispose()
        {
            Factory?.Dispose();
        }

        /// <summary>
        /// Creates a fresh DbContext for testing.
        /// </summary>
        public TrackerDbContext CreateContext() => Factory.CreateDbContext();

        /// <summary>
        /// Resets the database to initial state with just the test user.
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            await Factory.ResetAsync();
            
            using var context = Factory.CreateDbContext();
            TestUser = new User
            {
                Username = "testuser",
                DisplayName = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(TestUser);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Collection definition for tests that share the database fixture.
    /// </summary>
    [Xunit.CollectionDefinition("Database")]
    public class DatabaseCollection : Xunit.ICollectionFixture<DatabaseFixture>
    {
    }
}
