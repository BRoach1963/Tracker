using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the EditGoalDialog.
/// </summary>
public partial class EditGoalDialogViewModel : ObservableObject
{
    private GoalDetail? _existingGoal;
    private IDialogService? _dialogService;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditGoalResult? Result { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;
    
    #region Observable Properties
    
    [ObservableProperty]
    private string _dialogTitle = "New Goal";
    
    [ObservableProperty]
    private string _saveButtonText = "Create Goal";
    
    [ObservableProperty]
    private bool _isDeleteVisible;
    
    [ObservableProperty]
    private bool _isEditMode;
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    [ObservableProperty]
    private int _goalTypeIndex;
    
    [ObservableProperty]
    private int _timePeriodIndex;
    
    [ObservableProperty]
    private int _selectedYearIndex = 1; // Default to current year (index 1 in a list starting from previous year)
    
    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;
    [ObservableProperty]
    private int _healthIndex;
    
    [ObservableProperty]
    private string _healthReason = string.Empty;
    
    [ObservableProperty]
    private int _lifecycleIndex;
    
    [ObservableProperty]
    private int _visibilityIndex = 1; // Default to Team
    
    [ObservableProperty]
    private TeamMemberDetail? _selectedOwner;
    
    #endregion
    
    /// <summary>
    /// Available years for selection.
    /// </summary>
    public ObservableCollection<int> Years { get; } = new();
    
    /// <summary>
    /// Team members available for owner selection.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> TeamMembers { get; } = new();
    
    // Tag values for combo boxes (must match XAML order)
    private static readonly string[] GoalTypeTags = { "growth", "execution", "operational", "directional" };
    private static readonly string[] HealthTags = { "on_track", "needs_attention", "at_risk", "reframing_needed" };
    private static readonly string[] LifecycleTags = { "active", "evolving", "paused", "superseded", "retired" };
    private static readonly string[] VisibilityTags = { "private", "team", "organization" };
    
    public EditGoalDialogViewModel()
    {
        // Populate years
        var currentYear = DateTime.Now.Year;
        for (int year = currentYear - 1; year <= currentYear + 2; year++)
        {
            Years.Add(year);
        }
        
        // Set default dates based on current quarter
        SetDefaultDates();
    }
    
    private void SetDefaultDates()
    {
        var now = DateTime.Now;
        var quarter = (now.Month - 1) / 3 + 1;
        var quarterStart = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
        var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
        
        StartDate = quarterStart;
        EndDate = quarterEnd;
        
        // Select current quarter (Q1=0, Q2=1, Q3=2, Q4=3)
        TimePeriodIndex = quarter - 1;
    }
    
    /// <summary>
    /// Load an existing goal for editing.
    /// </summary>
    public void LoadGoal(GoalDetail goal)
    {
        _existingGoal = goal;
        
        DialogTitle = "Edit Goal";
        SaveButtonText = "Save Changes";
        IsDeleteVisible = true;
        IsEditMode = true;
        
        Title = goal.Title ?? string.Empty;
        Description = goal.Description ?? string.Empty;
        
        // Set goal type
        if (!string.IsNullOrEmpty(goal.GoalTypeValue))
        {
            GoalTypeIndex = GetIndexByTag(GoalTypeTags, goal.GoalTypeValue);
        }
        
        // Set dates
        if (goal.StartDate.HasValue)
            StartDate = goal.StartDate.Value;
        if (goal.DueDate.HasValue)
            EndDate = goal.DueDate.Value;
        
        // Set status (displayed as "health" in UI)
        if (!string.IsNullOrEmpty(goal.Status))
        {
            HealthIndex = GetIndexByTag(HealthTags, goal.Status);
        }
        
        // Owner is set in SetTeamMembers if called after LoadGoal
        if (goal.OwnerTeamMemberId != Guid.Empty && TeamMembers.Count > 0)
        {
            SelectedOwner = TeamMembers.FirstOrDefault(t => t.Id == goal.OwnerTeamMemberId);
        }
    }
    
    /// <summary>
    /// Set the list of team members for the owner dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        TeamMembers.Clear();
        foreach (var member in teamMembers)
        {
            TeamMembers.Add(member);
        }
        
        // If editing and we have an owner, select it
        if (_existingGoal != null && _existingGoal.OwnerTeamMemberId != Guid.Empty)
        {
            SelectedOwner = TeamMembers.FirstOrDefault(t => t.Id == _existingGoal.OwnerTeamMemberId);
        }
    }
    
    private static int GetIndexByTag(string[] tags, string tag)
    {
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i].Equals(tag, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return 0;
    }
    
    private static string? GetTagByIndex(string[] tags, int index)
    {
        if (index >= 0 && index < tags.Length)
        {
            return tags[index];
        }
        return null;
    }
    
    /// <summary>
    /// Sets the dialog service for showing confirmations.
    /// </summary>
    public void SetDialogService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    
    /// <summary>
    /// Returns true if the user has entered any data that would be lost on cancel.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            // For editing, less critical since data exists
            if (IsEditMode) return false;
            
            return !string.IsNullOrWhiteSpace(Title) ||
                   !string.IsNullOrWhiteSpace(Description) ||
                   SelectedOwner != null;
        }
    }
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        Debug.WriteLine($"[EditGoalDialog] CancelAsync called - HasUnsavedChanges: {HasUnsavedChanges}, DialogService: {_dialogService != null}");
        
        // Show confirmation if there's unsaved data during creation
        if (HasUnsavedChanges && _dialogService != null)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to close without saving?",
                "Discard",
                "Keep Editing");
            
            Debug.WriteLine($"[EditGoalDialog] Confirmation result: {confirmed}");
            
            if (!confirmed)
            {
                return;
            }
        }
        
        Debug.WriteLine("[EditGoalDialog] Closing dialog via CloseRequested");
        Result = null;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private void Save()
    {
        // Validate
        var title = Title?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }
        
        Result = new EditGoalResult
        {
            Id = _existingGoal?.Id,
            Title = title,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            GoalType = GetTagByIndex(GoalTypeTags, GoalTypeIndex),
            StartDate = StartDate,
            DueDate = EndDate,
            OwnerTeamMemberId = SelectedOwner?.Id,
            Status = GetTagByIndex(HealthTags, HealthIndex),
            IsDeleted = false
        };
        
        Debug.WriteLine($"[EditGoalDialog] Saving goal: {title}");
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private void Delete()
    {
        Result = new EditGoalResult
        {
            Id = _existingGoal?.Id,
            IsDeleted = true
        };
        
        Debug.WriteLine($"[EditGoalDialog] Deleting goal: {_existingGoal?.Id}");
        CloseRequested?.Invoke();
    }
}
