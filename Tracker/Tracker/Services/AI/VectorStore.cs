using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Local vector store using SQLite for persistent storage.
    /// Stores document chunks with their embeddings for semantic search.
    /// </summary>
    public class VectorStore : IDisposable
    {
        #region Fields

        private readonly string _connectionString;
        private readonly ILogger _logger;
        private bool _disposed;
        private bool _initialized;

        #endregion

        #region Singleton

        private static readonly Lazy<VectorStore> _instance =
            new(() => new VectorStore(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static VectorStore Instance => _instance.Value;

        #endregion

        #region Constructor

        private VectorStore()
        {
            _logger = LoggingManager.GetComponentLogger("VectorStore");
            
            // Store vectors in a separate SQLite database
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Tracker", "vectors.db");
            
            Directory.CreateDirectory(Path.GetDirectoryName(appDataPath)!);
            _connectionString = $"Data Source={appDataPath}";
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the vector store database schema.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS document_chunks (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        doc_id TEXT NOT NULL,
                        chunk_index INTEGER NOT NULL,
                        content TEXT NOT NULL,
                        embedding BLOB NOT NULL,
                        metadata TEXT,
                        created_at TEXT DEFAULT CURRENT_TIMESTAMP,
                        UNIQUE(doc_id, chunk_index)
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_doc_id ON document_chunks(doc_id);
                ";

                using var command = new SqliteCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                _initialized = true;
                _logger.Info("Vector store initialized");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize vector store");
                throw;
            }
        }

        #endregion

        #region Storage Operations

        /// <summary>
        /// Stores a document chunk with its embedding.
        /// </summary>
        public async Task StoreChunkAsync(string docId, int chunkIndex, string content, float[] embedding, string? metadata = null)
        {
            await InitializeAsync();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT OR REPLACE INTO document_chunks (doc_id, chunk_index, content, embedding, metadata)
                    VALUES (@docId, @chunkIndex, @content, @embedding, @metadata)
                ";

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@docId", docId);
                command.Parameters.AddWithValue("@chunkIndex", chunkIndex);
                command.Parameters.AddWithValue("@content", content);
                command.Parameters.AddWithValue("@embedding", SerializeEmbedding(embedding));
                command.Parameters.AddWithValue("@metadata", metadata ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to store chunk: {0}/{1}", docId, chunkIndex);
                throw;
            }
        }

        /// <summary>
        /// Stores multiple chunks at once (more efficient).
        /// </summary>
        public async Task StoreBatchAsync(List<(string DocId, int ChunkIndex, string Content, float[] Embedding, string? Metadata)> chunks)
        {
            await InitializeAsync();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                var sql = @"
                    INSERT OR REPLACE INTO document_chunks (doc_id, chunk_index, content, embedding, metadata)
                    VALUES (@docId, @chunkIndex, @content, @embedding, @metadata)
                ";

                foreach (var chunk in chunks)
                {
                    using var command = new SqliteCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("@docId", chunk.DocId);
                    command.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
                    command.Parameters.AddWithValue("@content", chunk.Content);
                    command.Parameters.AddWithValue("@embedding", SerializeEmbedding(chunk.Embedding));
                    command.Parameters.AddWithValue("@metadata", chunk.Metadata ?? (object)DBNull.Value);
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                _logger.Info("Stored {0} chunks in batch", chunks.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to store batch");
                throw;
            }
        }

        /// <summary>
        /// Deletes all chunks for a document.
        /// </summary>
        public async Task DeleteDocumentAsync(string docId)
        {
            await InitializeAsync();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "DELETE FROM document_chunks WHERE doc_id = @docId";
                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@docId", docId);
                var deleted = await command.ExecuteNonQueryAsync();

                _logger.Debug("Deleted {0} chunks for doc: {1}", deleted, docId);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to delete document: {0}", docId);
            }
        }

        /// <summary>
        /// Clears all stored vectors.
        /// </summary>
        public async Task ClearAllAsync()
        {
            await InitializeAsync();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqliteCommand("DELETE FROM document_chunks", connection);
                await command.ExecuteNonQueryAsync();

                _logger.Info("Cleared all vectors from store");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to clear vector store");
            }
        }

        /// <summary>
        /// Adds a single vector with metadata (for data entities).
        /// </summary>
        public async Task AddAsync(string id, float[] embedding, string content, Dictionary<string, object> metadata)
        {
            await InitializeAsync();

            try
            {
                var metadataJson = JsonSerializer.Serialize(metadata);
                
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT OR REPLACE INTO document_chunks (doc_id, chunk_index, content, embedding, metadata)
                    VALUES (@docId, @chunkIndex, @content, @embedding, @metadata)
                ";

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@docId", id);
                command.Parameters.AddWithValue("@chunkIndex", 0);
                command.Parameters.AddWithValue("@content", content);
                command.Parameters.AddWithValue("@embedding", SerializeEmbedding(embedding));
                command.Parameters.AddWithValue("@metadata", metadataJson);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to add vector: {0}", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes all vectors matching a metadata key-value pair.
        /// </summary>
        public async Task DeleteByMetadataAsync(string key, string value)
        {
            await InitializeAsync();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // SQLite JSON functions to search within metadata
                var sql = @"
                    DELETE FROM document_chunks 
                    WHERE json_extract(metadata, '$.' || @key) = @value
                ";

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@key", key);
                command.Parameters.AddWithValue("@value", value);
                var deleted = await command.ExecuteNonQueryAsync();

                _logger.Debug("Deleted {0} vectors with {1}={2}", deleted, key, value);
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to delete by metadata {0}={1}: {2}", key, value, ex.Message);
            }
        }

        #endregion

        #region Search Operations

        /// <summary>
        /// Searches for the most similar chunks to the query embedding.
        /// </summary>
        /// <param name="queryEmbedding">The embedding of the search query</param>
        /// <param name="topK">Number of results to return</param>
        /// <param name="minScore">Minimum similarity score (0-1)</param>
        /// <returns>List of matching chunks with their similarity scores</returns>
        public async Task<List<SearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5, float minScore = 0.3f)
        {
            await InitializeAsync();

            var results = new List<SearchResult>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Load all embeddings and compute similarity
                // Note: For large datasets, this should be optimized with approximate nearest neighbor
                var sql = "SELECT doc_id, chunk_index, content, embedding, metadata FROM document_chunks";
                using var command = new SqliteCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var candidates = new List<(SearchResult Result, float Score)>();

                while (await reader.ReadAsync())
                {
                    var embedding = DeserializeEmbedding((byte[])reader["embedding"]);
                    if (embedding == null) continue;

                    var score = EmbeddingService.CosineSimilarity(queryEmbedding, embedding);
                    
                    if (score >= minScore)
                    {
                        candidates.Add((new SearchResult
                        {
                            DocId = reader["doc_id"].ToString() ?? "",
                            ChunkIndex = Convert.ToInt32(reader["chunk_index"]),
                            Content = reader["content"].ToString() ?? "",
                            Metadata = reader["metadata"]?.ToString(),
                            Score = score
                        }, score));
                    }
                }

                // Sort by score descending and take top K
                results = candidates
                    .OrderByDescending(c => c.Score)
                    .Take(topK)
                    .Select(c => c.Result)
                    .ToList();

                _logger.Debug("Search found {0} results (top {1})", results.Count, topK);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Search failed");
            }

            return results;
        }

        /// <summary>
        /// Gets the count of stored chunks.
        /// </summary>
        public async Task<int> GetChunkCountAsync()
        {
            await InitializeAsync();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqliteCommand("SELECT COUNT(*) FROM document_chunks", connection);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets all indexed document IDs.
        /// </summary>
        public async Task<List<string>> GetIndexedDocumentsAsync()
        {
            await InitializeAsync();

            var docs = new List<string>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqliteCommand("SELECT DISTINCT doc_id FROM document_chunks", connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    docs.Add(reader["doc_id"].ToString() ?? "");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to get indexed documents");
            }

            return docs;
        }

        /// <summary>
        /// Gets the chunk count for a specific document.
        /// </summary>
        public async Task<int> GetDocumentChunkCountAsync(string docId)
        {
            await InitializeAsync();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqliteCommand(
                    "SELECT COUNT(*) FROM document_chunks WHERE doc_id = @docId", 
                    connection);
                command.Parameters.AddWithValue("@docId", docId);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Serialization

        private static byte[] SerializeEmbedding(float[] embedding)
        {
            // Store as raw bytes (4 bytes per float) for efficiency
            var bytes = new byte[embedding.Length * 4];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static float[]? DeserializeEmbedding(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length % 4 != 0)
                return null;

            var floats = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Result from a vector similarity search.
    /// </summary>
    public class SearchResult
    {
        public string DocId { get; set; } = "";
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = "";
        public string? Metadata { get; set; }
        public float Score { get; set; }
    }
}

