using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Tracker.Classes;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// SQL Server implementation of IVectorStore using VARBINARY for vector storage.
    /// 
    /// SQL Server does not have native vector operations like pgvector, so:
    /// - Vectors are stored as serialized float arrays in VARBINARY(MAX)
    /// - Cosine similarity calculations are performed in application code
    /// - For large-scale deployments, consider Azure Cognitive Search
    /// 
    /// Prerequisites:
    /// - SQL Server 2016+ 
    /// - Database schema from 07_CreateVectorEmbeddings.sql deployed
    /// </summary>
    public class SqlServerVectorStore : IVectorStore
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
        /// Creates a new SQL Server vector store.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <param name="organizationId">Organization ID for multi-tenant scoping</param>
        /// <param name="userId">Optional user ID for audit trails</param>
        public SqlServerVectorStore(string connectionString, Guid organizationId, Guid? userId = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _organizationId = organizationId;
            _userId = userId;
            _logger = LoggingManager.GetComponentLogger("SqlServerVectorStore");
        }

        /// <summary>
        /// Creates a SqlServerVectorStore from DatabaseSettings.
        /// </summary>
        public static SqlServerVectorStore FromSettings(DatabaseSettings settings, Guid organizationId, Guid? userId = null)
        {
            var connectionString = settings.GetConnectionString();
            return new SqlServerVectorStore(connectionString, organizationId, userId);
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

                // Verify VectorEmbeddings table exists
                await using var cmd = new SqlCommand(@"
                    SELECT CASE WHEN EXISTS(
                        SELECT 1 FROM sys.tables WHERE name = 'VectorEmbeddings'
                    ) THEN 1 ELSE 0 END", connection);

                var hasTable = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0) == 1;
                if (!hasTable)
                {
                    throw new InvalidOperationException(
                        "VectorEmbeddings table does not exist. Run 07_CreateVectorEmbeddings.sql first.");
                }

                _isInitialized = true;
                _logger.Info("SqlServerVectorStore initialized for organization {0}", _organizationId);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize SqlServerVectorStore");
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

            var id = Guid.NewGuid();
            var contentHash = ComputeHash(content);
            var embeddingBytes = SerializeEmbedding(embedding);
            var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null;

            // Use stored procedure for upsert
            await using var cmd = new SqlCommand("sp_StoreEmbedding", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@OrganizationId", _organizationId);
            cmd.Parameters.AddWithValue("@EntityType", entityType);
            cmd.Parameters.AddWithValue("@EntityId", entityId);
            cmd.Parameters.AddWithValue("@ChunkIndex", chunkIndex);
            cmd.Parameters.AddWithValue("@ChunkCount", 1);
            cmd.Parameters.AddWithValue("@Content", content);
            cmd.Parameters.AddWithValue("@Embedding", embeddingBytes);
            cmd.Parameters.AddWithValue("@EmbeddingDimensions", EmbeddingDimensions);
            cmd.Parameters.AddWithValue("@EmbeddingModel", DefaultModel);
            cmd.Parameters.AddWithValue("@TokenCount", EstimateTokenCount(content));
            cmd.Parameters.AddWithValue("@Metadata", metadataJson ?? (object)DBNull.Value);

            var outputId = cmd.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier);
            outputId.Direction = System.Data.ParameterDirection.Output;

            await cmd.ExecuteNonQueryAsync(cancellationToken);

            return (Guid)(outputId.Value ?? id);
        }

        public async Task StoreBatchAsync(
            IEnumerable<VectorStoreEntry> entries,
            CancellationToken cancellationToken = default)
        {
            var entryList = entries.ToList();
            if (!entryList.Any())
                return;

            await using var connection = await CreateConnectionAsync(cancellationToken);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                // First delete existing entries for these entities
                foreach (var group in entryList.GroupBy(e => (e.EntityType, e.EntityId)))
                {
                    await using var deleteCmd = new SqlCommand(@"
                        DELETE FROM VectorEmbeddings 
                        WHERE OrganizationId = @OrgId 
                          AND EntityType = @EntityType 
                          AND EntityId = @EntityId", connection, transaction);

                    deleteCmd.Parameters.AddWithValue("@OrgId", _organizationId);
                    deleteCmd.Parameters.AddWithValue("@EntityType", group.Key.EntityType);
                    deleteCmd.Parameters.AddWithValue("@EntityId", group.Key.EntityId);
                    await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // Then insert all new entries
                foreach (var entry in entryList)
                {
                    ValidateEmbedding(entry.Embedding);

                    var chunkCount = entryList.Count(e =>
                        e.EntityType == entry.EntityType && e.EntityId == entry.EntityId);

                    await using var cmd = new SqlCommand(@"
                        INSERT INTO VectorEmbeddings (
                            Id, OrganizationId, EntityType, EntityId, ChunkIndex, ChunkCount,
                            Content, ContentHash, Embedding, EmbeddingDimensions, EmbeddingModel, TokenCount
                        )
                        VALUES (
                            @Id, @OrgId, @EntityType, @EntityId, @ChunkIndex, @ChunkCount,
                            @Content, @ContentHash, @Embedding, @Dimensions, @Model, @Tokens
                        )", connection, transaction);

                    cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("@OrgId", _organizationId);
                    cmd.Parameters.AddWithValue("@EntityType", entry.EntityType);
                    cmd.Parameters.AddWithValue("@EntityId", entry.EntityId);
                    cmd.Parameters.AddWithValue("@ChunkIndex", entry.ChunkIndex);
                    cmd.Parameters.AddWithValue("@ChunkCount", chunkCount);
                    cmd.Parameters.AddWithValue("@Content", entry.Content);
                    cmd.Parameters.AddWithValue("@ContentHash", ComputeHash(entry.Content));
                    cmd.Parameters.AddWithValue("@Embedding", SerializeEmbedding(entry.Embedding));
                    cmd.Parameters.AddWithValue("@Dimensions", EmbeddingDimensions);
                    cmd.Parameters.AddWithValue("@Model", DefaultModel);
                    cmd.Parameters.AddWithValue("@Tokens", EstimateTokenCount(entry.Content));

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
            // StoreAsync already handles upsert via stored procedure
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

            // Build query to retrieve all embeddings for the org (filtered by entity types if specified)
            var sql = @"
                SELECT 
                    EntityType,
                    EntityId,
                    ChunkIndex,
                    Content,
                    Embedding,
                    CreatedAt
                FROM VectorEmbeddings
                WHERE OrganizationId = @OrgId";

            if (entityTypes != null && entityTypes.Length > 0)
            {
                sql += " AND EntityType IN (" +
                    string.Join(",", entityTypes.Select((_, i) => $"@Type{i}")) + ")";
            }

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@OrgId", _organizationId);

            if (entityTypes != null)
            {
                for (int i = 0; i < entityTypes.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@Type{i}", entityTypes[i]);
                }
            }

            // Load all candidates and calculate similarity in memory
            var candidates = new List<(VectorSearchResult Result, float[] Embedding)>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var embeddingBytes = (byte[])reader["Embedding"];
                var storedEmbedding = DeserializeEmbedding(embeddingBytes);

                candidates.Add((new VectorSearchResult
                {
                    EntityType = reader.GetString(0),
                    EntityId = reader.GetString(1),
                    ChunkIndex = reader.GetInt32(2),
                    Content = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(5)
                }, storedEmbedding));
            }

            // Calculate cosine similarity for all candidates
            var results = candidates
                .Select(c =>
                {
                    c.Result.Similarity = CosineSimilarity(queryEmbedding, c.Embedding);
                    return c.Result;
                })
                .Where(r => r.Similarity >= minSimilarity)
                .OrderByDescending(r => r.Similarity)
                .Take(topK)
                .ToList();

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
            // Metadata filtering not yet implemented
            _logger.Warn("SearchWithFilterAsync: Metadata filtering not yet implemented for SQL Server");
            return await SearchAsync(queryEmbedding, topK, entityTypes, minSimilarity, cancellationToken);
        }

        public async Task DeleteAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);

            var deletedCount = 0;
            await using var cmd = new SqlCommand("sp_DeleteEntityEmbeddings", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@OrganizationId", _organizationId);
            cmd.Parameters.AddWithValue("@EntityType", entityType);
            cmd.Parameters.AddWithValue("@EntityId", entityId);

            var outputCount = cmd.Parameters.Add("@DeletedCount", System.Data.SqlDbType.Int);
            outputCount.Direction = System.Data.ParameterDirection.Output;

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            deletedCount = (int)(outputCount.Value ?? 0);

            _logger.Debug("Deleted {0} embeddings for {1}/{2}", deletedCount, entityType, entityId);
        }

        public async Task DeleteAllOfTypeAsync(
            string entityType,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);

            await using var cmd = new SqlCommand(@"
                DELETE FROM VectorEmbeddings 
                WHERE OrganizationId = @OrgId 
                  AND EntityType = @EntityType", connection);

            cmd.Parameters.AddWithValue("@OrgId", _organizationId);
            cmd.Parameters.AddWithValue("@EntityType", entityType);

            var deleted = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.Info("Deleted {0} embeddings of type {1}", deleted, entityType);
        }

        public async Task ClearAllAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);

            await using var cmd = new SqlCommand(@"
                DELETE FROM VectorEmbeddings 
                WHERE OrganizationId = @OrgId", connection);

            cmd.Parameters.AddWithValue("@OrgId", _organizationId);

            var deleted = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.Warn("Cleared all {0} embeddings for organization {1}", deleted, _organizationId);
        }

        public async Task<int> CountAsync(string? entityType = null, CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);

            var sql = "SELECT COUNT(*) FROM VectorEmbeddings WHERE OrganizationId = @OrgId";
            if (!string.IsNullOrEmpty(entityType))
            {
                sql += " AND EntityType = @EntityType";
            }

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@OrgId", _organizationId);

            if (!string.IsNullOrEmpty(entityType))
            {
                cmd.Parameters.AddWithValue("@EntityType", entityType);
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

            await using var cmd = new SqlCommand(@"
                SELECT CASE WHEN EXISTS(
                    SELECT 1 FROM VectorEmbeddings 
                    WHERE OrganizationId = @OrgId 
                      AND EntityType = @EntityType 
                      AND EntityId = @EntityId
                ) THEN 1 ELSE 0 END", connection);

            cmd.Parameters.AddWithValue("@OrgId", _organizationId);
            cmd.Parameters.AddWithValue("@EntityType", entityType);
            cmd.Parameters.AddWithValue("@EntityId", entityId);

            return (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0) == 1;
        }

        public async Task<List<string>> GetIndexedEntityIdsAsync(
            string entityType,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);

            await using var cmd = new SqlCommand(@"
                SELECT DISTINCT EntityId 
                FROM VectorEmbeddings 
                WHERE OrganizationId = @OrgId 
                  AND EntityType = @EntityType
                ORDER BY EntityId", connection);

            cmd.Parameters.AddWithValue("@OrgId", _organizationId);
            cmd.Parameters.AddWithValue("@EntityType", entityType);

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

        private async Task<SqlConnection> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        /// <summary>
        /// Serializes a float array to bytes for VARBINARY storage.
        /// </summary>
        private static byte[] SerializeEmbedding(float[] embedding)
        {
            var bytes = new byte[embedding.Length * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Deserializes bytes from VARBINARY back to float array.
        /// </summary>
        private static float[] DeserializeEmbedding(byte[] bytes)
        {
            var embedding = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
            return embedding;
        }

        /// <summary>
        /// Computes cosine similarity between two embedding vectors.
        /// Returns a value from -1 to 1, where 1 = identical direction.
        /// </summary>
        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have same dimensions");

            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0;

            return (float)(dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB)));
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
