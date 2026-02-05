using Avalonia;
using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System;

namespace ProCohere.Avalonia.Views.Dialogs;

public partial class CreateSurveyDialog : Window
{
    public CreateSurveyDialog()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CreateSurveyDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
            viewModel.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is CreateSurveyDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }
        base.OnClosing(e);
    }
}
