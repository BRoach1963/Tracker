using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// A combined date and time selector control.
/// Uses CalendarDatePicker for date and a ComboBox with preset times.
/// </summary>
public partial class DateTimeSelector : UserControl
{
    private CalendarDatePicker? _datePicker;
    private ComboBox? _timeComboBox;
    private bool _isUpdating;

    /// <summary>
    /// The selected DateTime value.
    /// </summary>
    public static readonly StyledProperty<DateTime?> SelectedDateTimeProperty =
        AvaloniaProperty.Register<DateTimeSelector, DateTime?>(nameof(SelectedDateTime), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public DateTime? SelectedDateTime
    {
        get => GetValue(SelectedDateTimeProperty);
        set => SetValue(SelectedDateTimeProperty, value);
    }

    /// <summary>
    /// Minute increment for time options (default 15).
    /// </summary>
    public static readonly StyledProperty<int> MinuteIncrementProperty =
        AvaloniaProperty.Register<DateTimeSelector, int>(nameof(MinuteIncrement), 15);

    public int MinuteIncrement
    {
        get => GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    /// <summary>
    /// Whether to use 12-hour clock format (default true).
    /// </summary>
    public static readonly StyledProperty<bool> Use12HourClockProperty =
        AvaloniaProperty.Register<DateTimeSelector, bool>(nameof(Use12HourClock), true);

    public bool Use12HourClock
    {
        get => GetValue(Use12HourClockProperty);
        set => SetValue(Use12HourClockProperty, value);
    }

    public DateTimeSelector()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        _datePicker = this.FindControl<CalendarDatePicker>("DatePicker");
        _timeComboBox = this.FindControl<ComboBox>("TimeComboBox");

        if (_datePicker != null)
        {
            _datePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
        }

        if (_timeComboBox != null)
        {
            PopulateTimeOptions();
            _timeComboBox.SelectionChanged += TimeComboBox_SelectionChanged;
        }

        // Set initial value
        if (SelectedDateTime.HasValue)
        {
            UpdateControlsFromDateTime(SelectedDateTime.Value);
        }
        else
        {
            // Default to next hour
            var now = DateTime.Now;
            var nextHour = now.AddHours(1);
            nextHour = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 0);
            UpdateControlsFromDateTime(nextHour);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedDateTimeProperty && !_isUpdating)
        {
            var newValue = change.GetNewValue<DateTime?>();
            if (newValue.HasValue)
            {
                UpdateControlsFromDateTime(newValue.Value);
            }
        }
        else if (change.Property == MinuteIncrementProperty || change.Property == Use12HourClockProperty)
        {
            PopulateTimeOptions();
        }
    }

    private void PopulateTimeOptions()
    {
        if (_timeComboBox == null) return;

        var currentSelection = _timeComboBox.SelectedItem as TimeOption;
        _timeComboBox.Items.Clear();

        var increment = MinuteIncrement > 0 ? MinuteIncrement : 15;
        var use12Hour = Use12HourClock;

        for (int hour = 0; hour < 24; hour++)
        {
            for (int minute = 0; minute < 60; minute += increment)
            {
                var time = new TimeSpan(hour, minute, 0);
                var option = new TimeOption(time, use12Hour);
                _timeComboBox.Items.Add(option);

                // Restore selection if it matches
                if (currentSelection != null && currentSelection.Time == time)
                {
                    _timeComboBox.SelectedItem = option;
                }
            }
        }

        // Default to 9:00 AM if nothing selected
        if (_timeComboBox.SelectedItem == null && _timeComboBox.Items.Count > 0)
        {
            foreach (var item in _timeComboBox.Items)
            {
                if (item is TimeOption opt && opt.Time.Hours == 9 && opt.Time.Minutes == 0)
                {
                    _timeComboBox.SelectedItem = opt;
                    break;
                }
            }
        }
    }

    private void UpdateControlsFromDateTime(DateTime dateTime)
    {
        _isUpdating = true;
        try
        {
            if (_datePicker != null)
            {
                _datePicker.SelectedDate = dateTime.Date;
            }

            if (_timeComboBox != null)
            {
                var targetTime = dateTime.TimeOfDay;
                // Round to nearest increment
                var increment = MinuteIncrement > 0 ? MinuteIncrement : 15;
                var roundedMinutes = (int)(Math.Round(targetTime.TotalMinutes / increment) * increment);
                if (roundedMinutes >= 24 * 60) roundedMinutes = 0;
                var roundedTime = TimeSpan.FromMinutes(roundedMinutes);

                foreach (var item in _timeComboBox.Items)
                {
                    if (item is TimeOption opt && opt.Time == roundedTime)
                    {
                        _timeComboBox.SelectedItem = opt;
                        break;
                    }
                }
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateSelectedDateTime()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        try
        {
            var date = _datePicker?.SelectedDate?.Date ?? DateTime.Today;
            var time = (_timeComboBox?.SelectedItem as TimeOption)?.Time ?? TimeSpan.Zero;

            SelectedDateTime = date.Add(time);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedDateTime();
    }

    private void TimeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedDateTime();
    }

    /// <summary>
    /// Helper class for time dropdown items.
    /// </summary>
    private class TimeOption
    {
        public TimeSpan Time { get; }
        public string Display { get; }

        public TimeOption(TimeSpan time, bool use12Hour)
        {
            Time = time;

            if (use12Hour)
            {
                var hour = time.Hours;
                var period = hour >= 12 ? "PM" : "AM";
                if (hour == 0) hour = 12;
                else if (hour > 12) hour -= 12;
                Display = $"{hour}:{time.Minutes:D2} {period}";
            }
            else
            {
                Display = $"{time.Hours:D2}:{time.Minutes:D2}";
            }
        }

        public override string ToString() => Display;
    }
}
