using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Attributes;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ProCohere.Avalonia.Views;

[HelpContext("me-view", ContextName = "MeView")]
public partial class MeView : UserControl
{
    private MeViewModel? _viewModel;
    private const int CalendarStartHour = 5;  // 5 AM
    private const int CalendarEndHour = 21;   // 9 PM (exclusive, so last row is 8 PM)
    private const int HourRowHeight = 60;
    
    public MeView()
    {
        InitializeComponent();
        
        _viewModel = new MeViewModel();
        DataContext = _viewModel;
        
        // Subscribe to property changes to refresh views
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // Subscribe to dialog events
        _viewModel.CreateMeetingDialogRequested += OnCreateMeetingDialogRequested;
        _viewModel.EditMeetingDialogRequested += OnEditMeetingDialogRequested;
        _viewModel.CreateTaskDialogRequested += OnCreateTaskDialogRequested;
        _viewModel.EditTaskDialogRequested += OnEditTaskDialogRequested;
        _viewModel.CreateGoalDialogRequested += OnCreateGoalDialogRequested;
        _viewModel.CreateNoteDialogRequested += OnCreateNoteDialogRequested;
        
        // Initial population after control is loaded
        Loaded += MeView_Loaded;
    }

    private void MeView_Loaded(object? sender, RoutedEventArgs e)
    {
        BuildDayView();
        BuildWeekView();
    }

    #region Dialog Handlers

    /// <summary>
    /// Show the create meeting dialog.
    /// </summary>
    private async void OnCreateMeetingDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var result = await AppDialogService.ShowCreateMeetingAsync(window);
        
        if (result.WasDeleted && result.DeletedMeetingId.HasValue)
        {
            _viewModel.OnMeetingDeleted(result.DeletedMeetingId.Value);
        }
        else if (result.Success && result.Meeting != null)
        {
            _viewModel.OnMeetingSaved(result.Meeting);
        }
        else if (result.Error != null)
        {
            Debug.WriteLine($"[MeView] Create meeting error: {result.Error}");
        }
    }

    /// <summary>
    /// Show the edit meeting dialog for an existing meeting.
    /// </summary>
    private async void OnEditMeetingDialogRequested(object? sender, MeetingDetail meeting)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var result = await AppDialogService.ShowEditMeetingAsync(window, meeting);
        
        if (result.WasDeleted && result.DeletedMeetingId.HasValue)
        {
            _viewModel.OnMeetingDeleted(result.DeletedMeetingId.Value);
        }
        else if (result.Success && result.Meeting != null)
        {
            _viewModel.OnMeetingSaved(result.Meeting);
        }
        else if (result.Error != null)
        {
            Debug.WriteLine($"[MeView] Edit meeting error: {result.Error}");
        }
    }

    /// <summary>
    /// Show the create task dialog.
    /// </summary>
    private async void OnCreateTaskDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var result = await AppDialogService.ShowCreateTaskAsync(window);
        
        if (result.WasDeleted && result.DeletedTaskId.HasValue)
        {
            _viewModel.OnTaskDeleted(result.DeletedTaskId.Value);
        }
        else if (result.Success && result.Task != null)
        {
            _viewModel.OnTaskSaved(result.Task);
        }
        else if (result.Error != null)
        {
            Debug.WriteLine($"[MeView] Create task error: {result.Error}");
        }
    }

    /// <summary>
    /// Show the edit task dialog for an existing task.
    /// </summary>
    private async void OnEditTaskDialogRequested(object? sender, TaskDetail task)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var result = await AppDialogService.ShowEditTaskAsync(window, task);
        
        if (result.WasDeleted && result.DeletedTaskId.HasValue)
        {
            _viewModel.OnTaskDeleted(result.DeletedTaskId.Value);
        }
        else if (result.Success && result.Task != null)
        {
            _viewModel.OnTaskSaved(result.Task);
        }
        else if (result.Error != null)
        {
            Debug.WriteLine($"[MeView] Edit task error: {result.Error}");
        }
    }

    /// <summary>
    /// Show the create goal dialog.
    /// </summary>
    private async void OnCreateGoalDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var result = await AppDialogService.ShowCreateGoalAsync(window);
        
        if (result.Success && result.Goal != null)
        {
            // Refresh to include the new goal
            _viewModel.RefreshCommand.Execute(null);
        }
        else if (result.Error != null)
        {
            Debug.WriteLine($"[MeView] Create goal error: {result.Error}");
        }
    }

    /// <summary>
    /// Show the create note dialog (placeholder for now).
    /// </summary>
    private void OnCreateNoteDialogRequested(object? sender, EventArgs e)
    {
        // TODO: Implement note creation dialog when available
        NotificationService.Instance.ShowInfo("Coming Soon", "Note creation will be available soon.");
    }

    #endregion

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Dispatch to UI thread since PropertyChanged may come from background thread
        Dispatcher.UIThread.Post(() =>
        {
            if (e.PropertyName == nameof(MeViewModel.DayMeetings) || 
                e.PropertyName == nameof(MeViewModel.CurrentCalendarDate))
            {
                BuildDayView();
            }
            
            if (e.PropertyName == nameof(MeViewModel.WeekDays) ||
                e.PropertyName == nameof(MeViewModel.CurrentCalendarDate))
            {
                BuildWeekView();
            }
        });
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

        // Use a Grid to overlay the prep indicator
        var contentGrid = new Grid { Margin = new Thickness(4, 2, 4, 2) };
        
        var stack = new StackPanel();
        
        stack.Children.Add(new TextBlock
        {
            Text = meeting.Title,
            FontSize = showTime ? 12 : 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 14, 0) // Leave room for prep indicator
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
        
        // Phase 2: Add personal prep chips (only if there's space and items exist)
        if (showTime && (meeting.MyAgendaCount > 0 || meeting.MyFollowUpsOpenCount > 0))
        {
            var chipsPanel = new StackPanel 
            { 
                Orientation = global::Avalonia.Layout.Orientation.Horizontal, 
                Spacing = 4,
                Margin = new Thickness(0, 2, 0, 0)
            };
            
            if (meeting.MyAgendaCount > 0)
            {
                chipsPanel.Children.Add(CreatePrepChip($"Prep: {meeting.MyAgendaCount}"));
            }
            
            if (meeting.MyFollowUpsOpenCount > 0)
            {
                chipsPanel.Children.Add(CreatePrepChip($"Tasks: {meeting.MyFollowUpsOpenCount}"));
            }
            
            stack.Children.Add(chipsPanel);
        }
        
        contentGrid.Children.Add(stack);
        
        // Phase 3: Add prep state indicator (small dot in top-right)
        var prepIndicator = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = Brush.Parse(meeting.PrepStateColor),
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 2, 0),
            // Add a white border for visibility
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1)
        };
        ToolTip.SetTip(prepIndicator, meeting.PrepStateDisplay);
        contentGrid.Children.Add(prepIndicator);
        
        border.Child = contentGrid;
        return border;
    }
    
    private Border CreatePrepChip(string text)
    {
        return new Border
        {
            Background = Brush.Parse("#40FFFFFF"), // Semi-transparent white
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 1, 4, 1),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 9,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Medium
            }
        };
    }

    private void MeetingCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is MeetingDetail meeting && _viewModel != null)
        {
            _viewModel.OpenMeetingFlyoutCommand.Execute(meeting);
        }
    }

    private void Task_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is TaskDetail task && _viewModel != null)
        {
            _viewModel.OpenTaskFlyoutCommand.Execute(task);
        }
    }

    private void Meeting_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is MeetingDetail meeting && _viewModel != null)
        {
            _viewModel.OpenMeetingFlyoutCommand.Execute(meeting);
        }
    }

    private void Goal_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is GoalDetail goal && _viewModel != null)
        {
            _viewModel.OpenGoalFlyoutCommand.Execute(goal);
        }
    }

    private void Feedback_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is FeedbackDetail feedback && _viewModel != null)
        {
            _viewModel.OpenFeedbackFlyoutCommand.Execute(feedback);
        }
    }
}
