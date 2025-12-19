using System.Windows;
using System.Windows.Input;
using Tracker.Controls;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ReportsDialog.xaml
    /// </summary>
    public partial class ReportsDialog : BaseWindow
    {
        public ReportsDialog(object dataContext)
        {
            DataContext = dataContext;
            InitializeComponent();
        }

        // Use base class methods: OnDragHandleMouseDown, Close_Click, 
        // Minimize_Click, Maximize_Click, Restore_Click are inherited from BaseWindow
    }
}

