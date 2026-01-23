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
/// Only includes fields that exist in the procohere.goals table.
/// </summary>
public class EditGoalResult
{
    public bool IsDeleted { get; set; }
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GoalType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? OwnerTeamMemberId { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
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
        
        // Note: TimePeriod and Year properties don't exist in the database schema
        // The dialog will use default values for new goals
        
        // Set dates
        if (goal.StartDate.HasValue)
            StartDatePicker.SelectedDate = new DateTimeOffset(goal.StartDate.Value);
        if (goal.DueDate.HasValue)
            EndDatePicker.SelectedDate = new DateTimeOffset(goal.DueDate.Value);
        
        // Set status (displayed as "health" in UI)
        if (!string.IsNullOrEmpty(goal.Status))
        {
            SelectComboBoxByTag(HealthComboBox, goal.Status);
        }
        
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
        
        // Get status (from health dropdown)
        var healthItem = HealthComboBox.SelectedItem as ComboBoxItem;
        var status = healthItem?.Tag?.ToString();
        
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
            StartDate = StartDatePicker.SelectedDate?.DateTime,
            DueDate = EndDatePicker.SelectedDate?.DateTime,
            OwnerTeamMemberId = ownerTeamMemberId,
            Status = status,
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
