using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using DeepEndControls.Theming;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Help.Services;
using Tracker.Helpers;
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
        private bool _emptyDatabaseDetected = false;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

        public static void SetAppUserModelId(string appId)
        {
            SetCurrentProcessExplicitAppUserModelID(appId);
        }

        public App()
        {
            // Register Syncfusion license with error handling
            try
            {
                var licenseKey = "Ngo9BigBOggjHTQxAR8/V1NCaF1cWWhAYVJ2WmFZfVpgcl9GYlZVQmYuP1ZhSXxXdkxjWn9YcHZRQGFYWEM=";
                if (!string.IsNullOrWhiteSpace(licenseKey))
                {
                    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the app
                // Syncfusion controls will show a license watermark if registration fails
                System.Diagnostics.Debug.WriteLine($"Syncfusion license registration failed: {ex.Message}");
            }
            
            RegisterAppForToastNotifications();
        }

        private void InitializeTheme()
        {
            // Load user settings first to get the saved theme preference
            UserSettingsManager.Instance.Initialize();

            // Apply the saved theme (or default if none saved)
            ThemeManager.Instance.Initialize(UserSettingsManager.Instance.Settings.Theme);
        }

        /// <summary>
        /// Registers the global F1 key handler for context-sensitive help.
        /// </summary>
        private void RegisterHelpKeyHandler()
        {
            // Register global F1 handler on all UIElements
            EventManager.RegisterClassHandler(
                typeof(UIElement),
                Keyboard.KeyDownEvent,
                new KeyEventHandler(OnGlobalKeyDown),
                handledEventsToo: true);
        }

        /// <summary>
        /// Global key handler for F1 help.
        /// </summary>
        private void OnGlobalKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1 && !e.Handled)
            {
                e.Handled = true;
                
                // Get the focused element for context-sensitive help
                var focusedElement = Keyboard.FocusedElement as DependencyObject;
                
                // Show context-sensitive help
                HelpService.Instance.ShowContextHelp(focusedElement);
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Close all active toast notifications before shutdown
            NotificationManager.Instance.CloseAllToasts();
            
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
            SetupWizard? setupWindow = null;
            bool setupCompletedSuccessfully = false;
            
            var setupVm = new SetupWizardViewModel(() =>
            {
                // Mark as completed before closing window
                setupCompletedSuccessfully = true;
                
                // Close the setup window
                setupWindow?.Close();
            });

            setupWindow = new SetupWizard
            {
                DataContext = setupVm,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true
            };

            setupWindow.Closed += (s, e) =>
            {
                if (setupCompletedSuccessfully)
                {
                    // Setup completed successfully, continue with normal startup
                    _ = ContinueNormalStartup();
                }
                else
                {
                    // Setup was cancelled (window closed without completing), shut down
                    Shutdown();
                }
            };

            setupWindow.Show();
        }

        private async Task ContinueNormalStartup()
        {
            // Show splash screen
            _splashScreen = new Views.SplashScreen();
            
            // Wire up login events for integrated login
            _splashScreen.LoginSuccessful += OnSplashLoginSuccessful;
            
            await Current.Dispatcher.InvokeAsync(() => _splashScreen.Show());

            string? warningMessage = null;

            try
            {
                // Initialize application components with progress updates
                var initResult = await InitializeApplicationAsync();
                warningMessage = initResult.WarningMessage;
                _startupWarningMessage = warningMessage;
            }
            catch (Exception ex)
            {
                // Show error dialog instead of silently dying
                await Current.Dispatcher.InvokeAsync(() => _splashScreen?.Close());
                
                var result = MessageBoxHelper.Show(
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

            // Check if already authenticated (e.g., just completed setup with account creation)
            await Current.Dispatcher.InvokeAsync(() =>
            {
                _splashScreen?.CloseSplash(() =>
                {
                    if (!string.IsNullOrEmpty(warningMessage))
                    {
                        NotificationManager.Instance.ShowWarning("Startup Warning", warningMessage, 10);
                    }
                    
                    // Skip login if already authenticated with Supabase
                    if (Services.Backend.SupabaseService.Instance.IsSignedIn)
                    {
                        LoggingManager.GetComponentLogger("App").Info("User already authenticated, skipping login dialog");
                        ShutdownMode = ShutdownMode.OnLastWindowClose;
                        LaunchMainWindow();
                    }
                    else
                    {
                        ShowLoginDialog();
                    }
                });
            });
        }
        
        private string? _startupWarningMessage;
        
        /// <summary>
        /// Handles successful login from the integrated splash screen.
        /// </summary>
        private void OnSplashLoginSuccessful(object? sender, LoginSuccessEventArgs e)
        {
            // Login succeeded, launch main window
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            _splashScreen?.CloseSplash(() =>
            {
                if (!string.IsNullOrEmpty(_startupWarningMessage))
                {
                    NotificationManager.Instance.ShowWarning("Startup Warning", _startupWarningMessage, 10);
                }
                LaunchMainWindow();
            });
        }
        
        /// <summary>
        /// Launches the main application window after successful login.
        /// </summary>
        private void LaunchMainWindow()
        {
            DialogManager.Instance.LaunchDialogByType(DialogType.MainWindow, false, async () =>
            {
                // Check if database is empty and prompt user to add sample data
                if (_emptyDatabaseDetected)
                {
                    await Task.Delay(500); // Wait for main window to fully load
                    var result = MessageBoxHelper.Show(
                        "Your database is empty. Would you like to add sample data?\n\n" +
                        "This will populate your database with:\n" +
                        "• 7 team members (Steelers team)\n" +
                        "• Sample 1:1 meetings\n" +
                        "• Sample projects with OKRs and KPIs\n" +
                        "• Sample tasks\n" +
                        "• Linked items (Phase 1 features)",
                        "Empty Database",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var success = await TrackerDbManager.Instance!.SeedSampleDataAsync(forceReseed: false);
                        if (success)
                        {
                            // Refresh the UI
                            Messenger.Publish(new PropertyChangedMessage
                            {
                                ChangedProperty = PropertyChangedEnum.All,
                                RefreshData = true
                            });
                            NotificationManager.Instance.ShowSuccess("Sample Data Added", "Sample data has been added to your database.");
                        }
                    }
                    _emptyDatabaseDetected = false;
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
                // Initialize database (don't seed here - seeding happens in setup wizard or via Settings)
                await TrackerDbManager.Instance!.InitializeAsync(dbSettings, createIfNotExists: false, seedSampleData: false);
                
                // Check if database is empty and flag for prompt after login
                var hasData = await TrackerDbManager.Instance!.HasDataAsync();
                if (!hasData)
                {
                    _emptyDatabaseDetected = true;
                }
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

            // Stage 3.5: Initialize help system
            _splashScreen?.UpdateStatus("Initializing help system...");
            _splashScreen?.UpdateProgress(80);
            try
            {
                HelpService.Instance.Initialize();
                RegisterHelpKeyHandler();
            }
            catch (Exception ex)
            {
                // Help system failure is non-fatal
                LoggingManager.GetComponentLogger("App").Warn("Help system initialization failed: {0}", ex.Message);
            }
            await Task.Delay(100);

            // Stage 3.6: Initialize cloud services
            _splashScreen?.UpdateStatus("Connecting to cloud services...");
            _splashScreen?.UpdateProgress(85);
            try
            {
                // Initialize Supabase for cloud account features
                await Services.Backend.SupabaseService.Instance.InitializeAsync();
                
                // Validate subscription with backend
                await Services.Subscription.SubscriptionService.Instance.ValidateWithBackendAsync();
            }
            catch (Exception ex)
            {
                // Cloud service failure is non-fatal - app works offline
                LoggingManager.GetComponentLogger("App").Warn("Cloud services unavailable: {0}", ex.Message);
            }
            await Task.Delay(100);

            // Stage 3.7: Restore integration sessions (silent sign-in)
            _splashScreen?.UpdateStatus("Restoring integrations...");
            _splashScreen?.UpdateProgress(95);
            try
            {
                // Try to restore Microsoft 365 session (silent, no browser popup)
                if (UserSettingsManager.Instance.Settings.Microsoft365.IsEnabled)
                {
                    var m365Restored = await Services.Microsoft365.MicrosoftGraphAuthService.Instance.TrySignInSilentlyAsync();
                    if (m365Restored)
                    {
                        LoggingManager.GetComponentLogger("App").Info("Microsoft 365 session restored");
                    }
                }

                // Try to restore Google session (silent, no browser popup)
                if (UserSettingsManager.Instance.Settings.Google.IsConnected)
                {
                    var googleRestored = await Services.Google.GoogleAuthService.Instance.TrySilentSignInAsync();
                    if (googleRestored)
                    {
                        LoggingManager.GetComponentLogger("App").Info("Google session restored");
                    }
                }
            }
            catch (Exception ex)
            {
                // Integration restore failure is non-fatal
                LoggingManager.GetComponentLogger("App").Warn("Integration restore failed: {0}", ex.Message);
            }

            // Stage 4: Final preparations (100%)
            _splashScreen?.UpdateStatus("Ready!");
            _splashScreen?.UpdateProgress(100);
            await Task.Delay(200);

            return new InitializationResult(true, warningMessage);
        }

        private void ShowLoginDialog()
        {
            LoginDialog? loginWindow = null;
            LoginDialogViewModel? loginVm = null;
            bool loginCompletedSuccessfully = false;

            loginVm = new LoginDialogViewModel(() =>
            {
                // Mark as completed before closing window
                loginCompletedSuccessfully = loginVm?.Result.Cancelled == false;
                
                // Close the login window
                loginWindow?.Close();
            });

            loginWindow = new LoginDialog
            {
                DataContext = loginVm,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true
            };

            loginWindow.Closed += (s, e) =>
            {
                if (loginCompletedSuccessfully)
                {
                    // Login completed successfully, launch main window
                    ShutdownMode = ShutdownMode.OnLastWindowClose;
                    LaunchMainWindow();
                }
                else
                {
                    // Login was cancelled (window closed without completing), shut down
                    loginVm?.Dispose();
                    Shutdown();
                }
            };

            loginWindow.Show();
        }
    }
}
