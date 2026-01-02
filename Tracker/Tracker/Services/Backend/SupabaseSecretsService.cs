using System.Collections.Concurrent;
using Tracker.Logging;

namespace Tracker.Services.Backend
{
    /// <summary>
    /// Service for securely fetching API keys and secrets from Supabase app_secrets table.
    /// Keys are cached in memory and never stored locally.
    /// </summary>
    public class SupabaseSecretsService
    {
        #region Singleton

        private static readonly Lazy<SupabaseSecretsService> _instance =
            new(() => new SupabaseSecretsService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SupabaseSecretsService Instance => _instance.Value;

        #endregion

        #region Constants

        // Known secret key names
        public const string GeminiApiKey = "gemini_api_key";
        public const string GeminiModel = "gemini_model";
        public const string OpenAiApiKey = "openai_api_key";
        public const string OpenAiModel = "openai_model";
        public const string AnthropicApiKey = "anthropic_api_key";
        public const string AnthropicModel = "anthropic_model";

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, string> _secretsCache = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private bool _isLoaded;
        private DateTime _lastLoadTime = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

        #endregion

        #region Constructor

        private SupabaseSecretsService()
        {
            _logger = LoggingManager.GetComponentLogger("SupabaseSecrets");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a secret value by key name. Returns null if not found.
        /// </summary>
        public async Task<string?> GetSecretAsync(string keyName)
        {
            await EnsureSecretsLoadedAsync();
            return _secretsCache.TryGetValue(keyName, out var value) ? value : null;
        }

        /// <summary>
        /// Gets the Gemini API key.
        /// </summary>
        public async Task<string?> GetGeminiApiKeyAsync() => await GetSecretAsync(GeminiApiKey);

        /// <summary>
        /// Gets the Gemini model name.
        /// </summary>
        public async Task<string?> GetGeminiModelAsync() => await GetSecretAsync(GeminiModel);

        /// <summary>
        /// Gets the OpenAI API key.
        /// </summary>
        public async Task<string?> GetOpenAiApiKeyAsync() => await GetSecretAsync(OpenAiApiKey);

        /// <summary>
        /// Gets the OpenAI model name.
        /// </summary>
        public async Task<string?> GetOpenAiModelAsync() => await GetSecretAsync(OpenAiModel);

        /// <summary>
        /// Gets the Anthropic API key.
        /// </summary>
        public async Task<string?> GetAnthropicApiKeyAsync() => await GetSecretAsync(AnthropicApiKey);

        /// <summary>
        /// Gets the Anthropic model name.
        /// </summary>
        public async Task<string?> GetAnthropicModelAsync() => await GetSecretAsync(AnthropicModel);

        /// <summary>
        /// Forces a reload of all secrets from Supabase.
        /// </summary>
        public async Task RefreshSecretsAsync()
        {
            _isLoaded = false;
            _secretsCache.Clear();
            await EnsureSecretsLoadedAsync();
        }

        /// <summary>
        /// Checks if secrets have been loaded successfully.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        #endregion

        #region Private Methods

        private async Task EnsureSecretsLoadedAsync()
        {
            // Check if cache is expired
            if (_isLoaded && DateTime.UtcNow - _lastLoadTime < _cacheExpiry)
            {
                return;
            }

            await _loadLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_isLoaded && DateTime.UtcNow - _lastLoadTime < _cacheExpiry)
                {
                    return;
                }

                await LoadSecretsFromSupabaseAsync();
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private async Task LoadSecretsFromSupabaseAsync()
        {
            try
            {
                _logger.Info("Loading secrets from Supabase...");

                // Ensure Supabase is initialized and user is logged in
                if (!SupabaseService.Instance.IsInitialized)
                {
                    _logger.Warn("Supabase not initialized, cannot load secrets");
                    return;
                }

                if (!SupabaseService.Instance.IsSignedIn)
                {
                    _logger.Warn("User not signed in, cannot load secrets");
                    return;
                }

                // Query the app_secrets table
                var client = GetSupabaseClient();
                if (client == null)
                {
                    _logger.Error("Could not get Supabase client");
                    return;
                }

                var response = await client
                    .From<AppSecret>()
                    .Select("key_name, key_value")
                    .Get();

                if (response.Models == null || response.Models.Count == 0)
                {
                    _logger.Warn("No secrets found in app_secrets table");
                    return;
                }

                // Clear and reload cache
                _secretsCache.Clear();
                foreach (var secret in response.Models)
                {
                    if (!string.IsNullOrEmpty(secret.KeyName) && !string.IsNullOrEmpty(secret.KeyValue))
                    {
                        _secretsCache[secret.KeyName] = secret.KeyValue;
                        _logger.Debug("Loaded secret: {0}", secret.KeyName);
                    }
                }

                _isLoaded = true;
                _lastLoadTime = DateTime.UtcNow;
                _logger.Info("Successfully loaded {0} secrets from Supabase", _secretsCache.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load secrets from Supabase");
            }
        }

        private Supabase.Client? GetSupabaseClient()
        {
            // Use reflection to get the private client from SupabaseService
            // This is not ideal but necessary to reuse the existing authenticated client
            var field = typeof(SupabaseService).GetField("_client", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(SupabaseService.Instance) as Supabase.Client;
        }

        #endregion

        #region Inner Classes

        /// <summary>
        /// Model for app_secrets table row.
        /// </summary>
        [Supabase.Postgrest.Attributes.Table("app_secrets")]
        private class AppSecret : Supabase.Postgrest.Models.BaseModel
        {
            [Supabase.Postgrest.Attributes.Column("key_name")]
            public string? KeyName { get; set; }

            [Supabase.Postgrest.Attributes.Column("key_value")]
            public string? KeyValue { get; set; }
        }

        #endregion
    }
}
