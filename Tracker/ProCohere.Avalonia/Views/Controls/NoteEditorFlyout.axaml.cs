using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.Views.Dialogs;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Note editor flyout for creating and editing notes.
/// </summary>
public partial class NoteEditorFlyout : UserControl
{
    public NoteEditorFlyout()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles click on the "Shared" privacy option.
    /// Sets IsPrivate to false on the editing note.
    /// </summary>
    private void OnSharedOptionPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ViewModels.ChronicleViewModel vm)
        {
            vm.SetNotePrivacy(false);
        }
    }

    /// <summary>
    /// Handles click on the "Private" privacy option.
    /// Sets IsPrivate to true on the editing note.
    /// </summary>
    private void OnPrivateOptionPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ViewModels.ChronicleViewModel vm)
        {
            vm.SetNotePrivacy(true);
        }
    }

    /// <summary>
    /// Handles click on the "Add Link" button to open the entity picker dialog.
    /// </summary>
    private async void OnAddLinkPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not ViewModels.ChronicleViewModel vm)
            return;

        // Find the parent window
        var parentWindow = this.GetVisualRoot() as Window;
        if (parentWindow == null)
            return;

        // Open entity picker dialog (for notes: goal, task, person, meeting, project)
        var dialog = new EntityPickerDialog();
        dialog.SetAllowedTypes("goal", "task", "person", "meeting", "project");
        await dialog.ShowDialog(parentWindow);

        // If user selected an entity, add it as a link to the note
        if (dialog.Result != null)
        {
            vm.AddEntityLink(
                dialog.Result.EntityType,
                dialog.Result.EntityId,
                dialog.Result.EntityTitle);
        }
    }

    /// <summary>
    /// Handles click on the linked entity badge to remove the link.
    /// </summary>
    private void OnRemoveLinkPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ViewModels.ChronicleViewModel vm)
        {
            vm.ClearAllEntityLinks();
        }
    }
}
