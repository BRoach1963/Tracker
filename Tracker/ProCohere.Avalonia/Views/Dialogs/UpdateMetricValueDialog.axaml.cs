using Avalonia.Controls;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for updating a metric's value.
/// </summary>
public partial class UpdateMetricValueDialog : Window
{
    private readonly UpdateMetricValueDialogViewModel _viewModel;

    /// <summary>
    /// Result of the dialog - the new value data if updated, null if cancelled.
    /// </summary>
    public UpdateMetricValueResult? Result { get; private set; }

    public UpdateMetricValueDialog()
    {
        InitializeComponent();
        
        _viewModel = new UpdateMetricValueDialogViewModel();
        DataContext = _viewModel;
        
        _viewModel.CloseRequested += OnCloseRequested;
        
        // Focus the value field
        NewValueTextBox.AttachedToVisualTree += (s, e) => NewValueTextBox.Focus();
    }

    /// <summary>
    /// Initializes the dialog with current metric info.
    /// </summary>
    public void Initialize(string? currentValue, bool isManualMetric)
    {
        _viewModel.Initialize(currentValue, isManualMetric);
    }

    private void OnCloseRequested(object? sender, UpdateMetricValueResult? result)
    {
        Result = result;
        Close();
    }
}
