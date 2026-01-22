using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing a task.
/// </summary>
public partial class AddTaskDialog : Window
{
    private TaskDetail? _existingTask;
    private List<TeamMemberDetail> _teamMembers = new();
    
    /// <summary>
    /// Result of the dialog - the task data if saved, null if cancelled.
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
        _teamMembers = members.ToList();
        AssigneeComboBox.ItemsSource = _teamMembers;
        
        // If editing and we have an assignee, select it
        if (_existingTask?.OwnerTeamMemberId.HasValue == true)
        {
            var assignee = _teamMembers.FirstOrDefault(t => t.Id == _existingTask.OwnerTeamMemberId.Value);
            if (assignee != null)
            {
                AssigneeComboBox.SelectedItem = assignee;
            }
        }
    }
    
    /// <summary>
    /// Load an existing task for editing.
    /// </summary>
    public void LoadTask(TaskDetail task)
    {
        _existingTask = task;
        
        DialogTitle.Text = "Edit Task";
        CreateButton.Content = "Save Changes";
        DeleteButton.IsVisible = true;
        StatusSection.IsVisible = true;
        
        TitleTextBox.Text = task.Title;
        DescriptionTextBox.Text = task.Description ?? "";
        
        // Set priority
        if (!string.IsNullOrEmpty(task.Priority))
        {
            for (int i = 0; i < PriorityComboBox.Items.Count; i++)
            {
                var item = PriorityComboBox.Items[i] as ComboBoxItem;
                if (item?.Tag?.ToString()?.Equals(task.Priority, StringComparison.OrdinalIgnoreCase) == true)
                {
                    PriorityComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
        
        // Set status
        for (int i = 0; i < StatusComboBox.Items.Count; i++)
        {
            var item = StatusComboBox.Items[i] as ComboBoxItem;
            if (item?.Tag?.ToString() == task.Status)
            {
                StatusComboBox.SelectedIndex = i;
                break;
            }
        }
        
        // Set due date
        if (task.DueDate.HasValue)
        {
            DueDatePicker.SelectedDate = new DateTimeOffset(task.DueDate.Value);
        }
        
        // Assignee is set in SetTeamMembers if called after LoadTask
        if (task.OwnerTeamMemberId.HasValue && _teamMembers.Count > 0)
        {
            var assignee = _teamMembers.FirstOrDefault(t => t.Id == task.OwnerTeamMemberId.Value);
            if (assignee != null)
            {
                AssigneeComboBox.SelectedItem = assignee;
            }
        }
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
            TitleTextBox.Focus();
            return;
        }
        
        // Get priority
        var priorityItem = PriorityComboBox.SelectedItem as ComboBoxItem;
        var priority = priorityItem?.Tag?.ToString();
        
        // Get status (only for edit mode)
        string? status = null;
        if (_existingTask != null)
        {
            var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;
            status = statusItem?.Tag?.ToString() ?? "not_started";
        }

        Result = new AddTaskResult
        {
            Id = _existingTask?.Id,
            Title = title,
            Description = DescriptionTextBox.Text?.Trim(),
            Priority = priority,
            Status = status,
            DueDate = DueDatePicker.SelectedDate?.DateTime,
            AssigneeId = (AssigneeComboBox.SelectedItem as TeamMemberDetail)?.Id,
            IsDeleted = false
        };

        Debug.WriteLine($"[AddTaskDialog] Saving task: {title}");
        Close();
    }
    
    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = new AddTaskResult
        {
            Id = _existingTask?.Id,
            Title = _existingTask?.Title ?? "",
            IsDeleted = true
        };
        
        Debug.WriteLine($"[AddTaskDialog] Deleting task: {_existingTask?.Id}");
        Close();
    }
}

/// <summary>
/// Result data from the AddTaskDialog.
/// </summary>
public class AddTaskResult
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Priority { get; init; }
    public string? Status { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? AssigneeId { get; init; }
    public bool IsDeleted { get; init; }
}
