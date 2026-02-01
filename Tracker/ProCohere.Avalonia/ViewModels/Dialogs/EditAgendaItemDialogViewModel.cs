using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the EditAgendaItemDialog.
/// Supports linking agenda items to Goals, Metrics, Tasks per GOALS_SPEC.
/// </summary>
public partial class EditAgendaItemDialogViewModel : ObservableObject
{
    private readonly DialogAgendaItem _item;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditAgendaItemResult? Result { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;
    
    /// <summary>
    /// Raised when user wants to edit a talking point (View handles dialog).
    /// </summary>
    public event Action<TalkingPoint>? EditTalkingPointRequested;
    
    #region Observable Properties
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _displayTitle = string.Empty;
    
    [ObservableProperty]
    private string _sharedContext = string.Empty;
    
    [ObservableProperty]
    private string _privateContext = string.Empty;
    
    [ObservableProperty]
    private int _visibilityIndex;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTalkingPoints))]
    private ObservableCollection<TalkingPoint> _talkingPoints = new();
    
    [ObservableProperty]
    private bool _isAddPanelVisible;
    
    [ObservableProperty]
    private string _newTalkingPointText = string.Empty;
    
    #endregion
    
    #region Linked Entity Properties (per GOALS_SPEC)
    
    /// <summary>
    /// The type of linked entity: goal, metric, task, or null for none.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLinkedEntity))]
    [NotifyPropertyChangedFor(nameof(LinkedEntityTypeDisplay))]
    [NotifyPropertyChangedFor(nameof(LinkedEntityIcon))]
    private string? _linkedEntityType;
    
    /// <summary>
    /// The ID of the linked entity.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLinkedEntity))]
    private Guid? _linkedEntityId;
    
    /// <summary>
    /// The title of the linked entity for display.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLinkedEntity))]
    private string? _linkedEntityTitle;
    
    /// <summary>
    /// Whether the entity picker panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isEntityPickerVisible;
    
    /// <summary>
    /// Currently selected entity type tab (0=Goal, 1=Metric, 2=Task).
    /// </summary>
    [ObservableProperty]
    private int _entityTypeTabIndex;
    
    /// <summary>
    /// Whether entities are currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingEntities;
    
    /// <summary>
    /// Available goals for linking.
    /// </summary>
    public ObservableCollection<GoalDetail> AvailableGoals { get; } = new();
    
    /// <summary>
    /// Available metrics for linking.
    /// </summary>
    public ObservableCollection<MetricDetail> AvailableMetrics { get; } = new();
    
    /// <summary>
    /// Available tasks for linking.
    /// </summary>
    public ObservableCollection<TaskDetail> AvailableTasks { get; } = new();
    
    /// <summary>
    /// Whether there is a linked entity.
    /// </summary>
    public bool HasLinkedEntity => LinkedEntityId.HasValue && !string.IsNullOrEmpty(LinkedEntityType);
    
    /// <summary>
    /// Display text for the linked entity type.
    /// </summary>
    public string LinkedEntityTypeDisplay => LinkedEntityType?.ToLower() switch
    {
        "goal" => "Goal",
        "metric" => "Metric",
        "task" => "Task",
        _ => ""
    };
    
    /// <summary>
    /// Icon path for the linked entity type.
    /// </summary>
    public string LinkedEntityIcon => LinkedEntityType?.ToLower() switch
    {
        "goal" => "M5,16L3,5L8.5,10L12,4L15.5,10L21,5L19,16H5M19,19C19,19.55 18.55,20 18,20H6C5.45,20 5,19.55 5,19V18H19V19Z",
        "metric" => "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z",
        "task" => "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z",
        _ => ""
    };
    
    #endregion
    
    /// <summary>
    /// Whether there are any talking points (for empty state visibility).
    /// </summary>
    public bool HasTalkingPoints => TalkingPoints.Count > 0;
    
    // Visibility tag values matching XAML order: meeting (0), personal (1)
    private static readonly string[] VisibilityTags = { "meeting", "personal" };
    
    public EditAgendaItemDialogViewModel()
    {
        _item = new DialogAgendaItem();
        TalkingPoints.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasTalkingPoints));
    }
    
    public EditAgendaItemDialogViewModel(DialogAgendaItem item) : this()
    {
        _item = item;
        LoadItemData();
    }
    
    private void LoadItemData()
    {
        Title = _item.Title ?? string.Empty;
        DisplayTitle = _item.DisplayTitle ?? string.Empty;
        SharedContext = _item.SharedContext ?? string.Empty;
        PrivateContext = _item.PrivateContext ?? string.Empty;
        
        // Set visibility index
        var visibilityScope = _item.VisibilityScope ?? "meeting";
        VisibilityIndex = Array.IndexOf(VisibilityTags, visibilityScope);
        if (VisibilityIndex < 0) VisibilityIndex = 0;
        
        // Load linked entity (per GOALS_SPEC)
        LinkedEntityType = _item.LinkedEntityType;
        LinkedEntityId = _item.LinkedEntityId;
        LinkedEntityTitle = _item.LinkedEntityTitle ?? _item.LinkedEntityTitleSnapshot;
        
        // Load talking points
        TalkingPoints.Clear();
        foreach (var tp in _item.TalkingPoints)
        {
            TalkingPoints.Add(new TalkingPoint
            {
                Id = tp.Id,
                Text = tp.Text,
                Discussed = tp.Discussed,
                Order = tp.Order
            });
        }
    }
    
    #region Linked Entity Commands (per GOALS_SPEC)
    
    /// <summary>
    /// Opens the entity picker panel and loads available entities.
    /// </summary>
    [RelayCommand]
    private async Task ShowEntityPicker()
    {
        IsEntityPickerVisible = true;
        await LoadAvailableEntitiesAsync();
    }
    
    /// <summary>
    /// Closes the entity picker panel.
    /// </summary>
    [RelayCommand]
    private void HideEntityPicker()
    {
        IsEntityPickerVisible = false;
    }
    
    /// <summary>
    /// Clears the linked entity (unlinks).
    /// </summary>
    [RelayCommand]
    private void ClearLinkedEntity()
    {
        LinkedEntityType = null;
        LinkedEntityId = null;
        LinkedEntityTitle = null;
    }
    
    /// <summary>
    /// Selects a goal as the linked entity.
    /// </summary>
    [RelayCommand]
    private void SelectGoal(GoalDetail? goal)
    {
        if (goal == null) return;
        
        LinkedEntityType = "goal";
        LinkedEntityId = goal.Id;
        LinkedEntityTitle = goal.Title;
        IsEntityPickerVisible = false;
    }
    
    /// <summary>
    /// Selects a metric as the linked entity.
    /// </summary>
    [RelayCommand]
    private void SelectMetric(MetricDetail? metric)
    {
        if (metric == null) return;
        
        LinkedEntityType = "metric";
        LinkedEntityId = metric.Id;
        LinkedEntityTitle = metric.Name;
        IsEntityPickerVisible = false;
    }
    
    /// <summary>
    /// Selects a task as the linked entity.
    /// </summary>
    [RelayCommand]
    private void SelectTask(TaskDetail? task)
    {
        if (task == null) return;
        
        LinkedEntityType = "task";
        LinkedEntityId = task.Id;
        LinkedEntityTitle = task.Title;
        IsEntityPickerVisible = false;
    }
    
    /// <summary>
    /// Loads available goals, metrics, and tasks for linking.
    /// </summary>
    private async Task LoadAvailableEntitiesAsync()
    {
        if (IsLoadingEntities) return;
        
        IsLoadingEntities = true;
        try
        {
            // Load goals (linkable = active, not deleted)
            var goals = await GoalsService.Instance.GetLinkableGoalsAsync();
            AvailableGoals.Clear();
            foreach (var goal in goals)
            {
                AvailableGoals.Add(goal);
            }
            
            // Load metrics (all active metrics)
            var metrics = await MetricsService.Instance.GetAllMetricsAsync();
            AvailableMetrics.Clear();
            foreach (var metric in metrics.Where(m => !m.IsDeleted))
            {
                AvailableMetrics.Add(metric);
            }
            
            // Load tasks (linkable = open tasks only)
            var tasks = await TaskService.Instance.GetLinkableTasksAsync();
            AvailableTasks.Clear();
            foreach (var task in tasks)
            {
                AvailableTasks.Add(task);
            }
        }
        catch
        {
            // Silent fail - entities will show as empty
        }
        finally
        {
            IsLoadingEntities = false;
        }
    }
    
    #endregion
    
    #region Talking Points Commands
    
    [RelayCommand]
    private void ShowAddPanel()
    {
        IsAddPanelVisible = true;
        NewTalkingPointText = string.Empty;
    }
    
    [RelayCommand]
    private void CancelAdd()
    {
        IsAddPanelVisible = false;
        NewTalkingPointText = string.Empty;
    }
    
    [RelayCommand]
    private void ConfirmAdd()
    {
        var text = NewTalkingPointText?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        
        AddTalkingPoint(text);
        
        IsAddPanelVisible = false;
        NewTalkingPointText = string.Empty;
    }
    
    /// <summary>
    /// Called from View when Enter is pressed in the new talking point textbox.
    /// </summary>
    public void TryAddFromTextBox()
    {
        var text = NewTalkingPointText?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            AddTalkingPoint(text);
            NewTalkingPointText = string.Empty;
        }
    }
    
    private void AddTalkingPoint(string text)
    {
        var tp = new TalkingPoint
        {
            Id = Guid.NewGuid().ToString(),
            Text = text,
            Discussed = false,
            Order = TalkingPoints.Count
        };
        TalkingPoints.Add(tp);
    }
    
    [RelayCommand]
    private void RemoveTalkingPoint(TalkingPoint? tp)
    {
        if (tp == null) return;
        
        TalkingPoints.Remove(tp);
        
        // Reorder remaining points
        for (int i = 0; i < TalkingPoints.Count; i++)
        {
            TalkingPoints[i].Order = i;
        }
    }
    
    [RelayCommand]
    private void EditTalkingPoint(TalkingPoint? tp)
    {
        if (tp == null) return;
        EditTalkingPointRequested?.Invoke(tp);
    }
    
    /// <summary>
    /// Called from View after edit dialog completes.
    /// </summary>
    public void UpdateTalkingPointText(TalkingPoint tp, string newText)
    {
        if (string.IsNullOrWhiteSpace(newText)) return;
        
        tp.Text = newText.Trim();
        
        // Force refresh by removing and re-adding (ObservableCollection doesn't detect property changes)
        var index = TalkingPoints.IndexOf(tp);
        if (index >= 0)
        {
            TalkingPoints.RemoveAt(index);
            TalkingPoints.Insert(index, tp);
        }
    }
    
    #endregion
    
    #region Dialog Commands
    
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private void Save()
    {
        var title = Title?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }
        
        var visibility = VisibilityIndex >= 0 && VisibilityIndex < VisibilityTags.Length 
            ? VisibilityTags[VisibilityIndex] 
            : "meeting";
        
        Result = new EditAgendaItemResult
        {
            WasSaved = true,
            Title = title,
            DisplayTitle = string.IsNullOrWhiteSpace(DisplayTitle) ? null : DisplayTitle.Trim(),
            SharedContext = string.IsNullOrWhiteSpace(SharedContext) ? null : SharedContext.Trim(),
            PrivateContext = string.IsNullOrWhiteSpace(PrivateContext) ? null : PrivateContext.Trim(),
            VisibilityScope = visibility,
            TalkingPoints = TalkingPoints.ToList(),
            IsDirty = _item.Id != Guid.Empty,
            // Linked entity per GOALS_SPEC
            LinkedEntityType = LinkedEntityType,
            LinkedEntityId = LinkedEntityId,
            LinkedEntityTitle = LinkedEntityTitle
        };
        
        CloseRequested?.Invoke();
    }
    
    #endregion
}
