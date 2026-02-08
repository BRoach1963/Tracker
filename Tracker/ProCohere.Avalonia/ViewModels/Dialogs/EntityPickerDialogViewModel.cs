using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Entity Picker dialog.
/// Handles loading, filtering, and selection of entities (tasks, goals, metrics, projects, persons, meetings).
/// </summary>
public partial class EntityPickerDialogViewModel : ObservableObject
{
    #region Fields

    private readonly List<EntityPickerItem> _allItems = new();
    private CancellationTokenSource? _searchDebounceTokenSource;
    private HashSet<string>? _allowedTypes;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _currentFilter = "all";

    [ObservableProperty]
    private EntityPickerItem? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasItems;

    [ObservableProperty]
    private string _selectionHint = "Select an item to link";

    #endregion

    #region Collections

    /// <summary>
    /// Filtered items for display.
    /// </summary>
    public ObservableCollection<EntityPickerItem> FilteredItems { get; } = new();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether an item is selected and can be linked.
    /// </summary>
    public bool CanLink => SelectedItem != null;

    /// <summary>
    /// Whether filter buttons should be visible.
    /// </summary>
    public bool ShowFilters => _allowedTypes == null || _allowedTypes.Count != 1;

    #endregion

    #region Result

    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EntityPickerResult? Result { get; private set; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event EventHandler<EntityPickerResult?>? CloseRequested;

    #endregion

    #region Commands

    [RelayCommand]
    private void Select()
    {
        if (SelectedItem == null) return;

        Result = new EntityPickerResult
        {
            EntityId = SelectedItem.Id,
            EntityType = SelectedItem.EntityType,
            EntityTitle = SelectedItem.Title
        };

        CloseRequested?.Invoke(this, Result);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, null);
    }

    [RelayCommand]
    private void SetFilter(string filter)
    {
        CurrentFilter = filter;
        ApplyFilters();
    }

    #endregion

    #region Property Change Handlers

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search
        _searchDebounceTokenSource?.Cancel();
        _searchDebounceTokenSource = new CancellationTokenSource();

        var token = _searchDebounceTokenSource.Token;

        Task.Delay(250, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(ApplyFilters);
            }
        }, TaskScheduler.Default);
    }

    partial void OnSelectedItemChanged(EntityPickerItem? value)
    {
        OnPropertyChanged(nameof(CanLink));
        SelectionHint = value != null
            ? $"Selected: {value.Title}"
            : "Select an item to link";
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Set allowed entity types. If only one type, filter buttons are hidden.
    /// </summary>
    public void SetAllowedTypes(params string[] types)
    {
        if (types.Length > 0)
        {
            _allowedTypes = new HashSet<string>(types, StringComparer.OrdinalIgnoreCase);
            if (types.Length == 1)
            {
                CurrentFilter = types[0];
            }
            OnPropertyChanged(nameof(ShowFilters));
        }
    }

    /// <summary>
    /// Load all entities from services.
    /// </summary>
    public async Task LoadItemsAsync()
    {
        IsLoading = true;
        HasItems = false;

        try
        {
            _allItems.Clear();

            // Load tasks
            if (_allowedTypes == null || _allowedTypes.Contains("task"))
            {
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
            }

            // Load goals
            if (_allowedTypes == null || _allowedTypes.Contains("goal"))
            {
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
            }

            // Load metrics
            if (_allowedTypes == null || _allowedTypes.Contains("metric"))
            {
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
            }

            // Load people (team members)
            if (_allowedTypes == null || _allowedTypes.Contains("person"))
            {
                var members = await TeamService.Instance.GetVisibleTeamMembersAsync();
                foreach (var member in members.Where(m => !m.IsDeleted))
                {
                    _allItems.Add(new EntityPickerItem
                    {
                        Id = member.Id,
                        EntityType = "person",
                        Title = member.FullName,
                        Subtitle = member.JobTitle ?? "Team Member",
                        StatusText = member.Email ?? string.Empty
                    });
                }
            }

            // Load meetings
            if (_allowedTypes == null || _allowedTypes.Contains("meeting"))
            {
                var client = AuthService.Instance.GetProCohereClient();
                if (client != null)
                {
                    var meetingsResult = await client.From<Models.MeetingDetail>()
                        .Filter("is_deleted", Operator.Equals, "false")
                        .Order("scheduled_at", Ordering.Descending)
                        .Limit(50)
                        .Get();
                    
                    var meetings = meetingsResult.Models ?? new List<Models.MeetingDetail>();
                    foreach (var meeting in meetings)
                    {
                        var scheduledText = meeting.ScheduledAt?.ToString("MMM d, yyyy h:mm tt") ?? "Unscheduled";
                        _allItems.Add(new EntityPickerItem
                        {
                            Id = meeting.Id,
                            EntityType = "meeting",
                            Title = meeting.Title ?? "Untitled Meeting",
                            Subtitle = scheduledText,
                            StatusText = meeting.MeetingType?.Replace("_", " ") ?? string.Empty
                        });
                    }
                }
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EntityPickerDialogViewModel] Error loading items: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Methods

    private void ApplyFilters()
    {
        var searchText = SearchText?.ToLowerInvariant() ?? "";

        FilteredItems.Clear();

        foreach (var item in _allItems)
        {
            // Type filter
            if (CurrentFilter != "all" && !item.EntityType.Equals(CurrentFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            // Search filter
            if (!string.IsNullOrEmpty(searchText))
            {
                if (!item.Title.ToLowerInvariant().Contains(searchText) &&
                    !item.Subtitle.ToLowerInvariant().Contains(searchText))
                    continue;
            }

            FilteredItems.Add(item);
        }

        HasItems = FilteredItems.Count > 0;
    }

    #endregion
}
