using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Help.Attributes;
using Tracker.Helpers;
using Tracker.Services;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for creating a new reminder.
    /// </summary>
    [HelpContext("dialogs/add-reminder")]
    public partial class AddReminderDialog : Window
    {
        #region Properties

        public string ReminderTitle { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ReminderDate { get; set; } = DateTime.Today;
        public int SelectedHour { get; set; } = 9;
        public int SelectedMinute { get; set; } = 0;
        public string SelectedAmPm { get; set; } = "AM";
        public bool IsRecurring { get; set; }
        public int RecurrenceIntervalDays { get; set; } = 7;

        public DateTime ReminderDateTime
        {
            get
            {
                var hour = SelectedHour;
                if (SelectedAmPm == "PM" && hour != 12)
                    hour += 12;
                else if (SelectedAmPm == "AM" && hour == 12)
                    hour = 0;

                return ReminderDate.Date.AddHours(hour).AddMinutes(SelectedMinute);
            }
        }

        #endregion

        #region Constructor

        public AddReminderDialog()
        {
            InitializeComponent();
            DataContext = this;

            // Set defaults
            ReminderDate = DateTime.Today;
            var now = DateTime.Now;
            SelectedHour = now.Hour > 12 ? now.Hour - 12 : (now.Hour == 0 ? 12 : now.Hour);
            SelectedAmPm = now.Hour >= 12 ? "PM" : "AM";
            SelectedMinute = (now.Minute / 15) * 15; // Round to nearest 15
        }

        #endregion

        #region Event Handlers

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBoxHelper.Show("Please enter a title.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (ReminderDateTime <= DateTime.Now)
            {
                MessageBoxHelper.Show("Please select a future date and time.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create the reminder
            var id = await ReminderService.Instance.CreateCustomReminderAsync(
                TitleTextBox.Text,
                MessageTextBox.Text ?? string.Empty,
                ReminderDateTime,
                null,
                IsRecurring,
                IsRecurring ? $"FREQ=DAILY;INTERVAL={RecurrenceIntervalDays}" : null
            );

            if (id != Guid.Empty)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBoxHelper.Show("Failed to create reminder. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuickSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                DateTime targetDate;

                switch (tag)
                {
                    case "15":
                        targetDate = DateTime.Now.AddMinutes(15);
                        break;
                    case "60":
                        targetDate = DateTime.Now.AddHours(1);
                        break;
                    case "tomorrow9":
                        targetDate = DateTime.Today.AddDays(1).AddHours(9);
                        break;
                    case "monday":
                        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7;
                        if (daysUntilMonday == 0) daysUntilMonday = 7; // Next Monday, not today
                        targetDate = DateTime.Today.AddDays(daysUntilMonday).AddHours(9);
                        break;
                    default:
                        return;
                }

                ReminderDate = targetDate.Date;
                SelectedHour = targetDate.Hour > 12 ? targetDate.Hour - 12 : (targetDate.Hour == 0 ? 12 : targetDate.Hour);
                SelectedAmPm = targetDate.Hour >= 12 ? "PM" : "AM";
                SelectedMinute = targetDate.Minute;

                // Update UI
                DatePicker.SelectedDate = ReminderDate;
                HourComboBox.SelectedItem = SelectedHour;
                MinuteComboBox.SelectedItem = SelectedMinute;
                
                // Update AM/PM combobox
                foreach (ComboBoxItem item in AmPmComboBox.Items)
                {
                    if (item.Content?.ToString() == SelectedAmPm)
                    {
                        AmPmComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        #endregion
    }
}

