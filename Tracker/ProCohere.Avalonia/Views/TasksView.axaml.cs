using Avalonia.Controls;
using ProCohere.Avalonia.Attributes;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Standalone Tasks browse/manage page.
/// This is the authoritative destination for browsing and managing tasks.
/// Quick Access from Pulse navigates here (not to Me page).
/// Data is loaded upfront at app startup - no visibility-based loading needed.
/// DataContext binding is handled in XAML - TasksContent inherits from parent.
/// </summary>
[HelpContext("tasks", ContextName = "TasksView")]
public partial class TasksView : UserControl
{
    public TasksView()
    {
        InitializeComponent();
    }
}
