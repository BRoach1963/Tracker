using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result from the edit meeting dialog.
/// Contains either the saved/updated meeting, or deletion info.
/// </summary>
public class EditMeetingResult
{
    /// <summary>True if the meeting was deleted.</summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>The saved meeting (null if cancelled or deleted).</summary>
    public MeetingDetail? SavedMeeting { get; set; }
    
    /// <summary>The ID of the deleted meeting (if IsDeleted).</summary>
    public Guid? DeletedMeetingId { get; set; }
    
    /// <summary>Error message if save failed.</summary>
    public string? Error { get; set; }
    
    /// <summary>Creates a successful save result.</summary>
    public static EditMeetingResult Success(MeetingDetail meeting) => new()
    {
        SavedMeeting = meeting,
        IsDeleted = false
    };
    
    /// <summary>Creates a deleted result.</summary>
    public static EditMeetingResult Deleted(Guid meetingId) => new()
    {
        IsDeleted = true,
        DeletedMeetingId = meetingId
    };
    
    /// <summary>Creates an error result.</summary>
    public static EditMeetingResult Failed(string error) => new()
    {
        Error = error
    };
}
