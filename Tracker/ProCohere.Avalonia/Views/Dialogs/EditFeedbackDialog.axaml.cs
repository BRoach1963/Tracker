using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for editing existing feedback.
/// </summary>
public partial class EditFeedbackDialog : Window
{
    public EditFeedbackDialog()
    {
        InitializeComponent();
        
        // Wire up CloseRequested event
        DataContextChanged += (_, _) =>
        {
            if (DataContext is EditFeedbackDialogViewModel viewModel)
            {
                viewModel.CloseRequested += Close;
            }
        };
    }
}
