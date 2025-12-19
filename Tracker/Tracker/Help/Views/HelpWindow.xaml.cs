using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Help.Models;
using Tracker.Help.ViewModels;

namespace Tracker.Help.Views
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        private readonly HelpViewModel _viewModel;
        private static HelpWindow? _currentInstance;

        public HelpWindow()
        {
            InitializeComponent();
            _viewModel = new HelpViewModel();
            DataContext = _viewModel;

            // Track this instance
            _currentInstance = this;
            Closed += (s, e) => { if (_currentInstance == this) _currentInstance = null; };

            // Keyboard shortcuts
            PreviewKeyDown += HelpWindow_PreviewKeyDown;

            // Load default topic
            Loaded += async (s, e) =>
            {
                await _viewModel.NavigateToTopicAsync(Services.HelpService.Instance.GetTableOfContents()?.Children?.FirstOrDefault()?.TopicId 
                    ?? "getting-started/overview");
            };
        }

        /// <summary>
        /// Gets the current help window instance if open.
        /// </summary>
        public static HelpWindow? CurrentInstance => _currentInstance;

        /// <summary>
        /// Navigates the current window to a topic, or creates a new window.
        /// </summary>
        public static HelpWindow ShowForContext(HelpContext context)
        {
            if (_currentInstance != null)
            {
                // Navigate existing window
                _currentInstance.Activate();
                _currentInstance.Focus();
                _ = _currentInstance._viewModel.ShowContextHelpAsync(context);
                return _currentInstance;
            }

            // Create new window
            var window = new HelpWindow();
            window.Show();
            _ = window._viewModel.ShowContextHelpAsync(context);
            return window;
        }

        /// <summary>
        /// Navigates to a specific topic.
        /// </summary>
        public void NavigateToTopic(string topicId, string? section = null)
        {
            _ = _viewModel.NavigateToTopicAsync(topicId, section);
        }

        #region Window Chrome Events

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Maximize_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Navigation Events

        private void TocTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is HelpTocEntry entry && entry.TopicId != null)
            {
                _ = _viewModel.NavigateToTopicAsync(entry.TopicId);
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.SearchCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                _viewModel.ClearSearchCommand.Execute(null);
            }
        }

        private void SearchResult_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is HelpSearchResult result)
            {
                _viewModel.NavigateToResultCommand.Execute(result);
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private void HelpWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle Ctrl+key combinations
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.Add:
                    case Key.OemPlus:
                        _viewModel.ZoomInCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Subtract:
                    case Key.OemMinus:
                        _viewModel.ZoomOutCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.D0:
                    case Key.NumPad0:
                        _viewModel.ZoomResetCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.F:
                        // Focus search box
                        SearchBox.Focus();
                        SearchBox.SelectAll();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.Escape && SearchBox.IsFocused)
            {
                _viewModel.ClearSearchCommand.Execute(null);
                e.Handled = true;
            }
        }

        #endregion
    }
}

