using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.DataModels;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Quick Notes control with master-detail layout.
    /// </summary>
    public partial class QuickNotesControl : UserControl
    {
        public QuickNotesControl()
        {
            InitializeComponent();
            DataContext = new QuickNotesViewModel();
        }

        private void Note_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is QuickNote note)
            {
                if (DataContext is QuickNotesViewModel vm)
                {
                    vm.SelectedNote = note;
                }
            }
        }
    }
}
