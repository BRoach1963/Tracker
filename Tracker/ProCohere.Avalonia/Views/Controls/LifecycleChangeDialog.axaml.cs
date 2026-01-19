using Avalonia.Controls;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Dialog for changing goal lifecycle with reflection prompt.
/// Shows SupersededBy picker when Superseded lifecycle is selected.
/// </summary>
public partial class LifecycleChangeDialog : UserControl
{
    public LifecycleChangeDialog()
    {
        InitializeComponent();
    }
}
