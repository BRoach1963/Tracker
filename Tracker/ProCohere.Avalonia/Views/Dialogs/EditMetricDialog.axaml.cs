using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Result from the edit metric dialog.
/// </summary>
public class EditMetricResult
{
    public bool IsDeleted { get; set; }
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? BaselineValue { get; set; }
    public string? Unit { get; set; }
    public string? TargetDirection { get; set; }
    public string? Source { get; set; }
    public string? Scope { get; set; }
    public string? Frequency { get; set; }
    public Guid? OwnerTeamMemberId { get; set; }
    public string? Lifecycle { get; set; }
    public bool IsTeamVisible { get; set; }
    public bool IsOrgVisible { get; set; }
    public bool IsSensitive { get; set; }
}

/// <summary>
/// Dialog for creating or editing metrics.
/// </summary>
public partial class EditMetricDialog : Window
{
    private MetricDetail? _existingMetric;
    private List<TeamMemberDetail> _teamMembers = new();
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditMetricResult? Result { get; private set; }
    
    public EditMetricDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Load an existing metric for editing.
    /// </summary>
    public void LoadMetric(MetricDetail metric)
    {
        _existingMetric = metric;
        
        DialogTitle.Text = "Edit Metric";
        SaveButton.Content = "Save Changes";
        DeleteButton.IsVisible = true;
        LifecycleSection.IsVisible = true;
        
        NameTextBox.Text = metric.Name;
        DescriptionTextBox.Text = metric.Description ?? "";
        // Note: Category doesn't exist in DB schema, leave blank
        CategoryTextBox.Text = "";
        
        CurrentValueTextBox.Text = metric.CurrentValue?.ToString(CultureInfo.InvariantCulture) ?? "";
        TargetValueTextBox.Text = metric.TargetValue?.ToString(CultureInfo.InvariantCulture) ?? "";
        // Note: BaselineValue doesn't exist in DB schema, leave blank
        BaselineValueTextBox.Text = "";
        UnitTextBox.Text = metric.Unit ?? "";
        
        // Set direction
        if (!string.IsNullOrEmpty(metric.TargetDirection))
        {
            SelectComboBoxByTag(DirectionComboBox, metric.TargetDirection);
        }
        
        // Note: Source, Scope don't exist in DB schema - use defaults
        // SourceComboBox and ScopeComboBox will use their default selections
        
        // Set frequency
        if (!string.IsNullOrEmpty(metric.Frequency))
        {
            SelectComboBoxByTag(FrequencyComboBox, metric.Frequency);
        }
        
        // Note: Lifecycle, IsOrgVisible, IsTeamVisible, IsSensitive don't exist in DB schema
        // Use default values (LifecycleComboBox defaults to Active, VisibilityComboBox to Team)
        VisibilityComboBox.SelectedIndex = 1; // Default to Team
        IsSensitiveCheckBox.IsChecked = false;
        
        // Owner is set in SetTeamMembers if called after LoadMetric
        if (metric.OwnerTeamMemberId.HasValue && _teamMembers.Count > 0)
        {
            var owner = _teamMembers.FirstOrDefault(t => t.Id == metric.OwnerTeamMemberId.Value);
            if (owner != null)
            {
                OwnerComboBox.SelectedItem = owner;
            }
        }
    }
    
    /// <summary>
    /// Set the list of team members for the owner dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        _teamMembers = teamMembers.ToList();
        OwnerComboBox.ItemsSource = _teamMembers;
        
        // If editing and we have an owner, select it
        if (_existingMetric?.OwnerTeamMemberId.HasValue == true)
        {
            var owner = _teamMembers.FirstOrDefault(t => t.Id == _existingMetric.OwnerTeamMemberId.Value);
            if (owner != null)
            {
                OwnerComboBox.SelectedItem = owner;
            }
        }
    }
    
    private void SelectComboBoxByTag(ComboBox comboBox, string tag)
    {
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            var item = comboBox.Items[i] as ComboBoxItem;
            if (item?.Tag?.ToString()?.Equals(tag, StringComparison.OrdinalIgnoreCase) == true)
            {
                comboBox.SelectedIndex = i;
                break;
            }
        }
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
    
    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Validate
        var name = NameTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            NameTextBox.Focus();
            return;
        }
        
        // Parse current value
        if (!decimal.TryParse(CurrentValueTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var currentValue))
        {
            currentValue = 0;
        }
        
        // Parse optional target value
        decimal? targetValue = null;
        if (!string.IsNullOrWhiteSpace(TargetValueTextBox.Text) &&
            decimal.TryParse(TargetValueTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var tv))
        {
            targetValue = tv;
        }
        
        // Parse optional baseline value
        decimal? baselineValue = null;
        if (!string.IsNullOrWhiteSpace(BaselineValueTextBox.Text) &&
            decimal.TryParse(BaselineValueTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var bv))
        {
            baselineValue = bv;
        }
        
        // Get direction
        var directionItem = DirectionComboBox.SelectedItem as ComboBoxItem;
        var direction = directionItem?.Tag?.ToString();
        
        // Get source
        var sourceItem = SourceComboBox.SelectedItem as ComboBoxItem;
        var source = sourceItem?.Tag?.ToString();
        
        // Get scope
        var scopeItem = ScopeComboBox.SelectedItem as ComboBoxItem;
        var scope = scopeItem?.Tag?.ToString();
        
        // Get frequency
        var frequencyItem = FrequencyComboBox.SelectedItem as ComboBoxItem;
        var frequency = frequencyItem?.Tag?.ToString();
        
        // Get lifecycle
        var lifecycleItem = LifecycleComboBox.SelectedItem as ComboBoxItem;
        var lifecycle = lifecycleItem?.Tag?.ToString() ?? "active";
        
        // Get visibility
        var visibilityItem = VisibilityComboBox.SelectedItem as ComboBoxItem;
        var visibilityTag = visibilityItem?.Tag?.ToString() ?? "team";
        bool isTeamVisible = visibilityTag == "team" || visibilityTag == "organization";
        bool isOrgVisible = visibilityTag == "organization";
        
        // Get owner
        Guid? ownerTeamMemberId = null;
        if (OwnerComboBox.SelectedItem is TeamMemberDetail member)
        {
            ownerTeamMemberId = member.Id;
        }
        
        Result = new EditMetricResult
        {
            Id = _existingMetric?.Id,
            Name = name,
            Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
            Category = string.IsNullOrWhiteSpace(CategoryTextBox.Text) ? null : CategoryTextBox.Text.Trim(),
            CurrentValue = currentValue,
            TargetValue = targetValue,
            BaselineValue = baselineValue,
            Unit = string.IsNullOrWhiteSpace(UnitTextBox.Text) ? null : UnitTextBox.Text.Trim(),
            TargetDirection = direction,
            Source = source,
            Scope = scope,
            Frequency = frequency,
            OwnerTeamMemberId = ownerTeamMemberId,
            Lifecycle = lifecycle,
            IsTeamVisible = isTeamVisible,
            IsOrgVisible = isOrgVisible,
            IsSensitive = IsSensitiveCheckBox.IsChecked ?? false,
            IsDeleted = false
        };
        
        Debug.WriteLine($"[EditMetricDialog] Saving metric: {name}");
        Close();
    }
    
    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = new EditMetricResult
        {
            Id = _existingMetric?.Id,
            IsDeleted = true
        };
        
        Debug.WriteLine($"[EditMetricDialog] Deleting metric: {_existingMetric?.Id}");
        Close();
    }
}
