using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for giving feedback to a team member.
/// </summary>
public partial class AddFeedbackDialog : Window
{
    public AddFeedbackDialog()
    {
        InitializeComponent();
        
        // Wire up CloseRequested event
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AddFeedbackDialogViewModel viewModel)
            {
                viewModel.CloseRequested += Close;
            }
        };
    }
}
