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
        private Views.LoadingWindow? _loadingWindow;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

        public static void SetAppUserModelId(string appId)
        {
            SetCurrentProcessExplicitAppUserModelID(appId);
        }

        public App()
        {
            // Note: Npgsql.EnableLegacyTimestampBehavior is set in ModuleInitializer.cs
            // It must run before any Npgsql types are loaded.
            
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
                LoggingManager.GetComponentLogger("App").Warn("Syncfusion license registration failed: {0}", ex.Message);
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
            
            // Dispose AI services (prevent file locking)
            try
            {
                Services.AI.Insights.InsightEngine.Instance?.Dispose();
                Services.AI.Insights.InsightStore.Instance?.Dispose();
            }
            catch { /* Ignore disposal errors during shutdown */ }
            
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
                var logger = LoggingManager.GetComponentLogger("App");
                logger.Info(">>> ContinueNormalStartup - checking authentication status");
                
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    NotificationManager.Instance.ShowWarning("Startup Warning", warningMessage, 10);
                }
                
                var isSignedIn = Managers.AuthenticationManager.Instance.IsSignedIn;
                logger.Info(">>> IsSignedIn = {0}", isSignedIn);
                
                // Skip login if already authenticated with PostgreSQL
                if (isSignedIn)
                {
                    logger.Info(">>> ALREADY AUTHENTICATED - Starting loading window flow");
                    ShutdownMode = ShutdownMode.OnLastWindowClose;
                    
                    // HIDE splash IMMEDIATELY so loading window can show
                    if (_splashScreen != null)
                    {
                        _splashScreen.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    
                    // Show loading window IMMEDIATELY
                    logger.Info(">>> Creating LoadingWindow");
                    _loadingWindow = new Views.LoadingWindow();
                    logger.Info(">>> Showing LoadingWindow");
                    _loadingWindow.Show();
                    logger.Info(">>> Activating LoadingWindow");
                    _loadingWindow.Activate();
                    _loadingWindow.Topmost = true;
                    logger.Info(">>> LoadingWindow.IsVisible={0}, IsLoaded={1}, Topmost={2}", _loadingWindow.IsVisible, _loadingWindow.IsLoaded, _loadingWindow.Topmost);
                    
                    // Force complete render cycle
                    Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                    
                    // Close splash properly in background
                    Task.Run(() =>
                    {
                        System.Threading.Thread.Sleep(100);
                        Dispatcher.Invoke(() => _splashScreen?.Close());
                    });
                    
                    // Launch main window after delay
                    Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        await Dispatcher.InvokeAsync(() => LaunchMainWindow());
                    });
                }
                else
                {
                    // Not authenticated - show login dialog
                    _splashScreen?.CloseSplash(() => ShowLoginDialog());
                }
            });
        }
        
        private string? _startupWarningMessage;
        
        /// <summary>
        /// Handles successful login from the integrated splash screen.
        /// </summary>
        private void OnSplashLoginSuccessful(object? sender, LoginSuccessEventArgs e)
        {
            var logger = LoggingManager.GetComponentLogger("App");
            logger.Info(">>> LOGIN SUCCESS - Starting loading window flow");
            
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            
            // IMMEDIATELY hide the splash/login window
            if (_splashScreen != null)
            {
                logger.Info(">>> Collapsing splash window");
                _splashScreen.Visibility = System.Windows.Visibility.Collapsed;
            }
            
            // Check if user's settings require setup wizard (e.g., after "Change Database" was clicked)
            if (!UserSettingsManager.Instance.Settings.Database.SetupCompleted)
            {
                logger.Info(">>> User settings indicate setup not completed - showing setup wizard");
                _splashScreen?.Close();
                ShowSetupWizardAfterLogin();
                return;
            }
            
            // NOW show LoadingWindow - splash is completely hidden
            logger.Info(">>> Creating LoadingWindow");
            _loadingWindow = new Views.LoadingWindow();
            logger.Info(">>> Showing LoadingWindow");
            _loadingWindow.Show();
            logger.Info(">>> Activating LoadingWindow");
            _loadingWindow.Activate();
            _loadingWindow.Topmost = true;
            logger.Info(">>> LoadingWindow.IsVisible={0}, IsLoaded={1}", _loadingWindow.IsVisible, _loadingWindow.IsLoaded);
            
            // Force rendering
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            
            // Close the splash window properly in background
            Task.Run(async () =>
            {
                await Task.Delay(100);
                await Dispatcher.InvokeAsync(() =>
                {
                    _splashScreen?.Close();
                });
            });
            
            // Launch main window after loading window is visible
            Task.Run(async () =>
            {
                await Task.Delay(500);
                
                await Dispatcher.InvokeAsync(() =>
                {
                    if (!string.IsNullOrEmpty(_startupWarningMessage))
                    {
                        NotificationManager.Instance.ShowWarning("Startup Warning", _startupWarningMessage, 10);
                    }
                    LaunchMainWindow();
                });
            });
        }
        
        /// <summary>
        /// Shows the setup wizard after login when the user needs to reconfigure their database.
        /// </summary>
        private void ShowSetupWizardAfterLogin()
        {
            var logger = LoggingManager.GetComponentLogger("App");
            logger.Info(">>> Showing setup wizard after login (user requested database change)");
            
            // Prevent app shutdown when setup wizard closes (before loading window shows)
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            SetupWizard? setupWindow = null;
            bool setupCompletedSuccessfully = false;
            
            var setupVm = new SetupWizardViewModel(() =>
            {
                setupCompletedSuccessfully = true;
                setupWindow?.Close();
            });

            setupWindow = new SetupWizard
            {
                DataContext = setupVm,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true
            };

            setupWindow.Closed += async (s, e) =>
            {
                if (setupCompletedSuccessfully)
                {
                    // Re-initialize database with new settings
                    try
                    {
                        var initResult = await InitializeApplicationAsync();
                        _startupWarningMessage = initResult.WarningMessage;
                        
                        // Show loading window briefly then main window
                        _loadingWindow = new Views.LoadingWindow();
                        _loadingWindow.Show();
                        
                        await Task.Delay(500);
                        
                        await Dispatcher.InvokeAsync(() =>
                        {
                            // Restore normal shutdown mode now that main window will show
                            ShutdownMode = ShutdownMode.OnLastWindowClose;
                            
                            if (!string.IsNullOrEmpty(_startupWarningMessage))
                            {
                                NotificationManager.Instance.ShowWarning("Startup Warning", _startupWarningMessage, 10);
                            }
                            LaunchMainWindow();
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.Exception(ex, "Failed to initialize after setup wizard");
                        MessageBoxHelper.Show(
                            $"Failed to initialize database: {ex.Message}\n\nPlease restart the application.",
                            "Initialization Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Shutdown();
                    }
                }
                else
                {
                    // User cancelled - reset to previous state and go back to login
                    logger.Info(">>> Setup wizard cancelled - user should restart");
                    MessageBoxHelper.Show(
                        "Database setup was cancelled. Please restart Tracker to try again.",
                        "Setup Cancelled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Shutdown();
                }
            };

            setupWindow.Show();
        }

        /// <summary>
        /// Launches the main application window after successful login.
        /// </summary>
        private void LaunchMainWindow(bool isAdminLogin = false)
        {
            var logger = LoggingManager.GetComponentLogger("App");
            logger.Info(">>> LaunchMainWindow called (isAdminLogin={0})", isAdminLogin);
            
            // CLOSE loading window BEFORE creating MainWindow to avoid taskbar confusion
            logger.Info(">>> Closing LoadingWindow before MainWindow shows");
            _loadingWindow?.CloseWithFade();
            
            // Brief delay to ensure loading window is gone
            Task.Delay(100).ContinueWith(_ => Dispatcher.Invoke(() =>
            {
                if (isAdminLogin)
                {
                    // Launch Admin Window for database management
                    var adminWindow = new Views.AdminWindow();
                    logger.Info(">>> AdminWindow created");
                    adminWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    adminWindow.Show();
                    Application.Current.MainWindow = adminWindow;
                }
                else
                {
                    // Create MainWindow directly on UI thread
                    var mainWindow = new MainWindow(new ViewModels.TrackerMainViewModel());
                    logger.Info(">>> MainWindow created");
                    mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            

            
            mainWindow.Show();
            
            Application.Current.MainWindow = mainWindow;
                }
            }));
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
                // Initialize database - create schema if needed (especially for PostgreSQL)
                await TrackerDbManager.Instance!.InitializeAsync(dbSettings, createIfNotExists: true, seedSampleData: false);
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

            // Stage 3.55: Initialize AI Insights engine
            _splashScreen?.UpdateStatus("Initializing insights...");
            _splashScreen?.UpdateProgress(82);
            try
            {
                var insightSettings = UserSettingsManager.Instance?.Settings?.Insights;
                if (insightSettings?.IsEnabled ?? true)
                {
                    await Services.AI.Insights.InsightEngine.Instance.InitializeAsync();
                    
                    // Run initial analysis in background (don't block startup)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Services.AI.Insights.InsightEngine.Instance.RunAnalyzersAsync();
                            // Start periodic analysis
                            var intervalHours = insightSettings?.AnalysisIntervalHours ?? 4;
                            Services.AI.Insights.InsightEngine.Instance.StartPeriodicAnalysis(intervalHours);
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.GetComponentLogger("App").Warn("Insight analysis failed: {0}", ex.Message);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Insight engine failure is non-fatal
                LoggingManager.GetComponentLogger("App").Warn("Insight engine initialization failed: {0}", ex.Message);
            }
            await Task.Delay(50);

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
            var loginWindow = new LoginWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true
            };

            loginWindow.Closed += (s, e) =>
            {
                if (loginWindow.LoginSuccessful)
                {
                    var logger = LoggingManager.GetComponentLogger("App");
                    logger.Info(">>> LOGIN WINDOW SUCCESS - Starting loading window flow");
                    
                    ShutdownMode = ShutdownMode.OnLastWindowClose;
                    
                    // Check if user's settings require setup wizard (e.g., after "Change Database" was clicked)
                    if (!UserSettingsManager.Instance.Settings.Database.SetupCompleted)
                    {
                        logger.Info(">>> User settings indicate setup not completed - showing setup wizard");
                        ShowSetupWizardAfterLogin();
                        return;
                    }
                    
                    // Login completed successfully, show LoadingWindow then main window
                    
                    // Show LoadingWindow
                    logger.Info(">>> Creating LoadingWindow");
                    _loadingWindow = new Views.LoadingWindow();
                    logger.Info(">>> Showing LoadingWindow");
                    _loadingWindow.Show();
                    logger.Info(">>> Activating LoadingWindow");
                    _loadingWindow.Activate();
                    _loadingWindow.Topmost = true;
                    logger.Info(">>> LoadingWindow.IsVisible={0}, IsLoaded={1}", _loadingWindow.IsVisible, _loadingWindow.IsLoaded);
                    
                    // Force rendering
                    Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                    
                    // Launch main window after short delay
                    // Note: Admin login is now determined by user roles in Supabase, not a checkbox
                    Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        await Dispatcher.InvokeAsync(() => LaunchMainWindow(isAdminLogin: false));
                    });
                }
                else
                {
                    // Login was cancelled (window closed without completing), shut down
                    Shutdown();
                }
            };

            loginWindow.Show();
        }
    }
}
