using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Command;
using Tracker.DataModels;
using Tracker.Services;

namespace Tracker.Controls
{
    /// <summary>
    /// Control that displays a scheduling assistant with free/busy information.
    /// </summary>
    public partial class SchedulingAssistantControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        public static readonly DependencyProperty TeamMemberProperty =
            DependencyProperty.Register(nameof(TeamMember), typeof(TeamMember), typeof(SchedulingAssistantControl),
                new PropertyMetadata(null, OnTeamMemberChanged));

        public static readonly DependencyProperty MeetingDurationProperty =
            DependencyProperty.Register(nameof(MeetingDuration), typeof(TimeSpan), typeof(SchedulingAssistantControl),
                new PropertyMetadata(TimeSpan.FromMinutes(30)));

        public TeamMember? TeamMember
        {
            get => (TeamMember?)GetValue(TeamMemberProperty);
            set => SetValue(TeamMemberProperty, value);
        }

        public TimeSpan MeetingDuration
        {
            get => (TimeSpan)GetValue(MeetingDurationProperty);
            set => SetValue(MeetingDurationProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when a time slot is selected.
        /// </summary>
        public event EventHandler<TimeSlotSelectedEventArgs>? TimeSlotSelected;

        #endregion

        #region Properties

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged();
                    _ = LoadSchedulingDataAsync();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string? _calendarError;
        public string? CalendarError
        {
            get => _calendarError;
            set { _calendarError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasCalendarError)); }
        }

        public bool HasCalendarError => !string.IsNullOrEmpty(CalendarError);

        public string TeamMemberName => TeamMember?.FullName ?? "Team Member";

        public ObservableCollection<string> TimeLabels { get; } = new();
        public ObservableCollection<BusyBlock> ManagerBusyBlocks { get; } = new();
        public ObservableCollection<BusyBlock> TeamMemberBusyBlocks { get; } = new();
        public ObservableCollection<TimeSlot> SuggestedSlots { get; } = new();

        public bool HasNoSuggestedSlots => SuggestedSlots.Count == 0 && !IsLoading;

        #endregion

        #region Commands

        private ICommand? _previousDayCommand;
        private ICommand? _nextDayCommand;
        private ICommand? _todayCommand;
        private ICommand? _selectSlotCommand;

        public ICommand PreviousDayCommand =>
            _previousDayCommand ??= new TrackerCommand(_ => SelectedDate = SelectedDate.AddDays(-1));

        public ICommand NextDayCommand =>
            _nextDayCommand ??= new TrackerCommand(_ => SelectedDate = SelectedDate.AddDays(1));

        public ICommand TodayCommand =>
            _todayCommand ??= new TrackerCommand(_ => SelectedDate = DateTime.Today);

        public ICommand SelectSlotCommand =>
            _selectSlotCommand ??= new TrackerCommand(OnSlotSelected);

        #endregion

        #region Constructor

        public SchedulingAssistantControl()
        {
            InitializeComponent();
            DataContext = this;

            // Initialize time labels (7 AM to 8 PM)
            for (int hour = 7; hour <= 20; hour++)
            {
                var time = DateTime.Today.AddHours(hour);
                TimeLabels.Add(time.ToString("h tt"));
            }
        }

        #endregion

        #region Private Methods

        private static void OnTeamMemberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SchedulingAssistantControl control)
            {
                control.OnPropertyChanged(nameof(TeamMemberName));
                _ = control.LoadSchedulingDataAsync();
            }
        }

        private async Task LoadSchedulingDataAsync()
        {
            if (TeamMember == null) return;

            IsLoading = true;
            CalendarError = null;
            ManagerBusyBlocks.Clear();
            TeamMemberBusyBlocks.Clear();
            SuggestedSlots.Clear();

            try
            {
                var data = await SchedulingService.Instance.GetSchedulingDataAsync(TeamMember, SelectedDate);

                // Convert busy slots to visual blocks
                foreach (var slot in data.ManagerBusySlots)
                {
                    var block = CreateBusyBlock(slot, data.StartHour, data.EndHour);
                    if (block != null)
                        ManagerBusyBlocks.Add(block);
                }

                if (data.TeamMemberCalendarAvailable)
                {
                    foreach (var slot in data.TeamMemberBusySlots)
                    {
                        var block = CreateBusyBlock(slot, data.StartHour, data.EndHour);
                        if (block != null)
                            TeamMemberBusyBlocks.Add(block);
                    }
                }
                else
                {
                    CalendarError = data.TeamMemberCalendarError ?? "Calendar not available";
                }

                // Find available slots
                var available = await SchedulingService.Instance.FindAvailableSlotsAsync(
                    TeamMember, SelectedDate, MeetingDuration, 9, 17);

                foreach (var slot in available.Take(5)) // Show top 5 suggestions
                {
                    SuggestedSlots.Add(slot);
                }

                OnPropertyChanged(nameof(HasNoSuggestedSlots));
            }
            catch (Exception ex)
            {
                CalendarError = $"Error loading calendars: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private BusyBlock? CreateBusyBlock(BusySlot slot, int startHour, int endHour)
        {
            var dayStart = SelectedDate.Date.AddHours(startHour);
            var dayEnd = SelectedDate.Date.AddHours(endHour);

            // Clip to visible range
            var blockStart = slot.Start < dayStart ? dayStart : slot.Start;
            var blockEnd = slot.End > dayEnd ? dayEnd : slot.End;

            if (blockStart >= blockEnd) return null;

            var totalMinutes = (endHour - startHour) * 60.0;
            var pixelsPerMinute = 390.0 / totalMinutes; // Approximate height for 13 hours

            var topMinutes = (blockStart - dayStart).TotalMinutes;
            var heightMinutes = (blockEnd - blockStart).TotalMinutes;

            return new BusyBlock
            {
                Top = topMinutes * pixelsPerMinute,
                Height = Math.Max(heightMinutes * pixelsPerMinute, 4),
                Tooltip = $"{slot.Start:h:mm tt} - {slot.End:h:mm tt}"
                         + (string.IsNullOrEmpty(slot.Title) ? "" : $"\n{slot.Title}")
            };
        }

        private void OnSlotSelected(object? parameter)
        {
            if (parameter is TimeSlot slot)
            {
                TimeSlotSelected?.Invoke(this, new TimeSlotSelectedEventArgs(slot));
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Represents a visual block for a busy time period.
    /// </summary>
    public class BusyBlock
    {
        public double Top { get; set; }
        public double Height { get; set; }
        public string? Tooltip { get; set; }
    }

    /// <summary>
    /// Event args for time slot selection.
    /// </summary>
    public class TimeSlotSelectedEventArgs : EventArgs
    {
        public TimeSlot SelectedSlot { get; }

        public TimeSlotSelectedEventArgs(TimeSlot slot)
        {
            SelectedSlot = slot;
        }
    }

    #endregion
}
