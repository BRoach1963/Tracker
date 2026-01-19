using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace ProCohere.Avalonia.Views;

public partial class CircleView : UserControl
{
    private CircleViewModel? _viewModel;
    private const int CalendarStartHour = 5;  // 5 AM
    private const int CalendarEndHour = 21;   // 9 PM (exclusive, so last row is 8 PM)
    private const int HourRowHeight = 60;

    public CircleView()
    {
        InitializeComponent();
        _viewModel = new CircleViewModel();
        DataContext = _viewModel;
        
        Debug.WriteLine("[CircleView] Constructor - subscribing to PropertyChanged");
        
        // Subscribe to property changes to refresh views
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // Initial population after control is loaded
        Loaded += CircleView_Loaded;
    }

    private void CircleView_Loaded(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[CircleView] Loaded event - DayMeetings count: {_viewModel?.DayMeetings.Count() ?? -1}");
        Debug.WriteLine($"[CircleView] Loaded event - WeekDays count: {_viewModel?.WeekDays.Count ?? -1}");
        BuildDayView();
        BuildWeekView();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.WriteLine($"[CircleView] PropertyChanged: {e.PropertyName}");
        
        if (e.PropertyName == nameof(CircleViewModel.DayMeetings) || 
            e.PropertyName == nameof(CircleViewModel.CurrentDate))
        {
            Debug.WriteLine($"[CircleView] Rebuilding DayView - DayMeetings count: {_viewModel?.DayMeetings.Count() ?? -1}");
            BuildDayView();
        }
        
        if (e.PropertyName == nameof(CircleViewModel.WeekDays) ||
            e.PropertyName == nameof(CircleViewModel.CurrentDate))
        {
            Debug.WriteLine($"[CircleView] Rebuilding WeekView - WeekDays count: {_viewModel?.WeekDays.Count ?? -1}");
            BuildWeekView();
        }
    }

    #region Day View Building

    private void BuildDayView()
    {
        if (DayViewGrid == null || _viewModel == null) return;

        DayViewGrid.Children.Clear();
        DayViewGrid.ColumnDefinitions.Clear();
        DayViewGrid.RowDefinitions.Clear();

        // Columns: Time labels (60px) | Main content (*)
        DayViewGrid.ColumnDefinitions.Add(new ColumnDefinition(60, GridUnitType.Pixel));
        DayViewGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

        // One row per hour
        int numHours = CalendarEndHour - CalendarStartHour;
        for (int i = 0; i < numHours; i++)
        {
            DayViewGrid.RowDefinitions.Add(new RowDefinition(HourRowHeight, GridUnitType.Pixel));
        }

        // Add hour labels and grid lines
        for (int i = 0; i < numHours; i++)
        {
            int hour = CalendarStartHour + i;
            
            // Hour label
            var labelBorder = new Border
            {
                BorderBrush = Brush.Parse("#E5E7EB"),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var label = new TextBlock
            {
                Text = DateTime.Today.AddHours(hour).ToString("h tt"),
                FontSize = 11,
                Foreground = Brush.Parse("#9CA3AF"),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 8, 0)
            };
            labelBorder.Child = label;
            Grid.SetRow(labelBorder, i);
            Grid.SetColumn(labelBorder, 0);
            DayViewGrid.Children.Add(labelBorder);

            // Grid line for content column
            var gridLine = new Border
            {
                BorderBrush = Brush.Parse("#E5E7EB"),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            Grid.SetRow(gridLine, i);
            Grid.SetColumn(gridLine, 1);
            DayViewGrid.Children.Add(gridLine);
        }

        // Add meetings
        foreach (var meeting in _viewModel.DayMeetings)
        {
            var card = CreateMeetingCard(meeting, 280, true);
            
            // Calculate which row based on meeting hour
            int meetingHour = meeting.ScheduledAtLocal?.Hour ?? CalendarStartHour;
            int row = Math.Max(0, Math.Min(meetingHour - CalendarStartHour, numHours - 1));
            
            // Calculate top margin within the row based on minutes
            int minutes = meeting.ScheduledAtLocal?.Minute ?? 0;
            double topMargin = minutes; // 1 pixel per minute
            card.Margin = new Thickness(4, topMargin, 4, 0);
            card.VerticalAlignment = VerticalAlignment.Top;
            
            Grid.SetRow(card, row);
            Grid.SetColumn(card, 1);
            
            // If meeting spans multiple hours, use RowSpan
            int durationHours = (int)Math.Ceiling((meeting.DurationMinutes ?? 30) / 60.0);
            if (durationHours > 1 && row + durationHours <= numHours)
            {
                Grid.SetRowSpan(card, durationHours);
            }
            
            DayViewGrid.Children.Add(card);
        }
    }

    #endregion

    #region Week View Building

    private void BuildWeekView()
    {
        if (WeekViewGrid == null || _viewModel == null) return;

        WeekViewGrid.Children.Clear();
        WeekViewGrid.ColumnDefinitions.Clear();
        WeekViewGrid.RowDefinitions.Clear();

        int numHours = CalendarEndHour - CalendarStartHour;

        // Header row + hour rows
        WeekViewGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Header
        for (int i = 0; i < numHours; i++)
        {
            WeekViewGrid.RowDefinitions.Add(new RowDefinition(HourRowHeight, GridUnitType.Pixel));
        }

        // Columns: Time labels (60px) + 7 day columns
        WeekViewGrid.ColumnDefinitions.Add(new ColumnDefinition(60, GridUnitType.Pixel));
        for (int i = 0; i < 7; i++)
        {
            WeekViewGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }

        // Add day headers
        for (int dayIndex = 0; dayIndex < 7 && dayIndex < _viewModel.WeekDays.Count; dayIndex++)
        {
            var weekDay = _viewModel.WeekDays[dayIndex];
            var headerStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            headerStack.Children.Add(new TextBlock
            {
                Text = weekDay.DayName,
                FontSize = 11,
                Foreground = Brush.Parse("#9CA3AF"),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            
            var dayCircle = new Border
            {
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 4, 0, 0),
                Background = weekDay.IsToday ? Brush.Parse("#3B82F6") : Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            dayCircle.Child = new TextBlock
            {
                Text = weekDay.DayNumber.ToString(),
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Foreground = weekDay.IsToday ? Brushes.White : Brush.Parse("#1F2937"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            headerStack.Children.Add(dayCircle);
            
            Grid.SetRow(headerStack, 0);
            Grid.SetColumn(headerStack, dayIndex + 1);
            WeekViewGrid.Children.Add(headerStack);
        }

        // Add hour labels and grid lines
        for (int i = 0; i < numHours; i++)
        {
            int hour = CalendarStartHour + i;
            int rowIndex = i + 1; // +1 for header row
            
            // Hour label
            var labelBorder = new Border
            {
                BorderBrush = Brush.Parse("#E5E7EB"),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var label = new TextBlock
            {
                Text = DateTime.Today.AddHours(hour).ToString("h tt"),
                FontSize = 11,
                Foreground = Brush.Parse("#9CA3AF"),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 8, 0)
            };
            labelBorder.Child = label;
            Grid.SetRow(labelBorder, rowIndex);
            Grid.SetColumn(labelBorder, 0);
            WeekViewGrid.Children.Add(labelBorder);

            // Grid lines for each day column
            for (int dayIndex = 0; dayIndex < 7; dayIndex++)
            {
                var gridLine = new Border
                {
                    BorderBrush = Brush.Parse("#E5E7EB"),
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                Grid.SetRow(gridLine, rowIndex);
                Grid.SetColumn(gridLine, dayIndex + 1);
                WeekViewGrid.Children.Add(gridLine);
            }
        }

        // Add meetings for each day
        for (int dayIndex = 0; dayIndex < _viewModel.WeekDays.Count; dayIndex++)
        {
            var weekDay = _viewModel.WeekDays[dayIndex];
            foreach (var meeting in weekDay.Meetings)
            {
                var card = CreateMeetingCard(meeting, double.NaN, false); // NaN = stretch to fill
                card.HorizontalAlignment = HorizontalAlignment.Stretch;
                card.Margin = new Thickness(2);
                
                int meetingHour = meeting.ScheduledAtLocal?.Hour ?? CalendarStartHour;
                int row = Math.Max(0, Math.Min(meetingHour - CalendarStartHour, numHours - 1)) + 1; // +1 for header
                
                int minutes = meeting.ScheduledAtLocal?.Minute ?? 0;
                card.Margin = new Thickness(2, minutes + 2, 2, 0);
                card.VerticalAlignment = VerticalAlignment.Top;
                
                Grid.SetRow(card, row);
                Grid.SetColumn(card, dayIndex + 1);
                
                WeekViewGrid.Children.Add(card);
            }
        }
    }

    #endregion

    private Border CreateMeetingCard(MeetingDetail meeting, double width, bool showTime)
    {
        var border = new Border
        {
            Height = meeting.CalendarHeight,
            CornerRadius = new CornerRadius(4),
            Background = Brush.Parse(meeting.TypeColor),
            Tag = meeting,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        
        if (!double.IsNaN(width))
            border.Width = width;

        border.Tapped += MeetingCard_Tapped;

        var stack = new StackPanel { Margin = new Thickness(4, 2, 4, 2) };
        
        stack.Children.Add(new TextBlock
        {
            Text = meeting.Title,
            FontSize = showTime ? 12 : 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        
        if (showTime)
        {
            stack.Children.Add(new TextBlock
            {
                Text = meeting.TimeRangeDisplay,
                FontSize = 10,
                Foreground = Brushes.White,
                Opacity = 0.9
            });
        }
        
        border.Child = stack;
        return border;
    }

    private void StatFilter_All_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFilterCommand.Execute(TeamMemberFilter.All);
        }
    }

    private void StatFilter_Active_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFilterCommand.Execute(TeamMemberFilter.Active);
        }
    }

    private void StatFilter_NeedsAttention_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFilterCommand.Execute(TeamMemberFilter.NeedsAttention);
        }
    }

    private void MemberCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is TeamMemberDetail member)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectTeamMemberCommand.Execute(member);
            }
        }
    }

    private void MemberCard_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is TeamMemberDetail member)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.EditTeamMemberCommand.Execute(member);
            }
        }
    }

    private void ManagerBadge_Tapped(object? sender, RoutedEventArgs e)
    {
        // Stop the event from bubbling to MemberCard_Tapped
        e.Handled = true;
        
        TeamMemberDetail? manager = null;
        
        // Handle both Button and Border (for backwards compatibility)
        if (sender is Button button && button.Tag is TeamMemberDetail btnManager)
            manager = btnManager;
        else if (sender is Border border && border.Tag is TeamMemberDetail borderManager)
            manager = borderManager;
            
        if (manager != null && DataContext is CircleViewModel vm)
        {
            vm.SetManagerFilterCommand.Execute(manager);
        }
    }

    private void MeetingCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is MeetingDetail meeting)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectMeetingCommand.Execute(meeting);
            }
        }
    }

    #region Goals Tab Handlers

    private void GoalFilter_All_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetGoalFilterCommand.Execute(GoalFilter.All);
        }
    }

    private void GoalFilter_OnTrack_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetGoalFilterCommand.Execute(GoalFilter.OnTrack);
        }
    }

    private void GoalFilter_AtRisk_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetGoalFilterCommand.Execute(GoalFilter.AtRisk);
        }
    }

    private void GoalFilter_OffTrack_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetGoalFilterCommand.Execute(GoalFilter.OffTrack);
        }
    }

    private void GoalCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is GoalDetail goal)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectGoalCommand.Execute(goal);
            }
        }
    }

    #endregion

    #region Feedback Tab Handlers

    private void FeedbackFilter_All_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFeedbackFilterCommand.Execute(FeedbackFilter.All);
        }
    }

    private void FeedbackFilter_Praise_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFeedbackFilterCommand.Execute(FeedbackFilter.Praise);
        }
    }

    private void FeedbackFilter_Constructive_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFeedbackFilterCommand.Execute(FeedbackFilter.Constructive);
        }
    }

    private void FeedbackFilter_Coaching_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFeedbackFilterCommand.Execute(FeedbackFilter.Coaching);
        }
    }

    private void FeedbackCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is FeedbackDetail feedback)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectFeedbackCommand.Execute(feedback);
            }
        }
    }

    #endregion
}
