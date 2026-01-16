using System;
using Avalonia;
using Avalonia.Styling;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing application theme (Light/Dark mode).
/// </summary>
public class ThemeService
{
    #region Singleton

    private static ThemeService? _instance;
    private static readonly object _lock = new();

    public static ThemeService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ThemeService();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the theme changes.
    /// </summary>
    public event Action<bool>? ThemeChanged;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether dark theme is active.
    /// </summary>
    public bool IsDarkTheme
    {
        get => LocalSettingsService.Instance.IsDarkTheme;
        set
        {
            if (LocalSettingsService.Instance.IsDarkTheme != value)
            {
                LocalSettingsService.Instance.IsDarkTheme = value;
                ApplyTheme(value);
                ThemeChanged?.Invoke(value);
            }
        }
    }

    #endregion

    #region Constructor

    private ThemeService()
    {
        // Apply saved theme on startup
        ApplyTheme(LocalSettingsService.Instance.IsDarkTheme);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    public void ApplyTheme(bool isDark)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = isDark 
                ? ThemeVariant.Dark 
                : ThemeVariant.Light;
        }
    }

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    /// <summary>
    /// Initializes the theme service and applies the saved theme.
    /// Call this after the Application is initialized.
    /// </summary>
    public void Initialize()
    {
        ApplyTheme(LocalSettingsService.Instance.IsDarkTheme);
    }

    #endregion
}
