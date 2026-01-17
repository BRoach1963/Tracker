using Avalonia.Controls;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// UserControl for displaying meeting details in a flyout panel with vertical tabs.
/// Shows Overview, Agenda, Attendees, and Notes tabs.
/// </summary>
public partial class MeetingDetailFlyout : UserControl
{
    public MeetingDetailFlyout()
    {
        InitializeComponent();
    }
}
