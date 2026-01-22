using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Result from the edit goal dialog.
/// </summary>
public class EditGoalResult
{
    public bool IsDeleted { get; set; }
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GoalType { get; set; }
    public string? TimePeriod { get; set; }
    public int? Year { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? OwnerTeamMemberId { get; set; }
    public string? Health { get; set; }
    public string? HealthReason { get; set; }
    public string? Lifecycle { get; set; }
    public bool IsTeamVisible { get; set; }
    public bool IsOrgVisible { get; set; }
}

/// <summary>
/// Dialog for creating or editing goals.
/// </summary>
public partial class EditGoalDialog : Window
{
    private GoalDetail? _existingGoal;
    private List<TeamMemberDetail> _teamMembers = new();
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditGoalResult? Result { get; private set; }
    
    public EditGoalDialog()
    {
        InitializeComponent();
        
        // Populate year dropdown
        var currentYear = DateTime.Now.Year;
        var years = new List<ComboBoxItem>();
        for (int year = currentYear - 1; year <= currentYear + 2; year++)
        {
            years.Add(new ComboBoxItem { Content = year.ToString(), Tag = year });
        }
        YearComboBox.ItemsSource = years;
        YearComboBox.SelectedIndex = 1; // Current year
        
        // Set default dates based on current quarter
        SetDefaultDates();
    }
    
    private void SetDefaultDates()
    {
        var now = DateTime.Now;
        var quarter = (now.Month - 1) / 3 + 1;
        var quarterStart = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
        var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
        
        StartDatePicker.SelectedDate = new DateTimeOffset(quarterStart);
        EndDatePicker.SelectedDate = new DateTimeOffset(quarterEnd);
        
        // Select current quarter
        TimePeriodComboBox.SelectedIndex = quarter - 1;
    }
    
    /// <summary>
    /// Load an existing goal for editing.
    /// </summary>
    public void LoadGoal(GoalDetail goal)
    {
        _existingGoal = goal;
        
        DialogTitle.Text = "Edit Goal";
        SaveButton.Content = "Save Changes";
        DeleteButton.IsVisible = true;
        HealthSection.IsVisible = true;
        LifecycleSection.IsVisible = true;
        
        TitleTextBox.Text = goal.Title;
        DescriptionTextBox.Text = goal.Description ?? "";
        
        // Set goal type
        if (!string.IsNullOrEmpty(goal.GoalTypeValue))
        {
            SelectComboBoxByTag(GoalTypeComboBox, goal.GoalTypeValue);
        }
        
        // Set time period
        if (!string.IsNullOrEmpty(goal.TimePeriod))
        {
            SelectComboBoxByTag(TimePeriodComboBox, goal.TimePeriod);
        }
        
        // Set year
        if (goal.Year.HasValue)
        {
            for (int i = 0; i < YearComboBox.Items.Count; i++)
            {
                var item = YearComboBox.Items[i] as ComboBoxItem;
                if (item?.Tag is int year && year == goal.Year.Value)
                {
                    YearComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
        
        // Set dates
        if (goal.StartDate.HasValue)
            StartDatePicker.SelectedDate = new DateTimeOffset(goal.StartDate.Value);
        if (goal.EndDate.HasValue)
            EndDatePicker.SelectedDate = new DateTimeOffset(goal.EndDate.Value);
        
        // Set health
        if (!string.IsNullOrEmpty(goal.HealthValue))
        {
            SelectComboBoxByTag(HealthComboBox, goal.HealthValue);
        }
        HealthReasonTextBox.Text = goal.HealthReason ?? "";
        
        // Set lifecycle
        if (!string.IsNullOrEmpty(goal.LifecycleValue))
        {
            SelectComboBoxByTag(LifecycleComboBox, goal.LifecycleValue);
        }
        
        // Set visibility
        if (goal.IsOrgVisible)
            VisibilityComboBox.SelectedIndex = 2; // Organization
        else if (goal.IsTeamVisible)
            VisibilityComboBox.SelectedIndex = 1; // Team
        else
            VisibilityComboBox.SelectedIndex = 0; // Private
        
        // Owner is set in SetTeamMembers if called after LoadGoal
        if (goal.OwnerTeamMemberId.HasValue && _teamMembers.Count > 0)
        {
            var owner = _teamMembers.FirstOrDefault(t => t.Id == goal.OwnerTeamMemberId.Value);
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
        if (_existingGoal?.OwnerTeamMemberId.HasValue == true)
        {
            var owner = _teamMembers.FirstOrDefault(t => t.Id == _existingGoal.OwnerTeamMemberId.Value);
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
        var title = TitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            TitleTextBox.Focus();
            return;
        }
        
        // Get goal type
        var goalTypeItem = GoalTypeComboBox.SelectedItem as ComboBoxItem;
        var goalType = goalTypeItem?.Tag?.ToString();
        
        // Get time period
        var timePeriodItem = TimePeriodComboBox.SelectedItem as ComboBoxItem;
        var timePeriod = timePeriodItem?.Tag?.ToString();
        
        // Get year
        var yearItem = YearComboBox.SelectedItem as ComboBoxItem;
        var year = yearItem?.Tag as int?;
        
        // Get health
        var healthItem = HealthComboBox.SelectedItem as ComboBoxItem;
        var health = healthItem?.Tag?.ToString();
        
        // Get lifecycle
        var lifecycleItem = LifecycleComboBox.SelectedItem as ComboBoxItem;
        var lifecycle = lifecycleItem?.Tag?.ToString();
        
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
        
        Result = new EditGoalResult
        {
            Id = _existingGoal?.Id,
            Title = title,
            Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
            GoalType = goalType,
            TimePeriod = timePeriod,
            Year = year,
            StartDate = StartDatePicker.SelectedDate?.DateTime,
            EndDate = EndDatePicker.SelectedDate?.DateTime,
            OwnerTeamMemberId = ownerTeamMemberId,
            Health = health,
            HealthReason = string.IsNullOrWhiteSpace(HealthReasonTextBox.Text) ? null : HealthReasonTextBox.Text.Trim(),
            Lifecycle = lifecycle,
            IsTeamVisible = isTeamVisible,
            IsOrgVisible = isOrgVisible,
            IsDeleted = false
        };
        
        Debug.WriteLine($"[EditGoalDialog] Saving goal: {title}");
        Close();
    }
    
    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = new EditGoalResult
        {
            Id = _existingGoal?.Id,
            IsDeleted = true
        };
        
        Debug.WriteLine($"[EditGoalDialog] Deleting goal: {_existingGoal?.Id}");
        Close();
    }
}
