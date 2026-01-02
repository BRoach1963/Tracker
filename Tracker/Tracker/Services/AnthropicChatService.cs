using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracker.Interfaces;
using Tracker.Logging;
using Tracker.Services.Backend;

namespace Tracker.Services
{
    /// <summary>
    /// Chat provider implementation for Anthropic Claude API.
    /// Supports Claude 3 Haiku, Sonnet, and Opus models.
    /// </summary>
    public class AnthropicChatService : IChatProvider, IDisposable
    {
        #region Constants

        private const string BaseUrl = "https://api.anthropic.com/v1/messages";
        private const string DefaultModel = "claude-3-haiku-20240307";
        private const string ApiVersion = "2023-06-01";
        private const int DefaultMaxTokens = 1024;
        private const int TimeoutSeconds = 60;

        #endregion

        #region Fields

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private string? _apiKey;
        private string _model = DefaultModel;
        private int _maxTokens = DefaultMaxTokens;
        private bool _disposed;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public AnthropicChatService()
        {
            _logger = LoggingManager.GetComponentLogger("AnthropicChat");
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
        }

        #endregion

        #region IChatProvider Implementation

        public string ProviderName => "Anthropic Claude";

        public bool RequiresInternet => true;

        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

        public async Task<string> GetResponseAsync(string prompt, string? systemContext = null, CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage> { ChatMessage.User(prompt) };
            return await GetResponseAsync(messages, systemContext, cancellationToken);
        }

        public async Task<string> GetResponseAsync(IEnumerable<ChatMessage> messages, string? systemContext = null, CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync();

            if (!IsAvailable)
            {
                throw new InvalidOperationException("Anthropic API key is not configured.");
            }

            try
            {
                // Anthropic uses a different message format than OpenAI
                var requestMessages = messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }).ToList();

                // Build request body - Anthropic has system as a top-level field
                object requestBody;
                if (!string.IsNullOrEmpty(systemContext))
                {
                    requestBody = new
                    {
                        model = _model,
                        max_tokens = _maxTokens,
                        system = systemContext,
                        messages = requestMessages
                    };
                }
                else
                {
                    requestBody = new
                    {
                        model = _model,
                        max_tokens = _maxTokens,
                        messages = requestMessages
                    };
                }

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", ApiVersion);
                request.Content = content;

                _logger.Debug("Sending request to Anthropic ({0})...", _model);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.Debug("Anthropic response status: {0}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Anthropic API error: {0} - {1}", response.StatusCode, responseBody);
                    throw new HttpRequestException($"Anthropic API error: {response.StatusCode}");
                }

                var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Claude returns content as an array of content blocks
                var textContent = anthropicResponse?.Content?.FirstOrDefault(c => c.Type == "text");
                var text = textContent?.Text;

                if (string.IsNullOrEmpty(text))
                {
                    _logger.Warn("Anthropic returned empty response");
                    return "I apologize, but I wasn't able to generate a response. Please try again.";
                }

                _logger.Debug("Anthropic response received: {0} chars", text.Length);
                return text;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Anthropic request was cancelled");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.Exception(ex, "Network error communicating with Anthropic");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error calling Anthropic API");
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the service by loading API key from Supabase.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                _apiKey = await SupabaseSecretsService.Instance.GetAnthropicApiKeyAsync();
                var model = await SupabaseSecretsService.Instance.GetAnthropicModelAsync();
                
                if (!string.IsNullOrEmpty(model))
                {
                    _model = model;
                }

                _isInitialized = true;
                _logger.Info("Anthropic service initialized with model: {0}", _model);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize Anthropic service");
            }
        }

        #endregion

        #region Private Methods

        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
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
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Response Models

        private class AnthropicResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("role")]
            public string? Role { get; set; }

            [JsonPropertyName("content")]
            public List<ContentBlock>? Content { get; set; }

            [JsonPropertyName("model")]
            public string? Model { get; set; }

            [JsonPropertyName("stop_reason")]
            public string? StopReason { get; set; }

            [JsonPropertyName("usage")]
            public AnthropicUsage? Usage { get; set; }
        }

        private class ContentBlock
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class AnthropicUsage
        {
            [JsonPropertyName("input_tokens")]
            public int InputTokens { get; set; }

            [JsonPropertyName("output_tokens")]
            public int OutputTokens { get; set; }
        }

        #endregion
    }
}
