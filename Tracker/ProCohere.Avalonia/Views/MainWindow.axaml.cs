using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Dialogs;

namespace ProCohere.Avalonia.Views;

public partial class MainWindow : Window
{
    private bool _forceClose = false;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Wire up events after loading
        Loaded += OnLoaded;
    }
    
    /// <summary>
    /// Call this to force the window to close (bypass minimize-to-tray).
    /// Used when user clicks "Exit" from tray menu.
    /// </summary>
    public void ForceClose()
    {
        _forceClose = true;
    }
    
    /// <summary>
    /// Override closing to minimize to tray instead of closing (if enabled).
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Check if minimize-to-tray is enabled and not force closing
        if (LocalSettingsService.Instance.MinimizeToTray && !_forceClose)
        {
            e.Cancel = true;
            Hide();
            
            // Tray icon is already visible (defined in App.axaml)
            // Optionally show a balloon/notification that app is still running
            System.Diagnostics.Debug.WriteLine("MainWindow minimized to tray");
        }
        
        base.OnClosing(e);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Create and assign SettingsViewModel (DataContext=null in XAML to prevent inheritance errors)
        if (SettingsView != null)
        {
            var settingsViewModel = new SettingsViewModel();
            settingsViewModel.LogoutRequested += OnLogoutRequested;
            SettingsView.DataContext = settingsViewModel;
        }

        // Wire up MainWindow ViewModel logout event
        if (DataContext is MainWindowViewModel mainVm)
        {
            mainVm.SignOutRequested += OnLogoutRequested;
            mainVm.EditProfileRequested += OnEditProfileRequested;
        }
    }

    private async void OnEditProfileRequested()
    {
        try
        {
            // Load current user profile
            var profile = await AuthService.Instance.LoadUserProfileAsync();
            if (profile == null) return;

            // Create the dialog (non-modal, draggable window)
            var dialog = new EditAccountDialog();
            dialog.LoadProfile(profile);
            
            // Subscribe to save event to refresh UI
            dialog.ProfileSaved += async () =>
            {
                // Refresh MainWindowViewModel
                if (DataContext is MainWindowViewModel mainVm)
                {
                    await mainVm.RefreshUserInfoAsync();
                }
                
                // Also refresh SettingsView if it exists
                if (SettingsView?.DataContext is SettingsViewModel settingsVm)
                {
                    await settingsVm.LoadUserProfileAsync();
                }
            };
            
            // Show as non-modal window (can be dragged, doesn't block main window)
            dialog.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing edit profile dialog: {ex.Message}");
        }
    }

    private void OnLogoutRequested()
    {
        // Create login window with proper ViewModel and event handler
        var loginViewModel = new LoginViewModel();
        var loginWindow = new LoginWindow
        {
            DataContext = loginViewModel
        };

        // Get the desktop application lifetime to update MainWindow reference
        var desktop = App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        
        // When login succeeds, show main window and close this login window
        loginViewModel.LoginSuccessful += () =>
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
            
            if (desktop != null)
            {
                desktop.MainWindow = mainWindow;
            }
            
            mainWindow.Show();
            loginWindow.Close();
        };

        // Update the desktop's main window reference
        if (desktop != null)
        {
            desktop.MainWindow = loginWindow;
        }
        
        loginWindow.Show();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Cleanup event subscriptions
        if (SettingsView?.DataContext is SettingsViewModel settingsVm)
        {
            settingsVm.LogoutRequested -= OnLogoutRequested;
        }
        
        if (DataContext is MainWindowViewModel mainVm)
        {
            mainVm.SignOutRequested -= OnLogoutRequested;
            mainVm.EditProfileRequested -= OnEditProfileRequested;
        }
        
        base.OnClosed(e);
    }
}