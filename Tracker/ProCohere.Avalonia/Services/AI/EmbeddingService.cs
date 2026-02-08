using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// Configuration for the embedding service.
/// </summary>
public sealed class EmbeddingConfig
{
    /// <summary>Maximum characters per chunk for long content.</summary>
    public int MaxChunkLength { get; init; } = 2000;
    
    /// <summary>Overlap between chunks for context continuity.</summary>
    public int ChunkOverlap { get; init; } = 200;
    
    /// <summary>Minimum content length to bother embedding.</summary>
    public int MinContentLength { get; init; } = 10;
}

/// <summary>
/// Result of an embedding operation.
/// </summary>
public sealed class EmbeddingResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public float[]? Embedding { get; init; }
    public string ContentHash { get; init; } = string.Empty;
}

/// <summary>
/// Result of indexing an entity.
/// </summary>
public sealed class IndexingResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int ChunksIndexed { get; init; }
    public Guid? PrimaryEmbeddingId { get; init; }
}

/// <summary>
/// Service for creating and storing vector embeddings for entities.
/// Used for indexing content for later semantic search.
/// Thread-safe singleton with batching support.
/// </summary>
public sealed class EmbeddingService : IDisposable
{
    #region Singleton
    
    private static readonly Lazy<EmbeddingService> _instance =
        new(() => new EmbeddingService(), LazyThreadSafetyMode.ExecutionAndPublication);
    
    /// <summary>Gets the singleton instance.</summary>
    public static EmbeddingService Instance => _instance.Value;
    
    #endregion
    
    #region Constants
    
    private const string EmbeddingModel = "text-embedding-004";
    private const string BatchEmbeddingApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:batchEmbedContents";
    private const int EmbeddingDimensions = 768;
    private const int TimeoutSeconds = 30;
    private const int MaxBatchSize = 100; // Gemini limit
    
    #endregion
    
    #region Fields
    
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _indexLock;
    private readonly EmbeddingConfig _config;
    private string? _apiKey;
    private bool _disposed;
    
    #endregion
    
    #region Constructor
    
    private EmbeddingService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
        _indexLock = new SemaphoreSlim(1, 1);
        _config = new EmbeddingConfig();
        
        LoadApiKey();
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>Whether the service is available (API key configured).</summary>
    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);
    
    /// <summary>Last error message if an operation failed.</summary>
    public string? LastError { get; private set; }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Indexes an entity's content by creating and storing embeddings.
    /// Long content is automatically chunked.
    /// </summary>
    /// <param name="entityType">Type of entity (note, feedback, meeting_note, etc.).</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="content">Text content to embed.</param>
    /// <param name="metadata">Optional metadata to store with embedding.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Indexing result.</returns>
    public async Task<IndexingResult> IndexEntityAsync(
        string entityType,
        Guid entityId,
        string content,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        if (string.IsNullOrWhiteSpace(content) || content.Length < _config.MinContentLength)
        {
            return new IndexingResult { Success = true, ChunksIndexed = 0 };
        }
        
        if (!IsAvailable)
        {
            LastError = "Embedding service unavailable. API key not configured.";
            return new IndexingResult { Success = false, Error = LastError };
        }
        
        try
        {
            // Chunk the content
            var chunks = ChunkContent(content);
            
            // Get embeddings for all chunks
            var embeddings = await GetEmbeddingsBatchAsync(chunks, cancellationToken);
            
            if (embeddings == null || embeddings.Count != chunks.Count)
            {
                LastError = "Failed to generate embeddings for all chunks.";
                return new IndexingResult { Success = false, Error = LastError };
            }
            
            // Store embeddings in database
            var primaryId = await StoreEmbeddingsAsync(entityType, entityId, chunks, embeddings, metadata, cancellationToken);
            
            return new IndexingResult
            {
                Success = primaryId.HasValue,
                ChunksIndexed = chunks.Count,
                PrimaryEmbeddingId = primaryId
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LastError = $"Indexing failed: {ex.Message}";
            Debug.WriteLine($"[EmbeddingService] Error: {ex.Message}");
            return new IndexingResult { Success = false, Error = LastError };
        }
    }
    
    /// <summary>
    /// Deletes all embeddings for an entity.
    /// </summary>
    public async Task<bool> DeleteEntityEmbeddingsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null) return false;
            
#pragma warning disable CS8603 // Possible null reference return (Supabase Postgrest API signature)
            await client.From<VectorEmbedding>()
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .Set(e => e.IsDeleted, true)
                .Set(e => e.DeletedAt, DateTime.UtcNow)
                .Update();
#pragma warning restore CS8603
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EmbeddingService] Delete error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Checks if an entity needs re-indexing by comparing content hash.
    /// </summary>
    public async Task<bool> NeedsReindexingAsync(
        string entityType,
        Guid entityId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var hash = ComputeContentHash(content);
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null) return true;
            
            var existing = await client.From<VectorEmbedding>()
                .Where(e => e.EntityType == entityType && e.EntityId == entityId && e.ChunkIndex == 0)
                .Where(e => e.IsDeleted == false)
                .Select("content_hash")
                .Single();
            
            return existing?.ContentHash != hash;
        }
        catch
        {
            return true; // If we can't check, assume reindex needed
        }
    }
    
    /// <summary>
    /// Reloads the API key from settings.
    /// </summary>
    public void ReloadApiKey()
    {
        LoadApiKey();
    }
    
    #endregion
    
    #region Private Methods
    
    private void LoadApiKey()
    {
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
                  AppSettingsService.Instance.GetGeminiApiKey() ??
                  string.Empty;
    }
    
    private List<string> ChunkContent(string content)
    {
        var chunks = new List<string>();
        
        if (content.Length <= _config.MaxChunkLength)
        {
            chunks.Add(content);
            return chunks;
        }
        
        var currentPos = 0;
        while (currentPos < content.Length)
        {
            var length = Math.Min(_config.MaxChunkLength, content.Length - currentPos);
            var chunk = content.Substring(currentPos, length);
            
            // Try to break at sentence or paragraph boundary
            if (currentPos + length < content.Length)
            {
                var lastBreak = FindLastBreakPoint(chunk);
                if (lastBreak > _config.MaxChunkLength / 2)
                {
                    chunk = chunk[..lastBreak];
                }
            }
            
            chunks.Add(chunk.Trim());
            currentPos += chunk.Length - _config.ChunkOverlap;
            
            // Prevent infinite loop
            if (chunk.Length == 0) break;
        }
        
        return chunks;
    }
    
    private static int FindLastBreakPoint(string text)
    {
        // Prefer paragraph breaks, then sentence breaks, then word breaks
        var paragraphBreak = text.LastIndexOf("\n\n", StringComparison.Ordinal);
        if (paragraphBreak > 0) return paragraphBreak + 2;
        
        var sentenceBreak = text.LastIndexOfAny(new[] { '.', '!', '?' });
        if (sentenceBreak > 0) return sentenceBreak + 1;
        
        var wordBreak = text.LastIndexOf(' ');
        return wordBreak > 0 ? wordBreak : text.Length;
    }
    
    private async Task<List<float[]>?> GetEmbeddingsBatchAsync(
        List<string> texts,
        CancellationToken cancellationToken)
    {
        var allEmbeddings = new List<float[]>();
        
        // Process in batches if needed
        for (int i = 0; i < texts.Count; i += MaxBatchSize)
        {
            var batch = texts.Skip(i).Take(MaxBatchSize).ToList();
            var batchEmbeddings = await FetchBatchEmbeddingsAsync(batch, cancellationToken);
            
            if (batchEmbeddings == null)
            {
                return null;
            }
            
            allEmbeddings.AddRange(batchEmbeddings);
        }
        
        return allEmbeddings;
    }
    
    private async Task<List<float[]>?> FetchBatchEmbeddingsAsync(
        List<string> texts,
        CancellationToken cancellationToken)
    {
        try
        {
            var requests = texts.Select(t => new BatchEmbedRequest
            {
                Model = $"models/{EmbeddingModel}",
                Content = new EmbeddingContent
                {
                    Parts = new List<EmbeddingPart> { new() { Text = t } }
                }
            }).ToList();
            
            var batchRequest = new BatchEmbedContentsRequest { Requests = requests };
            
            var json = JsonSerializer.Serialize(batchRequest, JsonOptions.Default);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = $"{BatchEmbeddingApiUrl}?key={_apiKey}";
            
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[EmbeddingService] Batch API error: {response.StatusCode}");
                return null;
            }
            
            var result = JsonSerializer.Deserialize<BatchEmbedContentsResponse>(responseBody, JsonOptions.Default);
            
            if (result?.Embeddings == null || result.Embeddings.Count != texts.Count)
            {
                Debug.WriteLine("[EmbeddingService] Batch response mismatch");
                return null;
            }
            
            return result.Embeddings
                .Select(e => e.Values?.ToArray())
                .Where(e => e != null && e.Length == EmbeddingDimensions)
                .Cast<float[]>()
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EmbeddingService] Batch API exception: {ex.Message}");
            return null;
        }
    }
    
    private async Task<Guid?> StoreEmbeddingsAsync(
        string entityType,
        Guid entityId,
        List<string> chunks,
        List<float[]> embeddings,
        Dictionary<string, object>? metadata,
        CancellationToken cancellationToken)
    {
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;
        
        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }
        
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            // Soft-delete existing embeddings for this entity
            await DeleteEntityEmbeddingsAsync(entityType, entityId, cancellationToken);
            
            var contentHash = ComputeContentHash(string.Join(" ", chunks));
            var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null;
            Guid? primaryId = null;
            
            for (int i = 0; i < chunks.Count; i++)
            {
                var embedding = new VectorEmbedding
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = session.TeamMember.OrganizationId,
                    EntityType = entityType,
                    EntityId = entityId,
                    ChunkIndex = i,
                    ContentHash = contentHash,
                    ContentPreview = chunks[i].Length > 200 ? chunks[i][..200] + "..." : chunks[i],
                    Content = chunks[i],
                    Embedding = FormatEmbedding(embeddings[i]),
                    EmbeddingDimensions = EmbeddingDimensions,
                    ModelName = EmbeddingModel,
                    Metadata = metadataJson,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                
                var result = await client.From<VectorEmbedding>().Insert(embedding);
                
                if (i == 0)
                {
                    primaryId = result.Models?.FirstOrDefault()?.Id;
                }
            }
            
            Debug.WriteLine($"[EmbeddingService] Indexed {chunks.Count} chunks for {entityType}/{entityId}");
            return primaryId;
        }
        finally
        {
            _indexLock.Release();
        }
    }
    
    private static string FormatEmbedding(float[] embedding)
    {
        // Format as pgvector string
        return $"[{string.Join(",", embedding.Select(v => v.ToString("F6")))}]";
    }
    
    private static string ComputeContentHash(string content)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(bytes);
    }
    
    #endregion
    
    #region Nested Types
    
    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
    
    #region API Types
    
    private sealed class EmbeddingContent
    {
        [JsonPropertyName("parts")]
        public List<EmbeddingPart> Parts { get; set; } = new();
    }
    
    private sealed class EmbeddingPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
    
    private sealed class BatchEmbedRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        
        [JsonPropertyName("content")]
        public EmbeddingContent Content { get; set; } = new();
    }
    
    private sealed class BatchEmbedContentsRequest
    {
        [JsonPropertyName("requests")]
        public List<BatchEmbedRequest> Requests { get; set; } = new();
    }
    
    private sealed class BatchEmbedContentsResponse
    {
        [JsonPropertyName("embeddings")]
        public List<EmbeddingValues>? Embeddings { get; set; }
    }
    
    private sealed class EmbeddingValues
    {
        [JsonPropertyName("values")]
        public List<float>? Values { get; set; }
    }
    
    #endregion
    
    #endregion
    
    #region IDisposable
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _httpClient.Dispose();
        _indexLock.Dispose();
        _disposed = true;
    }
    
    #endregion
}
