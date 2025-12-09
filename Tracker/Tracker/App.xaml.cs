using System.Runtime.InteropServices;
using System.Windows;
using DeepEndControls.Theming;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.ViewModels;
using Tracker.ViewModels.DialogViewModels;
using Tracker.Views;
using Tracker.Views.Dialogs;

namespace Tracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Views.SplashScreen? _splashScreen;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

        public static void SetAppUserModelId(string appId)
        {
            SetCurrentProcessExplicitAppUserModelID(appId);
        }

        public App()
        {
            //TODO: Currently 7-day license key - replace with Community license key when available
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NCaF1cWWhAYVJ2WmFZfVpgcl9GYlZVQmYuP1ZhSXxXdkxjWn9YcHZRQGFYWEM=");
            RegisterAppForToastNotifications();
        }

        private void InitializeTheme()
        {
            // Load user settings first to get the saved theme preference
            UserSettingsManager.Instance.Initialize();
            
            // Apply the saved theme (or default if none saved)
            ThemeManager.Instance.Initialize(UserSettingsManager.Instance.Settings.Theme);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            UserSettingsManager.Instance.Shutdown();
            LoggingManager.Instance.Shutdown();
            TrackerDataManager.Instance.Shutdown();
            TrackerDbManager.Instance?.Shutdown();
        }

        private void RegisterAppForToastNotifications()
        {
            SetAppUserModelId("tracker.diveccosoftware.trackerapp");
        }

        private async void OnAppStartup(object sender, StartupEventArgs e)
        {
            // Prevent app from closing when splash screen closes
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Initialize theme first (required for UI)
            try
            {
                InitializeTheme();
            }
            catch
            {
                // Theme failure is non-fatal, defaults will be used
            }

            // Check if this is first launch (setup not completed)
            if (!UserSettingsManager.Instance.Settings.Database.SetupCompleted)
            {
                ShowSetupWizard();
                return;
            }

            // Normal startup flow
            await ContinueNormalStartup();
        }

        private void ShowSetupWizard()
        {
            var setupVm = new SetupWizardViewModel(() =>
            {
                // Setup completed, continue with normal startup
                _ = ContinueNormalStartup();
            });

            var setupWindow = new SetupWizard
            {
                DataContext = setupVm,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            setupWindow.Closed += (s, e) =>
            {
                // If setup was cancelled (window closed without completing), shut down
                if (!UserSettingsManager.Instance.Settings.Database.SetupCompleted)
                {
                    Shutdown();
                }
            };

            setupWindow.Show();
        }

        private async Task ContinueNormalStartup()
        {
            // Show splash screen
            _splashScreen = new Views.SplashScreen();
            
            await Current.Dispatcher.InvokeAsync(() => _splashScreen.Show());

            string? warningMessage = null;

            try
            {
                // Initialize application components with progress updates
                var initResult = await InitializeApplicationAsync();
                warningMessage = initResult.WarningMessage;
            }
            catch (Exception ex)
            {
                // Show error dialog instead of silently dying
                await Current.Dispatcher.InvokeAsync(() => _splashScreen?.Close());
                
                var result = MessageBox.Show(
                    $"An error occurred during startup:\n\n{ex.Message}\n\nWould you like to continue anyway (some features may not work)?",
                    "Startup Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (result != MessageBoxResult.Yes)
                {
                    Shutdown();
                    return;
                }
            }

            // Show login dialog BEFORE closing splash to prevent app shutdown
            await Current.Dispatcher.InvokeAsync(() => ShowLoginDialog());
            
            // Now restore normal shutdown mode
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            
            // Close splash with animation
            _splashScreen?.CloseSplash(() =>
            {
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    NotificationManager.Instance.ShowWarning("Startup Warning", warningMessage, 10);
                }
            });
        }

        private record InitializationResult(bool Success, string? WarningMessage = null);

        private async Task<InitializationResult> InitializeApplicationAsync()
        {
            string? warningMessage = null;
            var dbSettings = UserSettingsManager.Instance.Settings.Database;

            // Stage 1: Initialize logging (20%)
            _splashScreen?.UpdateStatus("Initializing logging...");
            _splashScreen?.UpdateProgress(20);
            try
            {
                await Task.Run(() => _ = LoggingManager.Instance);
            }
            catch
            {
                // Logging failure is non-fatal
            }
            await Task.Delay(150);

            // Stage 2: Initialize database manager (50%)
            _splashScreen?.UpdateStatus("Connecting to database...");
            _splashScreen?.UpdateProgress(50);
            try
            {
                await TrackerDbManager.Instance!.InitializeAsync(dbSettings, true);
            }
            catch (Exception ex)
            {
                // Database failure - warn but continue (app can work offline with mock data)
                warningMessage = $"Database connection failed: {ex.Message}. Some features may not work.";
                _splashScreen?.UpdateStatus("Database unavailable - continuing...");
            }
            await Task.Delay(150);

            // Stage 3: Initialize data manager (70%)
            _splashScreen?.UpdateStatus("Loading data...");
            _splashScreen?.UpdateProgress(70);
            try
            {
                await Task.Run(() => TrackerDataManager.Instance.Initialize());
            }
            catch (Exception ex)
            {
                // Data manager failure - warn but continue
                if (warningMessage == null)
                {
                    warningMessage = $"Data loading failed: {ex.Message}";
                }
            }
            await Task.Delay(150);

            // Stage 4: Final preparations (100%)
            _splashScreen?.UpdateStatus("Ready!");
            _splashScreen?.UpdateProgress(100);
            await Task.Delay(200);

            return new InitializationResult(true, warningMessage);
        }

        private void ShowLoginDialog()
        {
            var loginVm = new LoginDialogViewModel(null)
            {
                UserName = Environment.UserName
            };

            var loginWindow = new LoginDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var callback = new Action(() =>
            {
                if (loginVm.DialogResult.Cancelled)
                {
                    loginVm.Dispose();
                    loginWindow.Close();
                    return;
                }

                TrackerDbManager.Instance?.CheckUserAsync();

                Current.Dispatcher.BeginInvoke(() =>
                {
                    DialogManager.Instance.LaunchDialogByType(DialogType.MainWindow, false, () => { });
                    loginVm.Dispose();
                    loginWindow.Close();
                });
            });

            loginVm.Callback = callback;
            loginWindow.DataContext = loginVm;
            loginWindow.Show();
        }
    }
}
