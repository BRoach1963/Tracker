using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating a new goal.
/// </summary>
public partial class AddGoalDialog : Window
{
    public AddGoalDialog()
    {
        InitializeComponent();
        
        // Wire up CloseRequested event
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AddGoalDialogViewModel viewModel)
            {
                viewModel.CloseRequested += Close;
            }
        };
    }
}
