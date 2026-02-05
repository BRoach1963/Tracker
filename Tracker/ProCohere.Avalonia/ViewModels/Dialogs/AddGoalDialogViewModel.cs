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
/// ViewModel for the AddGoalDialog.
/// Handles creating new goals with optional pre-populated owner.
/// </summary>
public partial class AddGoalDialogViewModel : ObservableObject
{
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
    private static readonly string[] TimePeriodTags = { "Q1", "Q2", "Q3", "Q4", "H1", "H2", "Annual" };
    private static readonly string[] VisibilityTags = { "private", "team", "organization" };
    
    public AddGoalDialogViewModel()
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
    /// Set the list of team members for the owner dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        TeamMembers.Clear();
        foreach (var member in teamMembers)
        {
            TeamMembers.Add(member);
        }
    }
    
    /// <summary>
    /// Pre-select a specific team member as the owner.
    /// </summary>
    public void SetDefaultOwner(Guid teamMemberId)
    {
        if (teamMemberId == Guid.Empty) return;
        
        SelectedOwner = TeamMembers.FirstOrDefault(t => t.Id == teamMemberId);
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
    public bool HasUnsavedChanges =>
        !string.IsNullOrWhiteSpace(Title) ||
        !string.IsNullOrWhiteSpace(Description) ||
        SelectedOwner != null;
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        Debug.WriteLine($"[AddGoalDialog] CancelAsync called - HasUnsavedChanges: {HasUnsavedChanges}");
        
        // Show confirmation if there's unsaved data
        if (HasUnsavedChanges && _dialogService != null)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to close without saving?",
                "Discard",
                "Keep Editing");
            
            Debug.WriteLine($"[AddGoalDialog] Confirmation result: {confirmed}");
            
            if (!confirmed)
            {
                return;
            }
        }
        
        Debug.WriteLine("[AddGoalDialog] Closing dialog via CloseRequested");
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
            Id = null, // New goal
            Title = title,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            GoalType = GetTagByIndex(GoalTypeTags, GoalTypeIndex),
            StartDate = StartDate,
            DueDate = EndDate,
            OwnerTeamMemberId = SelectedOwner?.Id,
            Status = "active", // New goals always start as active
            IsDeleted = false
        };
        
        Debug.WriteLine($"[AddGoalDialog] Saving new goal: {title}");
        CloseRequested?.Invoke();
    }
}
