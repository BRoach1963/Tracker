using System.Windows;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for filling out a performance review.
    /// </summary>
    public partial class PerformanceReviewDialog : Window
    {
        private PerformanceReviewDialogViewModel? _viewModel;

        public PerformanceReviewDialog()
        {
            InitializeComponent();
            Closed += OnClosed;
        }

        public PerformanceReviewDialog(PerformanceReviewDialogViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            viewModel.RequestClose += (s, result) =>
            {
                DialogResult = result;
                Close();
            };
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            // Dispose the ViewModel to stop the auto-save timer
            _viewModel?.Dispose();
        }
    }
}
