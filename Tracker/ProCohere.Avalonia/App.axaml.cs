using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.Services.AI;
using ProCohere.Avalonia.Services.Insights;
using ProCohere.Avalonia.Services.Insights.Analyzers;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.ViewModels.Insights;
using ProCohere.Avalonia.Views;
using ProCohere.Avalonia.Views.Dialogs;

namespace ProCohere.Avalonia;

public partial class App : Application
{
    /// <summary>
    /// ViewModel for the system tray icon. Set as DataContext for App to enable tray menu bindings.
    /// </summary>
    public TrayIconViewModel TrayViewModel { get; } = new();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Set DataContext for tray icon bindings
        DataContext = TrayViewModel;
        
        // Initialize toast activation handler (must be early, before any toasts)
        ToastActivationHandler.Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            DisableAvaloniaDataAnnotationValidation();

            // Initialize theme service (applies saved theme preference)
            ThemeService.Instance.Initialize();
            
        // Initialize AI services
        InitializeAIServices();
        
            // Show splash screen while checking authentication
            var splashWindow = new SplashWindow();
            desktop.MainWindow = splashWindow;
            
            // Check auth in background, then switch to appropriate window
            _ = InitializeAndNavigateAsync(desktop, splashWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    /// <summary>
    /// Initializes the system tray service and wires up event handlers.
    /// </summary>
    private static void InitializeSystemTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        SystemTrayService.Instance.Initialize();
        
        // Set up visibility check for NotificationService (to know when to show native toasts)
        NotificationService.Instance.IsMainWindowVisible = () =>
        {
            return desktop.MainWindow?.IsVisible == true;
        };
        
        // When user clicks "Open" or double-clicks tray icon
        SystemTrayService.Instance.ShowWindowRequested += (_, _) =>
        {
            if (desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.WindowState = global::Avalonia.Controls.WindowState.Normal;
                desktop.MainWindow.Activate();
            }
        };
        
        // When user clicks "Exit" in tray menu
        SystemTrayService.Instance.ExitRequested += async (_, _) =>
        {
            // Ensure main window is visible for the confirmation dialog
            if (desktop.MainWindow != null && !desktop.MainWindow.IsVisible)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.WindowState = global::Avalonia.Controls.WindowState.Normal;
                desktop.MainWindow.Activate();
            }
            
            // Show confirmation dialog warning about notifications being silenced
            var confirmed = await ConfirmationService.Instance.ShowExitConfirmationAsync(
                "Exit ProCohere?",
                "If you exit the app, notifications and reminders will be silenced until you open the app again.\n\nAre you sure you want to exit?",
                "Exit",
                "Cancel");
            
            if (!confirmed)
                return;
            
            // Stop the reminder scheduler
            ReminderSchedulerService.Instance.Stop();
            
            // Close all toasts
            NotificationService.Instance.CloseAllToasts();
            
            // Clear native toast history on exit
            ClearNativeToasts();
            
            // Set flag to force close (bypass minimize-to-tray)
            if (desktop.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ForceClose();
            }
            desktop.Shutdown();
        };
    }
    
    /// <summary>
    /// Clears native Windows toast notification history for this app.
    /// </summary>
    private static void ClearNativeToasts()
    {
        try
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.History.Clear();
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    /// <summary>
    /// Initializes authentication and navigates to the appropriate window.
    /// </summary>
    private static async Task InitializeAndNavigateAsync(
        IClassicDesktopStyleApplicationLifetime desktop, 
        SplashWindow splashWindow)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[App] Starting InitializeAndNavigateAsync...");
            
            // Load appsettings.json
            System.Diagnostics.Debug.WriteLine("[App] Loading appsettings.json...");
            await AppSettingsService.Instance.LoadSettingsAsync();
            
            // Try auto-login with stored credentials (with 10 second timeout)
            System.Diagnostics.Debug.WriteLine("[App] Calling TryAutoLoginAsync...");
            bool autoLoginSuccess;
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                var loginTask = AuthService.Instance.TryAutoLoginAsync();
                var completedTask = await Task.WhenAny(loginTask, Task.Delay(10000, cts.Token));
                
                if (completedTask == loginTask)
                {
                    autoLoginSuccess = await loginTask;
                     cts.Cancel(); // Cancel the delay task
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[App] TryAutoLoginAsync timed out after 10 seconds");
                    autoLoginSuccess = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] TryAutoLoginAsync failed: {ex.Message}");
                autoLoginSuccess = false;
            }
            System.Diagnostics.Debug.WriteLine($"[App] TryAutoLoginAsync returned: {autoLoginSuccess}");

            if (autoLoginSuccess)
            {
                // Get the full user session (includes access check, team member, and role)
                System.Diagnostics.Debug.WriteLine("[App] Calling GetUserSessionAsync...");
                var session = await AuthService.Instance.GetUserSessionAsync("procohere");
                System.Diagnostics.Debug.WriteLine($"[App] GetUserSessionAsync returned, HasAccess: {session.HasAccess}");
                
                if (!session.HasAccess)
                {
                    // User authenticated but lost product access - sign them out
                    System.Diagnostics.Debug.WriteLine($"Auto-login user no longer has ProCohere access: {session.Error}");
                    await AuthService.Instance.SignOutAsync();
                    ShowLoginWindow(desktop, splashWindow);
                    return;
                }
                
                // Auto-login succeeded and has product access - go to main window
                System.Diagnostics.Debug.WriteLine("[App] Creating MainWindow...");
                var mainViewModel = new MainWindowViewModel();
                
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
                desktop.MainWindow = mainWindow;
                
                // Initialize confirmation service with main window
                ConfirmationService.Instance.Initialize(mainWindow);
                
                // Start the reminder scheduler
                ReminderSchedulerService.Instance.Start();
                
                // Show window IMMEDIATELY - data loads in background per-view
                mainWindow.Show();
                splashWindow.Close();
                
                // Initialize system tray
                InitializeSystemTray(desktop);
                
                System.Diagnostics.Debug.WriteLine("[App] MainWindow shown, splash closed");
                
                // Load data in background (non-blocking) - each view handles its own loading state
                _ = mainViewModel.InitializeAsync();
                
                // Show welcome toast
                NotificationService.Instance.ShowSuccess("Welcome Back", "You have been signed in successfully.");
                
                // Show critical insights popup after a short delay (non-blocking)
                _ = global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await Task.Delay(1000); // Let the window settle first
                    await ShowInsightPopupIfNeededAsync(mainWindow);
                });
            }
            else
            {
                // No stored credentials or auto-login failed - show login window
                System.Diagnostics.Debug.WriteLine("[App] No auto-login, showing login window...");
                ShowLoginWindow(desktop, splashWindow);
            }
        }
        catch (Exception ex)
        {
            // Log error and fall back to login screen
            System.Diagnostics.Debug.WriteLine($"[App] Auto-login failed with exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");
            ShowLoginWindow(desktop, splashWindow);
        }
    }

    /// <summary>
    /// Shows the login window and sets up the success handler.
    /// </summary>
    private static void ShowLoginWindow(
        IClassicDesktopStyleApplicationLifetime desktop, 
        SplashWindow splashWindow)
    {
        var loginViewModel = new LoginViewModel();
        var loginWindow = new LoginWindow
        {
            DataContext = loginViewModel
        };

        // When login succeeds, show main window
        loginViewModel.LoginSuccessful += async () =>
        {
            var mainViewModel = new MainWindowViewModel();
            
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            desktop.MainWindow = mainWindow;
            
            // Initialize confirmation service with main window
            ConfirmationService.Instance.Initialize(mainWindow);
            
            // Start the reminder scheduler
            ReminderSchedulerService.Instance.Start();
            
            // Show window IMMEDIATELY - data loads in background per-view
            mainWindow.Show();
            
            // Initialize system tray
            InitializeSystemTray(desktop);
            
            loginWindow.Close();
            
            // Load data in background (non-blocking)
            System.Diagnostics.Debug.WriteLine("[App] Loading application data after login (background)...");
            _ = mainViewModel.InitializeAsync();
            
            // Show critical insights popup after login (non-blocking)
            _ = global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(1000); // Let the window settle first
                await ShowInsightPopupIfNeededAsync(mainWindow);
            });
        };

        desktop.MainWindow = loginWindow;
        loginWindow.Show();
        splashWindow.Close();
    }
    
    /// <summary>
    /// Initializes AI services and providers.
    /// </summary>
    private void InitializeAIServices()
    {
        // AI services use singleton patterns and are self-initializing
        // Just ensure the factory is ready
        _ = ChatProviderFactory.Instance;
        
        // Register insight analyzers
        InitializeInsightAnalyzers();
        
        System.Diagnostics.Debug.WriteLine("[App] AI services initialized");
    }
    
    /// <summary>
    /// Registers all insight analyzers with the InsightEngine.
    /// </summary>
    private void InitializeInsightAnalyzers()
    {
        try
        {
            InsightEngine.Instance.RegisterAnalyzer(new ActionItemStalenessAnalyzer());
            InsightEngine.Instance.RegisterAnalyzer(new GoalTrajectoryAnalyzer());
            InsightEngine.Instance.RegisterAnalyzer(new MeetingCadenceAnalyzer());
            InsightEngine.Instance.RegisterAnalyzer(new MetricGapAnalyzer());
            InsightEngine.Instance.RegisterAnalyzer(new PersonalDateAnalyzer());
            InsightEngine.Instance.RegisterAnalyzer(new SurveySentimentAnalyzer());
            
            System.Diagnostics.Debug.WriteLine("[App] Insight analyzers registered");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Failed to register insight analyzers: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Shows the critical insights popup if there are insights requiring attention.
    /// Non-blocking - fires and forgets to avoid delaying app startup.
    /// </summary>
    private static async Task ShowInsightPopupIfNeededAsync(MainWindow mainWindow)
    {
        try
        {
            var session = AuthService.Instance.CurrentSession_ProCohere;
            var userId = session?.User?.Id ?? Guid.Empty;
            var orgId = session?.TeamMember?.OrganizationId ?? Guid.Empty;
            
            if (userId == Guid.Empty || orgId == Guid.Empty)
            {
                System.Diagnostics.Debug.WriteLine("[App] No valid session, skipping insight popup");
                return;
            }
            
            // Check for existing critical insights first (fast)
            var hasCritical = await InsightPopupViewModel.HasCriticalInsightsAsync();
            
            // If no critical insights exist, run analyzers to generate them
            if (!hasCritical)
            {
                System.Diagnostics.Debug.WriteLine("[App] No existing critical insights, running analyzers...");
                await InsightEngine.Instance.RunAnalysisAsync(userId, orgId);
                System.Diagnostics.Debug.WriteLine("[App] Analyzers completed");
                
                // Check again after running analyzers
                hasCritical = await InsightPopupViewModel.HasCriticalInsightsAsync();
            }
            
            if (!hasCritical)
            {
                System.Diagnostics.Debug.WriteLine("[App] No critical insights, skipping popup");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("[App] Critical insights found, showing popup");
            
            var viewModel = new InsightPopupViewModel();
            var dialog = new InsightPopupDialog
            {
                DataContext = viewModel
            };
            
            // Wire close event
            viewModel.CloseRequested += (_, _) => dialog.Close();
            
            // Wire View All to navigate to Pulse
            viewModel.ViewAllRequested += (_, _) =>
            {
                dialog.Close();
                if (mainWindow.DataContext is MainWindowViewModel mainVm)
                {
                    mainVm.SelectedNavigation = NavigationItem.Pulse;
                }
            };
            
            // Wire entity navigation
            viewModel.NavigateRequested += (_, args) =>
            {
                dialog.Close();
                if (mainWindow.DataContext is MainWindowViewModel mainVm)
                {
                    // Navigate to appropriate page based on entity type
                    switch (args.EntityType.ToLowerInvariant())
                    {
                        case "goal":
                            mainVm.SelectedNavigation = NavigationItem.Goals;
                            break;
                        case "metric":
                            mainVm.SelectedNavigation = NavigationItem.Metrics;
                            break;
                        case "task":
                        case "actionitem":
                            mainVm.SelectedNavigation = NavigationItem.Tasks;
                            break;
                        case "meeting":
                            mainVm.SelectedNavigation = NavigationItem.Me;
                            break;
                    }
                }
            };
            
            // Load data and show dialog
            _ = viewModel.LoadAsync();
            await dialog.ShowDialog(mainWindow);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Failed to show insight popup: {ex.Message}");
            // Non-critical - don't fail the app
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}