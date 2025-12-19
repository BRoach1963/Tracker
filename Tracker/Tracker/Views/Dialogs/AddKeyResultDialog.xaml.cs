using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddKeyResultDialog.xaml
    /// </summary>
    [HelpContext("dialogs/add-key-result")]
    public partial class AddKeyResultDialog
    {
        public AddKeyResultDialog(KeyResultViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}

