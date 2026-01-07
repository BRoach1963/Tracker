using System.IO;
using Microsoft.Data.Sqlite;
using Tracker.Classes;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Utility to migrate vector embeddings from legacy SQLite storage to
    /// PostgreSQL (pgvector) or SQL Server (VARBINARY) storage.
    /// 
    /// Usage:
    /// <code>
    /// var migrator = new VectorStoreMigrator(targetStore, orgId);
    /// var stats = await migrator.MigrateFromLegacyAsync(progress);
    /// </code>
    /// </summary>
    public class VectorStoreMigrator
    {
        private readonly IVectorStore _targetStore;
        private readonly Guid _organizationId;
        private readonly ILogger _logger;

        private static readonly string LegacyDbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tracker", "vectors.db");

        public VectorStoreMigrator(IVectorStore targetStore, Guid organizationId)
        {
            _targetStore = targetStore ?? throw new ArgumentNullException(nameof(targetStore));
            _organizationId = organizationId;
            _logger = LoggingManager.GetComponentLogger("VectorStoreMigrator");
        }

        /// <summary>
        /// Creates a migrator from database settings.
        /// </summary>
        public static async Task<VectorStoreMigrator> CreateAsync(
            DatabaseSettings settings,
            Guid organizationId,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var targetStore = await VectorStoreFactory.CreateAsync(settings, organizationId, userId, cancellationToken);
            return new VectorStoreMigrator(targetStore, organizationId);
        }

        /// <summary>
        /// Migrates all embeddings from the legacy SQLite vector store.
        /// </summary>
        /// <param name="progress">Optional progress callback (message, current, total)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Migration statistics</returns>
        public async Task<MigrationStats> MigrateFromLegacyAsync(
            Action<string, int, int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var stats = new MigrationStats();

            if (!File.Exists(LegacyDbPath))
            {
                _logger.Info("No legacy vector database found at {0}", LegacyDbPath);
                return stats;
            }

            _logger.Info("Starting migration from legacy vector store: {0}", LegacyDbPath);
            progress?.Invoke("Reading legacy embeddings...", 0, 0);

            try
            {
                // Read all embeddings from legacy store
                var legacyEntries = await ReadLegacyEntriesAsync(cancellationToken);
                stats.TotalLegacy = legacyEntries.Count;

                if (legacyEntries.Count == 0)
                {
                    _logger.Info("No embeddings found in legacy store");
                    return stats;
                }

                _logger.Info("Found {0} embeddings in legacy store", legacyEntries.Count);
                progress?.Invoke($"Migrating {legacyEntries.Count} embeddings...", 0, legacyEntries.Count);

                // Group by entity type for efficient batch operations
                var grouped = legacyEntries.GroupBy(e => e.EntityType);

                foreach (var group in grouped)
                {
                    var entityType = group.Key;
                    var entries = group.ToList();

                    try
                    {
                        // Convert to VectorStoreEntry for batch insert
                        var vectorEntries = entries.Select(e => new VectorStoreEntry
                        {
                            EntityType = e.EntityType,
                            EntityId = e.EntityId,
                            ChunkIndex = e.ChunkIndex,
                            Content = e.Content,
                            Embedding = e.Embedding
                        }).ToList();

                        await _targetStore.StoreBatchAsync(vectorEntries, cancellationToken);
                        stats.Migrated += entries.Count;

                        _logger.Debug("Migrated {0} {1} embeddings", entries.Count, entityType);
                    }
                    catch (Exception ex)
                    {
                        stats.Failed += entries.Count;
                        _logger.Warn("Failed to migrate {0} embeddings: {1}", entityType, ex.Message);
                    }

                    progress?.Invoke($"Migrating {entityType}...", stats.Migrated, stats.TotalLegacy);
                }

                stats.Duration = DateTime.Now - stats.StartTime;
                _logger.Info("Migration complete: {0}/{1} migrated, {2} failed in {3:F1}s",
                    stats.Migrated, stats.TotalLegacy, stats.Failed, stats.Duration.TotalSeconds);

                progress?.Invoke($"Migration complete: {stats.Migrated} embeddings", stats.Migrated, stats.TotalLegacy);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Migration failed");
                stats.Error = ex.Message;
                throw;
            }
        }

        /// <summary>
        /// Reads all entries from the legacy SQLite vector store.
        /// </summary>
        private async Task<List<LegacyVectorEntry>> ReadLegacyEntriesAsync(CancellationToken cancellationToken)
        {
            var entries = new List<LegacyVectorEntry>();
            var connectionString = $"Data Source={LegacyDbPath}";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new SqliteCommand(@"
                SELECT doc_id, chunk_index, content, embedding, metadata
                FROM document_chunks
                ORDER BY doc_id, chunk_index", connection);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var docId = reader.GetString(0);
                var chunkIndex = reader.GetInt32(1);
                var content = reader.GetString(2);
                var embeddingBlob = (byte[])reader["embedding"];

                // Parse entity type from doc_id (format: "type:id" or just "id")
                var (entityType, entityId) = ParseDocId(docId);

                var embedding = DeserializeEmbedding(embeddingBlob);
                if (embedding == null)
                {
                    _logger.Warn("Failed to deserialize embedding for {0}", docId);
                    continue;
                }

                entries.Add(new LegacyVectorEntry
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    ChunkIndex = chunkIndex,
                    Content = content,
                    Embedding = embedding
                });
            }

            return entries;
        }

        /// <summary>
        /// Parses a legacy doc_id into entity type and entity ID.
        /// </summary>
        private static (string EntityType, string EntityId) ParseDocId(string docId)
        {
            // Check for common entity type prefixes
            var prefixes = new[]
            {
                ("team_member:", "TeamMember"),
                ("meeting:", "Meeting"),
                ("task:", "Task"),
                ("goal:", "Goal"),
                ("okr:", "Objective"),
                ("kpi:", "KeyPerformanceIndicator"),
                ("project:", "Project"),
                ("pulse_survey:", "PulseSurvey"),
                ("note:", "Note"),
                ("document:", "Document")
            };

            foreach (var (prefix, type) in prefixes)
            {
                if (docId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return (type, docId[prefix.Length..]);
                }
            }

            // Try to infer from ID format
            if (Guid.TryParse(docId, out _))
            {
                return ("Unknown", docId);
            }

            // Default fallback
            return ("Unknown", docId);
        }

        /// <summary>
        /// Deserializes embedding bytes from legacy format (float array).
        /// </summary>
        private static float[]? DeserializeEmbedding(byte[] bytes)
        {
            try
            {
                var count = bytes.Length / sizeof(float);
                var embedding = new float[count];
                Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
                return embedding;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a legacy vector store exists.
        /// </summary>
        public static bool HasLegacyStore()
        {
            return File.Exists(LegacyDbPath);
        }

        /// <summary>
        /// Gets the size of the legacy vector store in bytes.
        /// </summary>
        public static long GetLegacyStoreSize()
        {
            if (!File.Exists(LegacyDbPath))
                return 0;

            return new FileInfo(LegacyDbPath).Length;
        }

        /// <summary>
        /// Gets the number of entries in the legacy vector store.
        /// </summary>
        public static async Task<int> GetLegacyEntryCountAsync()
        {
            if (!File.Exists(LegacyDbPath))
                return 0;

            try
            {
                var connectionString = $"Data Source={LegacyDbPath}";
                await using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                await using var cmd = new SqliteCommand("SELECT COUNT(*) FROM document_chunks", connection);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Deletes the legacy vector store after successful migration.
        /// </summary>
        public static void DeleteLegacyStore()
        {
            if (File.Exists(LegacyDbPath))
            {
                File.Delete(LegacyDbPath);
            }
        }

        #region Nested Types

        private class LegacyVectorEntry
        {
            public string EntityType { get; set; } = "";
            public string EntityId { get; set; } = "";
            public int ChunkIndex { get; set; }
            public string Content { get; set; } = "";
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }

        #endregion
    }

    /// <summary>
    /// Statistics from a vector store migration.
    /// </summary>
    public class MigrationStats
    {
        public DateTime StartTime { get; } = DateTime.Now;
        public int TotalLegacy { get; set; }
        public int Migrated { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Error { get; set; }

        public bool IsSuccess => Failed == 0 && string.IsNullOrEmpty(Error);
    }
}
