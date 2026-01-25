using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Controls.Meeting;

/// <summary>
/// Panel for displaying and managing meeting agenda items.
/// Minimal code-behind - just ComboBox selection that can't be bound directly.
/// </summary>
public partial class AgendaItemsPanel : UserControl
{
    public AgendaItemsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles visibility scope selection change.
    /// </summary>
    private void AgendaVisibilityComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not EditMeetingDialogViewModel vm) return;
        if (AgendaVisibilityComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            vm.NewAgendaVisibility = tag;
        }
    }
}
