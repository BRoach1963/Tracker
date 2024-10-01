using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for LoginDialog.xaml
    /// </summary>
    public partial class LoginDialog 
    {
        public LoginDialog()
        {
            InitializeComponent();
        }

        private void OnDragHandleMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            DragMove();
        }
    }
}
