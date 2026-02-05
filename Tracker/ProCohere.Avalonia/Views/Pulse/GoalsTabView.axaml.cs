using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Controls;

namespace ProCohere.Avalonia.Views.Pulse;

public partial class GoalsTabView : UserControl
{
    private GoalsViewModel? _viewModel;
    private Popup? _projectSelectorPopup;
    private ProjectSelectorPopover? _projectSelectorPopover;
    
    /// <summary>
    /// Converter: GoalHealth -> foreground color
    /// </summary>
    public static readonly FuncValueConverter<GoalHealth, IBrush> HealthToColorConverter =
        new(health => health switch
        {
            GoalHealth.OnTrack => new SolidColorBrush(Color.Parse("#22C55E")),
            GoalHealth.NeedsAttention => new SolidColorBrush(Color.Parse("#F59E0B")),
            GoalHealth.AtRisk => new SolidColorBrush(Color.Parse("#EF4444")),
            GoalHealth.ReframingNeeded => new SolidColorBrush(Color.Parse("#8B5CF6")),
            _ => new SolidColorBrush(Color.Parse("#6B7280"))
        });

    /// <summary>
    /// Converter: GoalHealth -> background color (subtle)
    /// </summary>
    public static readonly FuncValueConverter<GoalHealth, IBrush> HealthToBackgroundConverter =
        new(health => health switch
        {
            GoalHealth.OnTrack => new SolidColorBrush(Color.Parse("#1422C55E")),
            GoalHealth.NeedsAttention => new SolidColorBrush(Color.Parse("#14F59E0B")),
            GoalHealth.AtRisk => new SolidColorBrush(Color.Parse("#14EF4444")),
            GoalHealth.ReframingNeeded => new SolidColorBrush(Color.Parse("#148B5CF6")),
            _ => new SolidColorBrush(Color.Parse("#146B7280"))
        });

    /// <summary>
    /// Converter: bool? IsOnTrack -> Color for trajectory status dot
    /// </summary>
    public static readonly FuncValueConverter<bool?, Color> TrajectoryStatusColorConverter =
        new(isOnTrack => isOnTrack switch
        {
            true => Color.Parse("#22C55E"),   // Green - on track
            false => Color.Parse("#EF4444"),  // Red - off track
            null => Color.Parse("#9CA3AF")    // Gray - unknown
        });

    public GoalsTabView()
    {
        InitializeComponent();
        Log("[GoalsTabView] Initialized");
        
        // Create the project selector popup
        _projectSelectorPopover = new ProjectSelectorPopover();
        _projectSelectorPopover.ProjectSelected += OnProjectSelected;
        
        _projectSelectorPopup = new Popup
        {
            Child = _projectSelectorPopover,
            Placement = PlacementMode.Pointer,
            IsLightDismissEnabled = true
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        Log($"[GoalsTabView] OnDataContextChanged - NewContext type: {DataContext?.GetType().Name ?? "NULL"}");

        // Unsubscribe from old view model
        if (_viewModel != null)
        {
            _viewModel.ProjectSelectorRequested -= OnProjectSelectorRequested;
            Log("[GoalsTabView] Unsubscribed from old ViewModel");
        }

        // Subscribe to new view model
        _viewModel = DataContext as GoalsViewModel;
        if (_viewModel != null)
        {
            _viewModel.ProjectSelectorRequested += OnProjectSelectorRequested;
            Log($"[GoalsTabView] Subscribed to new ViewModel");
        }
    }

    private void OnProjectSelectorRequested(object? sender, EventArgs e)
    {
        Log("[GoalsTabView] ProjectSelectorRequested");
        if (_projectSelectorPopup != null)
        {
            _projectSelectorPopup.PlacementTarget = this;
            _projectSelectorPopup.IsOpen = true;
        }
    }
    
    private async void OnProjectSelected(object? sender, Project project)
    {
        Log($"[GoalsTabView] ProjectSelected: {project.Name}");
        _projectSelectorPopup?.Close();
        _viewModel?.HideProjectSelector();
        
        if (_viewModel != null)
        {
            await _viewModel.LinkGoalToProjectAsync(project.Id, project.Name);
        }
    }

    private void ScopeMyGoals_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[GoalsTabView] Scope: My Goals");
        if (DataContext is GoalsViewModel vm)
        {
            vm.SetScopeCommand.Execute("0");
        }
    }

    private void ScopeTeamGoals_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[GoalsTabView] Scope: Team Goals");
        if (DataContext is GoalsViewModel vm)
        {
            vm.SetScopeCommand.Execute("1");
        }
    }

    private void ScopeSharedGoals_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[GoalsTabView] Scope: Shared Goals");
        if (DataContext is GoalsViewModel vm)
        {
            vm.SetScopeCommand.Execute("2");
        }
    }

    private void GoalCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is GoalDetail goal)
        {
            Log($"[GoalsTabView] Goal selected: {goal.Title}");
            if (DataContext is GoalsViewModel vm)
            {
                vm.SelectGoalCommand.Execute(goal);
            }
        }
    }

    private void TabDetails_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[GoalsTabView] Tab: Details");
        if (DataContext is GoalsViewModel vm)
        {
            vm.SetDetailTabCommand.Execute("0");
        }
    }

    private void TabTrajectory_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[GoalsTabView] Tab: Trajectory");
        if (DataContext is GoalsViewModel vm)
        {
            vm.SetDetailTabCommand.Execute("1");
        }
    }

    private async void RunScenario_Click(object? sender, RoutedEventArgs e)
    {
        Log("[GoalsTabView] RunScenario_Click");
        if (DataContext is GoalsViewModel vm && vm.Trajectory != null)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window != null)
            {
                await AppDialogService.ShowWhatIfDialogAsync(window, vm.Trajectory);
            }
        }
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProCohere", "goals_tab_view.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(message);
    }
}
