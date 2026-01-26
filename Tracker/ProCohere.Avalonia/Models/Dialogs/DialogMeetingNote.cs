using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Meeting note model for the dialog that supports inline editing and tagging.
/// Wraps MeetingNote with UI-specific state.
/// </summary>
public partial class DialogMeetingNote : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.Empty;
    
    [ObservableProperty]
    private Guid _meetingId;
    
    [ObservableProperty]
    private Guid _authorId;
    
    /// <summary>
    /// Tracks whether this note has been modified and needs to be persisted.
    /// </summary>
    [ObservableProperty]
    private bool _isDirty;
    
    /// <summary>
    /// Whether this note is currently being edited inline.
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;
    
    /// <summary>
    /// The note content.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContent))]
    [NotifyPropertyChangedFor(nameof(ContentPreview))]
    private string _content = string.Empty;
    
    /// <summary>
    /// Temporary content while editing (before save).
    /// </summary>
    [ObservableProperty]
    private string _editContent = string.Empty;
    
    /// <summary>
    /// Whether this note is shared with all attendees.
    /// false = personal (private to author)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibilityIcon))]
    [NotifyPropertyChangedFor(nameof(VisibilityTooltip))]
    private bool _isShared;
    
    /// <summary>
    /// Tags assigned to this note.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTags))]
    private List<NoteTag> _tags = new();
    
    [ObservableProperty]
    private DateTime _createdAt = DateTime.UtcNow;
    
    [ObservableProperty]
    private DateTime _updatedAt = DateTime.UtcNow;
    
    /// <summary>
    /// Author's name for display (set from lookup).
    /// </summary>
    [ObservableProperty]
    private string? _authorName;
    
    #region Computed Properties
    
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);
    public bool HasTags => Tags.Count > 0;
    
    /// <summary>
    /// Preview of the content (first 150 chars).
    /// </summary>
    public string ContentPreview => Content.Length > 150 
        ? Content.Substring(0, 150) + "..." 
        : Content;
    
    /// <summary>
    /// Display timestamp.
    /// </summary>
    public string TimestampDisplay => UpdatedAt.ToLocalTime().ToString("MMM d, h:mm tt");
    
    /// <summary>
    /// Display for created timestamp.
    /// </summary>
    public string CreatedDisplay => CreatedAt.ToLocalTime().ToString("MMM d, h:mm tt");
    
    /// <summary>
    /// Whether this was recently updated (different from created).
    /// </summary>
    public bool WasEdited => (UpdatedAt - CreatedAt).TotalMinutes > 1;
    
    /// <summary>
    /// Icon for visibility.
    /// </summary>
    public string VisibilityIcon => IsShared
        ? "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z"  // Person icon
        : "M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17M18,8A2,2 0 0,1 20,10V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V10C4,8.89 4.9,8 6,8H7V6A5,5 0 0,1 12,1A5,5 0 0,1 17,6V8H18M12,3A3,3 0 0,0 9,6V8H15V6A3,3 0 0,0 12,3Z";  // Lock icon
    
    public string VisibilityTooltip => IsShared 
        ? "Shared with all attendees" 
        : "Private - only you can see this";
    
    #endregion
    
    #region Factory Methods
    
    /// <summary>
    /// Creates a DialogMeetingNote from a MeetingNote entity.
    /// </summary>
    public static DialogMeetingNote FromMeetingNote(MeetingNote note, string? authorName = null)
    {
        return new DialogMeetingNote
        {
            Id = note.Id,
            MeetingId = note.MeetingId,
            AuthorId = note.AuthorId,
            Content = note.Content,
            EditContent = note.Content,
            IsShared = note.IsShared,
            Tags = TagsFromCategories(note.Tags),
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            AuthorName = authorName,
            IsDirty = false,
            IsEditing = false
        };
    }
    
    /// <summary>
    /// Creates a new empty note for a meeting.
    /// </summary>
    public static DialogMeetingNote CreateNew(Guid meetingId, Guid authorId, string? authorName = null, bool isShared = false)
    {
        return new DialogMeetingNote
        {
            Id = Guid.Empty,  // New note - no ID yet
            MeetingId = meetingId,
            AuthorId = authorId,
            Content = string.Empty,
            EditContent = string.Empty,
            IsShared = isShared,
            Tags = new List<NoteTag>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AuthorName = authorName,
            IsDirty = true,  // New notes are dirty
            IsEditing = true  // Start in edit mode
        };
    }
    
    /// <summary>
    /// Converts category strings from database to NoteTag objects.
    /// </summary>
    private static List<NoteTag> TagsFromCategories(List<string>? categories)
    {
        if (categories == null || categories.Count == 0)
            return new List<NoteTag>();
        
        return NoteTag.StandardTags
            .Where(t => categories.Contains(t.Category))
            .ToList();
    }
    
    /// <summary>
    /// Gets the category strings for database storage.
    /// </summary>
    public List<string> GetTagCategories()
    {
        return Tags.Select(t => t.Category).ToList();
    }
    
    #endregion
    
    #region Edit Methods
    
    /// <summary>
    /// Start editing this note.
    /// </summary>
    public void BeginEdit()
    {
        EditContent = Content;
        IsEditing = true;
    }
    
    /// <summary>
    /// Cancel editing and revert changes.
    /// </summary>
    public void CancelEdit()
    {
        EditContent = Content;
        IsEditing = false;
    }
    
    /// <summary>
    /// Confirm editing and apply changes.
    /// </summary>
    public void ConfirmEdit()
    {
        if (EditContent != Content)
        {
            Content = EditContent;
            UpdatedAt = DateTime.UtcNow;
            IsDirty = true;
        }
        IsEditing = false;
    }
    
    #endregion
}

/// <summary>
/// Tag that can be applied to meeting notes.
/// </summary>
public class NoteTag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;  // "action", "decision", "question", "followup", "blocker", "idea", "risk"
    public string Color { get; set; } = "#6B7280";  // Default gray
    public string Icon { get; set; } = string.Empty;
    
    /// <summary>
    /// Standard tags available by default.
    /// </summary>
    public static readonly List<NoteTag> StandardTags = new()
    {
        new NoteTag { Id = Guid.Parse("00000000-0000-0000-0001-000000000001"), Name = "Action Item", Category = "action", Color = "#EF4444", Icon = "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M11,16.5L18,9.5L16.59,8.09L11,13.67L7.91,10.59L6.5,12L11,16.5Z" },
        new NoteTag { Id = Guid.Parse("00000000-0000-0000-0001-000000000002"), Name = "Decision", Category = "decision", Color = "#10B981", Icon = "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z" },
        new NoteTag { Id = Guid.Parse("00000000-0000-0000-0001-000000000003"), Name = "Question", Category = "question", Color = "#F59E0B", Icon = "M10,19H13V22H10V19M12,2C17.35,2.22 19.68,7.62 16.5,11.67C15.67,12.67 14.33,13.33 13.67,14.17C13,15 13,16 13,17H10C10,15.33 10,13.92 10.67,12.92C11.33,11.92 12.67,11.33 13.5,10.67C15.92,8.43 15.32,5.26 12,5A3,3 0 0,0 9,8H6A6,6 0 0,1 12,2Z" },
        new NoteTag { Id = Guid.Parse("00000000-0000-0000-0001-000000000004"), Name = "Follow-up", Category = "followup", Color = "#8B5CF6", Icon = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M16.2,16.2L11,13V7H12.5V12.2L17,14.9L16.2,16.2Z" },
        new NoteTag { Id = Guid.Parse("00000000-0000-0000-0001-000000000005"), Name = "Blocker", Category = "blocker", Color = "#DC2626", Icon = "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M12,6A6,6 0 0,1 18,12A6,6 0 0,1 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6M12,8A4,4 0 0,0 8,12A4,4 0 0,0 12,16A4,4 0 0,0 16,12A4,4 0 0,0 12,8Z" },
        new NoteTag { Id = Guid.Parse("00000000-0000-0000-0001-000000000006"), Name = "Idea", Category = "idea", Color = "#3B82F6", Icon = "M12,2A7,7 0 0,0 5,9C5,11.38 6.19,13.47 8,14.74V17A1,1 0 0,0 9,18H15A1,1 0 0,0 16,17V14.74C17.81,13.47 19,11.38 19,9A7,7 0 0,0 12,2M9,21A1,1 0 0,0 10,22H14A1,1 0 0,0 15,21V20H9V21Z" },
        new NoteTag { Id = Guid.Parse("00000000-0000-0000-0001-000000000007"), Name = "Risk", Category = "risk", Color = "#F97316", Icon = "M12,2L1,21H23M12,6L19.53,19H4.47M11,10V14H13V10M11,16V18H13V16" }
    };
}
