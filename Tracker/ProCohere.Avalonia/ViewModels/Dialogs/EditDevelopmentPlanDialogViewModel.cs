using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for creating and editing development plans.
/// Supports adding/editing plan details and managing items inline.
/// </summary>
public partial class EditDevelopmentPlanDialogViewModel : ViewModelBase
{
    #region Fields
    
    private readonly DevelopmentPlan? _existingPlan;
    
    #endregion
    
    #region Observable Properties
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _isSaving;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string? _description;
    
    [ObservableProperty]
    private int _statusIndex;
    
    [ObservableProperty]
    private DateTimeOffset? _startDate;
    
    [ObservableProperty]
    private DateTimeOffset? _targetDate;
    
    [ObservableProperty]
    private ObservableCollection<DevelopmentPlanItemViewModel> _items = new();
    
    // New item form
    [ObservableProperty]
    private string _newItemTitle = string.Empty;
    
    [ObservableProperty]
    private int _newItemTypeIndex;
    
    #endregion
    
    #region Computed Properties
    
    public bool IsEditing => _existingPlan != null;
    public string DialogTitle => IsEditing ? "Edit Development Plan" : "New Development Plan";
    public string SaveButtonText => IsEditing ? "Save Changes" : "Create Plan";
    
    public bool CanSave => !string.IsNullOrWhiteSpace(Title) && !IsSaving;
    
    private static readonly string[] StatusValues = { "draft", "active", "completed", "cancelled" };
    private static readonly string[] ItemTypeValues = { "training", "project", "mentoring", "reading", "certification", "workshop" };
    
    public string SelectedStatus => StatusValues.ElementAtOrDefault(StatusIndex) ?? "draft";
    
    #endregion
    
    #region Events
    
    public event Action<DevelopmentPlan?>? CloseRequested;
    
    #endregion
    
    #region Constructor
    
    public EditDevelopmentPlanDialogViewModel(DevelopmentPlan? existingPlan = null)
    {
        _existingPlan = existingPlan;
        
        if (existingPlan != null)
        {
            LoadFromPlan(existingPlan);
        }
    }
    
    private void LoadFromPlan(DevelopmentPlan plan)
    {
        Title = plan.Title;
        Description = plan.Description;
        StatusIndex = Array.IndexOf(StatusValues, plan.Status);
        if (StatusIndex < 0) StatusIndex = 0;
        
        StartDate = plan.StartDate.HasValue 
            ? new DateTimeOffset(plan.StartDate.Value) 
            : null;
        TargetDate = plan.TargetDate.HasValue 
            ? new DateTimeOffset(plan.TargetDate.Value) 
            : null;
        
        Items.Clear();
        foreach (var item in plan.Items)
        {
            Items.Add(new DevelopmentPlanItemViewModel(item));
        }
    }
    
    #endregion
    
    #region Commands
    
    [RelayCommand]
    private void AddItem()
    {
        if (string.IsNullOrWhiteSpace(NewItemTitle))
            return;
        
        var itemType = ItemTypeValues.ElementAtOrDefault(NewItemTypeIndex) ?? "training";
        
        var item = new DevelopmentPlanItemViewModel
        {
            Title = NewItemTitle.Trim(),
            ItemType = itemType,
            Status = "not_started"
        };
        
        Items.Add(item);
        
        // Clear form
        NewItemTitle = string.Empty;
        NewItemTypeIndex = 0;
    }
    
    [RelayCommand]
    private void RemoveItem(DevelopmentPlanItemViewModel? item)
    {
        if (item != null)
        {
            Items.Remove(item);
        }
    }
    
    [RelayCommand]
    private void ToggleItemComplete(DevelopmentPlanItemViewModel? item)
    {
        if (item != null)
        {
            item.Status = item.IsCompleted ? "not_started" : "completed";
        }
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanSave) return;
        
        IsSaving = true;
        ErrorMessage = null;
        
        try
        {
            DevelopmentPlan? savedPlan;
            
            if (IsEditing && _existingPlan != null)
            {
                // Update existing plan
                var success = await DevelopmentService.Instance.UpdatePlanAsync(
                    _existingPlan.Id,
                    Title.Trim(),
                    Description?.Trim(),
                    SelectedStatus,
                    StartDate?.DateTime,
                    TargetDate?.DateTime);
                
                if (!success)
                {
                    ErrorMessage = DevelopmentService.Instance.LastError ?? "Failed to update plan";
                    return;
                }
                
                // Update items
                await SyncItemsAsync(_existingPlan.Id);
                
                // Reload the plan to get updated data
                savedPlan = await DevelopmentService.Instance.GetPlanByIdAsync(_existingPlan.Id);
            }
            else
            {
                // Create new plan
                savedPlan = await DevelopmentService.Instance.CreatePlanAsync(
                    Title.Trim(),
                    Description?.Trim(),
                    StartDate?.DateTime,
                    TargetDate?.DateTime);
                
                if (savedPlan == null)
                {
                    ErrorMessage = DevelopmentService.Instance.LastError ?? "Failed to create plan";
                    return;
                }
                
                // If active status was selected, update it (create defaults to draft)
                if (SelectedStatus != "draft")
                {
                    await DevelopmentService.Instance.UpdatePlanAsync(
                        savedPlan.Id,
                        savedPlan.Title,
                        savedPlan.Description,
                        SelectedStatus,
                        savedPlan.StartDate,
                        savedPlan.TargetDate);
                }
                
                // Add items
                foreach (var item in Items)
                {
                    await DevelopmentService.Instance.CreateItemAsync(
                        savedPlan.Id,
                        item.Title,
                        item.Description,
                        item.ItemType);
                }
                
                // Reload to get items
                savedPlan = await DevelopmentService.Instance.GetPlanByIdAsync(savedPlan.Id);
            }
            
            CloseRequested?.Invoke(savedPlan);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving plan: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
    
    private async Task SyncItemsAsync(Guid planId)
    {
        // Get existing items
        var existingItems = await DevelopmentService.Instance.GetItemsByPlanAsync(planId);
        var existingIds = existingItems.Select(i => i.Id).ToHashSet();
        var currentIds = Items.Where(i => i.Id != Guid.Empty).Select(i => i.Id).ToHashSet();
        
        // Delete removed items
        foreach (var existing in existingItems)
        {
            if (!currentIds.Contains(existing.Id))
            {
                await DevelopmentService.Instance.DeleteItemAsync(existing.Id);
            }
        }
        
        // Update/create items
        foreach (var item in Items)
        {
            if (item.Id != Guid.Empty && existingIds.Contains(item.Id))
            {
                // Update existing
                await DevelopmentService.Instance.UpdateItemAsync(
                    item.Id,
                    item.Title,
                    item.Description,
                    item.ItemType,
                    item.Status,
                    item.DueDate?.DateTime);
            }
            else
            {
                // Create new
                await DevelopmentService.Instance.CreateItemAsync(
                    planId,
                    item.Title,
                    item.Description,
                    item.ItemType,
                    item.DueDate?.DateTime);
            }
        }
    }
    
    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(null);
    }
    
    #endregion
}

/// <summary>
/// ViewModel for a development plan item in the dialog.
/// </summary>
public partial class DevelopmentPlanItemViewModel : ObservableObject
{
    public Guid Id { get; set; }
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string? _description;
    
    [ObservableProperty]
    private string? _itemType;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    private string _status = "not_started";
    
    [ObservableProperty]
    private DateTimeOffset? _dueDate;
    
    public bool IsCompleted => Status == "completed";
    
    public string ItemTypeDisplay => ItemType switch
    {
        "training" => "Training",
        "project" => "Project",
        "mentoring" => "Mentoring",
        "reading" => "Reading",
        "certification" => "Certification",
        "workshop" => "Workshop",
        _ => ItemType ?? "Other"
    };
    
    public DevelopmentPlanItemViewModel() { }
    
    public DevelopmentPlanItemViewModel(DevelopmentPlanItem item)
    {
        Id = item.Id;
        Title = item.Title;
        Description = item.Description;
        ItemType = item.ItemType;
        Status = item.Status;
        DueDate = item.DueDate.HasValue ? new DateTimeOffset(item.DueDate.Value) : null;
    }
}
