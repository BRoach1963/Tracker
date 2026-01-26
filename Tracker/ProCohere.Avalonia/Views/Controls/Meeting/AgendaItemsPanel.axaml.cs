using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Controls.Meeting;

/// <summary>
/// Panel for displaying and managing meeting agenda items.
/// Adding new items now uses dialogs (triggered via ViewModel events).
/// </summary>
public partial class AgendaItemsPanel : UserControl
{
    public AgendaItemsPanel()
    {
        InitializeComponent();
    }
}
