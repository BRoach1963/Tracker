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
                
                // Style the context menu for modern appearance
                contextMenu.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
                contextMenu.ForeColor = System.Drawing.Color.White;
                contextMenu.Renderer = new ModernMenuRenderer();
                
                var openItem = new WinForms.ToolStripMenuItem("Open Tracker");
                openItem.Click += (s, e) => ShowWindowRequested?.Invoke(this, EventArgs.Empty);
                openItem.Font = new Font(openItem.Font.FontFamily, 10f, System.Drawing.FontStyle.Bold);
                openItem.ForeColor = System.Drawing.Color.White;
                contextMenu.Items.Add(openItem);

                contextMenu.Items.Add(new WinForms.ToolStripSeparator());

                var remindersItem = new WinForms.ToolStripMenuItem("Reminders Enabled");
                remindersItem.ForeColor = System.Drawing.Color.White;
                remindersItem.CheckOnClick = true;
                remindersItem.Checked = UserSettingsManager.Instance.ReminderSettings.EnableReminders;
                remindersItem.CheckedChanged += (s, e) =>
                {
                    var settings = UserSettingsManager.Instance.ReminderSettings;
                    settings.EnableReminders = ((WinForms.ToolStripMenuItem)s!).Checked;
                    UserSettingsManager.Instance.ReminderSettings = settings;
                    
                    if (settings.EnableReminders)
                        ReminderService.Instance.Start();
                    else
                        ReminderService.Instance.Stop();
                };
                contextMenu.Items.Add(remindersItem);

                contextMenu.Items.Add(new WinForms.ToolStripSeparator());

                var exitItem = new WinForms.ToolStripMenuItem("Exit");
                exitItem.ForeColor = System.Drawing.Color.White;
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
    
    /// <summary>
    /// Custom renderer for modern dark-themed context menu.
    /// </summary>
    internal class ModernMenuRenderer : WinForms.ToolStripProfessionalRenderer
    {
        public ModernMenuRenderer() : base(new ModernMenuColorTable()) { }
        
        protected override void OnRenderMenuItemBackground(WinForms.ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(62, 62, 66));
                e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(System.Drawing.Point.Empty, e.Item.Size));
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
        
        protected override void OnRenderItemCheck(WinForms.ToolStripItemImageRenderEventArgs e)
        {
            // Custom checkbox rendering
            if (e.Item is WinForms.ToolStripMenuItem menuItem && menuItem.Checked)
            {
                var checkRect = new System.Drawing.Rectangle(4, 4, 16, 16);
                
                // Draw checkbox background
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 122, 204)))
                {
                    e.Graphics.FillRectangle(brush, checkRect);
                }
                
                // Draw checkmark
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
                {
                    e.Graphics.DrawLine(pen, checkRect.Left + 4, checkRect.Top + 8, checkRect.Left + 7, checkRect.Bottom - 5);
                    e.Graphics.DrawLine(pen, checkRect.Left + 7, checkRect.Bottom - 5, checkRect.Right - 4, checkRect.Top + 4);
                }
            }
        }
        
        protected override void OnRenderSeparator(WinForms.ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(62, 62, 66));
            var rect = new System.Drawing.Rectangle(25, 3, e.Item.Width - 25, 1);
            e.Graphics.DrawLine(pen, rect.Left, rect.Top, rect.Right, rect.Top);
        }
    }
    
    /// <summary>
    /// Color table for modern dark-themed context menu.
    /// </summary>
    internal class ModernMenuColorTable : WinForms.ProfessionalColorTable
    {
        public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(62, 62, 66);
        public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(62, 62, 66);
        public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(62, 62, 66);
        public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(62, 62, 66);
        public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(62, 62, 66);
        public override System.Drawing.Color MenuItemPressedGradientBegin => System.Drawing.Color.FromArgb(62, 62, 66);
        public override System.Drawing.Color MenuItemPressedGradientEnd => System.Drawing.Color.FromArgb(62, 62, 66);
        public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.FromArgb(45, 45, 48);
        public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.FromArgb(45, 45, 48);
        public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.FromArgb(45, 45, 48);
        public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.FromArgb(45, 45, 48);
    }
}
