using Avalonia.Controls;
using Avalonia.Interactivity;

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
    public AddNoteDialog()
    {
        InitializeComponent();
    }
    
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
        
        Close(result);
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
