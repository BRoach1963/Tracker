using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.DataModels;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    public partial class FeedbackHistoryControl : UserControl
    {
        public FeedbackHistoryControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle click on a feedback item to select it.
        /// </summary>
        private void FeedbackItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Feedback feedback)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedFeedback = feedback;
                }
            }
        }
    }
}
