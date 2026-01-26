using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for displaying alert messages (info, success, warning, error).
/// </summary>
public partial class AlertDialog : Window
{
    /// <summary>
    /// Types of alert dialogs with different visual styles.
    /// </summary>
    public enum AlertType
    {
        /// <summary>Information alert (info icon, blue)</summary>
        Information,
        /// <summary>Success alert (checkmark icon, green)</summary>
        Success,
        /// <summary>Warning alert (warning icon, amber)</summary>
        Warning,
        /// <summary>Error alert (error icon, red)</summary>
        Error
    }
    
    private readonly PathIcon _dialogIcon;
    private readonly TextBlock _titleText;
    private readonly TextBlock _messageText;
    
    public AlertDialog()
    {
        InitializeComponent();
        
        _dialogIcon = this.FindControl<PathIcon>("DialogIcon")!;
        _titleText = this.FindControl<TextBlock>("TitleText")!;
        _messageText = this.FindControl<TextBlock>("MessageText")!;
    }
    
    public AlertDialog(string title, string message, AlertType type = AlertType.Information) : this()
    {
        _titleText.Text = title;
        _messageText.Text = message;
        ApplyAlertType(type);
    }
    
    private void ApplyAlertType(AlertType type)
    {
        var (iconKey, brushKey) = type switch
        {
            AlertType.Success => ("SuccessIcon", "BrushSuccess"),
            AlertType.Warning => ("WarningIcon", "BrushWarning"),
            AlertType.Error => ("ErrorIcon", "BrushError"),
            _ => ("InfoIcon", "BrushInfo")
        };
        
        _dialogIcon.Data = (StreamGeometry?)this.FindResource(iconKey);
        _dialogIcon.Foreground = (IBrush?)Application.Current?.FindResource(brushKey);
    }
    
    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
