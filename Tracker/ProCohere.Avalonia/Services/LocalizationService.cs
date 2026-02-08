using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Singleton service for managing application localization.
/// Provides access to localized strings and culture management.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    #region Singleton

    private static readonly Lazy<LocalizationService> _instance =
        new(() => new LocalizationService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static LocalizationService Instance => _instance.Value;

    #endregion

    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// Raised when the current culture changes. UI should refresh bindings.
    /// </summary>
    public event EventHandler? CultureChanged;

    #endregion

    #region Fields

    private ResourceManager? _resourceManager;
    private CultureInfo _currentCulture;
    private readonly Dictionary<string, string> _cache = new();

    #endregion

    #region Properties

    /// <summary>
    /// The current culture used for localization.
    /// </summary>
    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture?.Name != value?.Name)
            {
                _currentCulture = value ?? CultureInfo.CurrentUICulture;
                _cache.Clear(); // Clear cache on culture change
                OnCultureChanged();
            }
        }
    }

    /// <summary>
    /// List of supported cultures.
    /// </summary>
    public IReadOnlyList<CultureInfo> SupportedCultures { get; } = new List<CultureInfo>
    {
        new("en"),    // English (default)
        new("es"),    // Spanish
        new("fr"),    // French
        new("de"),    // German
        new("pt"),    // Portuguese
        new("ja"),    // Japanese
        new("zh"),    // Chinese (Simplified)
    };

    /// <summary>
    /// The current culture's display name.
    /// </summary>
    public string CurrentCultureDisplayName => _currentCulture.NativeName;

    #endregion

    #region Constructor

    private LocalizationService()
    {
        // Default to system culture, fallback to English
        _currentCulture = CultureInfo.CurrentUICulture;
        
        // Initialize resource manager pointing to our Strings resources
        try
        {
            _resourceManager = new ResourceManager(
                "ProCohere.Avalonia.Resources.Strings.Strings",
                typeof(LocalizationService).Assembly);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize ResourceManager: {ex.Message}");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key itself if not found.</returns>
    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        // Check cache first
        var cacheKey = $"{_currentCulture.Name}:{key}";
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        // Try to get from resources
        string? value = null;
        try
        {
            value = _resourceManager?.GetString(key, _currentCulture);
        }
        catch
        {
            // Resource not found
        }

        // Fallback to key if not found (makes missing translations obvious)
        var result = value ?? $"[{key}]";
        
        // Cache the result
        _cache[cacheKey] = result;
        
        return result;
    }

    /// <summary>
    /// Gets a localized string with format arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    public string GetFormat(string key, params object[] args)
    {
        var format = Get(key);
        try
        {
            return string.Format(_currentCulture, format, args);
        }
        catch
        {
            return format; // Return unformatted if format fails
        }
    }

    /// <summary>
    /// Sets the current culture by language code.
    /// </summary>
    /// <param name="languageCode">Two-letter language code (e.g., "en", "es").</param>
    public void SetCulture(string languageCode)
    {
        try
        {
            CurrentCulture = new CultureInfo(languageCode);
        }
        catch
        {
            CurrentCulture = new CultureInfo("en"); // Fallback to English
        }
    }

    /// <summary>
    /// Clears the string cache. Call after loading new resources.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    #endregion

    #region Private Methods

    private void OnCultureChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCultureDisplayName)));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}

/// <summary>
/// Static shorthand for accessing localized strings.
/// Usage: Loc.S("Key") or Loc.F("Key", arg1, arg2)
/// </summary>
public static class Loc
{
    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    public static string S(string key) => LocalizationService.Instance.Get(key);

    /// <summary>
    /// Gets a formatted localized string.
    /// </summary>
    public static string F(string key, params object[] args) => LocalizationService.Instance.GetFormat(key, args);
}
