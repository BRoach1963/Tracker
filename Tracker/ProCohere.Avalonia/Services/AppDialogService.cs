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

    #region Goal Dialogs (Future)

    // TODO: Implement when goal dialogs are needed
    // public static Task<GoalDialogResult> ShowCreateGoalAsync(Window parentWindow, TeamMemberDetail? owner = null);
    // public static Task<GoalDialogResult> ShowEditGoalAsync(Window parentWindow, GoalDetail goal);

    #endregion

    #region Task Dialogs (Future)

    // TODO: Implement when task dialogs are needed
    // public static Task<TaskDialogResult> ShowCreateTaskAsync(Window parentWindow, MeetingDetail? relatedMeeting = null);
    // public static Task<TaskDialogResult> ShowEditTaskAsync(Window parentWindow, TaskDetail task);

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

#endregion
