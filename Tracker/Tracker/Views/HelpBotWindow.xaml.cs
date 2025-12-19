using System.Windows;
using System.Windows.Input;

namespace Tracker.Views
{
    /// <summary>
    /// Window for the Help Bot chat interface.
    /// </summary>
    public partial class HelpBotWindow : Window
    {
        private static HelpBotWindow? _instance;
        private static readonly object _lock = new();

        public HelpBotWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows the Help Bot window (singleton pattern).
        /// </summary>
        public static void ShowHelpBot()
        {
            lock (_lock)
            {
                if (_instance == null || !_instance.IsLoaded)
                {
                    _instance = new HelpBotWindow();
                    _instance.Closed += (s, e) => { lock (_lock) { _instance = null; } };
                }

                if (_instance.WindowState == WindowState.Minimized)
                {
                    _instance.WindowState = WindowState.Normal;
                }

                _instance.Show();
                _instance.Activate();
            }
        }

        /// <summary>
        /// Toggles the Help Bot window visibility.
        /// </summary>
        public static void ToggleHelpBot()
        {
            lock (_lock)
            {
                if (_instance != null && _instance.IsVisible)
                {
                    _instance.Hide();
                }
                else
                {
                    ShowHelpBot();
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to toggle maximize (optional)
                return;
            }
            DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Hide instead of close to preserve state
            e.Cancel = true;
            Hide();
        }
    }
}

