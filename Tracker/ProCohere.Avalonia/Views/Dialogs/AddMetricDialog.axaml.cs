using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating a new metric.
/// </summary>
public partial class AddMetricDialog : Window
{
    public AddMetricDialog()
    {
        InitializeComponent();
        
        // Wire up CloseRequested event
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AddMetricDialogViewModel viewModel)
            {
                viewModel.CloseRequested += Close;
            }
        };
    }
}
