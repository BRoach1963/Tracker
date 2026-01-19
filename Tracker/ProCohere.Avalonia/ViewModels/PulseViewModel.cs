using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// Coordinator ViewModel for the Pulse section.
/// Manages sub-tab navigation between Goals, Metrics, and Tasks.
/// </summary>
public partial class PulseViewModel : ViewModelBase
{
    #region Static Converters

    /// <summary>
    /// Converter for sub-tab index to primary action button text.
    /// </summary>
    public static readonly IValueConverter SubTabToPrimaryActionConverter = 
        new FuncValueConverter<int, string>(tab => tab switch
        {
            0 => "New Goal",
            1 => "New Metric",
            2 => "New Task",
            _ => "New"
        });

    #endregion

    #region Sub-Tab Navigation

    /// <summary>
    /// Currently selected sub-tab: 0=Goals, 1=Metrics, 2=Tasks
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSubTabGoals))]
    [NotifyPropertyChangedFor(nameof(IsSubTabMetrics))]
    [NotifyPropertyChangedFor(nameof(IsSubTabTasks))]
    [NotifyPropertyChangedFor(nameof(PrimaryActionText))]
    private int _selectedSubTab = 0;

    public bool IsSubTabGoals => SelectedSubTab == 0;
    public bool IsSubTabMetrics => SelectedSubTab == 1;
    public bool IsSubTabTasks => SelectedSubTab == 2;

    [RelayCommand]
    private void SetSubTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
        {
            SelectedSubTab = index;
        }
    }

    #endregion

    #region Child ViewModels

    /// <summary>
    /// Goals sub-tab ViewModel.
    /// </summary>
    public GoalsViewModel GoalsViewModel { get; }

    /// <summary>
    /// Metrics sub-tab ViewModel.
    /// </summary>
    public MetricsViewModel MetricsViewModel { get; }

    /// <summary>
    /// Tasks sub-tab ViewModel (existing).
    /// </summary>
    public TasksViewModel TasksViewModel { get; }

    #endregion

    #region UI State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    #endregion

    #region Primary Action

    /// <summary>
    /// Primary action button text changes based on selected tab.
    /// </summary>
    public string PrimaryActionText => SelectedSubTab switch
    {
        0 => "+ New Goal",
        1 => "+ New Metric",
        2 => "+ New Task",
        _ => "+ New"
    };

    [RelayCommand]
    private void PrimaryAction()
    {
        switch (SelectedSubTab)
        {
            case 0:
                GoalsViewModel.CreateNewGoalCommand.Execute(null);
                break;
            case 1:
                MetricsViewModel.CreateNewMetricCommand.Execute(null);
                break;
            case 2:
                TasksViewModel.StartAddTaskCommand.Execute(null);
                break;
        }
    }

    #endregion

    public PulseViewModel()
    {
        // Initialize child ViewModels
        GoalsViewModel = new GoalsViewModel();
        MetricsViewModel = new MetricsViewModel();
        TasksViewModel = new TasksViewModel();
    }

    /// <summary>
    /// Loads data for the currently selected tab.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            switch (SelectedSubTab)
            {
                case 0:
                    await GoalsViewModel.LoadGoalsCommand.ExecuteAsync(null);
                    break;
                case 1:
                    await MetricsViewModel.LoadMetricsCommand.ExecuteAsync(null);
                    break;
                case 2:
                    await TasksViewModel.LoadTasksCommand.ExecuteAsync(null);
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedSubTabChanged(int value)
    {
        OnPropertyChanged(nameof(PrimaryActionText));
        // Load data for newly selected tab
        _ = LoadDataAsync();
    }
}

/// <summary>
/// Sub-tab enumeration for Pulse.
/// </summary>
public enum PulseSubTab
{
    Goals = 0,
    Metrics = 1,
    Tasks = 2
}
