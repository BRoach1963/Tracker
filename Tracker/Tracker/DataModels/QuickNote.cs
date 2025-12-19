using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A quick note or journal entry for capturing thoughts, observations, and reminders.
    /// Notes can optionally be linked to any entity (team member, project, OKR, KPI, etc.)
    /// using polymorphic linking.
    /// </summary>
    public class QuickNote : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Optional title for the note. If empty, the content preview is used.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The note content/text.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Category of the note.
        /// </summary>
        public NoteCategory Category { get; set; } = NoteCategory.General;

        #region Polymorphic Linking

        /// <summary>
        /// Type of entity this note is linked to (None for standalone notes).
        /// </summary>
        public NoteLinkedEntityType LinkedEntityType { get; set; } = NoteLinkedEntityType.None;

        /// <summary>
        /// ID of the linked entity. Null for standalone notes.
        /// </summary>
        public int? LinkedEntityId { get; set; }

        #endregion

        #region Legacy FK Properties (for backward compatibility and navigation)

        /// <summary>
        /// Optional: Link to a team member this note is about.
        /// Populated when LinkedEntityType == TeamMember.
        /// </summary>
        public int? TeamMemberId { get; set; }
        public TeamMember? TeamMember { get; set; }

        /// <summary>
        /// Optional: Link to a project this note is about.
        /// Populated when LinkedEntityType == Project.
        /// </summary>
        public int? ProjectId { get; set; }

        /// <summary>
        /// Optional: Link to a 1:1 meeting this note is related to.
        /// Populated when LinkedEntityType == OneOnOne.
        /// </summary>
        public int? OneOnOneId { get; set; }

        #endregion

        #region State Properties

        /// <summary>
        /// Whether this note is pinned (shows at top).
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Whether this note is archived (hidden from main view).
        /// </summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// Tags for easy filtering (comma-separated).
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Display title - uses Title if set, otherwise content preview.
        /// </summary>
        public string DisplayTitle => !string.IsNullOrWhiteSpace(Title) 
            ? Title 
            : (Content.Length > 50 ? Content.Substring(0, 50) + "..." : Content);

        /// <summary>
        /// Display helper for category.
        /// </summary>
        public string CategoryDisplay => Category.ToString();

        /// <summary>
        /// Display helper for linked entity type.
        /// </summary>
        public string LinkedEntityTypeDisplay => LinkedEntityType == NoteLinkedEntityType.None 
            ? string.Empty 
            : LinkedEntityType.ToString();

        /// <summary>
        /// Display helper for related entity - shows what the note is linked to.
        /// </summary>
        public string LinkedToDisplay
        {
            get
            {
                return LinkedEntityType switch
                {
                    NoteLinkedEntityType.TeamMember when TeamMember != null => 
                        $"{TeamMember.FirstName} {TeamMember.LastName}",
                    NoteLinkedEntityType.TeamMember => "Team Member",
                    NoteLinkedEntityType.Project => "Project",
                    NoteLinkedEntityType.OneOnOne => "1:1 Meeting",
                    NoteLinkedEntityType.OKR => "OKR",
                    NoteLinkedEntityType.KeyResult => "Key Result",
                    NoteLinkedEntityType.KPI => "KPI",
                    NoteLinkedEntityType.Task => "Task",
                    NoteLinkedEntityType.Goal => "Goal",
                    NoteLinkedEntityType.Feedback => "Feedback",
                    _ => string.Empty
                };
            }
        }

        /// <summary>
        /// Whether this note is linked to any entity.
        /// </summary>
        public bool HasLinkedEntity => LinkedEntityType != NoteLinkedEntityType.None && LinkedEntityId.HasValue;

        /// <summary>
        /// Preview of content (first 100 chars).
        /// </summary>
        public string Preview => Content.Length > 100 ? Content.Substring(0, 100) + "..." : Content;

        /// <summary>
        /// List of tags as an array.
        /// </summary>
        public string[] TagList => string.IsNullOrWhiteSpace(Tags) 
            ? Array.Empty<string>() 
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        /// <summary>
        /// Display string for when the note was created.
        /// </summary>
        public string CreatedDisplay
        {
            get
            {
                var days = (DateTime.Now - CreatedAt).TotalDays;
                return days switch
                {
                    < 1 => "Today",
                    < 2 => "Yesterday",
                    < 7 => $"{(int)days} days ago",
                    < 30 => $"{(int)(days / 7)} weeks ago",
                    _ => CreatedAt.ToString("MMM dd, yyyy")
                };
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sets the linked entity using the polymorphic approach.
        /// Also sets legacy FK for backward compatibility.
        /// </summary>
        public void SetLinkedEntity(NoteLinkedEntityType entityType, int? entityId)
        {
            LinkedEntityType = entityType;
            LinkedEntityId = entityId;

            // Set legacy FKs for backward compatibility
            TeamMemberId = entityType == NoteLinkedEntityType.TeamMember ? entityId : null;
            ProjectId = entityType == NoteLinkedEntityType.Project ? entityId : null;
            OneOnOneId = entityType == NoteLinkedEntityType.OneOnOne ? entityId : null;
        }

        /// <summary>
        /// Clears any linked entity.
        /// </summary>
        public void ClearLinkedEntity()
        {
            LinkedEntityType = NoteLinkedEntityType.None;
            LinkedEntityId = null;
            TeamMemberId = null;
            ProjectId = null;
            OneOnOneId = null;
            TeamMember = null;
        }

        #endregion
    }
}
