using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Input;
using ProCohere.Avalonia.Commands;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the About Dialog.
/// Displays app version, copyright, and links.
/// </summary>
public class AboutDialogViewModel : ViewModelBase
{
    #region Properties

    /// <summary>
    /// Application version string.
    /// </summary>
    public string VersionText { get; }

    /// <summary>
    /// Copyright text.
    /// </summary>
    public string CopyrightText => "© 2026 Prickly Cactus Software";

    #endregion

    #region Commands

    public ICommand OpenWebsiteCommand { get; }
    public ICommand OpenSupportCommand { get; }
    public ICommand CloseCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;

    #endregion

    #region Constructor

    public AboutDialogViewModel()
    {
        // Get version from assembly
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText = version != null 
            ? $"Version {version.Major}.{version.Minor}.{version.Build}" 
            : "Version 1.0.0";

        // Initialize commands
        OpenWebsiteCommand = new TrackerCommand(_ => OpenUrl("https://pricklycactussoftware.com/"));
        OpenSupportCommand = new TrackerCommand(_ => OpenUrl("https://pricklycactussoftware.com/contact/"));
        CloseCommand = new TrackerCommand(_ => CloseRequested?.Invoke());
    }

    #endregion

    #region Methods

    /// <summary>
    /// Opens a URL in the default browser.
    /// </summary>
    private static void OpenUrl(string url)
    {
        try
        {
            // Cross-platform URL opening
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AboutDialogViewModel] Failed to open URL: {ex.Message}");
        }
    }

    #endregion
}
