namespace Tracker.Services.AI
{
    /// <summary>
    /// Interface for vector storage operations.
    /// Abstracts the underlying storage mechanism (SQLite, PostgreSQL, SQL Server).
    /// 
    /// All implementations should:
    /// - Scope data to the current organization (via RLS or application filtering)
    /// - Support concurrent access
    /// - Handle large batch operations efficiently
    /// </summary>
    public interface IVectorStore : IDisposable
    {
        #region Initialization

        /// <summary>
        /// Initializes the vector store (creates schema if needed).
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether the store has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        #endregion

        #region Storage Operations

        /// <summary>
        /// Stores a single embedding.
        /// </summary>
        /// <param name="entityType">Type of entity (e.g., "team_member", "meeting", "task", "document")</param>
        /// <param name="entityId">ID of the source entity</param>
        /// <param name="content">Original text that was embedded</param>
        /// <param name="embedding">The embedding vector (typically 1536 dimensions for OpenAI)</param>
        /// <param name="chunkIndex">Index for multi-chunk documents (default 0)</param>
        /// <param name="metadata">Optional metadata dictionary</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The ID of the stored embedding</returns>
        Task<Guid> StoreAsync(
            string entityType,
            string entityId,
            string content,
            float[] embedding,
            int chunkIndex = 0,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores multiple embeddings in a batch (more efficient).
        /// </summary>
        Task StoreBatchAsync(
            IEnumerable<VectorStoreEntry> entries,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing embedding.
        /// </summary>
        Task UpdateAsync(
            string entityType,
            string entityId,
            string content,
            float[] embedding,
            int chunkIndex = 0,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        #endregion

        #region Search Operations

        /// <summary>
        /// Searches for similar embeddings using cosine similarity.
        /// </summary>
        /// <param name="queryEmbedding">The query embedding vector</param>
        /// <param name="topK">Maximum number of results</param>
        /// <param name="entityTypes">Filter by entity types (null = all types)</param>
        /// <param name="minSimilarity">Minimum similarity threshold (0.0 to 1.0)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of search results ordered by similarity (descending)</returns>
        Task<List<VectorSearchResult>> SearchAsync(
            float[] queryEmbedding,
            int topK = 10,
            string[]? entityTypes = null,
            float minSimilarity = 0.5f,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for similar embeddings with additional metadata filters.
        /// </summary>
        Task<List<VectorSearchResult>> SearchWithFilterAsync(
            float[] queryEmbedding,
            Dictionary<string, object> metadataFilters,
            int topK = 10,
            string[]? entityTypes = null,
            float minSimilarity = 0.5f,
            CancellationToken cancellationToken = default);

        #endregion

        #region Delete Operations

        /// <summary>
        /// Deletes all embeddings for a specific entity.
        /// </summary>
        Task DeleteAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all embeddings of a specific entity type.
        /// </summary>
        Task DeleteAllOfTypeAsync(
            string entityType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all embeddings (use with caution).
        /// </summary>
        Task ClearAllAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Query Operations

        /// <summary>
        /// Gets the count of stored embeddings.
        /// </summary>
        /// <param name="entityType">Filter by entity type (null = all)</param>
        Task<int> CountAsync(string? entityType = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an entity has embeddings stored.
        /// </summary>
        Task<bool> ExistsAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the entity IDs that have embeddings for a given type.
        /// </summary>
        Task<List<string>> GetIndexedEntityIdsAsync(
            string entityType,
            CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// Entry for batch storage operations.
    /// </summary>
    public class VectorStoreEntry
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public int ChunkIndex { get; set; } = 0;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Result from a vector similarity search.
    /// </summary>
    public class VectorSearchResult
    {
        /// <summary>
        /// Type of entity (e.g., "team_member", "meeting", "task", "document")
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the source entity.
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// Original text content that was embedded.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Chunk index (for multi-chunk documents).
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// Cosine similarity score (0.0 to 1.0, higher = more similar).
        /// </summary>
        public float Similarity { get; set; }

        /// <summary>
        /// Optional metadata associated with the embedding.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// When the embedding was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Display-friendly similarity percentage.
        /// </summary>
        public string SimilarityDisplay => $"{Similarity * 100:F1}%";
    }
}
