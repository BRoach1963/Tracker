using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.ViewModels;

namespace Tracker.Controls
{
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
                if (vm.KpiStatusFilter == KpiStatusEnum.OnTarget)
                    vm.KpiStatusFilter = null;
                else
                    vm.KpiStatusFilter = KpiStatusEnum.OnTarget;
            }
        }

        private void StatCard_BelowTarget_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                // Toggle: if already selected, go back to All  
                // Use OffTarget to represent "Below Target" filter
                if (vm.KpiStatusFilter == KpiStatusEnum.OffTarget)
                    vm.KpiStatusFilter = null;
                else
                    vm.KpiStatusFilter = KpiStatusEnum.OffTarget;
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
                vm.KpiStatusFilter = KpiStatusEnum.OnTarget;
            }
        }

        private void FilterBelowTarget_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                // Use OffTarget to represent "Below Target" (not on target)
                vm.KpiStatusFilter = KpiStatusEnum.OffTarget;
            }
        }

        #endregion

        #region KPI Selection

        private void Kpi_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is KeyPerformanceIndicator kpi)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedKpi = kpi;
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
