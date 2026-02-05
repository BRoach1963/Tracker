using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for simulating what-if scenarios on goal trajectories.
/// </summary>
public partial class WhatIfDialog : Window
{
    /// <summary>
    /// Converts IsPositiveChange boolean to appropriate color.
    /// Green for positive changes, red for negative.
    /// </summary>
    public static readonly FuncValueConverter<bool, IBrush> PositiveChangeColorConverter =
        new(isPositive => isPositive
            ? new SolidColorBrush(Color.Parse("#22C55E"))  // Green
            : new SolidColorBrush(Color.Parse("#EF4444"))); // Red

    public WhatIfDialog()
    {
        InitializeComponent();
        
        // Wire up CloseRequested event
        DataContextChanged += (_, _) =>
        {
            if (DataContext is WhatIfDialogViewModel viewModel)
            {
                viewModel.CloseRequested += Close;
            }
        };
    }
}
