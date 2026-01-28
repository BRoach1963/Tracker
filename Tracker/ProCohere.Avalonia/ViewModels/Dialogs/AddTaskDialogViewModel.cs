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
/// ViewModel for the AddTaskDialog.
/// </summary>
public partial class AddTaskDialogViewModel : ObservableObject
{
    private TaskDetail? _existingTask;
    private IDialogService? _dialogService;
    
    /// <summary>
    /// Result of the dialog - the task data if saved, null if cancelled.
    /// </summary>
    public AddTaskResult? Result { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;
    
    #region Observable Properties
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    [ObservableProperty]
    private DateTimeOffset? _dueDate;
    
    [ObservableProperty]
    private int _priorityIndex = -1;
    
    [ObservableProperty]
    private int _statusIndex;
    
    [ObservableProperty]
    private bool _isEditMode;
    
    [ObservableProperty]
    private string _dialogTitleText = "New Task";
    
    [ObservableProperty]
    private string _saveButtonText = "Create Task";
    
    [ObservableProperty]
    private ObservableCollection<TeamMemberDetail> _teamMembers = new();
    
    [ObservableProperty]
    private TeamMemberDetail? _selectedAssignee;
    
    #endregion
    
    // Priority tags matching XAML order: high (0), medium (1), low (2)
    private static readonly string[] PriorityTags = { "high", "medium", "low" };
    
    // Status tags matching XAML order: not_started (0), in_progress (1), completed (2), blocked (3)
    private static readonly string[] StatusTags = { "not_started", "in_progress", "completed", "blocked" };
    
    public AddTaskDialogViewModel()
    {
        // Set default due date to tomorrow
        DueDate = DateTimeOffset.Now.AddDays(1);
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
                   PriorityIndex >= 0 ||
                   SelectedAssignee != null;
        }
    }
    
    /// <summary>
    /// Sets the list of team members for the assignee dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> members)
    {
        TeamMembers = new ObservableCollection<TeamMemberDetail>(members);
        
        // If editing and we have an assignee, select it
        if (_existingTask?.OwnerTeamMemberId.HasValue == true)
        {
            SelectedAssignee = TeamMembers.FirstOrDefault(t => t.Id == _existingTask.OwnerTeamMemberId.Value);
        }
    }
    
    /// <summary>
    /// Load an existing task for editing.
    /// </summary>
    public void LoadTask(TaskDetail task)
    {
        _existingTask = task;
        
        IsEditMode = true;
        DialogTitleText = "Edit Task";
        SaveButtonText = "Save Changes";
        
        Title = task.Title;
        Description = task.Description ?? string.Empty;
        
        // Set priority index
        if (!string.IsNullOrEmpty(task.Priority))
        {
            var index = Array.FindIndex(PriorityTags, p => p.Equals(task.Priority, StringComparison.OrdinalIgnoreCase));
            PriorityIndex = index >= 0 ? index : -1;
        }
        
        // Set status index
        if (!string.IsNullOrEmpty(task.Status))
        {
            var index = Array.FindIndex(StatusTags, s => s.Equals(task.Status, StringComparison.OrdinalIgnoreCase));
            StatusIndex = index >= 0 ? index : 0;
        }
        
        // Set due date
        if (task.DueDate.HasValue)
        {
            DueDate = new DateTimeOffset(task.DueDate.Value);
        }
        
        // Assignee is set in SetTeamMembers if called after LoadTask
        if (task.OwnerTeamMemberId.HasValue && TeamMembers.Count > 0)
        {
            SelectedAssignee = TeamMembers.FirstOrDefault(t => t.Id == task.OwnerTeamMemberId.Value);
        }
    }
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        // Show confirmation if there's unsaved data during creation
        if (HasUnsavedChanges && _dialogService != null)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to close without saving?",
                "Discard",
                "Keep Editing");
            
            if (!confirmed)
            {
                return;
            }
        }
        
        Result = null;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private void Save()
    {
        var titleText = Title?.Trim();
        
        if (string.IsNullOrWhiteSpace(titleText))
        {
            // ViewModel can't focus - View handles this via CanExecute or validation
            return;
        }
        
        // Get priority from index
        string? priority = PriorityIndex >= 0 && PriorityIndex < PriorityTags.Length 
            ? PriorityTags[PriorityIndex] 
            : null;
        
        // Get status (only for edit mode)
        string? status = null;
        if (IsEditMode)
        {
            status = StatusIndex >= 0 && StatusIndex < StatusTags.Length 
                ? StatusTags[StatusIndex] 
                : "not_started";
        }

        Result = new AddTaskResult
        {
            Id = _existingTask?.Id,
            Title = titleText,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            Priority = priority,
            Status = status,
            DueDate = DueDate?.DateTime,
            AssigneeId = SelectedAssignee?.Id,
            IsDeleted = false
        };

        Debug.WriteLine($"[AddTaskDialog] Saving task: {titleText}");
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private void Delete()
    {
        Result = new AddTaskResult
        {
            Id = _existingTask?.Id,
            Title = _existingTask?.Title ?? string.Empty,
            IsDeleted = true
        };
        
        Debug.WriteLine($"[AddTaskDialog] Deleting task: {_existingTask?.Id}");
        CloseRequested?.Invoke();
    }
}
