using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A quick note or journal entry for capturing thoughts, observations, and reminders.
    /// Notes can optionally be linked to any entity (team member, project, goal, task, meeting)
    /// using explicit foreign key columns.
    /// Maps to Supabase 'notes' table (29 columns after ALTER).
    /// </summary>
    [Table("notes")]
    public class QuickNote : AuditableEntity
    {
        #region Primary Key & Foreign Keys

        /// <summary>
        /// Primary key.
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// The organization this note belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// The team member who authored/created this note.
        /// Maps to: author_team_member_id UUID NOT NULL
        /// </summary>
        [Column("author_team_member_id")]
        public Guid? AuthorTeamMemberId { get; set; }

        #endregion

        #region Content

        /// <summary>
        /// Optional title for the note. If empty, the content preview is used.
        /// Maps to: title VARCHAR(300) NULL
        /// </summary>
        [Column("title")]
        [MaxLength(300)]
        public string? Title { get; set; }

        /// <summary>
        /// The note content/text.
        /// Maps to: content TEXT NOT NULL
        /// </summary>
        [Column("content")]
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Content format: plain, markdown, html.
        /// Maps to: content_format VARCHAR(50) NOT NULL DEFAULT 'plain'
        /// </summary>
        [Column("content_format")]
        [MaxLength(50)]
        public string ContentFormat { get; set; } = "plain";

        /// <summary>
        /// Category of the note (stored as string).
        /// Maps to: category VARCHAR(100) NULL
        /// </summary>
        [Column("category")]
        [MaxLength(100)]
        public string? CategoryString { get; set; }

        /// <summary>
        /// Category enum (computed from CategoryString).
        /// </summary>
        [NotMapped]
        public NoteCategory Category
        {
            get => Enum.TryParse<NoteCategory>(CategoryString, true, out var cat) ? cat : NoteCategory.General;
            set => CategoryString = value.ToString();
        }

        /// <summary>
        /// Tags as JSON array.
        /// Maps to: tags JSONB NULL
        /// </summary>
        [Column("tags")]
        public string? TagsJson { get; set; }

        #endregion

        #region Linked Entity Foreign Keys (Explicit FKs)

        /// <summary>
        /// Link to a team member this note is about.
        /// Maps to: linked_team_member_id UUID NULL
        /// </summary>
        [Column("linked_team_member_id")]
        public Guid? LinkedTeamMemberId { get; set; }

        /// <summary>
        /// Link to a meeting this note is related to.
        /// Maps to: linked_meeting_id UUID NULL
        /// </summary>
        [Column("linked_meeting_id")]
        public Guid? LinkedMeetingId { get; set; }

        /// <summary>
        /// Link to a project this note is about.
        /// Maps to: linked_project_id UUID NULL
        /// </summary>
        [Column("linked_project_id")]
        public Guid? LinkedProjectId { get; set; }

        /// <summary>
        /// Link to a goal this note is about.
        /// Maps to: linked_goal_id UUID NULL
        /// </summary>
        [Column("linked_goal_id")]
        public Guid? LinkedGoalId { get; set; }

        /// <summary>
        /// Link to a task this note is about.
        /// Maps to: linked_task_id UUID NULL
        /// </summary>
        [Column("linked_task_id")]
        public Guid? LinkedTaskId { get; set; }

        #endregion

        #region State Properties

        /// <summary>
        /// Whether this note is private (only visible to author).
        /// Maps to: is_private BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_private")]
        public bool IsPrivate { get; set; } = true;

        /// <summary>
        /// Whether this note is pinned (shows at top).
        /// Maps to: is_pinned BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_pinned")]
        public bool IsPinned { get; set; }

        /// <summary>
        /// When the note was pinned.
        /// Maps to: pinned_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("pinned_at")]
        public DateTime? PinnedAt { get; set; }

        /// <summary>
        /// Whether this note is archived (hidden from main view).
        /// Maps to: is_archived BOOLEAN NOT NULL DEFAULT false (ADDED)
        /// </summary>
        [Column("is_archived")]
        public bool IsArchived { get; set; }

        /// <summary>
        /// When the note was archived.
        /// Maps to: archived_at TIMESTAMPTZ NULL (ADDED)
        /// </summary>
        [Column("archived_at")]
        public DateTime? ArchivedAt { get; set; }

        #endregion

        #region AI Features

        /// <summary>
        /// AI-generated summary of the note.
        /// Maps to: ai_summary TEXT NULL
        /// </summary>
        [Column("ai_summary")]
        public string? AiSummary { get; set; }

        /// <summary>
        /// AI-suggested actions as JSON.
        /// Maps to: ai_suggested_actions JSONB NULL
        /// </summary>
        [Column("ai_suggested_actions")]
        public string? AiSuggestedActionsJson { get; set; }

        #endregion

        #region Offline Sync

        /// <summary>
        /// Unique ID for offline sync.
        /// Maps to: sync_id UUID NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("sync_id")]
        public Guid? SyncId { get; set; }

        /// <summary>
        /// Version number for conflict resolution.
        /// Maps to: sync_version INT4 NULL DEFAULT 1
        /// </summary>
        [Column("sync_version")]
        public int? SyncVersion { get; set; } = 1;

        /// <summary>
        /// Last sync modification time.
        /// Maps to: sync_modified_at TIMESTAMPTZ NULL DEFAULT now()
        /// </summary>
        [Column("sync_modified_at")]
        public DateTime? SyncModifiedAt { get; set; }

        /// <summary>
        /// Sync status: synced, pending, conflict.
        /// Maps to: sync_status sync_status (enum) NULL DEFAULT 'synced'
        /// </summary>
        [Column("sync_status")]
        [MaxLength(50)]
        public string? SyncStatus { get; set; } = "synced";

        #endregion

        #region Navigation Properties

        /// <summary>
        /// The author of this note.
        /// </summary>
        public TeamMember? Author { get; set; }

        /// <summary>
        /// The team member this note is linked to.
        /// </summary>
        public TeamMember? LinkedTeamMember { get; set; }

        /// <summary>
        /// The meeting this note is linked to.
        /// </summary>
        public Meeting? LinkedMeeting { get; set; }

        #endregion

        #region Computed Properties (Not Mapped)

        /// <summary>
        /// Determines the linked entity type based on which FK is populated.
        /// Used by ViewModel for filtering.
        /// </summary>
        [NotMapped]
        public NoteLinkedEntityType LinkedEntityType
        {
            get
            {
                if (LinkedTeamMemberId.HasValue) return NoteLinkedEntityType.TeamMember;
                if (LinkedMeetingId.HasValue) return NoteLinkedEntityType.Meeting;
                if (LinkedProjectId.HasValue) return NoteLinkedEntityType.Project;
                if (LinkedGoalId.HasValue) return NoteLinkedEntityType.Goal;
                if (LinkedTaskId.HasValue) return NoteLinkedEntityType.Task;
                return NoteLinkedEntityType.None;
            }
        }

        /// <summary>
        /// Display title - uses Title if set, otherwise content preview.
        /// </summary>
        [NotMapped]
        public string DisplayTitle => !string.IsNullOrWhiteSpace(Title) 
            ? Title 
            : (Content.Length > 50 ? Content.Substring(0, 50) + "..." : Content);

        /// <summary>
        /// Display helper for category.
        /// </summary>
        [NotMapped]
        public string CategoryDisplay => Category.ToString();

        /// <summary>
        /// Display helper for linked entity type.
        /// </summary>
        [NotMapped]
        public string LinkedEntityTypeDisplay => LinkedEntityType == NoteLinkedEntityType.None 
            ? string.Empty 
            : LinkedEntityType.ToString();

        /// <summary>
        /// Display helper for related entity - shows what the note is linked to.
        /// </summary>
        [NotMapped]
        public string LinkedToDisplay
        {
            get
            {
                return LinkedEntityType switch
                {
                    NoteLinkedEntityType.TeamMember when LinkedTeamMember != null => 
                        $"{LinkedTeamMember.FirstName} {LinkedTeamMember.LastName}",
                    NoteLinkedEntityType.TeamMember => "Team Member",
                    NoteLinkedEntityType.Project => "Project",
                    NoteLinkedEntityType.Meeting => "Meeting",
                    NoteLinkedEntityType.Goal => "Goal",
                    NoteLinkedEntityType.Task => "Task",
                    _ => string.Empty
                };
            }
        }

        /// <summary>
        /// Whether this note is linked to any entity.
        /// </summary>
        [NotMapped]
        public bool HasLinkedEntity => LinkedEntityType != NoteLinkedEntityType.None;

        /// <summary>
        /// Preview of content (first 100 chars).
        /// </summary>
        [NotMapped]
        public string Preview => Content.Length > 100 ? Content.Substring(0, 100) + "..." : Content;

        /// <summary>
        /// List of tags as an array (parsed from TagsJson).
        /// </summary>
        [NotMapped]
        public string[] TagList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TagsJson)) return Array.Empty<string>();
                // Simple JSON array parsing - tags stored as ["tag1", "tag2"]
                var trimmed = TagsJson.Trim('[', ']');
                if (string.IsNullOrWhiteSpace(trimmed)) return Array.Empty<string>();
                return trimmed.Split(',')
                    .Select(t => t.Trim().Trim('"'))
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToArray();
            }
        }

        /// <summary>
        /// Tags as a comma-separated string (convenience property).
        /// </summary>
        [NotMapped]
        public string Tags
        {
            get => string.Join(", ", TagList);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    TagsJson = null;
                }
                else
                {
                    var tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToArray();
                    TagsJson = "[" + string.Join(",", tags.Select(t => $"\"{t}\"")) + "]";
                }
            }
        }

        /// <summary>
        /// TeamMemberId alias for LinkedTeamMemberId (backward compatibility).
        /// </summary>
        [NotMapped]
        public Guid? TeamMemberId
        {
            get => LinkedTeamMemberId;
            set => LinkedTeamMemberId = value;
        }

        /// <summary>
        /// Generic LinkedEntityId computed from whichever FK is set (for backward compatibility).
        /// Returns hash code of Guid for legacy int-based code.
        /// </summary>
        [NotMapped]
        public int? LinkedEntityId
        {
            get
            {
                var guid = LinkedProjectId ?? LinkedGoalId ?? LinkedTaskId ?? LinkedMeetingId;
                return guid?.GetHashCode();
            }
        }

        /// <summary>
        /// Display string for when the note was created.
        /// </summary>
        [NotMapped]
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
        /// Sets the linked team member.
        /// </summary>
        public void SetLinkedTeamMember(Guid? teamMemberId)
        {
            ClearLinkedEntity();
            LinkedTeamMemberId = teamMemberId;
        }

        /// <summary>
        /// Sets the linked meeting.
        /// </summary>
        public void SetLinkedMeeting(Guid? meetingId)
        {
            ClearLinkedEntity();
            LinkedMeetingId = meetingId;
        }

        /// <summary>
        /// Sets the linked project.
        /// </summary>
        public void SetLinkedProject(Guid? projectId)
        {
            ClearLinkedEntity();
            LinkedProjectId = projectId;
        }

        /// <summary>
        /// Sets the linked goal.
        /// </summary>
        public void SetLinkedGoal(Guid? goalId)
        {
            ClearLinkedEntity();
            LinkedGoalId = goalId;
        }

        /// <summary>
        /// Sets the linked task.
        /// </summary>
        public void SetLinkedTask(Guid? taskId)
        {
            ClearLinkedEntity();
            LinkedTaskId = taskId;
        }

        /// <summary>
        /// Clears any linked entity.
        /// </summary>
        public void ClearLinkedEntity()
        {
            LinkedTeamMemberId = null;
            LinkedMeetingId = null;
            LinkedProjectId = null;
            LinkedGoalId = null;
            LinkedTaskId = null;
            LinkedTeamMember = null;
            LinkedMeeting = null;
        }

        /// <summary>
        /// Sets a linked entity by type and ID.
        /// </summary>
        public void SetLinkedEntity(NoteLinkedEntityType entityType, int? entityIdHash)
        {
            ClearLinkedEntity();
            // Note: This is a legacy method that accepted int. 
            // For new code, use the type-specific methods like SetLinkedProject(Guid).
            // This method is kept for backward compatibility but doesn't actually set the GUID properly.
        }

        /// <summary>
        /// Sets a linked entity by type and Guid.
        /// </summary>
        public void SetLinkedEntity(NoteLinkedEntityType entityType, Guid? entityId)
        {
            ClearLinkedEntity();
            if (!entityId.HasValue) return;
            
            switch (entityType)
            {
                case NoteLinkedEntityType.TeamMember:
                    LinkedTeamMemberId = entityId;
                    break;
                case NoteLinkedEntityType.Meeting:
                    LinkedMeetingId = entityId;
                    break;
                case NoteLinkedEntityType.Project:
                    LinkedProjectId = entityId;
                    break;
                case NoteLinkedEntityType.Goal:
                case NoteLinkedEntityType.OKR:
                    LinkedGoalId = entityId;
                    break;
                case NoteLinkedEntityType.Task:
                    LinkedTaskId = entityId;
                    break;
            }
        }

        /// <summary>
        /// Archives the note.
        /// </summary>
        public void Archive()
        {
            IsArchived = true;
            ArchivedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Unarchives the note.
        /// </summary>
        public void Unarchive()
        {
            IsArchived = false;
            ArchivedAt = null;
        }

        /// <summary>
        /// Pins the note.
        /// </summary>
        public void Pin()
        {
            IsPinned = true;
            PinnedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Unpins the note.
        /// </summary>
        public void Unpin()
        {
            IsPinned = false;
            PinnedAt = null;
        }

        #endregion
    }
}

