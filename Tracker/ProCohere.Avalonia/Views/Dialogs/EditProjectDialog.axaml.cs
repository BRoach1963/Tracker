using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing a project.
/// </summary>
public partial class EditProjectDialog : Window
{
    private Project? _editingProject;
    private bool _isEditMode;
    private bool _forceClose;
    
    /// <summary>
    /// The result project after saving.
    /// </summary>
    public Project? Result { get; private set; }

    public EditProjectDialog()
    {
        InitializeComponent();
        StatusComboBox.SelectedIndex = 0; // Default to Active
    }

    /// <summary>
    /// Initialize the dialog for creating a new project.
    /// </summary>
    public void InitForCreate()
    {
        _isEditMode = false;
        _editingProject = null;
        DialogTitle.Text = "New Project";
        SaveButton.Content = "Create Project";
        StatusComboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Initialize the dialog for editing an existing project.
    /// </summary>
    public void InitForEdit(Project project)
    {
        _isEditMode = true;
        _editingProject = project;
        DialogTitle.Text = "Edit Project";
        SaveButton.Content = "Save Changes";
        
        // Populate fields
        NameTextBox.Text = project.Name ?? string.Empty;
        DescriptionTextBox.Text = project.Description ?? string.Empty;
        
        // Set status
        var status = project.Status?.ToLowerInvariant() ?? "active";
        StatusComboBox.SelectedIndex = status switch
        {
            "paused" => 1,
            "completed" => 2,
            _ => 0
        };
        
        // Set due date
        if (project.DueDate.HasValue)
            DueDatePicker.SelectedDate = project.DueDate.Value;
    }
    
    /// <summary>
    /// Returns true if the user has entered any data that would be lost on cancel.
    /// </summary>
    private bool HasUnsavedChanges
    {
        get
        {
            // For editing, less critical since data exists
            if (_isEditMode) return false;
            
            return !string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                   !string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ||
                   DueDatePicker.SelectedDate.HasValue;
        }
    }

    private async void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        // Show confirmation if there's unsaved data during creation
        if (HasUnsavedChanges)
        {
            var confirmed = await ConfirmationService.Instance.ShowConfirmationAsync(
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
        _forceClose = true;
        Close();
    }
    
    /// <summary>
    /// Handle window closing to show confirmation if there are unsaved changes.
    /// </summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_forceClose)
        {
            base.OnClosing(e);
            return;
        }
        
        if (HasUnsavedChanges)
        {
            e.Cancel = true;
            
            var confirmed = await ConfirmationService.Instance.ShowConfirmationAsync(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to close without saving?",
                "Discard",
                "Keep Editing");
            
            if (confirmed)
            {
                Result = null;
                _forceClose = true;
                Close();
            }
        }
        else
        {
            base.OnClosing(e);
        }
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Validate
        var name = NameTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorText.Text = "Project name is required.";
            ErrorText.IsVisible = true;
            return;
        }

        // Get status from combo box
        var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;
        var status = statusItem?.Tag?.ToString() ?? "active";

        // Build result
        var project = _editingProject ?? new Project();
        project.Name = name;
        project.Description = DescriptionTextBox.Text?.Trim();
        project.Status = status;
        // Convert DateTimeOffset to DateTime
        project.DueDate = DueDatePicker.SelectedDate?.Date;

        Result = project;
        _forceClose = true;
        Close();
    }
}
