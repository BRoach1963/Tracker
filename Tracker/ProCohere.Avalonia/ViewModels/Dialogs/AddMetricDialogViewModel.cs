using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the AddMetricDialog.
/// Handles creating new metrics with pre-populated defaults.
/// </summary>
public partial class AddMetricDialogViewModel : ObservableObject
{
    private IDialogService? _dialogService;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditMetricResult? Result { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;
    
    #region Observable Properties
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    [ObservableProperty]
    private string _category = string.Empty;
    
    [ObservableProperty]
    private string _currentValueText = "0";
    
    [ObservableProperty]
    private string _targetValueText = string.Empty;
    
    [ObservableProperty]
    private string _baselineValueText = string.Empty;
    
    [ObservableProperty]
    private string _unit = string.Empty;
    
    [ObservableProperty]
    private int _directionIndex;
    
    [ObservableProperty]
    private int _sourceIndex = 2; // Default to Manual
    
    [ObservableProperty]
    private int _scopeIndex;
    
    [ObservableProperty]
    private int _frequencyIndex = 2; // Default to Monthly
    
    [ObservableProperty]
    private int _visibilityIndex = 1; // Default to Team
    
    [ObservableProperty]
    private bool _isSensitive;
    
    [ObservableProperty]
    private TeamMemberDetail? _selectedOwner;
    
    #endregion
    
    /// <summary>
    /// Team members available for owner selection.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> TeamMembers { get; } = new();
    
    // Tag values for combo boxes (must match XAML order)
    private static readonly string[] DirectionTags = { "higher_is_better", "lower_is_better", "neutral" };
    private static readonly string[] SourceTags = { "system", "survey", "manual" };
    private static readonly string[] ScopeTags = { "individual", "team", "organization" };
    private static readonly string[] FrequencyTags = { "daily", "weekly", "monthly", "quarterly" };
    private static readonly string[] VisibilityTags = { "private", "team", "organization" };
    
    /// <summary>
    /// Set the list of team members for the owner dropdown.
    /// </summary>
    public void SetTeamMembers(System.Collections.Generic.IEnumerable<TeamMemberDetail> teamMembers)
    {
        TeamMembers.Clear();
        foreach (var member in teamMembers)
        {
            TeamMembers.Add(member);
        }
    }
    
    /// <summary>
    /// Pre-select a specific team member as the steward.
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
        !string.IsNullOrWhiteSpace(Name) ||
        !string.IsNullOrWhiteSpace(Description) ||
        !string.IsNullOrWhiteSpace(CurrentValueText) && CurrentValueText != "0" ||
        !string.IsNullOrWhiteSpace(TargetValueText) ||
        SelectedOwner != null;
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        Debug.WriteLine($"[AddMetricDialog] CancelAsync called - HasUnsavedChanges: {HasUnsavedChanges}");
        
        // Show confirmation if there's unsaved data
        if (HasUnsavedChanges && _dialogService != null)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to close without saving?",
                "Discard",
                "Keep Editing");
            
            Debug.WriteLine($"[AddMetricDialog] Confirmation result: {confirmed}");
            
            if (!confirmed)
            {
                return;
            }
        }
        
        Debug.WriteLine("[AddMetricDialog] Closing dialog via CloseRequested");
        Result = null;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private void Save()
    {
        // Validate
        var name = Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
        
        // Parse current value
        if (!decimal.TryParse(CurrentValueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var currentValue))
        {
            currentValue = 0;
        }
        
        // Parse optional target value
        decimal? targetValue = null;
        if (!string.IsNullOrWhiteSpace(TargetValueText) &&
            decimal.TryParse(TargetValueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var tv))
        {
            targetValue = tv;
        }
        
        // Parse optional baseline value
        decimal? baselineValue = null;
        if (!string.IsNullOrWhiteSpace(BaselineValueText) &&
            decimal.TryParse(BaselineValueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var bv))
        {
            baselineValue = bv;
        }
        
        // Get visibility
        var visibilityTag = GetTagByIndex(VisibilityTags, VisibilityIndex) ?? "team";
        bool isTeamVisible = visibilityTag == "team" || visibilityTag == "organization";
        bool isOrgVisible = visibilityTag == "organization";
        
        Result = new EditMetricResult
        {
            Id = null, // New metric
            Name = name,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
            CurrentValue = currentValue,
            TargetValue = targetValue,
            BaselineValue = baselineValue,
            Unit = string.IsNullOrWhiteSpace(Unit) ? null : Unit.Trim(),
            TargetDirection = GetTagByIndex(DirectionTags, DirectionIndex),
            Source = GetTagByIndex(SourceTags, SourceIndex),
            Scope = GetTagByIndex(ScopeTags, ScopeIndex),
            Frequency = GetTagByIndex(FrequencyTags, FrequencyIndex),
            OwnerTeamMemberId = SelectedOwner?.Id,
            Lifecycle = "active", // New metrics always start as active
            IsTeamVisible = isTeamVisible,
            IsOrgVisible = isOrgVisible,
            IsSensitive = IsSensitive,
            IsDeleted = false
        };
        
        Debug.WriteLine($"[AddMetricDialog] Saving new metric: {name}");
        CloseRequested?.Invoke();
    }
}
