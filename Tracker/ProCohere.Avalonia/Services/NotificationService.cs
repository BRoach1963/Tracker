using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Views.Toasts;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Manages in-app toast notifications and native Windows notifications.
/// Provides methods to show different types of notifications with automatic stacking and dismissal.
/// When the main window is hidden (minimized to tray), native Windows toasts are shown instead.
/// </summary>
public class NotificationService
{
    #region Fields

    private readonly List<ProCohereToast> _activeToasts = new();
    private readonly object _toastLock = new();

    /// <summary>
    /// Function to check if the main window is visible.
    /// Set by App.axaml.cs during initialization.
    /// </summary>
    public Func<bool>? IsMainWindowVisible { get; set; }

    #endregion

    #region Singleton Instance

    private static readonly Lazy<NotificationService> _lazyInstance =
        new(() => new NotificationService(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets the singleton instance of NotificationService.
    /// </summary>
    public static NotificationService Instance => _lazyInstance.Value;

    #endregion

    #region Constructor

    private NotificationService() { }

    #endregion

    #region Public Methods - In-App Toasts

    /// <summary>
    /// Shows an information toast.
    /// </summary>
    /// <param name="title">The toast title.</param>
    /// <param name="message">The toast message.</param>
    /// <param name="durationSeconds">Auto-dismiss duration in seconds.</param>
    public void ShowInfo(string title, string message, int durationSeconds = 5)
    {
        ShowToast(title, message, ToastType.Information, durationSeconds);
    }

    /// <summary>
    /// Shows a success toast.
    /// </summary>
    /// <param name="title">The toast title.</param>
    /// <param name="message">The toast message.</param>
    /// <param name="durationSeconds">Auto-dismiss duration in seconds.</param>
    public void ShowSuccess(string title, string message, int durationSeconds = 5)
    {
        ShowToast(title, message, ToastType.Success, durationSeconds);
    }

    /// <summary>
    /// Shows a warning toast.
    /// </summary>
    /// <param name="title">The toast title.</param>
    /// <param name="message">The toast message.</param>
    /// <param name="durationSeconds">Auto-dismiss duration in seconds.</param>
    public void ShowWarning(string title, string message, int durationSeconds = 5)
    {
        ShowToast(title, message, ToastType.Warning, durationSeconds);
    }

    /// <summary>
    /// Shows an error toast.
    /// </summary>
    /// <param name="title">The toast title.</param>
    /// <param name="message">The toast message.</param>
    /// <param name="durationSeconds">Auto-dismiss duration in seconds (default 7 for errors).</param>
    public void ShowError(string title, string message, int durationSeconds = 7)
    {
        ShowToast(title, message, ToastType.Error, durationSeconds);
    }

    /// <summary>
    /// Shows a toast notification with the specified type.
    /// If the main window is hidden, shows a native Windows toast instead.
    /// </summary>
    /// <param name="title">The toast title.</param>
    /// <param name="message">The toast message.</param>
    /// <param name="type">The toast type (determines color and icon).</param>
    /// <param name="durationSeconds">Auto-dismiss duration in seconds.</param>
    public void ShowToast(string title, string message, ToastType type = ToastType.Information, int durationSeconds = 5)
    {
        // If main window is hidden (minimized to tray), show native toast on Windows
        if (IsMainWindowVisible?.Invoke() == false)
        {
            SendNativeToast(title, message);
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            var toast = new ProCohereToast(title, message, type, durationSeconds);

            lock (_toastLock)
            {
                // Set stack position based on existing toasts
                toast.SetStackOffset(_activeToasts.Count);
                _activeToasts.Add(toast);
            }

            toast.Closed += (s, e) => OnToastClosed(toast);
            toast.Show();
        });
    }

    #endregion

    #region Public Methods - Native Windows Toasts

    /// <summary>
    /// Sends a native Windows toast notification.
    /// Only works on Windows; silently ignored on other platforms.
    /// </summary>
    /// <param name="title">The toast title.</param>
    /// <param name="message">The toast message.</param>
    public void SendNativeToast(string title, string message)
    {
        // Only supported on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationService] Native toast failed: {ex.Message}");
        }
    }

    #endregion

    #region Public Methods - Cleanup

    /// <summary>
    /// Closes all active toast notifications.
    /// Called during application shutdown to ensure clean exit.
    /// </summary>
    public void CloseAllToasts()
    {
        lock (_toastLock)
        {
            foreach (var toast in _activeToasts.ToList())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        toast.Close();
                    }
                    catch
                    {
                        // Ignore errors during shutdown
                    }
                });
            }
            _activeToasts.Clear();
        }
    }

    #endregion

    #region Private Methods

    private void OnToastClosed(ProCohereToast closedToast)
    {
        lock (_toastLock)
        {
            _activeToasts.Remove(closedToast);

            // Reposition remaining toasts
            for (int i = 0; i < _activeToasts.Count; i++)
            {
                var toast = _activeToasts[i];
                var index = i; // Capture for closure
                Dispatcher.UIThread.Post(() =>
                {
                    toast.AnimateToStackPosition(index);
                });
            }
        }
    }

    #endregion
}
