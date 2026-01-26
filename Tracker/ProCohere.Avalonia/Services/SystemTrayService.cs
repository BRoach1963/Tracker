using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Manages the system tray icon and provides events for tray interactions.
/// The actual TrayIcon is declared in App.axaml; this service coordinates behavior.
/// </summary>
public class SystemTrayService
{
    #region Singleton

    private static readonly Lazy<SystemTrayService> _lazyInstance =
        new(() => new SystemTrayService(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets the singleton instance of SystemTrayService.
    /// </summary>
    public static SystemTrayService Instance => _lazyInstance.Value;

    #endregion

    #region Fields

    private TrayIcon? _trayIcon;
    private bool _isInitialized;

    #endregion

    #region Events

    /// <summary>
    /// Fired when user requests to show the main window (double-click or "Open" menu).
    /// </summary>
    public event EventHandler? ShowWindowRequested;

    /// <summary>
    /// Fired when user requests to exit the application from tray menu.
    /// </summary>
    public event EventHandler? ExitRequested;

    #endregion

    #region Constructor

    private SystemTrayService()
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the system tray service with the tray icon from App.axaml.
    /// Call this after the application has started.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;

        // Get the TrayIcon from the application's TrayIcons collection
        if (Application.Current is App app)
        {
            var icons = TrayIcon.GetIcons(app);
            if (icons != null && icons.Count > 0)
            {
                _trayIcon = icons[0];
                
                // Wire up the Clicked event (double-click on Windows)
                _trayIcon.Clicked += OnTrayIconClicked;
            }
        }

        _isInitialized = true;
        System.Diagnostics.Debug.WriteLine("SystemTrayService initialized");
    }

    /// <summary>
    /// Shows the main application window.
    /// </summary>
    public void RequestShowWindow()
    {
        ShowWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Requests application exit.
    /// </summary>
    public void RequestExit()
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shows the tray icon (makes it visible).
    /// </summary>
    public void Show()
    {
        if (_trayIcon != null)
        {
            _trayIcon.IsVisible = true;
        }
    }

    /// <summary>
    /// Hides the tray icon.
    /// </summary>
    public void Hide()
    {
        if (_trayIcon != null)
        {
            _trayIcon.IsVisible = false;
        }
    }

    /// <summary>
    /// Gets whether the tray icon is currently visible.
    /// </summary>
    public bool IsVisible => _trayIcon?.IsVisible ?? false;

    /// <summary>
    /// Updates the tray icon tooltip text.
    /// </summary>
    public void UpdateTooltip(string text)
    {
        if (_trayIcon != null)
        {
            _trayIcon.ToolTipText = text;
        }
    }

    #endregion

    #region Event Handlers

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        // Tray icon clicked (double-click on Windows) - show window
        ShowWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleanup resources when application is shutting down.
    /// </summary>
    public void Dispose()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Clicked -= OnTrayIconClicked;
            _trayIcon.IsVisible = false;
        }
    }

    #endregion
}
