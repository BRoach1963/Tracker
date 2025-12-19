using System.Windows;
using System.Windows.Controls;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// A reusable calendar button control that displays a popup calendar when clicked.
    /// This control avoids the NullReferenceException that occurs with collapsed DatePickers.
    /// </summary>
    public partial class CalendarButton : UserControl
    {
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(CalendarButton),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        public CalendarButton()
        {
            InitializeComponent();
            Loaded += CalendarButton_Loaded;
            
            // Set the popup placement target to the button
            CalendarPopup.PlacementTarget = CalendarIconButton;
        }

        private void CalendarButton_Loaded(object sender, RoutedEventArgs e)
        {
            // Sync the calendar's selected date with the dependency property
            DateCalendar.SelectedDate = SelectedDate;
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CalendarButton)d;
            if (control.DateCalendar != null)
            {
                control.DateCalendar.SelectedDate = e.NewValue as DateTime?;
            }
        }

        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
        }

        private void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DateCalendar.SelectedDate.HasValue)
            {
                SelectedDate = DateCalendar.SelectedDate.Value;
                CalendarPopup.IsOpen = false;
            }
        }
    }
}
