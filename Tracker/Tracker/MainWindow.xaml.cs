using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeepEndControls.Theming;
using Tracker.Command;
using Tracker.Eventing;
using Tracker.Managers;
using Tracker.Services;
using Tracker.ViewModels;
using Tracker.Views.Dialogs;

namespace Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Controls.BaseWindow
    {
        private RoutedEventHandler? _loadedHandler;
        private RoutedEventHandler? _unloadedHandler;
        private bool _forceClose = false;

        #region Static Navigation Command

        /// <summary>
        /// Static command for keyboard navigation to tabs (Ctrl+1-5).
        /// </summary>
        public static readonly RoutedCommand NavigateToTabCommand = new RoutedCommand("NavigateToTab", typeof(MainWindow));

        #endregion

        public MainWindow(TrackerMainViewModel dataContext)
        {
            DataContext = dataContext;
            InitializeComponent(); 
            
            // Register command bindings for navigation
            CommandBindings.Add(new CommandBinding(NavigateToTabCommand, OnNavigateToTab));
            
            // Set Dashboard ViewModel
            DashboardControl.DataContext = new DashboardViewModel();
            
            // Apply the current theme to this window for DeepEndControls
            DeepEndThemeManager.SetTheme(this, ThemeManager.Instance.CurrentTheme);
            
            // Initialize system tray
            InitializeSystemTray();
            
            // Start reminder service
            ReminderService.Instance.Start();
            
            // Ensure data loads after window is fully loaded
            _loadedHandler = async (_, _) =>
            {
                // Load all data into the shared TrackerDataManager cache
                // All ViewModels will bind to this single source of truth
                await TrackerDataManager.Instance.RefreshAllDataAsync();
                
                // Notify all ViewModels that data is ready
                // (Primarily needed for ViewModels that need to recalculate derived values)
                DataMessenger.SendRefreshAll();
                
                // Capture progress snapshots for predictive analytics (runs in background)
                _ = Services.Analytics.ProgressSnapshotService.Instance.CaptureSnapshotsIfNeededAsync();
                
                // Show daily briefing after data is loaded (with slight delay for smooth UX)
                await Task.Delay(500);
                await Views.Dialogs.DailyBriefingDialog.ShowIfEnabledAsync(this);
            };
            this.Loaded += _loadedHandler;
            
            _unloadedHandler = (_, _) =>
            { 
                if(DataContext is IDisposable vm) vm.Dispose();
                if(DashboardControl.DataContext is IDisposable dashboardVm) dashboardVm.Dispose();
                
                // Unsubscribe from events
                if (_loadedHandler != null)
                {
                    this.Loaded -= _loadedHandler;
                }
                if (_unloadedHandler != null)
                {
                    this.Unloaded -= _unloadedHandler;
                }
            };
            this.Unloaded += _unloadedHandler;
            
            // Handle closing
            this.Closing += MainWindow_Closing;
        }

        private void InitializeSystemTray()
        {
            SystemTrayService.Instance.Initialize();
            
            SystemTrayService.Instance.ShowWindowRequested += (_, _) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };
            
            SystemTrayService.Instance.ExitRequested += (_, _) =>
            {
                _forceClose = true;
                Application.Current.Shutdown();
            };
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            var settings = UserSettingsManager.Instance.ReminderSettings;
            
            // If minimize to tray is enabled and not force closing
            if (settings.MinimizeToTray && !_forceClose)
            {
                e.Cancel = true;
                this.Hide();
                SystemTrayService.Instance.Show();
                SystemTrayService.Instance.ShowBalloon(
                    "Tracker",
                    "Tracker is still running. Double-click the tray icon to open.",
                    System.Windows.Forms.ToolTipIcon.None,
                    2000
                );
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Stop services
            ReminderService.Instance.Stop();
            ReminderService.Instance.Dispose();
            SystemTrayService.Instance.Dispose();
            
            Application.Current.Shutdown();
        }

        private void TabChangedEventHandler(object sender, SelectionChangedEventArgs e)
        {
            // Only handle TabControl selection changes (not RadioButton changes within content)
            if (e.Source != TabControl) return;
            
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
            {
                // Simple slide animation for main content transitions
                AnimateContentTransition(selectedTab.Name);
            }
        }

        private void AnimateContentTransition(string tabName)
        {
            // Get the content element that corresponds to the selected tab
            FrameworkElement? content = tabName switch
            {
                "Home" => DashboardControl,
                "Circle" => FindName("CircleContent") as FrameworkElement,
                "Pulse" => FindName("PulseContent") as FrameworkElement,
                "Chronicle" => FindName("ChronicleContent") as FrameworkElement,
                "Settings" => SettingsControl,
                _ => null
            };

            // For pillars with sub-navigation, we don't need individual control animation
            // The content is managed by the RadioButton visibility bindings
            
            if (content == null && tabName != "Circle" && tabName != "Pulse" && tabName != "Chronicle")
            {
                return;
            }

            // Apply a subtle fade-in animation to the content area
            if (content != null)
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                content.BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            // Open the account/profile dialog
            var profileDialog = new AccountDialog { Owner = this };
            profileDialog.ShowDialog();
        }

        #region Navigation Command Handler

        /// <summary>
        /// Handles the Ctrl+1-5 navigation keyboard shortcuts.
        /// </summary>
        private void OnNavigateToTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is string indexStr && int.TryParse(indexStr, out int tabIndex))
            {
                if (tabIndex >= 0 && tabIndex < TabControl.Items.Count)
                {
                    TabControl.SelectedIndex = tabIndex;
                    
                    // Announce navigation change for screen readers
                    var tabItem = TabControl.Items[tabIndex] as TabItem;
                    if (tabItem != null)
                    {
                        // Focus the tab to allow screen readers to announce the change
                        tabItem.Focus();
                    }
                }
            }
        }

        #endregion
    }
}
