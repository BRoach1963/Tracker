using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Interaction logic for ReportsControl.xaml
    /// </summary>
    [HelpContext("features/reports")]
    public partial class ReportsControl
    {
        public ReportsControl()
        {
            InitializeComponent();
            DataContext = new ReportsViewModel(() => { });
        }
    }
}

