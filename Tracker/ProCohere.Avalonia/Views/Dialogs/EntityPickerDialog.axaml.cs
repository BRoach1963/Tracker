using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Represents a selectable entity in the picker.
/// </summary>
public class EntityPickerItem
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty; // task, goal, metric, project
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? StatusText { get; set; }
    public bool HasStatus => !string.IsNullOrEmpty(StatusText);

    public string TypeIcon => EntityType.ToLower() switch
    {
        "task" => "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z", // Checkmark
        "goal" => "M5,16L3,5L8.5,10L12,4L15.5,10L21,5L19,16H5M19,19C19,19.55 18.55,20 18,20H6C5.45,20 5,19.55 5,19V18H19V19Z", // Flag/target
        "metric" => "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z", // Chart
        "project" => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z", // Folder
        _ => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
    };

    public IBrush TypeColor => EntityType.ToLower() switch
    {
        "task" => new SolidColorBrush(Color.Parse("#3498DB")),    // Blue
        "goal" => new SolidColorBrush(Color.Parse("#27AE60")),    // Green
        "metric" => new SolidColorBrush(Color.Parse("#9B59B6")),  // Purple
        "project" => new SolidColorBrush(Color.Parse("#E67E22")), // Orange
        _ => new SolidColorBrush(Color.Parse("#7F8C8D"))
    };
}

/// <summary>
/// Result from the entity picker dialog.
/// </summary>
public class EntityPickerResult
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityTitle { get; set; } = string.Empty;
}

/// <summary>
/// Dialog for searching and selecting existing items (tasks, goals, metrics, projects)
/// to link to an agenda item or prep item.
/// </summary>
public partial class EntityPickerDialog : Window
{
    private ObservableCollection<EntityPickerItem> _allItems = new();
    private ObservableCollection<EntityPickerItem> _filteredItems = new();
    private string _currentFilter = "all";
    private Timer? _searchDebounceTimer;
    
    /// <summary>
    /// The selected result (null if cancelled).
    /// </summary>
    public EntityPickerResult? Result { get; private set; }

    public EntityPickerDialog()
    {
        InitializeComponent();
        
        // Defer control access until after XAML is loaded
        Loaded += (s, e) =>
        {
            ResultsListBox.ItemsSource = _filteredItems;
        };
        
        // Load data when window opens
        Opened += async (s, e) => await LoadItemsAsync();
    }

    /// <summary>
    /// Optional: Filter to only show specific entity types.
    /// </summary>
    public void SetAllowedTypes(params string[] types)
    {
        // Hide filter buttons for types not in the list
        if (types.Length == 1)
        {
            // Single type - hide all filters
            var filterPanel = this.FindControl<StackPanel>("FilterPanel");
            if (filterPanel != null) filterPanel.IsVisible = false;
            _currentFilter = types[0];
        }
    }

    private async Task LoadItemsAsync()
    {
        LoadingPanel.IsVisible = true;
        EmptyPanel.IsVisible = false;
        ResultsListBox.IsVisible = false;

        try
        {
            _allItems.Clear();

            // Load tasks
            var tasks = await TaskService.Instance.GetTasksAsync();
            foreach (var task in tasks.Where(t => !t.IsDeleted))
            {
                _allItems.Add(new EntityPickerItem
                {
                    Id = task.Id,
                    EntityType = "task",
                    Title = task.Title,
                    Subtitle = task.OwnerName ?? "Unassigned",
                    StatusText = task.Status?.Replace("_", " ")
                });
            }

            // Load goals
            var goals = await GoalsService.Instance.GetMyGoalsAsync();
            foreach (var goal in goals.Where(g => !g.IsDeleted))
            {
                _allItems.Add(new EntityPickerItem
                {
                    Id = goal.Id,
                    EntityType = "goal",
                    Title = goal.Title,
                    Subtitle = goal.HealthDisplay,
                    StatusText = goal.LifecycleDisplay
                });
            }

            // Load metrics
            var metrics = await MetricsService.Instance.GetAllMetricsAsync();
            foreach (var metric in metrics.Where(m => !m.IsDeleted))
            {
                var valueText = $"Current: {metric.CurrentValue:F1}";
                _allItems.Add(new EntityPickerItem
                {
                    Id = metric.Id,
                    EntityType = "metric",
                    Title = metric.Name,
                    Subtitle = valueText,
                    StatusText = metric.LifecycleDisplay
                });
            }

            // Load projects (if we have a project service)
            // TODO: Add ProjectService when available

            ApplyFilters();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EntityPickerDialog] Error loading items: {ex.Message}");
        }
        finally
        {
            LoadingPanel.IsVisible = false;
            UpdateVisibility();
        }
    }

    private void ApplyFilters()
    {
        var searchText = SearchTextBox.Text?.ToLowerInvariant() ?? "";
        
        _filteredItems.Clear();
        
        foreach (var item in _allItems)
        {
            // Type filter
            if (_currentFilter != "all" && item.EntityType != _currentFilter)
                continue;
            
            // Search filter
            if (!string.IsNullOrEmpty(searchText))
            {
                if (!item.Title.ToLowerInvariant().Contains(searchText) &&
                    !item.Subtitle.ToLowerInvariant().Contains(searchText))
                    continue;
            }
            
            _filteredItems.Add(item);
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        var hasItems = _filteredItems.Count > 0;
        ResultsListBox.IsVisible = hasItems;
        EmptyPanel.IsVisible = !hasItems && !LoadingPanel.IsVisible;
    }

    private void SearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        // Debounce search
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer?.Dispose();
        
        _searchDebounceTimer = new Timer(250);
        _searchDebounceTimer.Elapsed += (s, args) =>
        {
            _searchDebounceTimer?.Stop();
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplyFilters());
        };
        _searchDebounceTimer.Start();
    }

    private void FilterBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is string filter)
        {
            _currentFilter = filter;
            
            // Update border styles - remove selected from all
            AllFilterBorder.Classes.Remove("selected");
            TaskFilterBorder.Classes.Remove("selected");
            GoalFilterBorder.Classes.Remove("selected");
            MetricFilterBorder.Classes.Remove("selected");
            ProjectFilterBorder.Classes.Remove("selected");
            
            // Add selected to clicked border
            border.Classes.Add("selected");
            
            ApplyFilters();
        }
    }

    private void ResultsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var hasSelection = ResultsListBox.SelectedItem != null;
        
        // Update Link button opacity based on selection
        LinkBorder.Opacity = hasSelection ? 1.0 : 0.5;
        
        if (ResultsListBox.SelectedItem is EntityPickerItem item)
        {
            SelectionHint.Text = $"Selected: {item.Title}";
        }
        else
        {
            SelectionHint.Text = "Select an item to link";
        }
    }

    private void ResultsListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ResultsListBox.SelectedItem != null)
        {
            SelectAndClose();
        }
    }

    private void LinkBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ResultsListBox.SelectedItem != null)
        {
            SelectAndClose();
        }
    }

    private void SelectAndClose()
    {
        if (ResultsListBox.SelectedItem is EntityPickerItem item)
        {
            Result = new EntityPickerResult
            {
                EntityId = item.Id,
                EntityType = item.EntityType,
                EntityTitle = item.Title
            };
            Close();
        }
    }

    private void CancelBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Result = null;
        Close();
    }
}
