using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tracker.Controls
{
    /// <summary>
    /// Interaction logic for DialogTitleBar.xaml
    /// </summary>
    public partial class DialogTitleBar : UserControl
    {
        public DialogTitleBar()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(IconData), typeof(Geometry), typeof(DialogTitleBar), new PropertyMetadata(Geometry.Parse("M22,5.5A3.5,3.5 0 0,0 18.5,2A3.5,3.5 0 0,0 15,5.5V6A3,3 0 0,1 12,9C10,9 9,6 6,6A4,4 0 0,0 2,10V11H4V10A2,2 0 0,1 6,8C6.86,8 7.42,8.45 8.32,9.24C9.28,10.27 10.6,10.9 12,11A5,5 0 0,0 17,6V5.5A1.5,1.5 0 0,1 18.5,4A1.5,1.5 0 0,1 20,5.5C19.86,6.35 19.58,7.18 19.17,7.94C18.5,9.2 18.11,10.58 18,12C18.09,13.37 18.5,14.71 19.21,15.89C19.6,16.54 19.87,17.25 20,18A2,2 0 0,1 18,20A2,2 0 0,1 16,18A3.75,3.75 0 0,0 12.25,14.25A3.75,3.75 0 0,0 8.5,18V18.5A1.5,1.5 0 0,1 7,20A3,3 0 0,1 4,17V15H6V13H0V15H2V17A5,5 0 0,0 7,22A3.5,3.5 0 0,0 10.5,18.5V18A1.75,1.75 0 0,1 12.25,16.25A1.75,1.75 0 0,1 14,18A4,4 0 0,0 18,22A4,4 0 0,0 22,18C22,16 20,14 20,12C20,10 22,7.5 22,5.5Z")));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(DialogTitle),
                typeof(string),
                typeof(DialogTitleBar),
                new PropertyMetadata(Tracker.Resources.Languages.Resources.Main_Title));

        public string DialogTitle
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public Geometry IconData
        {
            get { return (Geometry)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        #endregion

        #region RoutedEvents

        public static readonly RoutedEvent CloseClickedEvent = EventManager.RegisterRoutedEvent(
            "CloseClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DialogTitleBar));

        public static readonly RoutedEvent MinimizeClickedEvent = EventManager.RegisterRoutedEvent(
            "MinimizeClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DialogTitleBar));

        public static readonly RoutedEvent MaximizeClickedEvent = EventManager.RegisterRoutedEvent(
            "MaximizeClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DialogTitleBar));

        public static readonly RoutedEvent RestoreClickedEvent = EventManager.RegisterRoutedEvent(
            "RestoreClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DialogTitleBar));

        public event RoutedEventHandler CloseClicked
        {
            add => AddHandler(CloseClickedEvent, value);
            remove => RemoveHandler(CloseClickedEvent, value);
        }

        public event RoutedEventHandler MinimizeClicked
        {
            add => AddHandler(MinimizeClickedEvent, value);
            remove => RemoveHandler(MinimizeClickedEvent, value);
        }

        public event RoutedEventHandler MaximizeClicked
        {
            add => AddHandler(MaximizeClickedEvent, value);
            remove => RemoveHandler(MaximizeClickedEvent, value);
        }

        public event RoutedEventHandler RestoreClicked
        {
            add => AddHandler(RestoreClickedEvent, value);
            remove => RemoveHandler(RestoreClickedEvent, value);
        }

        #endregion

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseClickedEvent));
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(MinimizeClickedEvent));
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(MaximizeClickedEvent));
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(RestoreClickedEvent));
        }
    }
}
