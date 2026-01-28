using Avalonia.Controls;
using ProCohere.Avalonia.Models;
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

    #region Goal Dialogs (Future)

    // TODO: Implement when goal dialogs are needed
    // public static Task<GoalDialogResult> ShowCreateGoalAsync(Window parentWindow, TeamMemberDetail? owner = null);
    // public static Task<GoalDialogResult> ShowEditGoalAsync(Window parentWindow, GoalDetail goal);

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
                dialog.Result.AssigneeId);
            
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
            task.Title = dialog.Result.Title;
            task.Description = dialog.Result.Description;
            task.Priority = dialog.Result.Priority;
            task.Status = dialog.Result.Status;
            task.DueDate = dialog.Result.DueDate;
            task.OwnerTeamMemberId = dialog.Result.AssigneeId;
            
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
            
            var dialog = new EditMetricDialog();
            
            // Load team members for owner selection
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            dialog.SetTeamMembers(teamMembers);
            
            await dialog.ShowDialog(window);
            
            // Result is null if cancelled
            if (dialog.Result == null)
            {
                return null;
            }
            
            // Create a MetricDetail from the dialog result
            var newMetric = new MetricDetail
            {
                Id = Guid.Empty, // Will be assigned by service
                Name = dialog.Result.Name,
                Description = dialog.Result.Description,
                CurrentValue = dialog.Result.CurrentValue,
                TargetValue = dialog.Result.TargetValue,
                Unit = dialog.Result.Unit,
                TargetDirection = dialog.Result.TargetDirection,
                Frequency = dialog.Result.Frequency,
                OwnerTeamMemberId = dialog.Result.OwnerTeamMemberId
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

#endregion
