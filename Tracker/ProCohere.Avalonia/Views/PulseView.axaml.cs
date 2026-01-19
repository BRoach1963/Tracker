using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

public partial class PulseView : UserControl
{
    private PulseViewModel? _viewModel;

    public PulseView()
    {
        InitializeComponent();
        
        _viewModel = new PulseViewModel();
        DataContext = _viewModel;
        
        Log("[PulseView] Constructor - ViewModel created");
        
        // Set child DataContexts
        GoalsTab.DataContext = _viewModel.GoalsViewModel;
        MetricsTab.DataContext = _viewModel.MetricsViewModel;
        TasksTab.DataContext = _viewModel.TasksViewModel;
        
        Log($"[PulseView] Child DataContexts set - Goals: {_viewModel.GoalsViewModel != null}, Metrics: {_viewModel.MetricsViewModel != null}, Tasks: {_viewModel.TasksViewModel != null}");
        
        // Subscribe to property changes to update visibility
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Set initial visibility
        UpdateTabVisibility();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Log($"[PulseView] PropertyChanged: {e.PropertyName}");
        
        if (e.PropertyName == nameof(PulseViewModel.SelectedSubTab) ||
            e.PropertyName == nameof(PulseViewModel.IsSubTabGoals) ||
            e.PropertyName == nameof(PulseViewModel.IsSubTabMetrics) ||
            e.PropertyName == nameof(PulseViewModel.IsSubTabTasks))
        {
            UpdateTabVisibility();
        }
    }

    private void UpdateTabVisibility()
    {
        if (_viewModel == null) return;
        
        var goalsVisible = _viewModel.IsSubTabGoals;
        var metricsVisible = _viewModel.IsSubTabMetrics;
        var tasksVisible = _viewModel.IsSubTabTasks;
        
        Log($"[PulseView] UpdateTabVisibility - SelectedSubTab={_viewModel.SelectedSubTab}, Goals={goalsVisible}, Metrics={metricsVisible}, Tasks={tasksVisible}");
        
        GoalsTab.IsVisible = goalsVisible;
        MetricsTab.IsVisible = metricsVisible;
        TasksTab.IsVisible = tasksVisible;
        
        Log($"[PulseView] After update - GoalsTab.IsVisible={GoalsTab.IsVisible}, MetricsTab.IsVisible={MetricsTab.IsVisible}, TasksTab.IsVisible={TasksTab.IsVisible}");
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProCohere", "pulse_view.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(message);
    }
}
