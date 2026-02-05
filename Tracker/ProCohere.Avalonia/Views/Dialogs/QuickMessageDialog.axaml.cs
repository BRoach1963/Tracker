using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System;

namespace ProCohere.Avalonia.Views.Dialogs;

public partial class QuickMessageDialog : Window
{
    public QuickMessageDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is QuickMessageDialogViewModel vm)
        {
            vm.CloseRequested -= OnCloseRequested;
            vm.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
