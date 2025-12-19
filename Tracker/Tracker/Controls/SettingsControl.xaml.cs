using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    [HelpContext("dialogs/settings")]
    public partial class SettingsControl
    {
        public SettingsControl()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(() => { });
        }
    }
}

