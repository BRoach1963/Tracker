using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.DataModels;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    public partial class OneOnOnesControl : UserControl
    {
        public OneOnOnesControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle click on a team member to select them and show their meetings.
        /// </summary>
        private void TeamMember_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is TeamMember member)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedMemberForMeetings = member;
                    vm.SelectedOneOnOne = null; // Clear meeting selection when changing member
                    
                    // Default to upcoming filter
                    ShowUpcoming.IsChecked = true;
                }
            }
        }

        /// <summary>
        /// Handle click on a meeting to select it and show details.
        /// </summary>
        private void Meeting_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Meeting meeting)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedOneOnOne = meeting;
                }
            }
        }

        /// <summary>
        /// Handle meeting filter changes (Upcoming/Past/All).
        /// </summary>
        private void MeetingFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                if (ShowUpcoming.IsChecked == true)
                    vm.MeetingTimeFilter = MeetingTimeFilterEnum.Upcoming;
                else if (ShowPast.IsChecked == true)
                    vm.MeetingTimeFilter = MeetingTimeFilterEnum.Past;
                else
                    vm.MeetingTimeFilter = MeetingTimeFilterEnum.All;
            }
        }

        /// <summary>
        /// Handle double-click on grid to open meeting for editing.
        /// </summary>
        private void MeetingItem_Click(object sender, MouseButtonEventArgs e)
        {
            // Same as Meeting_Click for single click selection
            Meeting_Click(sender, e);
        }
    }

    /// <summary>
    /// Filter for meeting time range.
    /// </summary>
    public enum MeetingTimeFilterEnum
    {
        Upcoming,
        Past,
        All
    }
}
