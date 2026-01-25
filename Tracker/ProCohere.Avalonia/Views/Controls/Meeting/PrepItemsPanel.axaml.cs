using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Controls.Meeting;

/// <summary>
/// Panel for displaying and managing meeting prep items.
/// Minimal code-behind - just ComboBox selection that can't be bound directly.
/// </summary>
public partial class PrepItemsPanel : UserControl
{
    public PrepItemsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles visibility scope selection change.
    /// </summary>
    private void PrepVisibilityComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not EditMeetingDialogViewModel vm) return;
        if (PrepVisibilityComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            vm.NewPrepVisibility = tag;
        }
    }
}
