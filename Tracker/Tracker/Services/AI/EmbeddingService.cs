using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Service for generating text embeddings using Gemini's text-embedding-004 model.
    /// Embeddings are vector representations of text that capture semantic meaning.
    /// </summary>
    public class EmbeddingService : IDisposable
    {
        #region Constants

        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
        private const string EmbeddingModel = "text-embedding-004";
        private const int TimeoutSeconds = 30;
        private const int MaxBatchSize = 100; // Gemini limit

        #endregion

        #region Fields

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _apiKey;
        private bool _disposed;

        #endregion

        #region Singleton

        private static readonly Lazy<EmbeddingService> _instance =
            new(() => new EmbeddingService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static EmbeddingService Instance => _instance.Value;

        #endregion

        #region Constructor

        private EmbeddingService()
        {
            _logger = LoggingManager.GetComponentLogger("Embeddings");
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
            _apiKey = UserSettingsManager.Instance.Settings.AI.GeminiApiKey;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the embedding vector for a single text.
        /// </summary>
        /// <param name="text">Text to embed</param>
        /// <returns>Float array representing the embedding vector (768 dimensions)</returns>
        public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.Error("Cannot get embedding: API key not configured");
                return null;
            }

            try
            {
                var request = new EmbedRequest
                {
                    Content = new EmbedContent
                    {
                        Parts = new List<EmbedPart>
                        {
                            new EmbedPart { Text = text }
                        }
                    }
                };

                var url = $"{BaseUrl}/{EmbeddingModel}:embedContent?key={_apiKey}";

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Embedding API error: {0} - {1}", response.StatusCode, responseBody);
                    return null;
                }

                var embedResponse = JsonSerializer.Deserialize<EmbedResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var values = embedResponse?.Embedding?.Values;
                if (values == null || values.Count == 0)
                {
                    _logger.Warn("Embedding response had no values");
                    return null;
                }

                _logger.Debug("Generated embedding: {0} dimensions", values.Count);
                return values.ToArray();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error generating embedding");
                return null;
            }
        }

        /// <summary>
        /// Gets embeddings for multiple texts in a batch (more efficient).
        /// </summary>
        public async Task<List<float[]?>> GetEmbeddingsBatchAsync(List<string> texts, CancellationToken cancellationToken = default)
        {
            var results = new List<float[]?>();

            if (texts == null || texts.Count == 0)
                return results;

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.Error("Cannot get embeddings: API key not configured");
                return texts.Select(_ => (float[]?)null).ToList();
            }

            try
            {
                // Process in batches
                for (int i = 0; i < texts.Count; i += MaxBatchSize)
                {
                    var batch = texts.Skip(i).Take(MaxBatchSize).ToList();
                    var batchResults = await GetBatchEmbeddingsInternal(batch, cancellationToken);
                    results.AddRange(batchResults);

                    // Small delay between batches to avoid rate limiting
                    if (i + MaxBatchSize < texts.Count)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error in batch embedding");
                // Fill remaining with nulls
                while (results.Count < texts.Count)
                {
                    results.Add(null);
                }
            }

            return results;
        }

        /// <summary>
        /// Calculates cosine similarity between two embedding vectors.
        /// Returns value between -1 and 1, where 1 means identical.
        /// </summary>
        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return 0;

            float dotProduct = 0;
            float normA = 0;
            float normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0;

            return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
        }

        #endregion

        #region Private Methods

        private async Task<List<float[]?>> GetBatchEmbeddingsInternal(List<string> texts, CancellationToken cancellationToken)
        {
            var results = new List<float[]?>();

            var request = new BatchEmbedRequest
            {
                Requests = texts.Select(t => new EmbedRequest
                {
                    Model = $"models/{EmbeddingModel}",
                    Content = new EmbedContent
                    {
                        Parts = new List<EmbedPart> { new EmbedPart { Text = t } }
                    }
                }).ToList()
            };

            var url = $"{BaseUrl}/{EmbeddingModel}:batchEmbedContents?key={_apiKey}";

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, httpContent, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("Batch embedding API error: {0}", response.StatusCode);
                return texts.Select(_ => (float[]?)null).ToList();
            }

            var batchResponse = JsonSerializer.Deserialize<BatchEmbedResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (batchResponse?.Embeddings != null)
            {
                foreach (var emb in batchResponse.Embeddings)
                {
                    results.Add(emb.Values?.ToArray());
                }
            }

            _logger.Info("Generated {0} embeddings in batch", results.Count);
            return results;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        #endregion

        #region API Models

        private class EmbedRequest
        {
            public string? Model { get; set; }
            public EmbedContent Content { get; set; } = new();
        }

        private class EmbedContent
        {
            public List<EmbedPart> Parts { get; set; } = new();
        }

        private class EmbedPart
        {
            public string Text { get; set; } = string.Empty;
        }

        private class EmbedResponse
        {
            public EmbeddingData? Embedding { get; set; }
        }

        private class EmbeddingData
        {
            public List<float> Values { get; set; } = new();
        }

        private class BatchEmbedRequest
        {
            public List<EmbedRequest> Requests { get; set; } = new();
        }

        private class BatchEmbedResponse
        {
            public List<EmbeddingData>? Embeddings { get; set; }
        }

        #endregion
    }
}

