using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Help.Attributes;
using Tracker.Services;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Global search control for finding anything in the app.
    /// </summary>
    [HelpContext("features/search")]
    public partial class SearchControl : UserControl
    {
        public SearchControl()
        {
            InitializeComponent();
            DataContext = new SearchViewModel();
        }

        /// <summary>
        /// Event fired when a search result is selected.
        /// </summary>
        public event EventHandler<SearchResult>? ResultSelected;

        private void Result_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SearchResult result)
            {
                if (DataContext is SearchViewModel vm)
                {
                    vm.SelectedResult = result;
                }
                
                ResultSelected?.Invoke(this, result);
            }
        }
    }
}

