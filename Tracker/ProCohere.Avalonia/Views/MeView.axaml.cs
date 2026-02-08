using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ProCohere.Avalonia.Dialogs;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.ViewModels.Dialogs;
using ProCohere.Avalonia.Views.Dialogs;
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
        _viewModel.CreateDevelopmentPlanDialogRequested += OnCreateDevelopmentPlanDialogRequested;
        _viewModel.EditDevelopmentPlanDialogRequested += OnEditDevelopmentPlanDialogRequested;
        
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
            _viewModel.OnGoalSaved(result.Goal);
        }
        else if (result.Error != null)
        {
            Debug.WriteLine($"[MeView] Create goal error: {result.Error}");
        }
    }

    /// <summary>
    /// Show the create note dialog.
    /// </summary>
    private async void OnCreateNoteDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var dialog = new AddNoteDialog();
        var result = await dialog.ShowDialog<AddNoteResult?>(window);

        if (result != null && !string.IsNullOrWhiteSpace(result.Content))
        {
            var note = new Note
            {
                Title = result.Title,
                Content = result.Content
            };

            var created = await NotesService.Instance.CreateNoteAsync(note);
            if (created != null)
            {
                NotificationService.Instance.ShowSuccess("Note Created", "Your note has been saved.");
            }
        }
    }

    /// <summary>
    /// Show the create development plan dialog.
    /// </summary>
    private async void OnCreateDevelopmentPlanDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var viewModel = new EditDevelopmentPlanDialogViewModel();
        var dialog = new EditDevelopmentPlanDialog(viewModel);
        
        var result = await dialog.ShowDialog<DevelopmentPlan?>(window);
        
        if (result != null)
        {
            _viewModel.OnDevelopmentPlanSaved(result);
        }
    }

    /// <summary>
    /// Show the edit development plan dialog for an existing plan.
    /// </summary>
    private async void OnEditDevelopmentPlanDialogRequested(object? sender, DevelopmentPlan plan)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var viewModel = new EditDevelopmentPlanDialogViewModel(plan);
        var dialog = new EditDevelopmentPlanDialog(viewModel);
        
        var result = await dialog.ShowDialog<DevelopmentPlan?>(window);
        
        if (result != null)
        {
            _viewModel.OnDevelopmentPlanSaved(result);
        }
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

    private void DevelopmentPlan_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is DevelopmentPlan plan && _viewModel != null)
        {
            _viewModel.OpenDevelopmentPlanFlyoutCommand.Execute(plan);
        }
    }

    #region Flyout Action Handlers

    private async void EditGoal_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedGoal == null) return;

        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowEditGoalAsync(window, _viewModel.SelectedGoal);
        
        if (result.Success)
        {
            _viewModel.RefreshCommand.Execute(null);
        }
    }

    private async void DeleteGoal_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedGoal == null) return;

        var parent = TopLevel.GetTopLevel(this);
        if (parent is not Window window) return;

        var dialogService = new DialogService(window);
        var confirmed = await dialogService.ShowConfirmationAsync(
            "Delete Goal",
            $"Are you sure you want to delete '{_viewModel.SelectedGoal.Title}'?",
            "Delete",
            "Cancel");

        if (confirmed)
        {
            var goalsService = Services.GoalsService.Instance;
            await goalsService.DeleteGoalAsync(_viewModel.SelectedGoal.Id);
            _viewModel.CloseFlyoutCommand.Execute(null);
            _viewModel.RefreshCommand.Execute(null);
        }
    }

    private async void EditFeedback_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedFeedback == null) return;
        
        var feedback = _viewModel.SelectedFeedback;
        
        // Only the author can edit their feedback
        var currentMember = AuthService.Instance.CurrentTeamMember;
        if (currentMember == null || feedback.FromMemberId != currentMember.Id)
        {
            NotificationService.Instance.ShowWarning("Cannot Edit", "You can only edit feedback you created.");
            return;
        }
        
        // Determine recipient name
        var recipientName = feedback.RecipientInitials ?? "Team Member";
        
        var viewModel = new ViewModels.Dialogs.EditFeedbackDialogViewModel();
        
        var parent = TopLevel.GetTopLevel(this);
        if (parent is Window window)
        {
            viewModel.SetDialogService(new DialogService(window));
            
            var dialog = new Dialogs.EditFeedbackDialog
            {
                DataContext = viewModel
            };
            
            // Load the feedback data
            await viewModel.LoadFeedbackAsync(feedback.Id, recipientName);
            
            await dialog.ShowDialog(window);
            
            if (viewModel.WasSaved)
            {
                // Refresh to show updated data
                _viewModel.RefreshCommand.Execute(null);
                NotificationService.Instance.ShowSuccess("Success", "Feedback updated successfully.");
            }
        }
    }

    private async void DeleteFeedback_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedFeedback == null) return;
        
        var feedback = _viewModel.SelectedFeedback;
        
        // Only the author can delete their feedback
        var currentMember = AuthService.Instance.CurrentTeamMember;
        if (currentMember == null || feedback.FromMemberId != currentMember.Id)
        {
            NotificationService.Instance.ShowWarning("Cannot Delete", "You can only delete feedback you created.");
            return;
        }
        
        var parent = TopLevel.GetTopLevel(this);
        if (parent is Window window)
        {
            var dialogService = new DialogService(window);
            var confirmed = await dialogService.ShowConfirmationAsync(
                "Delete Feedback",
                "Are you sure you want to delete this feedback? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (confirmed)
            {
                var success = await FeedbackService.Instance.DeleteFeedbackAsync(feedback.Id);
                
                if (success)
                {
                    _viewModel.CloseFlyoutCommand?.Execute(null);
                    _viewModel.RefreshCommand.Execute(null);
                    NotificationService.Instance.ShowSuccess("Success", "Feedback deleted successfully.");
                }
                else
                {
                    NotificationService.Instance.ShowError("Error", FeedbackService.Instance.LastError ?? "Failed to delete feedback.");
                }
            }
        }
    }

    /// <summary>
    /// Opens the Add Feedback dialog for creating new feedback.
    /// Uses team member selection mode since no recipient is pre-selected.
    /// </summary>
    private async void GiveFeedback_Click(object? sender, RoutedEventArgs e)
    {
        var parent = TopLevel.GetTopLevel(this);
        if (parent is not Window window) return;

        var viewModel = new ViewModels.Dialogs.AddFeedbackDialogViewModel();
        
        // Initialize for team member selection mode (no pre-selected recipient)
        await viewModel.InitializeForTeamMemberSelectionAsync();
        
        // Set dialog service for confirmation
        viewModel.SetDialogService(new DialogService(window));
        
        var dialog = new AddFeedbackDialog
        {
            DataContext = viewModel
        };

        await dialog.ShowDialog(window);

        // If user saved, refresh the view
        if (viewModel.WasSaved)
        {
            _viewModel?.RefreshCommand.Execute(null);
            NotificationService.Instance.ShowSuccess("Success", "Feedback created successfully.");
        }
    }

    private void AddPrepItem_Click(object? sender, RoutedEventArgs e)
    {
        // Use the meeting context from the button to add prep item
        if (sender is Button button && button.DataContext is MeetingDetail meeting)
        {
            _viewModel?.AddPrepItemCommand.Execute(meeting);
        }
    }

    #endregion
}
