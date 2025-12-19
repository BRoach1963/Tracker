using System.Drawing;
using System.Windows;
using Tracker.Logging;
using Tracker.Managers;
using WinForms = System.Windows.Forms;

namespace Tracker.Services
{
    /// <summary>
    /// Manages the system tray icon and context menu.
    /// Allows the app to minimize to tray and show notifications.
    /// </summary>
    public class SystemTrayService : IDisposable
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

        private readonly ILogger _logger;
        private WinForms.NotifyIcon? _notifyIcon;
        private bool _disposed;
        private bool _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Fired when user requests to show the main window.
        /// </summary>
        public event EventHandler? ShowWindowRequested;

        /// <summary>
        /// Fired when user requests to exit the application.
        /// </summary>
        public event EventHandler? ExitRequested;

        #endregion

        #region Constructor

        private SystemTrayService()
        {
            _logger = LoggingManager.GetComponentLogger("SystemTray");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the system tray icon.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                _notifyIcon = new WinForms.NotifyIcon
                {
                    Text = "Tracker - Team Management",
                    Visible = false
                };

                // Try to load the app icon, fall back to a default
                try
                {
                    // Try multiple possible icon locations
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var possiblePaths = new[]
                    {
                        System.IO.Path.Combine(baseDir, "Tracker.ico"),
                        System.IO.Path.Combine(baseDir, "tracker.ico"),
                        System.IO.Path.Combine(baseDir, "Images", "Tracker.ico")
                    };
                    
                    string? foundPath = null;
                    foreach (var path in possiblePaths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            foundPath = path;
                            break;
                        }
                    }
                    
                    if (foundPath != null)
                    {
                        _notifyIcon.Icon = new Icon(foundPath);
                        _logger.Info("Loaded tray icon from: {0}", foundPath);
                    }
                    else
                    {
                        // Extract icon from running executable as fallback
                        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        if (exePath != null)
                        {
                            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(exePath);
                            _logger.Info("Using extracted exe icon for tray");
                        }
                        else
                        {
                            _notifyIcon.Icon = SystemIcons.Application;
                            _logger.Warn("Using system default icon for tray");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn("Failed to load tray icon: {0}", ex.Message);
                    _notifyIcon.Icon = SystemIcons.Application;
                }

                // Create context menu
                var contextMenu = new WinForms.ContextMenuStrip();
                
                var openItem = new WinForms.ToolStripMenuItem("Open Tracker");
                openItem.Click += (s, e) => ShowWindowRequested?.Invoke(this, EventArgs.Empty);
                openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);
                contextMenu.Items.Add(openItem);

                contextMenu.Items.Add(new WinForms.ToolStripSeparator());

                var remindersItem = new WinForms.ToolStripMenuItem("Reminders Enabled");
                remindersItem.Checked = UserSettingsManager.Instance.ReminderSettings.EnableReminders;
                remindersItem.Click += (s, e) =>
                {
                    var settings = UserSettingsManager.Instance.ReminderSettings;
                    settings.EnableReminders = !settings.EnableReminders;
                    UserSettingsManager.Instance.ReminderSettings = settings;
                    ((WinForms.ToolStripMenuItem)s!).Checked = settings.EnableReminders;
                    
                    if (settings.EnableReminders)
                        ReminderService.Instance.Start();
                    else
                        ReminderService.Instance.Stop();
                };
                contextMenu.Items.Add(remindersItem);

                contextMenu.Items.Add(new WinForms.ToolStripSeparator());

                var exitItem = new WinForms.ToolStripMenuItem("Exit");
                exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;

                // Double-click opens the window
                _notifyIcon.DoubleClick += (s, e) => ShowWindowRequested?.Invoke(this, EventArgs.Empty);

                _isInitialized = true;
                _logger.Info("System tray initialized");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize system tray");
            }
        }

        /// <summary>
        /// Shows the tray icon.
        /// </summary>
        public void Show()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }

        /// <summary>
        /// Hides the tray icon.
        /// </summary>
        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        /// <summary>
        /// Shows a balloon notification.
        /// </summary>
        public void ShowBalloon(string title, string message, WinForms.ToolTipIcon icon = WinForms.ToolTipIcon.Info, int timeout = 3000)
        {
            try
            {
                if (_notifyIcon != null && _notifyIcon.Visible)
                {
                    _notifyIcon.ShowBalloonTip(timeout, title, message, icon);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error showing balloon notification");
            }
        }

        /// <summary>
        /// Updates the tray icon tooltip text.
        /// </summary>
        public void UpdateTooltip(string text)
        {
            if (_notifyIcon != null)
            {
                // NotifyIcon.Text has a 63 character limit
                _notifyIcon.Text = text.Length > 63 ? text.Substring(0, 60) + "..." : text;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }

            _disposed = true;
        }

        #endregion
    }
}
