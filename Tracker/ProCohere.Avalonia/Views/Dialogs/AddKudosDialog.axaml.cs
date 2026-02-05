using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for giving kudos recognition to a team member.
/// </summary>
public partial class AddKudosDialog : Window
{
    public AddKudosDialog()
    {
        InitializeComponent();
        
        // Wire up CloseRequested event
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AddKudosDialogViewModel viewModel)
            {
                viewModel.CloseRequested += Close;
            }
        };
    }
}
