using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the EditMetricDialog.
/// </summary>
public partial class EditMetricDialogViewModel : ObservableObject
{
    private MetricDetail? _existingMetric;
    
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
    private string _dialogTitle = "New Metric";
    
    [ObservableProperty]
    private string _saveButtonText = "Create Metric";
    
    [ObservableProperty]
    private bool _isDeleteVisible;
    
    [ObservableProperty]
    private bool _isLifecycleVisible;
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    [ObservableProperty]
    private string _category = string.Empty;
    
    [ObservableProperty]
    private string _currentValueText = string.Empty;
    
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
    private int _lifecycleIndex;
    
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
    private static readonly string[] LifecycleTags = { "active", "dormant", "retired" };
    private static readonly string[] VisibilityTags = { "private", "team", "organization" };
    
    /// <summary>
    /// Load an existing metric for editing.
    /// </summary>
    public void LoadMetric(MetricDetail metric)
    {
        _existingMetric = metric;
        
        DialogTitle = "Edit Metric";
        SaveButtonText = "Save Changes";
        IsDeleteVisible = true;
        IsLifecycleVisible = true;
        
        Name = metric.Name;
        Description = metric.Description ?? string.Empty;
        Category = string.Empty; // Category doesn't exist in DB schema
        
        CurrentValueText = metric.CurrentValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        TargetValueText = metric.TargetValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        BaselineValueText = string.Empty; // BaselineValue doesn't exist in DB schema
        Unit = metric.Unit ?? string.Empty;
        
        // Set direction
        if (!string.IsNullOrEmpty(metric.TargetDirection))
        {
            DirectionIndex = GetIndexByTag(DirectionTags, metric.TargetDirection);
        }
        
        // Set frequency
        if (!string.IsNullOrEmpty(metric.Frequency))
        {
            FrequencyIndex = GetIndexByTag(FrequencyTags, metric.Frequency);
        }
        
        // Default visibility to Team (index 1)
        VisibilityIndex = 1;
        IsSensitive = false;
        
        // Owner is set in SetTeamMembers if called after LoadMetric
        if (metric.OwnerTeamMemberId.HasValue && TeamMembers.Count > 0)
        {
            SelectedOwner = TeamMembers.FirstOrDefault(t => t.Id == metric.OwnerTeamMemberId.Value);
        }
    }
    
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
        
        // If editing and we have an owner, select it
        if (_existingMetric?.OwnerTeamMemberId.HasValue == true)
        {
            SelectedOwner = TeamMembers.FirstOrDefault(t => t.Id == _existingMetric.OwnerTeamMemberId.Value);
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
    
    [RelayCommand]
    private void Cancel()
    {
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
            // Focus handled by View
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
            Id = _existingMetric?.Id,
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
            Lifecycle = GetTagByIndex(LifecycleTags, LifecycleIndex) ?? "active",
            IsTeamVisible = isTeamVisible,
            IsOrgVisible = isOrgVisible,
            IsSensitive = IsSensitive,
            IsDeleted = false
        };
        
        Debug.WriteLine($"[EditMetricDialog] Saving metric: {name}");
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private void Delete()
    {
        Result = new EditMetricResult
        {
            Id = _existingMetric?.Id,
            IsDeleted = true
        };
        
        Debug.WriteLine($"[EditMetricDialog] Deleting metric: {_existingMetric?.Id}");
        CloseRequested?.Invoke();
    }
}
