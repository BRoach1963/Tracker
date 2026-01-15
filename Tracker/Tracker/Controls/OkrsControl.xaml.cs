using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Common.Enums;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Interaction logic for OkrsControl.xaml
    /// </summary>
    public partial class OkrsControl : UserControl
    {
        public OkrsControl()
        {
            InitializeComponent();
        }

        private GoalsViewModel? ViewModel => DataContext as GoalsViewModel;

        #region Status Card Click Handlers

        private void StatCard_OnTrack_Click(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.SetStatusFilter(GoalStatus.OnTrack);
        }

        private void StatCard_AtRisk_Click(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.SetStatusFilter(GoalStatus.AtRisk);
        }

        private void StatCard_OffTrack_Click(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.SetStatusFilter(GoalStatus.OffTrack);
        }

        private void StatCard_All_Click(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.SetStatusFilter(null);
        }

        #endregion

        #region Filter Button Click Handlers

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.SetStatusFilter(null);
        }

        private void FilterOnTrack_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.SetStatusFilter(GoalStatus.OnTrack);
        }

        private void FilterAtRisk_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.SetStatusFilter(GoalStatus.AtRisk);
        }

        private void FilterOffTrack_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.SetStatusFilter(GoalStatus.OffTrack);
        }

        #endregion
    }
}
