using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    public partial class TeamMembersControl : UserControl
    {
        public TeamMembersControl()
        {
            InitializeComponent();
        }

        #region Stat Card Click Handlers

        private void StatCard_Active_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleFilter(TeamMemberFilterEnum.Active);
        }

        private void StatCard_Inactive_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleFilter(TeamMemberFilterEnum.Inactive);
        }

        private void StatCard_OnTrack_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleFilter(TeamMemberFilterEnum.OneOnOneOnTrack);
        }

        private void StatCard_Overdue_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleFilter(TeamMemberFilterEnum.OneOnOneOverdue);
        }

        private void StatCard_OpenTasks_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleFilter(TeamMemberFilterEnum.HasOpenTasks);
        }

        /// <summary>
        /// Toggle a filter - clicking the same filter again clears it.
        /// </summary>
        private void ToggleFilter(TeamMemberFilterEnum filter)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.MemberFilter = vm.MemberFilter == filter 
                    ? TeamMemberFilterEnum.All 
                    : filter;
            }
        }

        #endregion

        #region Filter Button Click Handlers

        private void FilterButton_All_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.MemberFilter = TeamMemberFilterEnum.All;
            }
        }

        private void FilterButton_Active_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.MemberFilter = TeamMemberFilterEnum.Active;
            }
        }

        private void FilterButton_NeedsAttention_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.MemberFilter = TeamMemberFilterEnum.NeedsAttention;
            }
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.MemberFilter = TeamMemberFilterEnum.All;
            }
        }

        #endregion

        #region Member Card Handlers

        /// <summary>
        /// Handle single click on a member card to select that member.
        /// Double-click is handled via InputBinding in XAML.
        /// </summary>
        private void MemberCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is TeamMember member)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedTeamMember = member;
                }
            }
        }

        /// <summary>
        /// Handle click on a member in the quick-select list (needs attention).
        /// </summary>
        private void MemberQuickSelect_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is TeamMember member)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedTeamMember = member;
                }
            }
        }

        /// <summary>
        /// Handle double-click to open the member for editing.
        /// </summary>
        private void TeamMembersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm && vm.SelectedTeamMemberWrapper != null)
            {
                vm.TeamMemberEditCommand?.Execute(vm.SelectedTeamMemberWrapper);
            }
        }

        /// <summary>
        /// Open email client for the selected member.
        /// </summary>
        private void EmailMember_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is TeamMember member)
            {
                if (!string.IsNullOrWhiteSpace(member.Email))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = $"mailto:{member.Email}",
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // Silently fail if no email client is configured
                    }
                }
            }
        }

        /// <summary>
        /// Open a social media link in the browser.
        /// </summary>
        private void OpenSocialLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string url && !string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Silently fail if URL cannot be opened
                }
            }
        }

        #endregion
    }
}
