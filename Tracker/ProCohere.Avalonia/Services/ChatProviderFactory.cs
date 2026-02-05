using System;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Factory for creating and managing AI chat providers.
/// Simplified for Gemini-only implementation to reduce complexity.
/// </summary>
public sealed class ChatProviderFactory : IDisposable
{
    #region Singleton

    private static readonly Lazy<ChatProviderFactory> _instance = 
        new(() => new ChatProviderFactory(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static ChatProviderFactory Instance => _instance.Value;

    #endregion

    #region Fields

    private GeminiChatService? _geminiProvider;
    private bool _disposed;
    private readonly object _lock = new();

    #endregion

    #region Constructor

    private ChatProviderFactory() { }

    #endregion

    #region Public Properties

    /// <summary>
    /// Currently selected provider (Gemini-only for cost efficiency).
    /// </summary>
    public AIProviderType SelectedProvider => AIProviderType.Gemini;

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the configured AI provider instance.
    /// </summary>
    /// <returns>The AI provider instance</returns>
    public async Task<IChatProvider> GetProviderAsync()
    {
        await Task.CompletedTask; // For potential async initialization

        lock (_lock)
        {
            if (_geminiProvider == null)
            {
                _geminiProvider = new GeminiChatService();
            }

            return _geminiProvider;
        }
    }

    /// <summary>
    /// Gets whether any provider is available and configured.
    /// </summary>
    public async Task<bool> IsAnyProviderAvailableAsync()
    {
        var provider = await GetProviderAsync();
        return provider.IsAvailable;
    }

    /// <summary>
    /// Gets the name of the current provider for UI display.
    /// </summary>
    public async Task<string> GetCurrentProviderNameAsync()
    {
        var provider = await GetProviderAsync();
        return provider.ProviderName;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                _geminiProvider?.Dispose();
                _geminiProvider = null;
            }
            
            _disposed = true;
        }
    }

    #endregion
}