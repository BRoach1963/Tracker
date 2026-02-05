using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;
using ProCohere.Avalonia.Views.Dialogs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Centralized service for showing application dialogs.
/// 
/// This static service encapsulates all dialog creation and display logic,
/// eliminating code duplication across views. ViewModels raise events,
/// Views subscribe and call this service with the parent Window reference.
/// 
/// Pattern:
/// - ShowCreate{Entity}Async - Create new entity
/// - ShowEdit{Entity}Async - Edit existing entity
/// 
/// All methods handle:
/// - Loading required data (team members, etc.)
/// - Configuring the dialog
/// - Processing and returning results
/// </summary>
public static class AppDialogService
{
    #region Meeting Dialogs

    /// <summary>
    /// Shows the create meeting dialog.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="preSelectedAttendee">Optional team member to pre-select for 1:1 meetings</param>
    /// <returns>Result containing the created meeting or cancellation info</returns>
    public static async Task<MeetingDialogResult> ShowCreateMeetingAsync(
        Window parentWindow, 
        TeamMemberDetail? preSelectedAttendee = null)
    {
        try
        {
            var dialog = new EditMeetingDialog();
            
            // Load team members for attendee selection (exclude self)
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers.Where(t => t.Relation != "self"));
            
            // Pre-select attendee if provided (e.g., "Schedule Meeting with John")
            if (preSelectedAttendee != null)
            {
                dialog.PreSelectAttendee(preSelectedAttendee);
            }
            
            await dialog.ShowDialog(parentWindow);
            
            if (dialog.Result == null)
            {
                return MeetingDialogResult.Cancelled();
            }
            
            if (dialog.Result.SavedMeeting != null)
            {
                // Set current user ID for ownership checks
                var currentUserId = AuthService.Instance.CurrentTeamMember?.Id;
                if (currentUserId.HasValue)
                {
                    dialog.Result.SavedMeeting.CurrentUserTeamMemberId = currentUserId;
                }
                
                return MeetingDialogResult.Created(dialog.Result.SavedMeeting);
            }
            
            if (dialog.Result.Error != null)
            {
                return MeetingDialogResult.Failed(dialog.Result.Error);
            }
            
            return MeetingDialogResult.Cancelled();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing create meeting dialog: {ex.Message}");
            return MeetingDialogResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Shows the edit meeting dialog for an existing meeting.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="meeting">The meeting to edit</param>
    /// <returns>Result containing the updated/deleted meeting or cancellation info</returns>
    public static async Task<MeetingDialogResult> ShowEditMeetingAsync(
        Window parentWindow, 
        MeetingDetail meeting)
    {
        try
        {
            var dialog = new EditMeetingDialog();
            
            // Load team members for attendee selection (exclude self)
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers.Where(t => t.Relation != "self"));
            
            // Load the existing meeting
            await dialog.LoadMeetingAsync(meeting);
            
            await dialog.ShowDialog(parentWindow);
            
            if (dialog.Result == null)
            {
                return MeetingDialogResult.Cancelled();
            }
            
            if (dialog.Result.DeletedMeetingId.HasValue)
            {
                return MeetingDialogResult.Deleted(dialog.Result.DeletedMeetingId.Value);
            }
            
            if (dialog.Result.SavedMeeting != null)
            {
                // Set current user ID for ownership checks
                var currentUserId = AuthService.Instance.CurrentTeamMember?.Id;
                if (currentUserId.HasValue)
                {
                    dialog.Result.SavedMeeting.CurrentUserTeamMemberId = currentUserId;
                }
                
                return MeetingDialogResult.Updated(dialog.Result.SavedMeeting);
            }
            
            if (dialog.Result.Error != null)
            {
                return MeetingDialogResult.Failed(dialog.Result.Error);
            }
            
            return MeetingDialogResult.Cancelled();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing edit meeting dialog: {ex.Message}");
            return MeetingDialogResult.Failed(ex.Message);
        }
    }

    #endregion

    #region Confirmation Dialogs

    /// <summary>
    /// Shows a confirmation dialog and returns the user's choice.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Message to display</param>
    /// <param name="confirmText">Text for confirm button (default: "Confirm")</param>
    /// <param name="cancelText">Text for cancel button (default: "Cancel")</param>
    /// <returns>True if user confirmed, false if cancelled</returns>
    public static async Task<bool> ShowConfirmationAsync(
        Window parentWindow,
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel")
    {
        var dialog = new ConfirmationDialog(
            title, 
            message, 
            confirmText, 
            cancelText, 
            ConfirmationDialog.ConfirmationType.Default);
        await dialog.ShowDialog(parentWindow);
        return dialog.IsConfirmed;
    }

    /// <summary>
    /// Shows a destructive action confirmation dialog (styled with danger colors).
    /// Use for delete operations and other destructive actions.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Message to display</param>
    /// <param name="confirmText">Text for confirm button (default: "Delete")</param>
    /// <param name="cancelText">Text for cancel button (default: "Cancel")</param>
    /// <returns>True if user confirmed, false if cancelled</returns>
    public static async Task<bool> ShowDestructiveConfirmationAsync(
        Window parentWindow,
        string title,
        string message,
        string confirmText = "Delete",
        string cancelText = "Cancel")
    {
        var dialog = new ConfirmationDialog(
            title, 
            message, 
            confirmText, 
            cancelText, 
            ConfirmationDialog.ConfirmationType.Destructive);
        await dialog.ShowDialog(parentWindow);
        return dialog.IsConfirmed;
    }

    #endregion

    #region Alert Dialogs

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    public static async Task ShowErrorAsync(Window parentWindow, string title, string message)
    {
        var dialog = new AlertDialog(title, message, AlertDialog.AlertType.Error);
        await dialog.ShowDialog(parentWindow);
    }

    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    public static async Task ShowInfoAsync(Window parentWindow, string title, string message)
    {
        var dialog = new AlertDialog(title, message, AlertDialog.AlertType.Information);
        await dialog.ShowDialog(parentWindow);
    }

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    public static async Task ShowWarningAsync(Window parentWindow, string title, string message)
    {
        var dialog = new AlertDialog(title, message, AlertDialog.AlertType.Warning);
        await dialog.ShowDialog(parentWindow);
    }

    /// <summary>
    /// Shows a success message dialog.
    /// </summary>
    public static async Task ShowSuccessAsync(Window parentWindow, string title, string message)
    {
        var dialog = new AlertDialog(title, message, AlertDialog.AlertType.Success);
        await dialog.ShowDialog(parentWindow);
    }

    #endregion

    #region Goal Dialogs

    /// <summary>
    /// Shows the create goal dialog.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <returns>Result containing the created goal or cancellation info</returns>
    public static async Task<GoalDialogResult> ShowCreateGoalAsync(Window parentWindow)
    {
        try
        {
            var dialog = new EditGoalDialog();
            
            // Load team members for owner selection
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers);
            
            await dialog.ShowDialog(parentWindow);
            
            if (dialog.Result == null)
            {
                return GoalDialogResult.Cancelled();
            }
            
            if (dialog.Result.IsDeleted)
            {
                return GoalDialogResult.Cancelled();
            }
            
            // Create goal detail object
            var goalToCreate = new GoalDetail
            {
                Title = dialog.Result.Title ?? string.Empty,
                Description = dialog.Result.Description,
                GoalTypeValue = dialog.Result.GoalType,
                StartDate = dialog.Result.StartDate,
                DueDate = dialog.Result.DueDate,
                OwnerTeamMemberId = dialog.Result.OwnerTeamMemberId ?? Guid.Empty,
                Status = dialog.Result.Status ?? "active"
            };
            
            // Create goal in database
            var savedGoal = await GoalsService.Instance.CreateGoalAsync(goalToCreate);
            
            if (savedGoal != null)
            {
                return GoalDialogResult.Created(savedGoal);
            }
            
            return GoalDialogResult.Failed("Failed to create goal");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing create goal dialog: {ex.Message}");
            return GoalDialogResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Shows the edit goal dialog for an existing goal.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="goal">The goal to edit</param>
    /// <returns>Result containing the updated/deleted goal or cancellation info</returns>
    public static async Task<GoalDialogResult> ShowEditGoalAsync(Window parentWindow, GoalDetail goal)
    {
        try
        {
            var dialog = new EditGoalDialog();
            
            // Load team members for owner selection
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers);
            
            // Load the existing goal
            dialog.LoadGoal(goal);
            
            await dialog.ShowDialog(parentWindow);
            
            if (dialog.Result == null)
            {
                return GoalDialogResult.Cancelled();
            }
            
            if (dialog.Result.IsDeleted && dialog.Result.Id.HasValue)
            {
                // Delete goal
                await GoalsService.Instance.DeleteGoalAsync(dialog.Result.Id.Value);
                return GoalDialogResult.Deleted(dialog.Result.Id.Value);
            }
            
            if (dialog.Result.Id.HasValue)
            {
                // Update existing goal by modifying the passed object
                goal.Title = dialog.Result.Title;
                goal.Description = dialog.Result.Description;
                goal.GoalTypeValue = dialog.Result.GoalType;
                goal.StartDate = dialog.Result.StartDate;
                goal.DueDate = dialog.Result.DueDate;
                goal.OwnerTeamMemberId = dialog.Result.OwnerTeamMemberId ?? goal.OwnerTeamMemberId;
                goal.Status = dialog.Result.Status ?? goal.Status;
                
                var updatedGoal = await GoalsService.Instance.UpdateGoalAsync(goal);
                
                if (updatedGoal != null)
                {
                    return GoalDialogResult.Updated(updatedGoal);
                }
            }
            
            return GoalDialogResult.Failed("Failed to update goal");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing edit goal dialog: {ex.Message}");
            return GoalDialogResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Shows the what-if scenario dialog for trajectory simulation.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="trajectory">Current trajectory to simulate scenarios for</param>
    public static async Task ShowWhatIfDialogAsync(Window parentWindow, TrajectoryResult trajectory)
    {
        try
        {
            var viewModel = new WhatIfDialogViewModel();
            viewModel.Initialize(trajectory);
            
            var dialog = new WhatIfDialog
            {
                DataContext = viewModel
            };
            
            await dialog.ShowDialog(parentWindow);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing what-if dialog: {ex.Message}");
        }
    }

    #endregion

    #region Task Dialogs

    /// <summary>
    /// Shows the create task dialog.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <returns>Result containing the created task or cancellation info</returns>
    public static async Task<TaskDialogResult> ShowCreateTaskAsync(Window parentWindow)
    {
        try
        {
            var dialog = new AddTaskDialog();
            
            // Load team members for assignee selection (include self for assigning tasks to yourself)
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers);
            
            await dialog.ShowDialog(parentWindow);
            
            if (dialog.Result == null)
            {
                return TaskDialogResult.Cancelled();
            }
            
            if (dialog.Result.IsDeleted)
            {
                // Shouldn't happen for create, but handle it
                return TaskDialogResult.Cancelled();
            }
            
            // Create the task in the database
            var created = await TaskService.Instance.CreateTaskAsync(
                dialog.Result.Title,
                dialog.Result.Description,
                dialog.Result.Priority,
                dialog.Result.DueDate,
                dialog.Result.AssigneeId,
                null,
                null,
                dialog.Result.GoalId,
                dialog.Result.IsRecurring,
                dialog.Result.RecurrencePattern,
                dialog.Result.RecurrenceInterval,
                dialog.Result.RecurrenceEndDate);
            
            if (created != null)
            {
                return TaskDialogResult.Created(created);
            }
            
            return TaskDialogResult.Failed(TaskService.Instance.LastError ?? "Failed to create task");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing create task dialog: {ex.Message}");
            return TaskDialogResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Shows the edit task dialog for an existing task.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="task">The task to edit</param>
    /// <returns>Result containing the updated/deleted task or cancellation info</returns>
    public static async Task<TaskDialogResult> ShowEditTaskAsync(Window parentWindow, TaskDetail task)
    {
        try
        {
            var dialog = new AddTaskDialog();
            
            // Load team members for assignee selection
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers);
            
            // Load existing task data
            dialog.LoadTask(task);
            
            await dialog.ShowDialog(parentWindow);
            
            if (dialog.Result == null)
            {
                return TaskDialogResult.Cancelled();
            }
            
            if (dialog.Result.IsDeleted && dialog.Result.Id.HasValue)
            {
                // Delete the task
                var deleted = await TaskService.Instance.DeleteTaskAsync(dialog.Result.Id.Value);
                if (deleted)
                {
                    return TaskDialogResult.Deleted(dialog.Result.Id.Value);
                }
                return TaskDialogResult.Failed(TaskService.Instance.LastError ?? "Failed to delete task");
            }
            
            // Update the task with the modified values
            task.Title = dialog.Result.Title ?? task.Title;
            task.Description = dialog.Result.Description;
            task.Priority = dialog.Result.Priority;
            task.Status = dialog.Result.Status ?? task.Status;
            task.DueDate = dialog.Result.DueDate;
            task.OwnerTeamMemberId = dialog.Result.AssigneeId;
            task.GoalId = dialog.Result.GoalId;
            task.IsRecurring = dialog.Result.IsRecurring;
            task.RecurrencePattern = dialog.Result.IsRecurring ? dialog.Result.RecurrencePattern : null;
            task.RecurrenceInterval = dialog.Result.IsRecurring ? dialog.Result.RecurrenceInterval : 1;
            task.RecurrenceEndDate = dialog.Result.IsRecurring ? dialog.Result.RecurrenceEndDate : null;
            
            var updated = await TaskService.Instance.UpdateTaskAsync(task);
            
            if (updated != null)
            {
                return TaskDialogResult.Updated(updated);
            }
            
            return TaskDialogResult.Failed(TaskService.Instance.LastError ?? "Failed to update task");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing edit task dialog: {ex.Message}");
            return TaskDialogResult.Failed(ex.Message);
        }
    }

    #endregion
    
    #region Metric Dialogs
    
    /// <summary>
    /// Shows the create metric dialog.
    /// </summary>
    /// <returns>The created metric, or null if cancelled</returns>
    public static async Task<MetricDetail?> ShowCreateMetricAsync()
    {
        try
        {
            var window = GetMainWindow();
            if (window == null) return null;
            
            var viewModel = new AddMetricDialogViewModel();
            
            // Load team members for owner selection
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            viewModel.SetTeamMembers(teamMembers);
            
            // Set dialog service for confirmation
            viewModel.SetDialogService(new DialogService(window));
            
            var dialog = new AddMetricDialog
            {
                DataContext = viewModel
            };
            
            await dialog.ShowDialog(window);
            
            // Result is null if cancelled
            if (viewModel.Result == null || viewModel.Result.IsDeleted)
            {
                return null;
            }
            
            // Create a MetricDetail from the dialog result
            var newMetric = new MetricDetail
            {
                Id = Guid.Empty, // Will be assigned by service
                Name = viewModel.Result.Name,
                Description = viewModel.Result.Description,
                CurrentValue = viewModel.Result.CurrentValue,
                TargetValue = viewModel.Result.TargetValue,
                Unit = viewModel.Result.Unit,
                TargetDirection = viewModel.Result.TargetDirection,
                Frequency = viewModel.Result.Frequency,
                OwnerTeamMemberId = viewModel.Result.OwnerTeamMemberId
            };
            
            // Create the metric in the database
            var created = await MetricsService.Instance.CreateMetricAsync(newMetric);
            return created;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing create metric dialog: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Shows the edit metric dialog for an existing metric.
    /// </summary>
    /// <param name="metric">The metric to edit</param>
    /// <returns>The updated metric, or null if cancelled</returns>
    public static async Task<MetricDetail?> ShowEditMetricAsync(MetricDetail metric)
    {
        try
        {
            var window = GetMainWindow();
            if (window == null) return null;
            
            var dialog = new EditMetricDialog();
            
            // Load team members for owner selection
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers);
            
            // Load existing metric data
            dialog.LoadMetric(metric);
            
            await dialog.ShowDialog(window);
            
            // Result is null if cancelled
            if (dialog.Result == null)
            {
                return null;
            }
            
            if (dialog.Result.IsDeleted)
            {
                // Handle deletion
                await MetricsService.Instance.DeleteMetricAsync(metric.Id);
                return null;
            }
            
            // Update the metric from dialog values
            metric.Name = dialog.Result.Name;
            metric.Description = dialog.Result.Description;
            metric.CurrentValue = dialog.Result.CurrentValue;
            metric.TargetValue = dialog.Result.TargetValue;
            metric.Unit = dialog.Result.Unit;
            metric.TargetDirection = dialog.Result.TargetDirection;
            metric.Frequency = dialog.Result.Frequency;
            metric.OwnerTeamMemberId = dialog.Result.OwnerTeamMemberId;
            
            // Update the metric in the database
            var updated = await MetricsService.Instance.UpdateMetricAsync(metric);
            return updated;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing edit metric dialog: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Shows the update metric value dialog.
    /// </summary>
    /// <param name="metric">The metric to update</param>
    /// <returns>Result containing the new value and notes, or null if cancelled</returns>
    public static async Task<Models.Dialogs.UpdateMetricValueResult?> ShowUpdateMetricValueAsync(MetricDetail metric)
    {
        try
        {
            var window = GetMainWindow();
            if (window == null) return null;
            
            var dialog = new UpdateMetricValueDialog();
            dialog.Initialize(
                metric.CurrentValue?.ToString(), 
                metric.SourceEnum == MetricSource.Manual);
            
            await dialog.ShowDialog(window);
            
            return dialog.Result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] Error showing update metric value dialog: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Gets the main application window.
    /// </summary>
    private static Window? GetMainWindow()
    {
        return global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
    
    #region Kudos Dialogs

    /// <summary>
    /// Shows the give kudos dialog.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="recipientMemberId">Team member receiving the kudos</param>
    /// <param name="recipientName">Name of the recipient for display</param>
    /// <returns>Result containing the created kudos or cancellation info</returns>
    public static async Task<KudosDialogResult> ShowGiveKudosAsync(
        Window parentWindow,
        Guid recipientMemberId,
        string recipientName)
    {
        try
        {
            var viewModel = new AddKudosDialogViewModel();
            viewModel.SetRecipient(recipientMemberId, recipientName);
            
            var dialog = new AddKudosDialog
            {
                DataContext = viewModel
            };
            
            // Set up dialog service for confirmations
            var dialogService = new DialogService(parentWindow);
            viewModel.SetDialogService(dialogService);
            
            await dialog.ShowDialog(parentWindow);
            
            if (viewModel.WasSaved && viewModel.CreatedKudos != null)
            {
                return KudosDialogResult.Created(viewModel.CreatedKudos);
            }
            
            return KudosDialogResult.Cancelled();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] ShowGiveKudosAsync error: {ex}");
            return KudosDialogResult.Failed($"Failed to show kudos dialog: {ex.Message}");
        }
    }

    #endregion

    #region Quick Message Dialogs

    /// <summary>
    /// Shows the quick message dialog.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <param name="recipientEmail">Email address of the recipient</param>
    /// <param name="recipientName">Name of the recipient for display</param>
    /// <returns>Result containing send status or cancellation info</returns>
    public static async Task<MessageResult> ShowQuickMessageAsync(
        Window parentWindow,
        string recipientEmail,
        string recipientName)
    {
        try
        {
            var viewModel = new QuickMessageDialogViewModel();
            viewModel.SetRecipient(recipientEmail, recipientName);
            
            var dialog = new QuickMessageDialog
            {
                DataContext = viewModel
            };
            
            await dialog.ShowDialog(parentWindow);
            
            return viewModel.Result ?? MessageResult.Cancelled();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] ShowQuickMessageAsync error: {ex}");
            return MessageResult.Failed($"Failed to show message dialog: {ex.Message}");
        }
    }

    #endregion

    #region Survey Dialogs

    /// <summary>
    /// Shows the create survey dialog.
    /// </summary>
    /// <param name="parentWindow">Parent window for modal dialog</param>
    /// <returns>Created survey or null if cancelled</returns>
    public static async Task<Survey?> ShowCreateSurveyAsync(Window parentWindow)
    {
        try
        {
            var viewModel = new CreateSurveyDialogViewModel();
            
            var dialog = new CreateSurveyDialog
            {
                DataContext = viewModel
            };
            
            await dialog.ShowDialog(parentWindow);
            
            if (viewModel.Result != null)
            {
                // Save to database
                var created = await SurveyService.Instance.CreateSurveyAsync(viewModel.Result);
                
                if (created == null)
                {
                    // Show error if save failed
                    await ShowConfirmationAsync(
                        parentWindow,
                        "Survey Creation Failed",
                        $"Failed to create survey: {SurveyService.Instance.LastError}",
                        "OK");
                    return null;
                }
                
                return created;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDialogService] ShowCreateSurveyAsync error: {ex}");
            await ShowConfirmationAsync(
                parentWindow,
                "Error",
                $"Failed to show survey dialog: {ex.Message}",
                "OK");
            return null;
        }
    }

    #endregion
    
    #endregion
}

#region Result Types

/// <summary>
/// Result from a meeting dialog operation.
/// </summary>
public class MeetingDialogResult
{
    /// <summary>
    /// The meeting that was created or updated (null if cancelled/deleted).
    /// </summary>
    public MeetingDetail? Meeting { get; init; }
    
    /// <summary>
    /// The ID of the meeting that was deleted (null if not deleted).
    /// </summary>
    public Guid? DeletedMeetingId { get; init; }
    
    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// True if the user cancelled the dialog.
    /// </summary>
    public bool WasCancelled { get; init; }
    
    /// <summary>
    /// True if a meeting was created (not edited).
    /// </summary>
    public bool WasCreated { get; init; }
    
    /// <summary>
    /// True if a meeting was updated (not created).
    /// </summary>
    public bool WasUpdated { get; init; }
    
    /// <summary>
    /// True if a meeting was deleted.
    /// </summary>
    public bool WasDeleted => DeletedMeetingId.HasValue;
    
    /// <summary>
    /// True if the operation was successful (created, updated, or deleted).
    /// </summary>
    public bool Success => WasCreated || WasUpdated || WasDeleted;

    // Factory methods for clean result creation
    
    public static MeetingDialogResult Created(MeetingDetail meeting) => new()
    {
        Meeting = meeting,
        WasCreated = true
    };
    
    public static MeetingDialogResult Updated(MeetingDetail meeting) => new()
    {
        Meeting = meeting,
        WasUpdated = true
    };
    
    public static MeetingDialogResult Deleted(Guid meetingId) => new()
    {
        DeletedMeetingId = meetingId
    };
    
    public static MeetingDialogResult Cancelled() => new()
    {
        WasCancelled = true
    };
    
    public static MeetingDialogResult Failed(string error) => new()
    {
        Error = error
    };
}

/// <summary>
/// Result from a task dialog operation.
/// </summary>
public class TaskDialogResult
{
    /// <summary>
    /// The task that was created or updated (null if cancelled/deleted).
    /// </summary>
    public TaskDetail? Task { get; init; }
    
    /// <summary>
    /// The ID of the task that was deleted (null if not deleted).
    /// </summary>
    public Guid? DeletedTaskId { get; init; }
    
    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// True if the user cancelled the dialog.
    /// </summary>
    public bool WasCancelled { get; init; }
    
    /// <summary>
    /// True if a task was created (not edited).
    /// </summary>
    public bool WasCreated { get; init; }
    
    /// <summary>
    /// True if a task was updated (not created).
    /// </summary>
    public bool WasUpdated { get; init; }
    
    /// <summary>
    /// True if a task was deleted.
    /// </summary>
    public bool WasDeleted => DeletedTaskId.HasValue;
    
    /// <summary>
    /// True if the operation was successful (created, updated, or deleted).
    /// </summary>
    public bool Success => WasCreated || WasUpdated || WasDeleted;

    // Factory methods for clean result creation
    
    public static TaskDialogResult Created(TaskDetail task) => new()
    {
        Task = task,
        WasCreated = true
    };
    
    public static TaskDialogResult Updated(TaskDetail task) => new()
    {
        Task = task,
        WasUpdated = true
    };
    
    public static TaskDialogResult Deleted(Guid taskId) => new()
    {
        DeletedTaskId = taskId
    };
    
    public static TaskDialogResult Cancelled() => new()
    {
        WasCancelled = true
    };
    
    public static TaskDialogResult Failed(string error) => new()
    {
        Error = error
    };
}

/// <summary>
/// Result from a goal dialog operation.
/// </summary>
public class GoalDialogResult
{
    /// <summary>
    /// The goal that was created or updated (null if cancelled/deleted).
    /// </summary>
    public GoalDetail? Goal { get; init; }
    
    /// <summary>
    /// The ID of the goal that was deleted (null if not deleted).
    /// </summary>
    public Guid? DeletedGoalId { get; init; }
    
    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// True if the user cancelled the dialog.
    /// </summary>
    public bool WasCancelled { get; init; }
    
    /// <summary>
    /// True if a goal was created (not edited).
    /// </summary>
    public bool WasCreated { get; init; }
    
    /// <summary>
    /// True if a goal was updated (not created).
    /// </summary>
    public bool WasUpdated { get; init; }
    
    /// <summary>
    /// True if a goal was deleted.
    /// </summary>
    public bool WasDeleted => DeletedGoalId.HasValue;
    
    /// <summary>
    /// True if the operation was successful (created, updated, or deleted).
    /// </summary>
    public bool Success => WasCreated || WasUpdated || WasDeleted;

    // Factory methods for clean result creation
    
    public static GoalDialogResult Created(GoalDetail goal) => new()
    {
        Goal = goal,
        WasCreated = true
    };
    
    public static GoalDialogResult Updated(GoalDetail goal) => new()
    {
        Goal = goal,
        WasUpdated = true
    };
    
    public static GoalDialogResult Deleted(Guid goalId) => new()
    {
        DeletedGoalId = goalId
    };
    
    public static GoalDialogResult Cancelled() => new()
    {
        WasCancelled = true
    };
    
    public static GoalDialogResult Failed(string error) => new()
    {
        Error = error
    };
}

/// <summary>
/// Result from a kudos dialog operation.
/// </summary>
public class KudosDialogResult
{
    /// <summary>
    /// The kudos that was created (null if cancelled).
    /// </summary>
    public Kudos? Kudos { get; init; }
    
    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// True if the user cancelled the dialog.
    /// </summary>
    public bool WasCancelled { get; init; }
    
    /// <summary>
    /// True if kudos was created.
    /// </summary>
    public bool WasCreated { get; init; }
    
    /// <summary>
    /// True if the operation was successful.
    /// </summary>
    public bool Success => WasCreated;

    // Factory methods for clean result creation
    
    public static KudosDialogResult Created(Kudos kudos) => new()
    {
        Kudos = kudos,
        WasCreated = true
    };
    
    public static KudosDialogResult Cancelled() => new()
    {
        WasCancelled = true
    };
    
    public static KudosDialogResult Failed(string error) => new()
    {
        Error = error
    };
}

#endregion

#region About Dialog

/// <summary>
/// Shows the About dialog.
/// </summary>
public static async Task ShowAboutDialogAsync(Window parentWindow)
{
    var dialog = new AboutDialog();
    var vm = new AboutDialogViewModel();
    
    vm.CloseRequested += () => dialog.Close();
    
    dialog.DataContext = vm;
    await dialog.ShowDialog(parentWindow);
}

#endregion
