using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views.Pulse;

public partial class GoalsTabView : UserControl
{
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

    public GoalsTabView()
    {
        InitializeComponent();
        Log("[GoalsTabView] Initialized");
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
