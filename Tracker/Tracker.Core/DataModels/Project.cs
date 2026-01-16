using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Core.Common.Enums;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// A work initiative with defined scope and timeline.
    /// Maps to Supabase 'projects' table (21 columns after ALTER).
    /// </summary>
    [Table("projects")]
    public class Project : AuditableEntity
    {
        #region Primary Key & Foreign Keys

        /// <summary>
        /// UUID Primary key.
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this project belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member who owns/leads this project.
        /// Maps to: owner_team_member_id UUID NULL
        /// </summary>
        [Column("owner_team_member_id")]
        public Guid? OwnerTeamMemberId { get; set; }

        /// <summary>
        /// User who created this project.
        /// Maps to: created_by_user_id UUID NOT NULL
        /// </summary>
        [Column("created_by_user_id")]
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Source agenda item that initiated this project.
        /// Maps to: source_agenda_item_id UUID NULL (ADDED)
        /// </summary>
        [Column("source_agenda_item_id")]
        public Guid? SourceAgendaItemId { get; set; }

        /// <summary>
        /// Source meeting from which this project originated.
        /// Maps to: source_meeting_id UUID NULL (ADDED)
        /// </summary>
        [Column("source_meeting_id")]
        public Guid? SourceMeetingId { get; set; }

        #endregion

        #region Content

        /// <summary>
        /// Project name.
        /// Maps to: name VARCHAR(300) NOT NULL
        /// </summary>
        [Column("name")]
        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Project description.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Hex color code for UI (e.g., "#FF5733").
        /// Maps to: color VARCHAR(7) NULL
        /// </summary>
        [Column("color")]
        [MaxLength(7)]
        public string? Color { get; set; }

        #endregion

        #region Dates

        /// <summary>
        /// Planned start date.
        /// Maps to: start_date DATE NULL
        /// </summary>
        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Target end date.
        /// Maps to: target_end_date DATE NULL
        /// </summary>
        [Column("target_end_date")]
        public DateTime? TargetEndDate { get; set; }

        /// <summary>
        /// Actual end date when completed.
        /// Maps to: actual_end_date DATE NULL
        /// </summary>
        [Column("actual_end_date")]
        public DateTime? ActualEndDate { get; set; }

        #endregion

        #region Status & Progress

        /// <summary>
        /// Current status (stored as string for enum).
        /// Maps to: status task_status (enum) NOT NULL DEFAULT 'not_started'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string StatusString { get; set; } = "not_started";

        /// <summary>
        /// Current status as enum.
        /// </summary>
        [NotMapped]
        public WorkItemStatus Status
        {
            get => StatusString switch
            {
                "not_started" => WorkItemStatus.NotStarted,
                "in_progress" => WorkItemStatus.InProgress,
                "completed" => WorkItemStatus.Completed,
                "blocked" => WorkItemStatus.Blocked,
                "cancelled" => WorkItemStatus.Cancelled,
                _ => WorkItemStatus.NotStarted
            };
            set => StatusString = value switch
            {
                WorkItemStatus.NotStarted => "not_started",
                WorkItemStatus.InProgress => "in_progress",
                WorkItemStatus.Completed => "completed",
                WorkItemStatus.Blocked => "blocked",
                WorkItemStatus.Cancelled => "cancelled",
                _ => "not_started"
            };
        }

        /// <summary>
        /// Progress percentage 0-100.
        /// Maps to: progress_percent NUMERIC NOT NULL DEFAULT 0
        /// </summary>
        [Column("progress_percent")]
        public decimal ProgressPercent { get; set; } = 0m;

        /// <summary>
        /// Priority level (stored as string for enum).
        /// Maps to: priority task_priority (enum) NOT NULL DEFAULT 'medium'
        /// </summary>
        [Column("priority")]
        [MaxLength(50)]
        public string PriorityString { get; set; } = "medium";

        /// <summary>
        /// Priority level as enum.
        /// </summary>
        [NotMapped]
        public WorkItemPriority Priority
        {
            get => PriorityString switch
            {
                "low" => WorkItemPriority.Low,
                "medium" => WorkItemPriority.Medium,
                "high" => WorkItemPriority.High,
                "critical" => WorkItemPriority.Critical,
                _ => WorkItemPriority.Medium
            };
            set => PriorityString = value switch
            {
                WorkItemPriority.Low => "low",
                WorkItemPriority.Medium => "medium",
                WorkItemPriority.High => "high",
                WorkItemPriority.Critical => "critical",
                _ => "medium"
            };
        }

        /// <summary>
        /// Whether visible to the team.
        /// Maps to: is_team_visible BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_team_visible")]
        public bool IsTeamVisible { get; set; } = true;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Team member who owns/leads this project.
        /// </summary>
        public TeamMember? Owner { get; set; }

        /// <summary>
        /// Tasks within this project.
        /// </summary>
        public List<TrackerTask> Tasks { get; set; } = new();

        /// <summary>
        /// Milestones within this project.
        /// </summary>
        public List<Milestone> Milestones { get; set; } = new();

        /// <summary>
        /// Team members assigned to this project.
        /// </summary>
        public List<TeamMember> TeamMembers { get; set; } = new();

        #endregion
    }
}
