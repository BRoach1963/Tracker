using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Controls.Meeting;

/// <summary>
/// Panel for displaying and managing meeting prep items.
/// Adding new items now uses dialogs (triggered via ViewModel events).
/// </summary>
public partial class PrepItemsPanel : UserControl
{
    public PrepItemsPanel()
    {
        InitializeComponent();
    }
}
