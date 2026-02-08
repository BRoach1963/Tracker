using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Dialogs;

/// <summary>
/// Dialog for creating and editing development plans.
/// </summary>
public partial class EditDevelopmentPlanDialog : Window
{
    public EditDevelopmentPlanDialog()
    {
        InitializeComponent();
    }
    
    public EditDevelopmentPlanDialog(EditDevelopmentPlanDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
    }
    
    private void OnCloseRequested(Models.DevelopmentPlan? result)
    {
        if (DataContext is EditDevelopmentPlanDialogViewModel vm)
        {
            vm.CloseRequested -= OnCloseRequested;
        }
        
        Close(result);
    }
}
