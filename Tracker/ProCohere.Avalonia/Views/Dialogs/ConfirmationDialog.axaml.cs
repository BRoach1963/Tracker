using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for confirming actions, especially destructive ones like deletes.
/// </summary>
public partial class ConfirmationDialog : Window
{
    /// <summary>
    /// Types of confirmation dialogs with different visual styles.
    /// </summary>
    public enum ConfirmationType
    {
        /// <summary>Standard confirmation (question icon, neutral styling)</summary>
        Default,
        /// <summary>Warning confirmation (warning icon, caution styling)</summary>
        Warning,
        /// <summary>Destructive confirmation (delete icon, danger styling)</summary>
        Destructive
    }
    
    /// <summary>
    /// Gets whether the user confirmed the action.
    /// </summary>
    public bool IsConfirmed { get; private set; }
    
    private readonly PathIcon _dialogIcon;
    private readonly TextBlock _titleText;
    private readonly TextBlock _messageText;
    private readonly Button _confirmButton;
    
    public ConfirmationDialog()
    {
        InitializeComponent();
        
        _dialogIcon = this.FindControl<PathIcon>("DialogIcon")!;
        _titleText = this.FindControl<TextBlock>("TitleText")!;
        _messageText = this.FindControl<TextBlock>("MessageText")!;
        _confirmButton = this.FindControl<Button>("ConfirmButton")!;
    }
    
    public ConfirmationDialog(
        string title, 
        string message, 
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        ConfirmationType type = ConfirmationType.Default) : this()
    {
        _titleText.Text = title;
        _messageText.Text = message;
        _confirmButton.Content = confirmText;
        
        var cancelButton = this.FindControl<Button>("CancelButton")!;
        cancelButton.Content = cancelText;
        
        ApplyConfirmationType(type);
    }
    
    private void ApplyConfirmationType(ConfirmationType type)
    {
        switch (type)
        {
            case ConfirmationType.Warning:
                _dialogIcon.Data = (StreamGeometry?)this.FindResource("WarningIcon");
                _dialogIcon.Foreground = (IBrush?)Application.Current?.FindResource("BrushWarning");
                break;
                
            case ConfirmationType.Destructive:
                _dialogIcon.Data = (StreamGeometry?)this.FindResource("DeleteIcon");
                _dialogIcon.Foreground = (IBrush?)Application.Current?.FindResource("BrushError");
                _confirmButton.Classes.Remove("primary");
                _confirmButton.Classes.Add("danger");
                break;
                
            case ConfirmationType.Default:
            default:
                // Keep default styling (question icon, warning color)
                break;
        }
    }
    
    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close();
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }
}
