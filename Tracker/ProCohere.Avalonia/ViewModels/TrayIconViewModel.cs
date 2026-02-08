using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the system tray icon context menu.
/// Provides commands and state for the NativeMenu in App.axaml.
/// </summary>
public partial class TrayIconViewModel : ObservableObject
{
    #region Observable Properties

    /// <summary>
    /// Gets or sets whether reminders are enabled.
    /// Persisted to local settings.
    /// </summary>
    [ObservableProperty]
    private bool _remindersEnabled;

    /// <summary>
    /// Tooltip text shown when hovering over the tray icon.
    /// </summary>
    [ObservableProperty]
    private string _toolTipText = "ProCohere - Team Coaching";

    #endregion

    #region Constructor

    public TrayIconViewModel()
    {
        // Load initial state from settings
        _remindersEnabled = LocalSettingsService.Instance.EnableReminders;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to open/show the main application window.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        SystemTrayService.Instance.RequestShowWindow();
    }

    /// <summary>
    /// Command to exit the application completely.
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        SystemTrayService.Instance.RequestExit();
    }

    #endregion

    #region Property Changed Handlers

    partial void OnRemindersEnabledChanged(bool value)
    {
        // Persist to settings
        LocalSettingsService.Instance.EnableReminders = value;
        System.Diagnostics.Debug.WriteLine($"Reminders enabled: {value}");
    }

    #endregion
}
