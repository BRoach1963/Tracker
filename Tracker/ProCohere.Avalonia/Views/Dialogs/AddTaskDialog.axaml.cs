using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating a new task.
/// </summary>
public partial class AddTaskDialog : Window
{
    /// <summary>
    /// Result of the dialog - the task data if created, null if cancelled.
    /// </summary>
    public AddTaskResult? Result { get; private set; }

    public AddTaskDialog()
    {
        InitializeComponent();
        
        // Set default due date to tomorrow
        DueDatePicker.SelectedDate = DateTimeOffset.Now.AddDays(1);
        
        // Focus the title field
        TitleTextBox.AttachedToVisualTree += (s, e) => TitleTextBox.Focus();
    }

    /// <summary>
    /// Sets the list of team members for the assignee dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> members)
    {
        AssigneeComboBox.ItemsSource = members;
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void CreateButton_Click(object? sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(title))
        {
            // Could show validation error, but for now just return
            TitleTextBox.Focus();
            return;
        }

        Result = new AddTaskResult
        {
            Title = title,
            Description = DescriptionTextBox.Text?.Trim(),
            Priority = (PriorityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
            DueDate = DueDatePicker.SelectedDate?.DateTime,
            AssigneeId = (AssigneeComboBox.SelectedItem as TeamMemberDetail)?.Id
        };

        Close();
    }
}

/// <summary>
/// Result data from the AddTaskDialog.
/// </summary>
public class AddTaskResult
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? AssigneeId { get; init; }
}
