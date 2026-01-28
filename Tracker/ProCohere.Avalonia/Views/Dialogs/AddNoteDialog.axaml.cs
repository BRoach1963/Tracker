using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Result from the add note dialog.
/// </summary>
public class AddNoteResult
{
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Simple dialog for adding a note with title and content.
/// </summary>
public partial class AddNoteDialog : Window
{
    private bool _forceClose;
    
    public AddNoteDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Returns true if the user has entered any data that would be lost on cancel.
    /// </summary>
    private bool HasUnsavedChanges =>
        !string.IsNullOrWhiteSpace(TitleTextBox.Text) ||
        !string.IsNullOrWhiteSpace(ContentTextBox.Text);
    
    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var content = ContentTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            // Could show validation error, but for now just don't save
            return;
        }
        
        var result = new AddNoteResult
        {
            Title = string.IsNullOrWhiteSpace(TitleTextBox.Text) ? null : TitleTextBox.Text.Trim(),
            Content = content
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
