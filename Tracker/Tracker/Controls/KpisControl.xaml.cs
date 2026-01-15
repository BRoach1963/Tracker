using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Control for displaying and filtering metrics (formerly KPIs).
    /// </summary>
    public partial class KpisControl : UserControl
    {
        public KpisControl()
        {
            InitializeComponent();
        }

        #region Stat Card Click Handlers

        private void StatCard_OnTarget_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                // Toggle: if already selected, go back to All
                if (vm.KpiStatusFilter == GoalStatus.OnTrack)
                    vm.KpiStatusFilter = null;
                else
                    vm.KpiStatusFilter = GoalStatus.OnTrack;
            }
        }

        private void StatCard_BelowTarget_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                // Toggle: if already selected, go back to All  
                // Use OffTrack to represent "Below Target" filter
                if (vm.KpiStatusFilter == GoalStatus.OffTrack)
                    vm.KpiStatusFilter = null;
                else
                    vm.KpiStatusFilter = GoalStatus.OffTrack;
            }
        }

        private void StatCard_All_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.KpiStatusFilter = null;
            }
        }

        #endregion

        #region Filter Button Click Handlers

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.KpiStatusFilter = null;
            }
        }

        private void FilterOnTarget_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.KpiStatusFilter = GoalStatus.OnTrack;
            }
        }

        private void FilterBelowTarget_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                // Use OffTrack to represent "Below Target" (not on target)
                vm.KpiStatusFilter = GoalStatus.OffTrack;
            }
        }

        #endregion

        #region KPI Selection

        private void Kpi_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Metric metric)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedKpi = metric;
                }
            }
        }

        private void KpisGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm && vm.SelectedKpi != null)
            {
                vm.EditKpiCommand?.Execute(vm.SelectedKpi);
            }
        }

        #endregion
    }
}
