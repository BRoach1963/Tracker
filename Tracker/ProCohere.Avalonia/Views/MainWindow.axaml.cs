using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Wire up SettingsView logout event
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Get the SettingsView and wire up its ViewModel's logout event
        if (SettingsView?.DataContext is SettingsViewModel settingsVm)
        {
            settingsVm.LogoutRequested += OnLogoutRequested;
        }
        else if (SettingsView != null)
        {
            // If DataContext isn't set yet, create and assign the ViewModel
            var settingsViewModel = new SettingsViewModel();
            settingsViewModel.LogoutRequested += OnLogoutRequested;
            SettingsView.DataContext = settingsViewModel;
        }
    }

    private void OnLogoutRequested()
    {
        // Navigate back to login window
        var loginWindow = new LoginWindow();
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
        
        base.OnClosed(e);
    }
}