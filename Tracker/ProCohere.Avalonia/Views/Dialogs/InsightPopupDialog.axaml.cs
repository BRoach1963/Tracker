using Avalonia.Controls;
using Avalonia.Input;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Code-behind for the Insight Popup Dialog.
/// Minimal - all logic in ViewModel.
/// </summary>
public partial class InsightPopupDialog : Window
{
    public InsightPopupDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Handle drag on header to allow moving the dialog.
    /// </summary>
    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
