using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A work initiative with defined scope and timeline.
    /// Maps directly to Supabase 'projects' table.
    /// </summary>
    public class Project : AuditableEntity
    {
        /// <summary>
        /// UUID Primary key
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this project belongs to (UUID FK to organizations)
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member who owns/leads this project (UUID FK to team_members, nullable)
        /// </summary>
        public Guid? OwnerTeamMemberId { get; set; }
        public TeamMember? Owner { get; set; }

        /// <summary>
        /// User who created this project (UUID FK to users)
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Project name (VARCHAR 300)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Project description (TEXT, nullable)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Hex color code for UI (VARCHAR 7, nullable) - e.g., "#FF5733"
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Planned start date (DATE, nullable)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Target end date (DATE, nullable)
        /// </summary>
        public DateTime? TargetEndDate { get; set; }

        /// <summary>
        /// Actual end date when completed (DATE, nullable)
        /// </summary>
        public DateTime? ActualEndDate { get; set; }

        /// <summary>
        /// Current status - maps to task_status enum
        /// </summary>
        public WorkItemStatus Status { get; set; } = WorkItemStatus.NotStarted;

        /// <summary>
        /// Progress percentage 0-100 (DECIMAL 5,2)
        /// </summary>
        public decimal ProgressPercent { get; set; } = 0m;

        /// <summary>
        /// Priority level - maps to task_priority enum
        /// </summary>
        public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;

        /// <summary>
        /// Whether visible to the team (BOOLEAN)
        /// </summary>
        public bool IsTeamVisible { get; set; } = true;

        /// <summary>
        /// Tasks within this project
        /// </summary>
        public List<TrackerTask> Tasks { get; set; } = new();

        /// <summary>
        /// Milestones within this project
        /// </summary>
        public List<Milestone> Milestones { get; set; } = new();

        /// <summary>
        /// Team members assigned to this project
        /// </summary>
        public List<TeamMember> TeamMembers { get; set; } = new();

        /// <summary>
        /// Source agenda item that initiated this project. UUID FK to meeting_agenda_items. Nullable.
        /// </summary>
        public Guid? SourceAgendaItemId { get; set; }

        /// <summary>
        /// Source meeting from which this project originated. UUID FK to meetings. Nullable.
        /// </summary>
        public Guid? SourceMeetingId { get; set; }
    }
}
