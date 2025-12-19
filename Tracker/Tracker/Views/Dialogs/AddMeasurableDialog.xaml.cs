using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddMeasurableDialog.xaml
    /// </summary>
    [HelpContext("dialogs/add-measurable")]
    public partial class AddMeasurableDialog
    {
        public AddMeasurableDialog(MeasurableViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}

