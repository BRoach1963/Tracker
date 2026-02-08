using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Result from the add agenda item dialog.
/// </summary>
public class AddAgendaItemResult
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VisibilityScope { get; set; } = "meeting";
    public bool IsPrivate => VisibilityScope == "personal";
}

/// <summary>
/// Simple dialog for adding an agenda item to a meeting.
/// </summary>
public partial class AddAgendaItemDialog : Window
{
    private bool _forceClose;
    
    public AddAgendaItemDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Returns true if the user has entered any data that would be lost on cancel.
    /// </summary>
    private bool HasUnsavedChanges =>
        !string.IsNullOrWhiteSpace(TitleTextBox.Text) ||
        !string.IsNullOrWhiteSpace(DescriptionTextBox.Text);
    
    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            // Title is required
            NotificationService.Instance.ShowWarning("Title Required", "Please enter a title for the agenda item.");
            TitleTextBox.Focus();
            return;
        }
        
        var visibilityScope = "meeting";
        if (VisibilityComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string tag)
        {
            visibilityScope = tag;
        }
        
        var result = new AddAgendaItemResult
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
            VisibilityScope = visibilityScope
        };
        
        _forceClose = true;
        Close(result);
    }
    
    private async void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        // Show confirmation if there's unsaved data
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
        
        _forceClose = true;
        Close(null);
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
        
        // Check for unsaved changes
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
                _forceClose = true;
                Close(null);
            }
        }
        else
        {
            base.OnClosing(e);
        }
    }
}
