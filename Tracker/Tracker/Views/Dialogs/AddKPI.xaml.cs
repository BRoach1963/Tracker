using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddKPI.xaml
    /// Dialog for creating/editing Metrics.
    /// </summary>
    [HelpContext("dialogs/add-metric")]
    public partial class AddKPI 
    {
        public AddKPI(NewMetricViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
