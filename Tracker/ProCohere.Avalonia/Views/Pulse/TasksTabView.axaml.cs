using System;
using System.Globalization;
using System.IO;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Dialogs;

namespace ProCohere.Avalonia.Views.Pulse;

public partial class TasksTabView : UserControl
{
    private TasksViewModel? _viewModel;

    public TasksTabView()
    {
        InitializeComponent();
        Log("[TasksTabView] Constructor called");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        Log($"[TasksTabView] OnDataContextChanged - NewContext type: {DataContext?.GetType().Name ?? "NULL"}");

        // Unsubscribe from old view model
        if (_viewModel != null)
        {
            _viewModel.AddTaskDialogRequested -= OnAddTaskDialogRequested;
            Log("[TasksTabView] Unsubscribed from old ViewModel");
        }

        // Subscribe to new view model
        _viewModel = DataContext as TasksViewModel;
        if (_viewModel != null)
        {
            _viewModel.AddTaskDialogRequested += OnAddTaskDialogRequested;
            Log($"[TasksTabView] Subscribed to new ViewModel - FilteredTasks.Count={_viewModel.FilteredTasks.Count}");
        }
    }

    private async void OnAddTaskDialogRequested(object? sender, EventArgs e)
    {
        Log("[TasksTabView] AddTaskDialogRequested");
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null)
        {
            Log("[TasksTabView] Cannot show dialog - window or viewmodel is null");
            return;
        }

        var dialog = new AddTaskDialog();
        dialog.SetTeamMembers(_viewModel.TeamMembers);

        await dialog.ShowDialog(window);
        
        var result = dialog.Result;
        if (result != null)
        {
            Log($"[TasksTabView] Creating task: {result.Title}");
            await _viewModel.CreateTaskFromDialogAsync(
                result.Title,
                result.Description,
                result.Priority,
                result.DueDate,
                result.AssigneeId);
        }
    }

    private void FilterAll_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[TasksTabView] FilterAll_Tapped");
        _viewModel?.SetFilterCommand.Execute("0");
    }

    private void FilterToday_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[TasksTabView] FilterToday_Tapped");
        _viewModel?.SetFilterCommand.Execute("1");
    }

    private void FilterOverdue_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[TasksTabView] FilterOverdue_Tapped");
        _viewModel?.SetFilterCommand.Execute("2");
    }

    private void FilterCompleted_Tapped(object? sender, TappedEventArgs e)
    {
        Log("[TasksTabView] FilterCompleted_Tapped");
        _viewModel?.SetFilterCommand.Execute("3");
    }

    private void TaskCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { Tag: TaskDetail task })
        {
            Log($"[TasksTabView] TaskCard_Tapped: {task.Title}");
            _viewModel?.SelectTaskCommand.Execute(task);
        }
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProCohere", "tasks_tab_view.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(message);
    }
}

/// <summary>
/// Converter for priority to background color.
/// </summary>
public class PriorityToBackgroundConverter : IValueConverter
{
    public static readonly PriorityToBackgroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var priority = value?.ToString()?.ToLowerInvariant();
        return priority switch
        {
            "high" or "urgent" => new SolidColorBrush(Color.Parse("#EF4444")),   // Red
            "medium" => new SolidColorBrush(Color.Parse("#F59E0B")),              // Amber
            "low" => new SolidColorBrush(Color.Parse("#10B981")),                 // Green
            _ => new SolidColorBrush(Color.Parse("#64748B"))                      // Slate gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
