using Avalonia.Controls;
using ProCohere.Avalonia.Attributes;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Standalone Goals browse/manage page.
/// This is the authoritative destination for browsing and managing goals.
/// Quick Access from Pulse navigates here (not to Circle tabs).
/// Data is loaded upfront at app startup - no visibility-based loading needed.
/// DataContext binding is handled in XAML - GoalsContent inherits from parent.
/// </summary>
[HelpContext("goals", ContextName = "GoalsView")]
public partial class GoalsView : UserControl
{
    public GoalsView()
    {
        InitializeComponent();
    }
}
