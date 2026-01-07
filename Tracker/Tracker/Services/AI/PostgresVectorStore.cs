using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Tracker.Classes;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// PostgreSQL implementation of IVectorStore using pgvector extension.
    /// 
    /// Features:
    /// - Native vector operations via pgvector
    /// - HNSW indexing for fast approximate nearest neighbor search
    /// - Row Level Security for multi-tenant isolation
    /// - Automatic session context setting for RLS
    /// 
    /// Prerequisites:
    /// - PostgreSQL 15+ with pgvector extension installed
    /// - Database schema from 04_CreateSchema_Vectors.sql deployed
    /// - RLS policies from 05_CreateRlsPolicies.sql applied
    /// </summary>
    public class PostgresVectorStore : IVectorStore
    {
        private readonly string _connectionString;
        private readonly Guid _organizationId;
        private readonly Guid? _userId;
        private readonly ILogger _logger;
        private bool _isInitialized;
        private bool _disposed;

        private const int EmbeddingDimensions = 1536; // OpenAI text-embedding-3-small
        private const string DefaultModel = "text-embedding-3-small";

        /// <summary>
        /// Creates a new PostgreSQL vector store.
        /// </summary>
        /// <param name="connectionString">PostgreSQL connection string</param>
        /// <param name="organizationId">Organization ID for multi-tenant scoping</param>
        /// <param name="userId">Optional user ID for RLS context</param>
        public PostgresVectorStore(string connectionString, Guid organizationId, Guid? userId = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _organizationId = organizationId;
            _userId = userId;
            _logger = LoggingManager.GetComponentLogger("PostgresVectorStore");
        }

        /// <summary>
        /// Creates a PostgresVectorStore from DatabaseSettings.
        /// </summary>
        public static PostgresVectorStore FromSettings(DatabaseSettings settings, Guid organizationId, Guid? userId = null)
        {
            var connectionString = settings.GetPostgresAuthConnectionString();
            return new PostgresVectorStore(connectionString, organizationId, userId);
        }

        #region IVectorStore Implementation

        public bool IsInitialized => _isInitialized;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
                return;

            try
            {
                await using var connection = await CreateConnectionAsync(cancellationToken);

                // Verify pgvector extension exists
                await using var cmd = new NpgsqlCommand(
                    "SELECT EXISTS(SELECT 1 FROM pg_extension WHERE extname = 'vector')",
                    connection);

                var hasVector = (bool)(await cmd.ExecuteScalarAsync(cancellationToken) ?? false);
                if (!hasVector)
                {
                    throw new InvalidOperationException(
                        "pgvector extension is not installed. Run: CREATE EXTENSION vector;");
                }

                // Verify vector_embeddings table exists
                cmd.CommandText = @"
                    SELECT EXISTS(
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'vector_embeddings'
                    )";
                var hasTable = (bool)(await cmd.ExecuteScalarAsync(cancellationToken) ?? false);
                if (!hasTable)
                {
                    throw new InvalidOperationException(
                        "vector_embeddings table does not exist. Run the schema scripts first.");
                }

                _isInitialized = true;
                _logger.Info("PostgresVectorStore initialized for organization {0}", _organizationId);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize PostgresVectorStore");
                throw;
            }
        }

        public async Task<Guid> StoreAsync(
            string entityType,
            string entityId,
            string content,
            float[] embedding,
            int chunkIndex = 0,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            ValidateEmbedding(embedding);

            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            var id = Guid.NewGuid();
            var contentHash = ComputeHash(content);
            var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null;

            // Use INSERT ... ON CONFLICT for upsert behavior
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO vector_embeddings (
                    id, organization_id, entity_type, entity_id, chunk_index, chunk_count,
                    content, content_hash, embedding, embedding_model, token_count
                )
                VALUES (
                    @id, @org_id, @entity_type, @entity_id, @chunk_index, 1,
                    @content, @content_hash, @embedding::vector, @model, @tokens
                )
                ON CONFLICT (organization_id, entity_type, entity_id, chunk_index)
                DO UPDATE SET
                    content = EXCLUDED.content,
                    content_hash = EXCLUDED.content_hash,
                    embedding = EXCLUDED.embedding,
                    modified_at = CURRENT_TIMESTAMP
                RETURNING id", connection);

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("org_id", _organizationId);
            cmd.Parameters.AddWithValue("entity_type", entityType);
            cmd.Parameters.AddWithValue("entity_id", entityId);
            cmd.Parameters.AddWithValue("chunk_index", chunkIndex);
            cmd.Parameters.AddWithValue("content", content);
            cmd.Parameters.AddWithValue("content_hash", contentHash);
            cmd.Parameters.AddWithValue("embedding", FormatVectorForPgvector(embedding));
            cmd.Parameters.AddWithValue("model", DefaultModel);
            cmd.Parameters.AddWithValue("tokens", EstimateTokenCount(content));

            var resultId = await cmd.ExecuteScalarAsync(cancellationToken);
            return (Guid)(resultId ?? id);
        }

        public async Task StoreBatchAsync(
            IEnumerable<VectorStoreEntry> entries,
            CancellationToken cancellationToken = default)
        {
            var entryList = entries.ToList();
            if (!entryList.Any())
                return;

            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            // Use COPY for bulk insert (much faster than individual inserts)
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                // First delete existing entries for these entities
                foreach (var group in entryList.GroupBy(e => (e.EntityType, e.EntityId)))
                {
                    await using var deleteCmd = new NpgsqlCommand(@"
                        DELETE FROM vector_embeddings 
                        WHERE organization_id = @org_id 
                          AND entity_type = @entity_type 
                          AND entity_id = @entity_id", connection, transaction);

                    deleteCmd.Parameters.AddWithValue("org_id", _organizationId);
                    deleteCmd.Parameters.AddWithValue("entity_type", group.Key.EntityType);
                    deleteCmd.Parameters.AddWithValue("entity_id", group.Key.EntityId);
                    await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // Then insert all new entries
                foreach (var entry in entryList)
                {
                    ValidateEmbedding(entry.Embedding);

                    await using var cmd = new NpgsqlCommand(@"
                        INSERT INTO vector_embeddings (
                            id, organization_id, entity_type, entity_id, chunk_index, chunk_count,
                            content, content_hash, embedding, embedding_model, token_count
                        )
                        VALUES (
                            @id, @org_id, @entity_type, @entity_id, @chunk_index, @chunk_count,
                            @content, @content_hash, @embedding::vector, @model, @tokens
                        )", connection, transaction);

                    var chunkCount = entryList.Count(e =>
                        e.EntityType == entry.EntityType && e.EntityId == entry.EntityId);

                    cmd.Parameters.AddWithValue("id", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("org_id", _organizationId);
                    cmd.Parameters.AddWithValue("entity_type", entry.EntityType);
                    cmd.Parameters.AddWithValue("entity_id", entry.EntityId);
                    cmd.Parameters.AddWithValue("chunk_index", entry.ChunkIndex);
                    cmd.Parameters.AddWithValue("chunk_count", chunkCount);
                    cmd.Parameters.AddWithValue("content", entry.Content);
                    cmd.Parameters.AddWithValue("content_hash", ComputeHash(entry.Content));
                    cmd.Parameters.AddWithValue("embedding", FormatVectorForPgvector(entry.Embedding));
                    cmd.Parameters.AddWithValue("model", DefaultModel);
                    cmd.Parameters.AddWithValue("tokens", EstimateTokenCount(entry.Content));

                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.Debug("Stored {0} embeddings in batch", entryList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpdateAsync(
            string entityType,
            string entityId,
            string content,
            float[] embedding,
            int chunkIndex = 0,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            // StoreAsync already handles upsert via ON CONFLICT
            await StoreAsync(entityType, entityId, content, embedding, chunkIndex, metadata, cancellationToken);
        }

        public async Task<List<VectorSearchResult>> SearchAsync(
            float[] queryEmbedding,
            int topK = 10,
            string[]? entityTypes = null,
            float minSimilarity = 0.5f,
            CancellationToken cancellationToken = default)
        {
            ValidateEmbedding(queryEmbedding);

            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            // Build query with optional entity type filter
            var sql = @"
                SELECT 
                    entity_type,
                    entity_id,
                    chunk_index,
                    content,
                    created_at,
                    1 - (embedding <=> @query_embedding::vector) AS similarity
                FROM vector_embeddings
                WHERE organization_id = @org_id
                  AND 1 - (embedding <=> @query_embedding::vector) >= @min_similarity";

            if (entityTypes != null && entityTypes.Length > 0)
            {
                sql += " AND entity_type = ANY(@entity_types)";
            }

            sql += @"
                ORDER BY embedding <=> @query_embedding::vector
                LIMIT @top_k";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("org_id", _organizationId);
            cmd.Parameters.AddWithValue("query_embedding", FormatVectorForPgvector(queryEmbedding));
            cmd.Parameters.AddWithValue("min_similarity", minSimilarity);
            cmd.Parameters.AddWithValue("top_k", topK);

            if (entityTypes != null && entityTypes.Length > 0)
            {
                cmd.Parameters.AddWithValue("entity_types", entityTypes);
            }

            var results = new List<VectorSearchResult>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new VectorSearchResult
                {
                    EntityType = reader.GetString(0),
                    EntityId = reader.GetString(1),
                    ChunkIndex = reader.GetInt32(2),
                    Content = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4),
                    Similarity = (float)reader.GetDouble(5)
                });
            }

            return results;
        }

        public async Task<List<VectorSearchResult>> SearchWithFilterAsync(
            float[] queryEmbedding,
            Dictionary<string, object> metadataFilters,
            int topK = 10,
            string[]? entityTypes = null,
            float minSimilarity = 0.5f,
            CancellationToken cancellationToken = default)
        {
            // For now, metadata filtering is not implemented in PostgreSQL
            // We would need a JSONB column and appropriate indexes
            _logger.Warn("SearchWithFilterAsync: Metadata filtering not yet implemented for PostgreSQL");
            return await SearchAsync(queryEmbedding, topK, entityTypes, minSimilarity, cancellationToken);
        }

        public async Task DeleteAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM vector_embeddings 
                WHERE organization_id = @org_id 
                  AND entity_type = @entity_type 
                  AND entity_id = @entity_id", connection);

            cmd.Parameters.AddWithValue("org_id", _organizationId);
            cmd.Parameters.AddWithValue("entity_type", entityType);
            cmd.Parameters.AddWithValue("entity_id", entityId);

            var deleted = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.Debug("Deleted {0} embeddings for {1}/{2}",
                deleted, entityType, entityId);
        }

        public async Task DeleteAllOfTypeAsync(
            string entityType,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM vector_embeddings 
                WHERE organization_id = @org_id 
                  AND entity_type = @entity_type", connection);

            cmd.Parameters.AddWithValue("org_id", _organizationId);
            cmd.Parameters.AddWithValue("entity_type", entityType);

            var deleted = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.Info("Deleted {0} embeddings of type {1}", deleted, entityType);
        }

        public async Task ClearAllAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM vector_embeddings 
                WHERE organization_id = @org_id", connection);

            cmd.Parameters.AddWithValue("org_id", _organizationId);

            var deleted = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.Warn("Cleared all {0} embeddings for organization {1}",
                deleted, _organizationId);
        }

        public async Task<int> CountAsync(string? entityType = null, CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            var sql = "SELECT COUNT(*) FROM vector_embeddings WHERE organization_id = @org_id";
            if (!string.IsNullOrEmpty(entityType))
            {
                sql += " AND entity_type = @entity_type";
            }

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("org_id", _organizationId);

            if (!string.IsNullOrEmpty(entityType))
            {
                cmd.Parameters.AddWithValue("entity_type", entityType);
            }

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result ?? 0);
        }

        public async Task<bool> ExistsAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            await using var cmd = new NpgsqlCommand(@"
                SELECT EXISTS(
                    SELECT 1 FROM vector_embeddings 
                    WHERE organization_id = @org_id 
                      AND entity_type = @entity_type 
                      AND entity_id = @entity_id
                )", connection);

            cmd.Parameters.AddWithValue("org_id", _organizationId);
            cmd.Parameters.AddWithValue("entity_type", entityType);
            cmd.Parameters.AddWithValue("entity_id", entityId);

            return (bool)(await cmd.ExecuteScalarAsync(cancellationToken) ?? false);
        }

        public async Task<List<string>> GetIndexedEntityIdsAsync(
            string entityType,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await SetSessionContextAsync(connection, cancellationToken);

            await using var cmd = new NpgsqlCommand(@"
                SELECT DISTINCT entity_id 
                FROM vector_embeddings 
                WHERE organization_id = @org_id 
                  AND entity_type = @entity_type
                ORDER BY entity_id", connection);

            cmd.Parameters.AddWithValue("org_id", _organizationId);
            cmd.Parameters.AddWithValue("entity_type", entityType);

            var results = new List<string>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }

        #endregion

        #region Helper Methods

        private async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        /// <summary>
        /// Sets the session context for Row Level Security.
        /// This tells PostgreSQL which organization the current user belongs to.
        /// </summary>
        private async Task SetSessionContextAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            await using var cmd = new NpgsqlCommand(
                "SELECT set_session_context(@org_id, @user_id)",
                connection);

            cmd.Parameters.AddWithValue("org_id", _organizationId);
            cmd.Parameters.AddWithValue("user_id", _userId.HasValue ? (object)_userId.Value : DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Formats a float array as a pgvector string literal.
        /// Example: [0.1, 0.2, 0.3] -> "[0.1,0.2,0.3]"
        /// </summary>
        private static string FormatVectorForPgvector(float[] embedding)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < embedding.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(embedding[i].ToString("G9", System.Globalization.CultureInfo.InvariantCulture));
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static void ValidateEmbedding(float[] embedding)
        {
            if (embedding == null || embedding.Length == 0)
                throw new ArgumentException("Embedding cannot be null or empty", nameof(embedding));

            if (embedding.Length != EmbeddingDimensions)
            {
                throw new ArgumentException(
                    $"Embedding must have {EmbeddingDimensions} dimensions, got {embedding.Length}",
                    nameof(embedding));
            }
        }

        private static string ComputeHash(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static int EstimateTokenCount(string content)
        {
            // Rough estimate: ~4 characters per token for English text
            return content.Length / 4;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
