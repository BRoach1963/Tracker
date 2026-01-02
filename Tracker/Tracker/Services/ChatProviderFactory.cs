using Tracker.Common.Enums;
using Tracker.Interfaces;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services
{
    /// <summary>
    /// Supported AI providers.
    /// </summary>
    public enum AIProviderType
    {
        /// <summary>Google Gemini (default, free tier available)</summary>
        Gemini,
        
        /// <summary>OpenAI GPT models</summary>
        OpenAI,
        
        /// <summary>Anthropic Claude models</summary>
        Anthropic
    }

    /// <summary>
    /// Factory for creating and managing AI chat providers.
    /// Handles provider selection and lazy initialization.
    /// </summary>
    public class ChatProviderFactory : IDisposable
    {
        #region Singleton

        private static readonly Lazy<ChatProviderFactory> _instance =
            new(() => new ChatProviderFactory(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ChatProviderFactory Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly Dictionary<AIProviderType, IChatProvider> _providers = new();
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private AIProviderType _selectedProvider = AIProviderType.Gemini;
        private bool _disposed;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the selected provider changes.
        /// </summary>
        public event EventHandler<AIProviderType>? ProviderChanged;

        #endregion

        #region Constructor

        private ChatProviderFactory()
        {
            _logger = LoggingManager.GetComponentLogger("ChatProviderFactory");
            LoadSelectedProvider();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The currently selected AI provider.
        /// </summary>
        public AIProviderType SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                if (_selectedProvider != value)
                {
                    _selectedProvider = value;
                    SaveSelectedProvider();
                    ProviderChanged?.Invoke(this, value);
                    _logger.Info("AI provider changed to: {0}", value);
                }
            }
        }

        /// <summary>
        /// Gets a list of all available providers.
        /// </summary>
        public IReadOnlyList<AIProviderType> AvailableProviders => 
            Enum.GetValues<AIProviderType>().ToList();

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the currently selected chat provider.
        /// </summary>
        public async Task<IChatProvider> GetProviderAsync()
        {
            return await GetProviderAsync(_selectedProvider);
        }

        /// <summary>
        /// Gets a specific chat provider by type.
        /// </summary>
        public async Task<IChatProvider> GetProviderAsync(AIProviderType providerType)
        {
            await _initLock.WaitAsync();
            try
            {
                if (_providers.TryGetValue(providerType, out var existingProvider))
                {
                    return existingProvider;
                }

                var provider = await CreateProviderAsync(providerType);
                _providers[providerType] = provider;
                return provider;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Gets the display name for a provider type.
        /// </summary>
        public static string GetProviderDisplayName(AIProviderType provider) => provider switch
        {
            AIProviderType.Gemini => "Google Gemini",
            AIProviderType.OpenAI => "OpenAI (GPT)",
            AIProviderType.Anthropic => "Anthropic (Claude)",
            _ => provider.ToString()
        };

        /// <summary>
        /// Gets a description for a provider type.
        /// </summary>
        public static string GetProviderDescription(AIProviderType provider) => provider switch
        {
            AIProviderType.Gemini => "Google's Gemini AI. Fast, capable, and offers a generous free tier. Recommended for most users.",
            AIProviderType.OpenAI => "OpenAI's GPT models. Industry-leading quality. Uses your subscription credits.",
            AIProviderType.Anthropic => "Anthropic's Claude models. Excellent for nuanced conversations and analysis. Uses your subscription credits.",
            _ => "Unknown provider"
        };

        /// <summary>
        /// Checks if a provider is available (has API key configured).
        /// </summary>
        public async Task<bool> IsProviderAvailableAsync(AIProviderType provider)
        {
            try
            {
                var chatProvider = await GetProviderAsync(provider);
                return chatProvider.IsAvailable;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reinitializes all providers (useful after secrets are refreshed).
        /// </summary>
        public async Task ReinitializeProvidersAsync()
        {
            await _initLock.WaitAsync();
            try
            {
                // Dispose existing providers
                foreach (var provider in _providers.Values)
                {
                    if (provider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _providers.Clear();
                _logger.Info("All providers disposed and cleared for reinitialization");
            }
            finally
            {
                _initLock.Release();
            }
        }

        #endregion

        #region Private Methods

        private async Task<IChatProvider> CreateProviderAsync(AIProviderType providerType)
        {
            _logger.Debug("Creating provider: {0}", providerType);

            return providerType switch
            {
                AIProviderType.Gemini => await CreateGeminiProviderAsync(),
                AIProviderType.OpenAI => await CreateOpenAIProviderAsync(),
                AIProviderType.Anthropic => await CreateAnthropicProviderAsync(),
                _ => throw new ArgumentException($"Unknown provider type: {providerType}")
            };
        }

        private async Task<IChatProvider> CreateGeminiProviderAsync()
        {
            var provider = new GeminiChatService();
            // GeminiChatService loads from settings in constructor, but we should update it
            // to use SupabaseSecretsService. For now, return as-is.
            await Task.CompletedTask;
            return provider;
        }

        private async Task<IChatProvider> CreateOpenAIProviderAsync()
        {
            var provider = new OpenAIChatService();
            await provider.InitializeAsync();
            return provider;
        }

        private async Task<IChatProvider> CreateAnthropicProviderAsync()
        {
            var provider = new AnthropicChatService();
            await provider.InitializeAsync();
            return provider;
        }

        private void LoadSelectedProvider()
        {
            try
            {
                var settings = UserSettingsManager.Instance.Settings.AI;
                if (Enum.TryParse<AIProviderType>(settings.SelectedProvider, out var provider))
                {
                    _selectedProvider = provider;
                }
                _logger.Debug("Loaded selected provider: {0}", _selectedProvider);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load selected provider, using default");
            }
        }

        private void SaveSelectedProvider()
        {
            try
            {
                UserSettingsManager.Instance.Settings.AI.SelectedProvider = _selectedProvider.ToString();
                UserSettingsManager.Instance.SaveSettings();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to save selected provider");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var provider in _providers.Values)
                {
                    if (provider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _providers.Clear();
                _initLock.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
