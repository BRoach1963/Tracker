using Tracker.Classes;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Factory for creating IVectorStore instances based on database configuration.
    /// 
    /// Usage:
    /// <code>
    /// // For multi-tenant scenarios (recommended)
    /// var store = await VectorStoreFactory.CreateAsync(settings, organizationId, userId);
    /// 
    /// // For legacy/migration scenarios
    /// var legacyStore = VectorStoreFactory.CreateLegacy(settings);
    /// </code>
    /// </summary>
    public static class VectorStoreFactory
    {
        private static readonly ILogger _logger = LoggingManager.GetComponentLogger("VectorStoreFactory");

        /// <summary>
        /// Creates and initializes an IVectorStore instance for multi-tenant scenarios.
        /// </summary>
        /// <param name="settings">Database settings determining which provider to use</param>
        /// <param name="organizationId">Organization ID for data scoping</param>
        /// <param name="userId">Optional user ID for audit trails</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An initialized IVectorStore implementation</returns>
        public static async Task<IVectorStore> CreateAsync(
            DatabaseSettings settings,
            Guid organizationId,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var provider = settings.GetVectorStorageProvider();
            
            _logger.Info("Creating vector store for provider: {0}, org: {1}", provider, organizationId);

            IVectorStore store = provider switch
            {
                VectorStorageProvider.PostgreSQL => PostgresVectorStore.FromSettings(settings, organizationId, userId),
                VectorStorageProvider.SqlServer => SqlServerVectorStore.FromSettings(settings, organizationId, userId),
                _ => new LegacyVectorStoreAdapter()
            };

            await store.InitializeAsync(cancellationToken);
            return store;
        }

        /// <summary>
        /// Creates an IVectorStore without initialization (call InitializeAsync later).
        /// </summary>
        public static IVectorStore CreateUninitialized(
            DatabaseSettings settings,
            Guid organizationId,
            Guid? userId = null)
        {
            var provider = settings.GetVectorStorageProvider();
            
            return provider switch
            {
                VectorStorageProvider.PostgreSQL => PostgresVectorStore.FromSettings(settings, organizationId, userId),
                VectorStorageProvider.SqlServer => SqlServerVectorStore.FromSettings(settings, organizationId, userId),
                _ => new LegacyVectorStoreAdapter()
            };
        }

        /// <summary>
        /// Creates an IVectorStore instance based on the current database settings.
        /// For backwards compatibility - uses legacy adapter when org context not available.
        /// </summary>
        /// <param name="settings">Database settings determining which provider to use</param>
        /// <returns>An IVectorStore implementation appropriate for the database type</returns>
        [Obsolete("Use CreateAsync with organizationId for multi-tenant support")]
        public static IVectorStore Create(DatabaseSettings settings)
        {
            var provider = settings.GetVectorStorageProvider();
            
            _logger.Warn("Creating vector store without organization context - using legacy adapter");

            // Without organization context, we can only use the legacy adapter
            return new LegacyVectorStoreAdapter();
        }

        /// <summary>
        /// Gets the current vector storage provider from settings.
        /// </summary>
        public static VectorStorageProvider GetProvider(DatabaseSettings settings)
        {
            return settings.GetVectorStorageProvider();
        }

        /// <summary>
        /// Gets a friendly display name for the provider.
        /// </summary>
        public static string GetProviderDisplayName(DatabaseSettings settings)
        {
            return settings.GetVectorStorageProvider() switch
            {
                VectorStorageProvider.PostgreSQL => "PostgreSQL (pgvector)",
                VectorStorageProvider.SqlServer => "SQL Server",
                VectorStorageProvider.Legacy => "Legacy (local)",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Checks if the configured provider is available.
        /// </summary>
        public static async Task<bool> IsProviderAvailableAsync(
            DatabaseSettings settings,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var provider = settings.GetVectorStorageProvider();

                if (provider == VectorStorageProvider.Legacy)
                    return true; // Legacy is always available

                // Create a test store with placeholder org ID
                var testStore = CreateUninitialized(settings, Guid.Empty);
                await testStore.InitializeAsync(cancellationToken);
                testStore.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn("Vector storage provider not available: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets the singleton legacy vector store instance.
        /// Provided for backwards compatibility with existing code.
        /// </summary>
        /// <remarks>
        /// Prefer using CreateAsync(settings, orgId) for new code.
        /// </remarks>
        public static VectorStore LegacyInstance => VectorStore.Instance;
    }
}
