using System.Windows.Controls;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Performance Reviews control for managing review templates and cycles.
    /// </summary>
    public partial class PerformanceReviewsControl : UserControl
    {
        public PerformanceReviewsControl()
        {
            InitializeComponent();
            DataContext = new PerformanceReviewsViewModel();
        }
    }
}
