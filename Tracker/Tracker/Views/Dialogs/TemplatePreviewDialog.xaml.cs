using System.Windows;
using Tracker.Controls;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// A dialog to preview a review template before using it in a cycle.
    /// Shows the structure of sections and questions without allowing input.
    /// </summary>
    public partial class TemplatePreviewDialog : BaseWindow
    {
        public TemplatePreviewDialog()
        {
            InitializeComponent();
        }

        public TemplatePreviewDialog(TemplatePreviewViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
