using Avalonia.Controls;
using ProCohere.Avalonia.Attributes;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Standalone Metrics browse/manage page.
/// This is the authoritative destination for browsing and managing metrics.
/// Quick Access from Pulse navigates here (not to Circle tabs).
/// Data is loaded upfront at app startup - no visibility-based loading needed.
/// DataContext binding is handled in XAML - MetricsContent inherits from parent.
/// </summary>
[HelpContext("metrics", ContextName = "MetricsView")]
public partial class MetricsView : UserControl
{
    public MetricsView()
    {
        InitializeComponent();
    }
}
