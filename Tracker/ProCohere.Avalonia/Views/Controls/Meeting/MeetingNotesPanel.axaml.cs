using System;
using Avalonia.Controls;
using Avalonia.Input;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Controls.Meeting;

/// <summary>
/// Panel for meeting notes.
/// Pure UI - all state in ViewModel.
/// </summary>
public partial class MeetingNotesPanel : UserControl
{
    public MeetingNotesPanel()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Handles click on tag picker buttons.
    /// Routes to ViewModel's ToggleNoteTagCommand.
    /// </summary>
    private void TagButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.Tag is not Tuple<object?, object?> tuple) return;
        if (DataContext is not EditMeetingDialogViewModel vm) return;
        
        vm.ToggleNoteTagCommand.Execute(tuple);
    }
}
