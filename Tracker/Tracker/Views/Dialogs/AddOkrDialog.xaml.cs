using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddOkrDialog.xaml
    /// </summary>
    [HelpContext("dialogs/add-okr")]
    public partial class AddOkrDialog 
    {
        public AddOkrDialog(NewOkrViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
