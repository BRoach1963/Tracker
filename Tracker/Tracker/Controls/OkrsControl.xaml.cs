using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Common.Enums;
using Tracker.Controls.CustomControls;
using Tracker.DataModels;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// OKRs control with 3-panel layout:
    /// - Left panel: OKR cards with search/filter
    /// - Top-right panel: Key Results for selected OKR  
    /// - Bottom-right panel: KR details with linked measurables
    /// 
    /// Uses OkrsViewModel for data and commands.
    /// </summary>
    public partial class OkrsControl : UserControl
    {
        private OkrsViewModel? _viewModel;

        public OkrsControl()
        {
            InitializeComponent();
            
            // Create and set ViewModel
            _viewModel = new OkrsViewModel();
            DataContext = _viewModel;
        }

        #region Stat Card Click Handlers

        private void StatCard_OnTrack_Click(object sender, MouseButtonEventArgs e)
        {
            // Toggle: if already selected, go back to All
            if (_viewModel?.StatusFilter == ObjectiveStatusEnum.OnTrack)
                _viewModel.StatusFilter = null;
            else if (_viewModel != null)
                _viewModel.StatusFilter = ObjectiveStatusEnum.OnTrack;
        }

        private void StatCard_AtRisk_Click(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.StatusFilter == ObjectiveStatusEnum.AtRisk)
                _viewModel.StatusFilter = null;
            else if (_viewModel != null)
                _viewModel.StatusFilter = ObjectiveStatusEnum.AtRisk;
        }

        private void StatCard_OffTrack_Click(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.StatusFilter == ObjectiveStatusEnum.OffTrack)
                _viewModel.StatusFilter = null;
            else if (_viewModel != null)
                _viewModel.StatusFilter = ObjectiveStatusEnum.OffTrack;
        }

        private void StatCard_All_Click(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.StatusFilter = null;
        }

        #endregion

        #region Filter Button Event Handlers

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null) _viewModel.StatusFilter = null;
        }

        private void FilterOnTrack_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null) _viewModel.StatusFilter = ObjectiveStatusEnum.OnTrack;
        }

        private void FilterAtRisk_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null) _viewModel.StatusFilter = ObjectiveStatusEnum.AtRisk;
        }

        private void FilterOffTrack_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null) _viewModel.StatusFilter = ObjectiveStatusEnum.OffTrack;
        }

        #endregion

        #region OKR Card Event Handlers

        private void OkrCard_CardClicked(object sender, RoutedEventArgs e)
        {
            if (sender is OkrCard card && card.Okr != null)
            {
                _viewModel?.SelectOkr(card.Okr);
            }
        }

        private void OkrCard_EditRequested(object sender, RoutedEventArgs e)
        {
            if (sender is OkrCard card && card.Okr != null)
            {
                _viewModel?.EditOkrCommand.Execute(card.Okr);
            }
        }

        private void OkrCard_DuplicateRequested(object sender, RoutedEventArgs e)
        {
            if (sender is OkrCard card && card.Okr != null)
            {
                _viewModel?.DuplicateOkrCommand.Execute(card.Okr);
            }
        }

        private void OkrCard_DeleteRequested(object sender, RoutedEventArgs e)
        {
            if (sender is OkrCard card && card.Okr != null)
            {
                _viewModel?.DeleteOkrCommand.Execute(card.Okr);
            }
        }

        #endregion

        #region Key Result Item Event Handlers

        private void KeyResultItem_ItemClicked(object sender, RoutedEventArgs e)
        {
            if (sender is KeyResultItem item && item.KeyResult != null)
            {
                _viewModel?.SelectKeyResult(item.KeyResult);
            }
        }

        private void KeyResultItem_EditRequested(object sender, RoutedEventArgs e)
        {
            if (sender is KeyResultItem item && item.KeyResult != null)
            {
                _viewModel?.EditKeyResultCommand.Execute(item.KeyResult);
            }
        }

        private void KeyResultItem_DuplicateRequested(object sender, RoutedEventArgs e)
        {
            if (sender is KeyResultItem item && item.KeyResult != null)
            {
                _viewModel?.DuplicateKeyResultCommand.Execute(item.KeyResult);
            }
        }

        private void KeyResultItem_DeleteRequested(object sender, RoutedEventArgs e)
        {
            if (sender is KeyResultItem item && item.KeyResult != null)
            {
                _viewModel?.DeleteKeyResultCommand.Execute(item.KeyResult);
            }
        }

        #endregion

        #region Measurable Event Handlers

        private void MeasurableItem_RemoveClicked(object sender, RoutedEventArgs e)
        {
            if (sender is MeasurableItem item && item.DataContext is KeyResultMeasurable measurable)
            {
                _viewModel?.RemoveMeasurableCommand.Execute(measurable);
            }
        }

        #endregion
    }
}
