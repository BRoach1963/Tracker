using Avalonia.Controls;
using ProCohere.Avalonia.Views.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Centralized service for showing confirmation and alert dialogs from ViewModels.
/// Similar pattern to NotificationService - a singleton that gets the main window reference.
/// 
/// Usage:
/// - Call Initialize() from App.axaml.cs after main window is created
/// - Use ShowConfirmationAsync/ShowDestructiveConfirmationAsync for delete confirmations
/// - Use ShowErrorAsync/ShowInfoAsync/ShowWarningAsync for alert dialogs
/// </summary>
public class ConfirmationService
{
    #region Singleton Instance

    private static readonly Lazy<ConfirmationService> _lazyInstance =
        new(() => new ConfirmationService(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets the singleton instance of ConfirmationService.
    /// </summary>
    public static ConfirmationService Instance => _lazyInstance.Value;

    #endregion

    #region Fields

    private Window? _mainWindow;

    #endregion

    #region Constructor

    private ConfirmationService() { }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the service with the main window reference.
    /// Call from App.axaml.cs after the main window is created.
    /// </summary>
    public void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
    }

    #endregion

    #region Confirmation Dialogs

    /// <summary>
    /// Shows a confirmation dialog and returns the user's choice.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Message to display</param>
    /// <param name="confirmText">Text for confirm button (default: "Confirm")</param>
    /// <param name="cancelText">Text for cancel button (default: "Cancel")</param>
    /// <returns>True if user confirmed, false if cancelled</returns>
    public async Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel")
    {
        if (_mainWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("[ConfirmationService] Main window not set - returning false");
            return false;
        }

        var dialog = new ConfirmationDialog(
            title, 
            message, 
            confirmText, 
            cancelText, 
            ConfirmationDialog.ConfirmationType.Default);
        await dialog.ShowDialog(_mainWindow);
        return dialog.IsConfirmed;
    }

    /// <summary>
    /// Shows a destructive action confirmation dialog (styled with danger colors).
    /// Use for delete operations and other destructive actions.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Message to display</param>
    /// <param name="confirmText">Text for confirm button (default: "Delete")</param>
    /// <param name="cancelText">Text for cancel button (default: "Cancel")</param>
    /// <returns>True if user confirmed, false if cancelled</returns>
    public async Task<bool> ShowDestructiveConfirmationAsync(
        string title,
        string message,
        string confirmText = "Delete",
        string cancelText = "Cancel")
    {
        if (_mainWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("[ConfirmationService] Main window not set - returning false");
            return false;
        }

        var dialog = new ConfirmationDialog(
            title, 
            message, 
            confirmText, 
            cancelText, 
            ConfirmationDialog.ConfirmationType.Destructive);
        await dialog.ShowDialog(_mainWindow);
        return dialog.IsConfirmed;
    }

    /// <summary>
    /// Shows an exit confirmation dialog (styled with exit/door icon).
    /// Use for app exit or logout confirmations.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Message to display</param>
    /// <param name="confirmText">Text for confirm button (default: "Exit")</param>
    /// <param name="cancelText">Text for cancel button (default: "Cancel")</param>
    /// <returns>True if user confirmed, false if cancelled</returns>
    public async Task<bool> ShowExitConfirmationAsync(
        string title,
        string message,
        string confirmText = "Exit",
        string cancelText = "Cancel")
    {
        if (_mainWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("[ConfirmationService] Main window not set - returning false");
            return false;
        }

        var dialog = new ConfirmationDialog(
            title, 
            message, 
            confirmText, 
            cancelText, 
            ConfirmationDialog.ConfirmationType.Exit);
        await dialog.ShowDialog(_mainWindow);
        return dialog.IsConfirmed;
    }

    #endregion

    #region Alert Dialogs

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
    {
        if (_mainWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("[ConfirmationService] Main window not set - cannot show error dialog");
            return;
        }

        var dialog = new AlertDialog(title, message, AlertDialog.AlertType.Error);
        await dialog.ShowDialog(_mainWindow);
    }

    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    public async Task ShowInfoAsync(string title, string message)
    {
        if (_mainWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("[ConfirmationService] Main window not set - cannot show info dialog");
            return;
        }

        var dialog = new AlertDialog(title, message, AlertDialog.AlertType.Information);
        await dialog.ShowDialog(_mainWindow);
    }

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    public async Task ShowWarningAsync(string title, string message)
    {
        if (_mainWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("[ConfirmationService] Main window not set - cannot show warning dialog");
            return;
        }

        var dialog = new AlertDialog(title, message, AlertDialog.AlertType.Warning);
        await dialog.ShowDialog(_mainWindow);
    }

    /// <summary>
    /// Shows a success message dialog.
    /// </summary>
    public async Task ShowSuccessAsync(string title, string message)
    {
        if (_mainWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("[ConfirmationService] Main window not set - cannot show success dialog");
            return;
        }

        var dialog = new AlertDialog(title, message, AlertDialog.AlertType.Success);
        await dialog.ShowDialog(_mainWindow);
    }

    #endregion
}
