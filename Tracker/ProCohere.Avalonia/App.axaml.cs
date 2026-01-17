using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views;

namespace ProCohere.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            DisableAvaloniaDataAnnotationValidation();

            // Initialize theme service (applies saved theme preference)
            ThemeService.Instance.Initialize();

            // Show splash screen while checking authentication
            var splashWindow = new SplashWindow();
            desktop.MainWindow = splashWindow;
            
            // Check auth in background, then switch to appropriate window
            _ = InitializeAndNavigateAsync(desktop, splashWindow);
        }

        base.OnFrameworkInitializationCompleted();
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
            // Try auto-login with stored credentials
            var autoLoginSuccess = await AuthService.Instance.TryAutoLoginAsync();

            if (autoLoginSuccess)
            {
                // Get the full user session (includes access check, team member, and role)
                var session = await AuthService.Instance.GetUserSessionAsync("procohere");
                
                if (!session.HasAccess)
                {
                    // User authenticated but lost product access - sign them out
                    System.Diagnostics.Debug.WriteLine($"Auto-login user no longer has ProCohere access: {session.Error}");
                    await AuthService.Instance.SignOutAsync();
                    ShowLoginWindow(desktop, splashWindow);
                    return;
                }
                
                // Auto-login succeeded and has product access - go to main window
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                splashWindow.Close();
            }
            else
            {
                // No stored credentials or auto-login failed - show login window
                ShowLoginWindow(desktop, splashWindow);
            }
        }
        catch (Exception ex)
        {
            // Log error and fall back to login screen
            System.Diagnostics.Debug.WriteLine($"Auto-login failed: {ex.Message}");
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
        loginViewModel.LoginSuccessful += () =>
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            loginWindow.Close();
        };

        desktop.MainWindow = loginWindow;
        loginWindow.Show();
        splashWindow.Close();
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