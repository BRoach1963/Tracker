using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Controls
{
    /// <summary>
    /// Calendar view control for displaying meetings in month, week, or day format.
    /// </summary>
    public partial class CalendarViewControl : UserControl, INotifyPropertyChanged
    {
        private readonly ILogger _logger = LoggingManager.GetComponentLogger("CalendarView");
        
        private DateTime _currentDate = DateTime.Today;
        private CalendarViewMode _viewMode = CalendarViewMode.Month;
        private ObservableCollection<OneOnOne> _meetings = new();
        private ObservableCollection<TeamMember> _teamMembers = new();
        private TeamMember? _selectedTeamMember;

        public event PropertyChangedEventHandler? PropertyChanged;
        
        /// <summary>
        /// Raised when a meeting is clicked for editing.
        /// </summary>
        public event EventHandler<MeetingClickedEventArgs>? MeetingClicked;

        /// <summary>
        /// Raised when an empty day/time slot is clicked for creating a new meeting.
        /// </summary>
        public event EventHandler<DateClickedEventArgs>? DateClicked;

        public CalendarViewControl()
        {
            InitializeComponent();
            Loaded += CalendarViewControl_Loaded;
        }

        #region Properties

        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged();
                RefreshCalendar();
            }
        }

        public CalendarViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                _viewMode = value;
                OnPropertyChanged();
                UpdateViewVisibility();
                RefreshCalendar();
            }
        }

        public ObservableCollection<OneOnOne> Meetings
        {
            get => _meetings;
            set
            {
                _meetings = value;
                OnPropertyChanged();
                RefreshCalendar();
            }
        }

        public ObservableCollection<TeamMember> TeamMembers
        {
            get => _teamMembers;
            set
            {
                _teamMembers = value;
                OnPropertyChanged();
                PopulateTeamMemberFilter();
            }
        }

        public TeamMember? SelectedTeamMember
        {
            get => _selectedTeamMember;
            set
            {
                _selectedTeamMember = value;
                OnPropertyChanged();
                RefreshCalendar();
            }
        }

        #endregion

        #region Initialization

        private async void CalendarViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            _logger.Info("CalendarViewControl loaded, initializing...");
            await LoadDataAsync();
            _logger.Info($"Loaded {_meetings.Count} meetings for calendar");
            InitializeDayHeaders();
            RefreshCalendar();
            _logger.Info("Calendar initialization complete");
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load team members for filter from TrackerDataManager (single source of truth)
                var teamMembers = await TrackerDataManager.Instance.GetTeamData();
                _teamMembers = new ObservableCollection<TeamMember>(teamMembers);
                PopulateTeamMemberFilter();

                // Load meetings for current month (expand range for week view edges)
                await LoadMeetingsForPeriodAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load calendar data");
            }
        }

        private async Task LoadMeetingsForPeriodAsync()
        {
            try
            {
                // Get a wider range to handle week/month views crossing month boundaries
                var startDate = _currentDate.AddMonths(-1);
                var endDate = _currentDate.AddMonths(2);

                _logger.Info($"Loading meetings from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                
                var meetings = await TrackerDbManager.Instance.GetMeetingsInRangeAsync(startDate, endDate);
                _logger.Info($"Retrieved {meetings.Count} meetings from database");
                
                // Filter by team member if selected
                if (_selectedTeamMember != null)
                {
                    meetings = meetings.Where(m => m.TeamMember?.Id == _selectedTeamMember.Id).ToList();
                    _logger.Info($"Filtered to {meetings.Count} meetings for team member {_selectedTeamMember.FullName}");
                }

                _meetings = new ObservableCollection<OneOnOne>(meetings);
                OnPropertyChanged(nameof(Meetings));
                _logger.Info($"_meetings collection updated with {_meetings.Count} items");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load meetings for period");
            }
        }

        private void InitializeDayHeaders()
        {
            if (DayHeaders == null) return;
            DayHeaders.Children.Clear();
            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            
            foreach (var day in dayNames)
            {
                DayHeaders.Children.Add(new TextBlock
                {
                    Text = day,
                    Style = TryFindResource("DayHeaderStyle") as Style
                });
            }
        }

        private void PopulateTeamMemberFilter()
        {
            TeamMemberFilter.Items.Clear();
            TeamMemberFilter.Items.Add(new ComboBoxItem { Content = "All Team Members", Tag = null });
            
            foreach (var member in _teamMembers.OrderBy(m => m.FullName))
            {
                TeamMemberFilter.Items.Add(new ComboBoxItem { Content = member.FullName, Tag = member });
            }
            
            TeamMemberFilter.SelectedIndex = 0;
        }

        #endregion

        #region View Switching

        private void UpdateViewVisibility()
        {
            // Named elements may not be initialized yet (constructor/init order or design-time);
            // guard against null to avoid crashes when the control is referenced early.
            if (MonthViewGrid != null)
                MonthViewGrid.Visibility = _viewMode == CalendarViewMode.Month ? Visibility.Visible : Visibility.Collapsed;
            if (WeekViewGrid != null)
                WeekViewGrid.Visibility = _viewMode == CalendarViewMode.Week ? Visibility.Visible : Visibility.Collapsed;
            if (DayViewGrid != null)
                DayViewGrid.Visibility = _viewMode == CalendarViewMode.Day ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            if (MonthViewButton.IsChecked == true) ViewMode = CalendarViewMode.Month;
            else if (WeekViewButton.IsChecked == true) ViewMode = CalendarViewMode.Week;
            else if (DayViewButton.IsChecked == true) ViewMode = CalendarViewMode.Day;
        }

        #endregion

        #region Navigation

        private void PreviousPeriod_Click(object sender, RoutedEventArgs e)
        {
            CurrentDate = _viewMode switch
            {
                CalendarViewMode.Month => _currentDate.AddMonths(-1),
                CalendarViewMode.Week => _currentDate.AddDays(-7),
                CalendarViewMode.Day => _currentDate.AddDays(-1),
                _ => _currentDate
            };
        }

        private void NextPeriod_Click(object sender, RoutedEventArgs e)
        {
            CurrentDate = _viewMode switch
            {
                CalendarViewMode.Month => _currentDate.AddMonths(1),
                CalendarViewMode.Week => _currentDate.AddDays(7),
                CalendarViewMode.Day => _currentDate.AddDays(1),
                _ => _currentDate
            };
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            CurrentDate = DateTime.Today;
        }

        private void TeamMemberFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeamMemberFilter.SelectedItem is ComboBoxItem item)
            {
                _selectedTeamMember = item.Tag as TeamMember;
                _ = LoadMeetingsForPeriodAsync().ContinueWith(_ => 
                    Dispatcher.Invoke(RefreshCalendar));
            }
        }

        #endregion

        #region Calendar Rendering

        private void RefreshCalendar()
        {
            UpdatePeriodLabel();
            
            switch (_viewMode)
            {
                case CalendarViewMode.Month:
                    RenderMonthView();
                    break;
                case CalendarViewMode.Week:
                    RenderWeekView();
                    break;
                case CalendarViewMode.Day:
                    RenderDayView();
                    break;
            }
        }

        private void UpdatePeriodLabel()
        {
            PeriodLabel.Text = _viewMode switch
            {
                CalendarViewMode.Month => _currentDate.ToString("MMMM yyyy"),
                CalendarViewMode.Week => GetWeekRangeLabel(),
                CalendarViewMode.Day => _currentDate.ToString("dddd, MMMM d, yyyy"),
                _ => _currentDate.ToString("MMMM yyyy")
            };
        }

        private string GetWeekRangeLabel()
        {
            var startOfWeek = _currentDate.AddDays(-(int)_currentDate.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);
            
            if (startOfWeek.Month == endOfWeek.Month)
            {
                return $"{startOfWeek:MMM d} - {endOfWeek:d}, {endOfWeek:yyyy}";
            }
            return $"{startOfWeek:MMM d} - {endOfWeek:MMM d}, {endOfWeek:yyyy}";
        }

        #endregion

        #region Month View

        private void RenderMonthView()
        {
            if (MonthGrid == null) return;
            
            MonthGrid.Children.Clear();
            
            var firstOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            var startDay = firstOfMonth.AddDays(-(int)firstOfMonth.DayOfWeek);
            
            for (int i = 0; i < 42; i++) // 6 weeks x 7 days
            {
                var date = startDay.AddDays(i);
                var cell = CreateMonthDayCell(date, date.Month == _currentDate.Month);
                MonthGrid.Children.Add(cell);
            }
        }

        private Border CreateMonthDayCell(DateTime date, bool isCurrentMonth)
        {
            var cell = new Border
            {
                Style = TryFindResource("DayCellStyle") as Style,
                Tag = date,
                Cursor = Cursors.Hand
            };
            
            if (!isCurrentMonth)
            {
                cell.Opacity = 0.4;
            }
            
            var stack = new StackPanel { Margin = new Thickness(4) };
            
            // Day number
            var dayNumber = new TextBlock
            {
                Text = date.Day.ToString(),
                FontSize = 12,
                FontWeight = date == DateTime.Today ? FontWeights.Bold : FontWeights.Normal,
                Foreground = date == DateTime.Today 
                    ? TryFindResource("AccentBrush") as Brush ?? FindResource("ForegroundBrush") as Brush
                    : TryFindResource("ForegroundBrush") as Brush ?? Brushes.Black,
                Margin = new Thickness(0, 0, 0, 4)
            };
            
            // Today indicator
            if (date == DateTime.Today)
            {
                var todayBorder = new Border
                {
                    Background = TryFindResource("AccentBrush") as Brush ?? Brushes.Blue,
                    CornerRadius = new CornerRadius(10),
                    Width = 24,
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                todayBorder.Child = new TextBlock
                {
                    Text = date.Day.ToString(),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = TryFindResource("BackgroundBrush") as Brush ?? Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stack.Children.Add(todayBorder);
            }
            else
            {
                stack.Children.Add(dayNumber);
            }
            
            // Meetings for this day
            var dayMeetings = _meetings
                .Where(m => m.Date.Date == date.Date)
                .OrderBy(m => m.StartTime)
                .Take(3) // Show max 3, then "+X more"
                .ToList();
            
            foreach (var meeting in dayMeetings)
            {
                var meetingBlock = CreateMeetingBlock(meeting, true);
                stack.Children.Add(meetingBlock);
            }
            
            var totalMeetings = _meetings.Count(m => m.Date.Date == date.Date);
            if (totalMeetings > 3)
            {
                var moreText = new TextBlock
                {
                    Text = $"+{totalMeetings - 3} more",
                    FontSize = 10,
                    Foreground = TryFindResource("HintTextBrush") as Brush ?? Brushes.Gray,
                    Margin = new Thickness(2, 2, 0, 0)
                };
                stack.Children.Add(moreText);
            }
            
            cell.Child = stack;
            cell.MouseLeftButtonUp += (s, e) => OnDateClicked(date);
            
            return cell;
        }

        #endregion

        #region Week View

        private void RenderWeekView()
        {
            if (WeekDayHeaders == null || WeekTimeGrid == null) return;
            
            WeekDayHeaders.Children.Clear();
            WeekTimeGrid.Children.Clear();
            WeekTimeGrid.RowDefinitions.Clear();
            WeekTimeGrid.ColumnDefinitions.Clear();
            
            var startOfWeek = _currentDate.AddDays(-(int)_currentDate.DayOfWeek);
            
            // Day headers with dates
            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                var header = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                header.Children.Add(new TextBlock
                {
                    Text = date.ToString("ddd"),
                    FontSize = 11,
                    Foreground = TryFindResource("HintTextBrush") as Brush ?? Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                header.Children.Add(new TextBlock
                {
                    Text = date.Day.ToString(),
                    FontSize = 16,
                    FontWeight = date == DateTime.Today ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = date == DateTime.Today 
                        ? TryFindResource("AccentBrush") as Brush ?? Brushes.Blue
                        : TryFindResource("ForegroundBrush") as Brush ?? Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 8)
                });
                WeekDayHeaders.Children.Add(header);
            }
            
            // Time column + 7 day columns
            WeekTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            for (int i = 0; i < 7; i++)
            {
                WeekTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            
            // Hour rows (6 AM to 8 PM)
            for (int hour = 6; hour <= 20; hour++)
            {
                WeekTimeGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
                
                // Time label
                var timeLabel = new TextBlock
                {
                    Text = DateTime.Today.AddHours(hour).ToString("h tt"),
                    FontSize = 10,
                    Foreground = TryFindResource("HintTextBrush") as Brush ?? Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetRow(timeLabel, hour - 6);
                Grid.SetColumn(timeLabel, 0);
                WeekTimeGrid.Children.Add(timeLabel);
                
                // Day cells
                for (int day = 0; day < 7; day++)
                {
                    var cellBorder = new Border
                    {
                        BorderBrush = TryFindResource("BorderBrush") as Brush ?? Brushes.LightGray,
                        BorderThickness = new Thickness(1, 0, 0, 1),
                        Background = Brushes.Transparent
                    };
                    Grid.SetRow(cellBorder, hour - 6);
                    Grid.SetColumn(cellBorder, day + 1);
                    WeekTimeGrid.Children.Add(cellBorder);
                }
            }
            
            // Add meeting blocks
            for (int day = 0; day < 7; day++)
            {
                var date = startOfWeek.AddDays(day);
                var dayMeetings = _meetings.Where(m => m.Date.Date == date.Date).ToList();
                
                foreach (var meeting in dayMeetings)
                {
                    var startTime = meeting.StartTime;
                    var startHour = startTime.Hours;
                    if (startHour >= 6 && startHour <= 20)
                    {
                        var block = CreateMeetingBlock(meeting, false);
                        var duration = (meeting.EndTime - startTime).TotalHours;
                        block.Height = Math.Max(duration * 60 - 4, 20);
                        block.Margin = new Thickness(2, startTime.Minutes + 2, 2, 0);
                        block.VerticalAlignment = VerticalAlignment.Top;
                        
                        Grid.SetRow(block, startHour - 6);
                        Grid.SetColumn(block, day + 1);
                        WeekTimeGrid.Children.Add(block);
                    }
                }
            }
        }

        #endregion

        #region Day View

        private void RenderDayView()
        {
            if (DayTimeGrid == null || DayHeaderLabel == null) return;
            
            DayHeaderLabel.Text = _currentDate.ToString("dddd, MMMM d");
            DayTimeGrid.Children.Clear();
            DayTimeGrid.RowDefinitions.Clear();
            DayTimeGrid.ColumnDefinitions.Clear();
            
            DayTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            DayTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Hour rows (6 AM to 8 PM)
            for (int hour = 6; hour <= 20; hour++)
            {
                DayTimeGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
                
                // Time label
                var timeLabel = new TextBlock
                {
                    Text = DateTime.Today.AddHours(hour).ToString("h:mm tt"),
                    FontSize = 11,
                    Foreground = TryFindResource("HintTextBrush") as Brush ?? Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(12, 4, 0, 0)
                };
                Grid.SetRow(timeLabel, hour - 6);
                Grid.SetColumn(timeLabel, 0);
                DayTimeGrid.Children.Add(timeLabel);
                
                // Hour cell
                var cellBorder = new Border
                {
                    BorderBrush = TryFindResource("BorderBrush") as Brush ?? Brushes.LightGray,
                    BorderThickness = new Thickness(1, 0, 0, 1),
                    Background = Brushes.Transparent
                };
                Grid.SetRow(cellBorder, hour - 6);
                Grid.SetColumn(cellBorder, 1);
                DayTimeGrid.Children.Add(cellBorder);
            }
            
            // Add meeting blocks
            var dayMeetings = _meetings.Where(m => m.Date.Date == _currentDate.Date).ToList();
            
            foreach (var meeting in dayMeetings)
            {
                var startTime = meeting.StartTime;
                var startHour = startTime.Hours;
                if (startHour >= 6 && startHour <= 20)
                {
                    var block = CreateMeetingBlock(meeting, false);
                    var duration = (meeting.EndTime - startTime).TotalHours;
                    block.Height = Math.Max(duration * 60 - 4, 20);
                    block.Margin = new Thickness(2, startTime.Minutes + 2, 2, 0);
                    block.VerticalAlignment = VerticalAlignment.Top;
                    
                    Grid.SetRow(block, startHour - 6);
                    Grid.SetColumn(block, 1);
                    DayTimeGrid.Children.Add(block);
                }
            }
        }

        #endregion

        #region Meeting Block Creation

        private Border CreateMeetingBlock(OneOnOne meeting, bool compact)
        {
            var block = new Border
            {
                Style = TryFindResource("MeetingBlockStyle") as Style,
                Tag = meeting
            };
            
            // Color based on status
            block.Background = meeting.Status switch
            {
                MeetingStatusEnum.Completed => TryFindResource("SuccessBrush") as Brush ?? (TryFindResource("AccentBrush") as Brush ?? Brushes.Green),
                MeetingStatusEnum.Canceled => TryFindResource("ErrorBrush") as Brush ?? Brushes.Gray,
                _ => TryFindResource("AccentBrush") as Brush ?? Brushes.Blue
            };
            
            var stack = new StackPanel();
            
            if (compact)
            {
                // Compact mode for month view
                var title = new TextBlock
                {
                    Text = $"{meeting.StartTime:hh\\:mm} {meeting.TeamMemberName}",
                    FontSize = 10,
                    Foreground = TryFindResource("BackgroundBrush") as Brush ?? Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                stack.Children.Add(title);
            }
            else
            {
                // Full mode for week/day view
                var timeText = new TextBlock
                {
                    Text = $"{meeting.StartTime:hh\\:mm} - {meeting.EndTime:hh\\:mm}",
                    FontSize = 10,
                    Foreground = TryFindResource("BackgroundBrush") as Brush ?? Brushes.White,
                    Opacity = 0.8
                };
                stack.Children.Add(timeText);
                
                var title = new TextBlock
                {
                    Text = meeting.TeamMemberName,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TryFindResource("BackgroundBrush") as Brush ?? Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                stack.Children.Add(title);
                
                if (!string.IsNullOrEmpty(meeting.Description))
                {
                    var desc = new TextBlock
                    {
                        Text = meeting.Description,
                        FontSize = 10,
                        Foreground = TryFindResource("BackgroundBrush") as Brush ?? Brushes.White,
                        Opacity = 0.8,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    stack.Children.Add(desc);
                }
            }
            
            block.Child = stack;
            block.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                OnMeetingClicked(meeting);
            };
            
            return block;
        }

        #endregion

        #region Events

        private void OnMeetingClicked(OneOnOne meeting)
        {
            MeetingClicked?.Invoke(this, new MeetingClickedEventArgs(meeting));
        }

        private void OnDateClicked(DateTime date)
        {
            DateClicked?.Invoke(this, new DateClickedEventArgs(date));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the calendar data from the database.
        /// </summary>
        public async Task RefreshAsync()
        {
            await LoadMeetingsForPeriodAsync();
            RefreshCalendar();
        }

        /// <summary>
        /// Navigates to a specific date.
        /// </summary>
        public void NavigateToDate(DateTime date)
        {
            CurrentDate = date;
        }

        #endregion
    }

    #region Enums and Event Args

    public enum CalendarViewMode
    {
        Month,
        Week,
        Day
    }

    public class MeetingClickedEventArgs : EventArgs
    {
        public OneOnOne Meeting { get; }
        public MeetingClickedEventArgs(OneOnOne meeting) => Meeting = meeting;
    }

    public class DateClickedEventArgs : EventArgs
    {
        public DateTime Date { get; }
        public DateClickedEventArgs(DateTime date) => Date = date;
    }

    #endregion
}
