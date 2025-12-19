using System.Windows.Controls;

namespace Tracker.Controls
{
    /// <summary>
    /// Control for viewing and searching application logs.
    /// </summary>
    public partial class LogViewerControl : UserControl
    {
        public LogViewerControl()
        {
            InitializeComponent();
            DataContext = new ViewModels.LogViewerViewModel();
        }
    }
}

