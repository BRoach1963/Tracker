using System.Windows;
using System.Windows.Threading;

namespace Tracker.Views.Toasts
{
    /// <summary>
    /// Interaction logic for TrackerToast.xaml
    /// </summary>
    public partial class TrackerToast : Window
    {
        private DispatcherTimer _dismissTimer;
        private const int ToastDuration = 5;

        public TrackerToast(string title, string message)
        {
            InitializeComponent();
            ToastTitle.Text = title;
            ToastMessage.Text = message;

 
            _dismissTimer = new DispatcherTimer();
            _dismissTimer.Interval = TimeSpan.FromSeconds(ToastDuration);
            _dismissTimer.Tick += DismissTimer_Tick;
            _dismissTimer.Start();

        }

        private void ToastWindow_Loaded(object sender, RoutedEventArgs e)
        {

            PositionToast();
             
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void DismissTimer_Tick(object? sender, EventArgs e)
        {
            _dismissTimer.Stop();
            Close();
        }

        private void ToastWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
 
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(OpacityProperty, fadeOut);
 
            e.Cancel = true;
            Dispatcher.BeginInvoke(new Action(() => { this.Hide(); }), DispatcherPriority.ContextIdle, null);
        }

        private void Toast_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _dismissTimer.Stop();
            _dismissTimer.Tick -= DismissTimer_Tick; // Unsubscribe from the event to prevent memory leaks
        }

        private void PositionToast()
        {

            var workingArea = SystemParameters.WorkArea;
             
            this.Left = workingArea.Right - this.Width - 10;  
            this.Top = workingArea.Top + 10;  
            return;

        }
    }
}
