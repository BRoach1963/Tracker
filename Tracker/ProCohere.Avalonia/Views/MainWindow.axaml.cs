using System;
using System.Threading.Tasks;
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
    private readonly HelpWindowFactory _helpWindowFactory;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize help factory with this window as parent
        _helpWindowFactory = new HelpWindowFactory(this);
        
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
        System.Diagnostics.Debug.WriteLine($"[MainWindow] OnClosing: MinimizeToTray={LocalSettingsService.Instance.MinimizeToTray}, ForceClose={_forceClose}");
        
        // Check if minimize-to-tray is enabled and not force closing
        if (LocalSettingsService.Instance.MinimizeToTray && !_forceClose)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] Canceling close, hiding window...");
            e.Cancel = true;
            Hide();
            System.Diagnostics.Debug.WriteLine("[MainWindow] Window hidden, sending native toast...");
            
            // Show native toast to let user know app is still running
            // Must be called after Hide() so the window is hidden
            try
            {
                NotificationService.Instance.SendNativeToast(
                    "ProCohere Minimized", 
                    "The app is still running in the system tray. Right-click the tray icon to exit.");
                System.Diagnostics.Debug.WriteLine("[MainWindow] Native toast call completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Native toast failed: {ex.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] Actually closing window");
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
            mainVm.HelpRequested += OnHelpRequestedAsync;
        }
        
        // Wire up BriefingViewModel navigation events
        if (BriefingViewControl != null)
        {
            var briefingVm = BriefingViewControl.GetViewModel();
            briefingVm.NavigateToProjectRequested += (_, projectId) => NavigateToProject(projectId);
            briefingVm.NavigateToTaskRequested += (_, taskId) => NavigateToTask(taskId);
            briefingVm.NavigateToGoalRequested += (_, goalId) => NavigateToGoal(goalId);
            briefingVm.NavigateToMetricRequested += (_, metricId) => NavigateToMetric(metricId);
            briefingVm.NavigateToMeetingRequested += (_, meetingId) => NavigateToMeeting(meetingId);
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
    
    private async Task OnHelpRequestedAsync(string? initialTopicId)
    {
        try
        {
            await _helpWindowFactory.ShowHelpWindowAsync(initialTopicId ?? "overview");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to show help window: {ex.Message}");
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
            mainVm.HelpRequested -= OnHelpRequestedAsync;
        }
        
        base.OnClosed(e);
    }

    #region Navigation Helpers

    /// <summary>
    /// Navigates to Projects tab (already implemented via existing event).
    /// </summary>
    private void NavigateToProject(Guid projectId)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedNavigation = NavigationItem.Projects;
            // TODO: Select specific project if needed
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Navigated to Projects for project {projectId}");
        }
    }

    /// <summary>
    /// Navigates to Tasks tab and selects specific task.
    /// </summary>
    private void NavigateToTask(Guid taskId)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedNavigation = NavigationItem.Tasks;
            // TODO: TasksViewModel.SelectTask(taskId) when implemented
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Navigated to Tasks for task {taskId}");
        }
    }

    /// <summary>
    /// Navigates to Goals tab and selects specific goal.
    /// </summary>
    private void NavigateToGoal(Guid goalId)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedNavigation = NavigationItem.Goals;
            // TODO: GoalsViewModel.SelectGoal(goalId) when implemented
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Navigated to Goals for goal {goalId}");
        }
    }

    /// <summary>
    /// Navigates to Metrics tab and selects specific metric.
    /// </summary>
    private void NavigateToMetric(Guid metricId)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedNavigation = NavigationItem.Metrics;
            // TODO: MetricsViewModel.SelectMetric(metricId) when implemented
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Navigated to Metrics for metric {metricId}");
        }
    }

    /// <summary>
    /// Navigates to Me tab for meeting (1-on-1s).
    /// </summary>
    private void NavigateToMeeting(Guid meetingId)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedNavigation = NavigationItem.Me;
            // TODO: MeViewModel.SelectMeeting(meetingId) when implemented
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Navigated to Me for meeting {meetingId}");
        }
    }

    #endregion
    
    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            await AppDialogService.ShowAboutDialogAsync(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing About dialog: {ex.Message}");
        }
    }
}