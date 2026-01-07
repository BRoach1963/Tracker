using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Adapter that wraps the legacy SQLite VectorStore to implement IVectorStore.
    /// This allows gradual migration from the old singleton pattern to the new interface.
    /// 
    /// Use this adapter during transition period. Will be removed once all code
    /// is migrated to use PostgreSQL/SQL Server vector stores.
    /// </summary>
    public class LegacyVectorStoreAdapter : IVectorStore
    {
        private readonly VectorStore _legacyStore;
        private readonly ILogger _logger;
        private bool _disposed;

        /// <summary>
        /// Creates a new adapter wrapping the legacy VectorStore singleton.
        /// </summary>
        public LegacyVectorStoreAdapter()
        {
            _legacyStore = VectorStore.Instance;
            _logger = LoggingManager.GetComponentLogger("LegacyVectorStoreAdapter");
        }

        /// <inheritdoc />
        public bool IsInitialized => true; // Legacy store initializes lazily

        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _legacyStore.InitializeAsync();
        }

        /// <inheritdoc />
        public async Task<Guid> StoreAsync(
            string entityType,
            string entityId,
            string content,
            float[] embedding,
            int chunkIndex = 0,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            // Legacy store uses "doc_id" format: "{entityType}:{entityId}"
            var docId = $"{entityType}:{entityId}";
            var metadataJson = metadata != null 
                ? System.Text.Json.JsonSerializer.Serialize(metadata) 
                : null;

            await _legacyStore.StoreChunkAsync(docId, chunkIndex, content, embedding, metadataJson);
            
            // Return a deterministic GUID based on the doc ID and chunk
            return GenerateId(docId, chunkIndex);
        }

        /// <inheritdoc />
        public async Task StoreBatchAsync(
            IEnumerable<VectorStoreEntry> entries,
            CancellationToken cancellationToken = default)
        {
            var chunks = entries.Select(e => (
                DocId: $"{e.EntityType}:{e.EntityId}",
                ChunkIndex: e.ChunkIndex,
                Content: e.Content,
                Embedding: e.Embedding,
                Metadata: e.Metadata != null 
                    ? System.Text.Json.JsonSerializer.Serialize(e.Metadata) 
                    : (string?)null
            )).ToList();

            await _legacyStore.StoreBatchAsync(chunks);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(
            string entityType,
            string entityId,
            string content,
            float[] embedding,
            int chunkIndex = 0,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            // Legacy store uses INSERT OR REPLACE, so Store == Update
            await StoreAsync(entityType, entityId, content, embedding, chunkIndex, metadata, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<List<VectorSearchResult>> SearchAsync(
            float[] queryEmbedding,
            int topK = 10,
            string[]? entityTypes = null,
            float minSimilarity = 0.5f,
            CancellationToken cancellationToken = default)
        {
            var legacyResults = await _legacyStore.SearchAsync(queryEmbedding, topK, minSimilarity);
            
            return legacyResults
                .Where(r => entityTypes == null || MatchesEntityType(r.DocId, entityTypes))
                .Select(r => ConvertResult(r))
                .ToList();
        }

        /// <inheritdoc />
        public async Task<List<VectorSearchResult>> SearchWithFilterAsync(
            float[] queryEmbedding,
            Dictionary<string, object> metadataFilters,
            int topK = 10,
            string[]? entityTypes = null,
            float minSimilarity = 0.5f,
            CancellationToken cancellationToken = default)
        {
            // Legacy store doesn't support metadata filtering
            // Fall back to regular search with post-filtering
            _logger.Warn("Legacy adapter does not support metadata filtering - returning unfiltered results");
            return await SearchAsync(queryEmbedding, topK, entityTypes, minSimilarity, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default)
        {
            var docId = $"{entityType}:{entityId}";
            await _legacyStore.DeleteDocumentAsync(docId);
        }

        /// <inheritdoc />
        public async Task DeleteAllOfTypeAsync(
            string entityType,
            CancellationToken cancellationToken = default)
        {
            // Legacy store doesn't have efficient type-based deletion
            // This would require iterating all docs - not recommended
            _logger.Warn("Legacy adapter DeleteAllOfTypeAsync not efficiently supported");
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ClearAllAsync(CancellationToken cancellationToken = default)
        {
            await _legacyStore.ClearAllAsync();
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(string? entityType = null, CancellationToken cancellationToken = default)
        {
            return await _legacyStore.GetChunkCountAsync();
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default)
        {
            var docId = $"{entityType}:{entityId}";
            var count = await _legacyStore.GetDocumentChunkCountAsync(docId);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetIndexedEntityIdsAsync(
            string entityType,
            CancellationToken cancellationToken = default)
        {
            // This would require a new query in legacy store
            // For now, return empty list
            _logger.Warn("Legacy adapter GetIndexedEntityIdsAsync not supported");
            await Task.CompletedTask;
            return new List<string>();
        }

        #region Helpers

        private static Guid GenerateId(string docId, int chunkIndex)
        {
            // Generate deterministic GUID from doc ID and chunk index
            var input = $"{docId}:{chunkIndex}";
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }

        private static bool MatchesEntityType(string docId, string[] entityTypes)
        {
            foreach (var type in entityTypes)
            {
                if (docId.StartsWith($"{type}:", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static VectorSearchResult ConvertResult(SearchResult legacyResult)
        {
            var parts = legacyResult.DocId.Split(':', 2);
            var entityType = parts.Length > 0 ? parts[0] : "unknown";
            var entityId = parts.Length > 1 ? parts[1] : legacyResult.DocId;

            Dictionary<string, object>? metadata = null;
            if (!string.IsNullOrEmpty(legacyResult.Metadata))
            {
                try
                {
                    metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(legacyResult.Metadata);
                }
                catch { /* Ignore parsing errors */ }
            }

            return new VectorSearchResult
            {
                EntityType = entityType,
                EntityId = entityId,
                Content = legacyResult.Content,
                ChunkIndex = legacyResult.ChunkIndex,
                Similarity = legacyResult.Score,  // Score maps to Similarity
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow // Legacy doesn't track this
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                // Don't dispose the singleton legacy store
                _disposed = true;
            }
        }

        #endregion
    }
}
