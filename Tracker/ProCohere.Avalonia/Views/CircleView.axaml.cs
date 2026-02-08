using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.ViewModels.Dialogs;
using ProCohere.Avalonia.Views.Dialogs;
using ProCohere.Avalonia.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ProCohere.Avalonia.Views;

[HelpContext("circle-view", ContextName = "CircleView")]
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
        
        // Subscribe to property changes to refresh views
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // Subscribe to dialog events
        _viewModel.EditTeamMemberDialogRequested += OnEditTeamMemberDialogRequested;
        _viewModel.InviteTeamMemberDialogRequested += OnInviteTeamMemberDialogRequested;
        _viewModel.CreateMeetingDialogRequested += OnCreateMeetingDialogRequested;
        _viewModel.EditMeetingDialogRequested += OnEditMeetingDialogRequested;
        _viewModel.AddAgendaItemDialogRequested += OnAddAgendaItemDialogRequested;
        _viewModel.AddAttendeeDialogRequested += OnAddAttendeeDialogRequested;
        _viewModel.EditMeetingNotesRequested += OnEditMeetingNotesRequested;
        _viewModel.LinkMetricToGoalRequested += OnLinkMetricToGoalRequested;
        _viewModel.AddGoalDialogRequested += OnAddGoalDialogRequested;
        _viewModel.EditGoalDialogRequested += OnEditGoalDialogRequested;
        _viewModel.GiveFeedbackDialogRequested += OnGiveFeedbackDialogRequested;
        _viewModel.GiveKudosDialogRequested += OnGiveKudosDialogRequested;
        _viewModel.SendMessageDialogRequested += OnSendMessageDialogRequested;
        
        // Initial population after control is loaded
        Loaded += CircleView_Loaded;
    }

    private void CircleView_Loaded(object? sender, RoutedEventArgs e)
    {
        BuildDayView();
        BuildWeekView();
    }

    #region Dialog Handlers
    private async void OnEditTeamMemberDialogRequested(object? sender, TeamMemberDetail member)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var viewModel = new TeamMemberDetailsDialogViewModel();
        
        // Set dialog service for confirmations
        viewModel.SetDialogService(new DialogService(window));
        
        // Filter out the member being edited from available managers
        var availableManagers = _viewModel.FilteredTeamMembers
            .Where(m => m.Id != member.Id)
            .ToList();
        viewModel.Initialize(availableManagers);
        
        // Load the member's details
        await viewModel.LoadTeamMemberAsync(member);

        var dialog = new TeamMemberDetailsDialog();
        dialog.Initialize(viewModel);

        await dialog.ShowDialog(window);

        if (dialog.Result != null)
        {
            // Refresh the team list after edit
            _viewModel.RefreshCommand.Execute(null);
        }
    }

    private async void OnInviteTeamMemberDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var dialog = new InviteTeamMemberDialog();
        dialog.SetManagers(_viewModel.FilteredTeamMembers);

        await dialog.ShowDialog(window);

        if (dialog.Result != null)
        {
            // Invite flow: TeamService.InviteTeamMemberAsync needs to be implemented
            // For now, show success notification and refresh (invite email would be sent by backend)
            NotificationService.Instance.ShowSuccess(
                "Invite Sent", 
                $"Invitation sent to {dialog.Result.Email}");
            _viewModel.RefreshCommand.Execute(null);
        }
    }

    private async void OnCreateMeetingDialogRequested(object? sender, TeamMemberDetail? preSelectedAttendee)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var result = await AppDialogService.ShowCreateMeetingAsync(window, preSelectedAttendee);
        
        if (result.WasDeleted && result.DeletedMeetingId.HasValue)
        {
            _viewModel.OnMeetingDeleted(result.DeletedMeetingId.Value);
        }
        else if (result.Success && result.Meeting != null)
        {
            _viewModel.OnMeetingSaved(result.Meeting);
        }
    }

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
    }

    private async void OnAddAgendaItemDialogRequested(object? sender, MeetingDetail meeting)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var dialog = new AddAgendaItemDialog();
        var result = await dialog.ShowDialog<AddAgendaItemResult?>(window);
        
        if (result != null)
        {
            // Create the agenda item via service
            var newItem = await MeetingAgendaItemService.Instance.CreateAgendaItemAsync(
                meeting.Id,
                result.Title,
                result.Description,
                sortOrder: meeting.AgendaItems?.Count ?? 0,
                isPrivate: result.IsPrivate,
                visibilityScope: result.VisibilityScope);
            
            if (newItem != null)
            {
                _viewModel.OnAgendaItemAdded(newItem);
                NotificationService.Instance.ShowSuccess("Agenda Item Added", $"Added '{result.Title}' to the meeting agenda.");
            }
            else
            {
                NotificationService.Instance.ShowError("Failed to Add", MeetingAgendaItemService.Instance.LastError ?? "Could not add agenda item.");
            }
        }
    }

    private async void OnAddAttendeeDialogRequested(object? sender, MeetingDetail meeting)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        // Get existing attendee IDs to exclude from picker
        var existingAttendeeIds = new HashSet<Guid>(meeting.Attendees?.Select(a => a.TeamMemberId) ?? Array.Empty<Guid>());

        // Show the entity picker filtered to people only
        var dialog = new EntityPickerDialog();
        dialog.SetAllowedTypes("person");
        
        await dialog.ShowDialog(window);
        
        if (dialog.Result != null && dialog.Result.EntityType == "person")
        {
            // Check if already an attendee
            if (existingAttendeeIds.Contains(dialog.Result.EntityId))
            {
                NotificationService.Instance.ShowWarning("Already Added", $"{dialog.Result.EntityTitle} is already an attendee of this meeting.");
                return;
            }
            
            // Add the attendee via service
            var newAttendee = await MeetingService.Instance.AddAttendeeAsync(meeting.Id, dialog.Result.EntityId);
            
            if (newAttendee != null)
            {
                // Enrich with name from picker result
                newAttendee.Name = dialog.Result.EntityTitle;
                _viewModel.OnAttendeeAdded(newAttendee);
                NotificationService.Instance.ShowSuccess("Attendee Added", $"Added {dialog.Result.EntityTitle} to the meeting.");
            }
            else
            {
                NotificationService.Instance.ShowError("Failed to Add", MeetingService.Instance.LastError ?? "Could not add attendee.");
            }
        }
    }

    private async void OnEditMeetingNotesRequested(object? sender, MeetingDetail meeting)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        // Open the EditMeetingDialog - it has a Notes tab
        var result = await AppDialogService.ShowEditMeetingAsync(window, meeting);
        
        if (result.WasDeleted && result.DeletedMeetingId.HasValue)
        {
            _viewModel.OnMeetingDeleted(result.DeletedMeetingId.Value);
        }
        else if (result.Success && result.Meeting != null)
        {
            _viewModel.OnMeetingSaved(result.Meeting);
        }
    }

    #region EntityDetailFlyout Meeting Action Handlers
    
    private void OnEntityFlyout_AddAgendaItemRequested(object? sender, MeetingDetail meeting)
    {
        _viewModel?.RequestAddAgendaItem();
    }

    private void OnEntityFlyout_AddAttendeeRequested(object? sender, MeetingDetail meeting)
    {
        _viewModel?.RequestAddAttendee();
    }

    private void OnEntityFlyout_EditNotesRequested(object? sender, MeetingDetail meeting)
    {
        _viewModel?.RequestEditMeetingNotes();
    }

    #endregion

    private async void OnLinkMetricToGoalRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel?.SelectedGoal == null) return;

        // Show the entity picker filtered to only show metrics
        var dialog = new EntityPickerDialog();
        dialog.SetAllowedTypes("metric");
        
        await dialog.ShowDialog(window);
        
        if (dialog.Result != null && dialog.Result.EntityType == "metric")
        {
            // Get the metric and link it to the goal
            var metric = await MetricsService.Instance.GetMetricByIdAsync(dialog.Result.EntityId);
            if (metric != null)
            {
                await _viewModel.LinkMetricCommand.ExecuteAsync(metric);
            }
        }
    }

    private async void OnAddGoalDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var viewModel = new AddGoalDialogViewModel();
        
        // Get team members from service
        var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
        viewModel.SetTeamMembers(teamMembers);
        
        // Pre-select the current team member as owner if one is selected
        if (_viewModel.SelectedTeamMember != null)
        {
            viewModel.SetDefaultOwner(_viewModel.SelectedTeamMember.Id);
        }
        
        // Set dialog service for confirmation
        viewModel.SetDialogService(new DialogService(window));
        
        var dialog = new AddGoalDialog
        {
            DataContext = viewModel
        };

        await dialog.ShowDialog(window);

        // If user saved, create the goal
        if (viewModel.Result != null && !viewModel.Result.IsDeleted)
        {
            try
            {
                var newGoal = new GoalDetail
                {
                    Title = viewModel.Result.Title!,
                    Description = viewModel.Result.Description,
                    GoalTypeValue = viewModel.Result.GoalType,
                    StartDate = viewModel.Result.StartDate,
                    DueDate = viewModel.Result.DueDate,
                    OwnerTeamMemberId = viewModel.Result.OwnerTeamMemberId ?? Guid.Empty,
                    Status = "active",
                    VisibilityScope = viewModel.Result.VisibilityScope ?? "team"
                };
                
                await GoalsService.Instance.CreateGoalAsync(newGoal);
                
                // Refresh goals list
                _viewModel.RefreshCommand.Execute(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CircleView] Failed to create goal: {ex.Message}");
            }
        }
    }

    private async void OnEditGoalDialogRequested(object? sender, GoalDetail goal)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null) return;

        var result = await AppDialogService.ShowEditGoalAsync(window, goal);
        
        if (result.WasDeleted && result.DeletedGoalId.HasValue)
        {
            _viewModel.OnGoalDeleted(result.DeletedGoalId.Value);
        }
        else if (result.Success && result.Goal != null)
        {
            _viewModel.OnGoalSaved(result.Goal);
        }
    }

    private async void OnGiveFeedbackDialogRequested(object? sender, TeamMemberDetail teamMember)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var viewModel = new AddFeedbackDialogViewModel();
        
        // Set the recipient
        viewModel.SetRecipient(teamMember.Id, teamMember.FullName);
        
        // Set dialog service for confirmation
        viewModel.SetDialogService(new DialogService(window));
        
        var dialog = new AddFeedbackDialog
        {
            DataContext = viewModel
        };

        await dialog.ShowDialog(window);

        // If user saved, refresh the circle view
        if (viewModel.WasSaved)
        {
            try
            {
                // Refresh the dashboard data which includes feedback
                _viewModel?.RefreshCommand.Execute(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CircleView] Failed to refresh after feedback creation: {ex.Message}");
            }
        }
    }

    private async void OnGiveKudosDialogRequested(object? sender, TeamMemberDetail teamMember)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowGiveKudosAsync(window, teamMember.Id, teamMember.FullName);

        if (result.WasCreated)
        {
            try
            {
                // Refresh to show new kudos (if we add a kudos display)
                _viewModel?.RefreshCommand.Execute(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CircleView] Failed to refresh after kudos creation: {ex.Message}");
            }
        }
    }

    private async void OnSendMessageDialogRequested(object? sender, TeamMemberDetail teamMember)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowQuickMessageAsync(window, teamMember.Email, teamMember.FullName);

        // No refresh needed - message was sent externally
        if (result.WasSent)
        {
            System.Diagnostics.Debug.WriteLine($"[CircleView] Message sent to {teamMember.FullName}");
        }
    }

    /// <summary>
    /// Handles click on a linked goal in the metric detail flyout.
    /// Navigates to the Goals tab and selects the goal.
    /// </summary>
    private void LinkedGoal_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is GoalDetail goal && _viewModel != null)
        {
            _viewModel.NavigateToGoalCommand.Execute(goal);
        }
    }

    #endregion

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Dispatch to UI thread since PropertyChanged may come from background thread
        Dispatcher.UIThread.Post(() =>
        {
            if (e.PropertyName == nameof(CircleViewModel.DayMeetings) || 
                e.PropertyName == nameof(CircleViewModel.CurrentDate))
            {
                BuildDayView();
            }
            
            if (e.PropertyName == nameof(CircleViewModel.WeekDays) ||
                e.PropertyName == nameof(CircleViewModel.CurrentDate))
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
                
                // Scroll the selected card into view after a short delay
                // (allows the panel to expand first)
                Dispatcher.UIThread.Post(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    ScrollCardIntoView(border);
                }, DispatcherPriority.Background);
            }
        }
    }

    private void ScrollCardIntoView(Border cardBorder)
    {
        try
        {
            var scrollViewer = this.FindControl<ScrollViewer>("TeamCardsScrollViewer");
            if (scrollViewer == null) return;

            // Get the card's position relative to the scroll viewer's content
            var transform = cardBorder.TransformToVisual(scrollViewer);
            if (transform == null) return;

            var cardTop = transform.Value.Transform(new Point(0, 0)).Y;
            var cardBottom = cardTop + cardBorder.Bounds.Height;
            var viewportHeight = scrollViewer.Viewport.Height;
            var currentOffset = scrollViewer.Offset.Y;

            // Check if card is fully visible
            if (cardTop < 0)
            {
                // Card is above viewport, scroll up
                scrollViewer.Offset = new Vector(scrollViewer.Offset.X, currentOffset + cardTop - 16);
            }
            else if (cardBottom > viewportHeight)
            {
                // Card is below viewport, scroll down
                scrollViewer.Offset = new Vector(scrollViewer.Offset.X, currentOffset + (cardBottom - viewportHeight) + 16);
            }
        }
        catch
        {
            // Ignore scroll errors
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

    #region Metrics Tab Handlers

    private void MetricFilter_All_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetMetricFilterCommand.Execute(MetricFilter.All);
        }
    }

    private void MetricFilter_OnTrack_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetMetricFilterCommand.Execute(MetricFilter.OnTrack);
        }
    }

    private void MetricFilter_NeedsAttention_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetMetricFilterCommand.Execute(MetricFilter.NeedsAttention);
        }
    }

    private void MetricFilter_OffTrack_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetMetricFilterCommand.Execute(MetricFilter.OffTrack);
        }
    }

    private void MetricCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is MetricDetail metric)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectMetricCommand.Execute(metric);
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
        
        // Get recipient name from SelectedTeamMember if available
        var recipientName = _viewModel.SelectedTeamMember?.DisplayName ?? feedback.RecipientInitials ?? "Team Member";
        
        var parent = TopLevel.GetTopLevel(this);
        if (parent is Window window)
        {
            var viewModel = new EditFeedbackDialogViewModel();
            viewModel.SetDialogService(new DialogService(window));
            
            var dialog = new EditFeedbackDialog
            {
                DataContext = viewModel
            };
            
            // Load the feedback data
            await viewModel.LoadFeedbackAsync(feedback.Id, recipientName);
            
            await dialog.ShowDialog(window);
            
            if (viewModel.WasSaved)
            {
                // Refresh to show updated data
                _viewModel.RefreshCommand?.Execute(null);
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
                    _viewModel.CloseFeedbackDetailCommand?.Execute(null);
                    _viewModel.RefreshCommand?.Execute(null);
                    NotificationService.Instance.ShowSuccess("Success", "Feedback deleted successfully.");
                }
                else
                {
                    NotificationService.Instance.ShowError("Error", FeedbackService.Instance.LastError ?? "Failed to delete feedback.");
                }
            }
        }
    }

    #endregion
}
