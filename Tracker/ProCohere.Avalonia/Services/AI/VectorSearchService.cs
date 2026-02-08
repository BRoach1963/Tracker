using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// Result from a vector/semantic search operation.
/// </summary>
public sealed class VectorSearchResult
{
    /// <summary>Vector embedding record ID.</summary>
    public Guid Id { get; init; }
    
    /// <summary>Type of entity (note, feedback, meeting_note, etc.).</summary>
    public string EntityType { get; init; } = string.Empty;
    
    /// <summary>ID of the source entity.</summary>
    public Guid EntityId { get; init; }
    
    /// <summary>Chunk index for long content split across multiple embeddings.</summary>
    public int ChunkIndex { get; init; }
    
    /// <summary>Preview of the content (first N characters).</summary>
    public string? ContentPreview { get; init; }
    
    /// <summary>Full text content that was embedded.</summary>
    public string? Content { get; init; }
    
    /// <summary>Cosine similarity score (0-1, higher is more similar).</summary>
    public double Similarity { get; init; }
}

/// <summary>
/// Provides vector/semantic search capabilities using Gemini embeddings and Supabase pgvector.
/// Thread-safe singleton service with query caching for performance.
/// </summary>
public sealed class VectorSearchService : IDisposable
{
    #region Singleton
    
    private static readonly Lazy<VectorSearchService> _instance =
        new(() => new VectorSearchService(), LazyThreadSafetyMode.ExecutionAndPublication);
    
    /// <summary>Gets the singleton instance.</summary>
    public static VectorSearchService Instance => _instance.Value;
    
    #endregion
    
    #region Constants
    
    private const string EmbeddingModel = "text-embedding-004";
    private const string EmbeddingApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent";
    private const int EmbeddingDimensions = 768;
    private const int MaxCacheSize = 100;
    private const int CacheExpirationMinutes = 30;
    private const int TimeoutSeconds = 15;
    private const double DefaultMinSimilarity = 0.4;
    private const int DefaultTopK = 10;
    
    #endregion
    
    #region Fields
    
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, CachedEmbedding> _embeddingCache;
    private readonly SemaphoreSlim _rateLimiter;
    private string? _apiKey;
    private bool _disposed;
    
    #endregion
    
    #region Constructor
    
    private VectorSearchService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
        _embeddingCache = new ConcurrentDictionary<string, CachedEmbedding>();
        _rateLimiter = new SemaphoreSlim(5, 5); // Max 5 concurrent embedding requests
        
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
    /// Performs a semantic search across indexed content.
    /// </summary>
    /// <param name="query">Natural language search query.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="entityTypes">Optional filter for specific entity types.</param>
    /// <param name="minSimilarity">Minimum similarity threshold (0-1).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of search results ordered by similarity.</returns>
    public async Task<List<VectorSearchResult>> SearchAsync(
        string query,
        int topK = DefaultTopK,
        string[]? entityTypes = null,
        double minSimilarity = DefaultMinSimilarity,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<VectorSearchResult>();
        }
        
        if (!IsAvailable)
        {
            LastError = "Vector search is not available. Gemini API key not configured.";
            Debug.WriteLine($"[VectorSearch] {LastError}");
            return new List<VectorSearchResult>();
        }
        
        try
        {
            // Step 1: Get embedding for the query
            var embedding = await GetEmbeddingAsync(query, cancellationToken);
            if (embedding == null)
            {
                LastError = "Failed to generate embedding for query.";
                return new List<VectorSearchResult>();
            }
            
            // Step 2: Search via Supabase RPC
            return await SearchByEmbeddingAsync(embedding, topK, entityTypes, minSimilarity, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[VectorSearch] Search cancelled");
            throw;
        }
        catch (Exception ex)
        {
            LastError = $"Search failed: {ex.Message}";
            Debug.WriteLine($"[VectorSearch] Error: {ex.Message}");
            return new List<VectorSearchResult>();
        }
    }
    
    /// <summary>
    /// Gets an embedding vector for the given text.
    /// Uses caching to avoid redundant API calls.
    /// </summary>
    /// <param name="text">Text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Embedding vector, or null on failure.</returns>
    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        
        // Check cache first
        var cacheKey = ComputeHash(text);
        if (_embeddingCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            Debug.WriteLine("[VectorSearch] Embedding cache hit");
            return cached.Embedding;
        }
        
        // Rate limit concurrent requests
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring semaphore
            if (_embeddingCache.TryGetValue(cacheKey, out cached) && !cached.IsExpired)
            {
                return cached.Embedding;
            }
            
            var embedding = await FetchEmbeddingFromApiAsync(text, cancellationToken);
            
            if (embedding != null)
            {
                // Add to cache, evicting oldest if full
                if (_embeddingCache.Count >= MaxCacheSize)
                {
                    EvictOldestCacheEntry();
                }
                
                _embeddingCache[cacheKey] = new CachedEmbedding(embedding);
            }
            
            return embedding;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
    
    /// <summary>
    /// Clears the embedding cache.
    /// </summary>
    public void ClearCache()
    {
        _embeddingCache.Clear();
        Debug.WriteLine("[VectorSearch] Cache cleared");
    }
    
    /// <summary>
    /// Reloads the API key from settings.
    /// Call this if settings change.
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
        
        Debug.WriteLine($"[VectorSearch] API key configured: {IsAvailable}");
    }
    
    private async Task<float[]?> FetchEmbeddingFromApiAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            var request = new EmbeddingRequest
            {
                Content = new EmbeddingContent
                {
                    Parts = new List<EmbeddingPart> { new() { Text = text } }
                }
            };
            
            var json = JsonSerializer.Serialize(request, JsonOptions.Default);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = $"{EmbeddingApiUrl}?key={_apiKey}";
            
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[VectorSearch] Embedding API error: {response.StatusCode} - {responseBody}");
                return null;
            }
            
            var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody, JsonOptions.Default);
            var values = result?.Embedding?.Values;
            
            if (values == null || values.Count != EmbeddingDimensions)
            {
                Debug.WriteLine($"[VectorSearch] Invalid embedding dimensions: {values?.Count ?? 0}");
                return null;
            }
            
            return values.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VectorSearch] Embedding API exception: {ex.Message}");
            return null;
        }
    }
    
    private async Task<List<VectorSearchResult>> SearchByEmbeddingAsync(
        float[] embedding,
        int topK,
        string[]? entityTypes,
        double minSimilarity,
        CancellationToken cancellationToken)
    {
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<VectorSearchResult>();
        }
        
        try
        {
            // Format embedding as pgvector string: [0.1,0.2,0.3,...]
            var embeddingString = $"[{string.Join(",", embedding.Select(v => v.ToString("F6")))}]";
            
            // Call RPC function
            var parameters = new Dictionary<string, object>
            {
                ["p_query_embedding"] = embeddingString,
                ["p_top_k"] = topK,
                ["p_min_similarity"] = minSimilarity
            };
            
            if (entityTypes != null && entityTypes.Length > 0)
            {
                parameters["p_entity_types"] = entityTypes;
            }
            
            var response = await client.Rpc("vector_search", parameters);
            
            if (response == null)
            {
                Debug.WriteLine("[VectorSearch] RPC returned null");
                return new List<VectorSearchResult>();
            }
            
            // Parse the JSON response
            var responseJson = response.Content ?? "[]";
            var results = JsonSerializer.Deserialize<List<VectorSearchRpcResult>>(responseJson, JsonOptions.Default);
            
            if (results == null)
            {
                return new List<VectorSearchResult>();
            }
            
            return results.Select(r => new VectorSearchResult
            {
                Id = r.Id,
                EntityType = r.EntityType ?? string.Empty,
                EntityId = r.EntityId,
                ChunkIndex = r.ChunkIndex,
                ContentPreview = r.ContentPreview,
                Content = r.Content,
                Similarity = r.Similarity
            }).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VectorSearch] RPC error: {ex.Message}");
            LastError = $"Search query failed: {ex.Message}";
            return new List<VectorSearchResult>();
        }
    }
    
    private static string ComputeHash(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
    
    private void EvictOldestCacheEntry()
    {
        var oldest = _embeddingCache
            .OrderBy(kvp => kvp.Value.CreatedAt)
            .FirstOrDefault();
        
        if (!string.IsNullOrEmpty(oldest.Key))
        {
            _embeddingCache.TryRemove(oldest.Key, out _);
        }
    }
    
    #endregion
    
    #region Nested Types
    
    private sealed class CachedEmbedding
    {
        public float[] Embedding { get; }
        public DateTime CreatedAt { get; }
        
        public bool IsExpired => DateTime.UtcNow - CreatedAt > TimeSpan.FromMinutes(CacheExpirationMinutes);
        
        public CachedEmbedding(float[] embedding)
        {
            Embedding = embedding;
            CreatedAt = DateTime.UtcNow;
        }
    }
    
    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
    
    #region API Request/Response Types
    
    private sealed class EmbeddingRequest
    {
        [JsonPropertyName("content")]
        public EmbeddingContent Content { get; set; } = new();
    }
    
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
    
    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public EmbeddingData? Embedding { get; set; }
    }
    
    private sealed class EmbeddingData
    {
        [JsonPropertyName("values")]
        public List<float>? Values { get; set; }
    }
    
    private sealed class VectorSearchRpcResult
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("entity_type")]
        public string? EntityType { get; set; }
        
        [JsonPropertyName("entity_id")]
        public Guid EntityId { get; set; }
        
        [JsonPropertyName("chunk_index")]
        public int ChunkIndex { get; set; }
        
        [JsonPropertyName("content_preview")]
        public string? ContentPreview { get; set; }
        
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        
        [JsonPropertyName("similarity")]
        public double Similarity { get; set; }
    }
    
    #endregion
    
    #endregion
    
    #region IDisposable
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _httpClient.Dispose();
        _rateLimiter.Dispose();
        _embeddingCache.Clear();
        _disposed = true;
    }
    
    #endregion
}
