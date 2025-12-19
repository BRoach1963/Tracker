using Tracker.Common.Enums;
using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddOneOnOneDialog.xaml
    /// </summary>
    [HelpContext("dialogs/add-one-on-one")]
    public partial class AddOneOnOneDialog
    {
        public AddOneOnOneDialog(OneOnOneViewModel vm) : base(DialogType.AddOneOnOne)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
