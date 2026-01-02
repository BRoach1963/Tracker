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
    /// Chat provider implementation for OpenAI API.
    /// Supports GPT-4o, GPT-4o-mini, and other OpenAI models.
    /// </summary>
    public class OpenAIChatService : IChatProvider, IDisposable
    {
        #region Constants

        private const string BaseUrl = "https://api.openai.com/v1/chat/completions";
        private const string DefaultModel = "gpt-4o-mini";
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

        public OpenAIChatService()
        {
            _logger = LoggingManager.GetComponentLogger("OpenAIChat");
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
        }

        #endregion

        #region IChatProvider Implementation

        public string ProviderName => "OpenAI";

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
                throw new InvalidOperationException("OpenAI API key is not configured.");
            }

            try
            {
                var requestMessages = new List<object>();

                // Add system message if provided
                if (!string.IsNullOrEmpty(systemContext))
                {
                    requestMessages.Add(new { role = "system", content = systemContext });
                }

                // Add conversation messages
                foreach (var msg in messages)
                {
                    requestMessages.Add(new { role = msg.Role, content = msg.Content });
                }

                var requestBody = new
                {
                    model = _model,
                    messages = requestMessages,
                    max_tokens = _maxTokens,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                request.Content = content;

                _logger.Debug("Sending request to OpenAI ({0})...", _model);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.Debug("OpenAI response status: {0}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("OpenAI API error: {0} - {1}", response.StatusCode, responseBody);
                    throw new HttpRequestException($"OpenAI API error: {response.StatusCode}");
                }

                var openAiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var text = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrEmpty(text))
                {
                    _logger.Warn("OpenAI returned empty response");
                    return "I apologize, but I wasn't able to generate a response. Please try again.";
                }

                _logger.Debug("OpenAI response received: {0} chars", text.Length);
                return text;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("OpenAI request was cancelled");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.Exception(ex, "Network error communicating with OpenAI");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error calling OpenAI API");
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
                _apiKey = await SupabaseSecretsService.Instance.GetOpenAiApiKeyAsync();
                var model = await SupabaseSecretsService.Instance.GetOpenAiModelAsync();
                
                if (!string.IsNullOrEmpty(model))
                {
                    _model = model;
                }

                _isInitialized = true;
                _logger.Info("OpenAI service initialized with model: {0}", _model);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize OpenAI service");
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

        private class OpenAIResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("choices")]
            public List<Choice>? Choices { get; set; }

            [JsonPropertyName("usage")]
            public Usage? Usage { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("message")]
            public ResponseMessage? Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private class ResponseMessage
        {
            [JsonPropertyName("role")]
            public string? Role { get; set; }

            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        private class Usage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }

        #endregion
    }
}
