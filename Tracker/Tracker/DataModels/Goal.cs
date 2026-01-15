using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Goal type - distinguishes organizational goals from team and personal goals.
    /// </summary>
    public enum GoalType
    {
        /// <summary>Organizational or strategic goal (company-wide).</summary>
        Organizational = 0,
        /// <summary>Team goal.</summary>
        Team = 1,
        /// <summary>Personal development goal for a team member.</summary>
        Personal = 2
    }

    /// <summary>
    /// Goal (formerly OKR/Objective) - what we want to achieve.
    /// Progress is calculated from linked Targets.
    /// Maps to: goals (29 columns - after ALTER adds type + provenance)
    /// </summary>
    [Table("goals")]
    public class Goal : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this goal belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member who owns this goal.
        /// Maps to: owner_team_member_id UUID NULL
        /// </summary>
        [Column("owner_team_member_id")]
        public Guid? OwnerTeamMemberId { get; set; }

        /// <summary>
        /// User who created this goal.
        /// Maps to: created_by_user_id UUID NOT NULL
        /// </summary>
        [Column("created_by_user_id")]
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Goal title - what we want to achieve.
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Extended description.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Goal type (stored as string for PostgreSQL enum).
        /// Maps to: type goal_type (enum) NOT NULL DEFAULT 'organizational'
        /// </summary>
        [Column("type")]
        [MaxLength(20)]
        public string TypeString { get; set; } = "organizational";

        /// <summary>
        /// Goal type as enum.
        /// </summary>
        [NotMapped]
        public GoalType Type
        {
            get => TypeString switch
            {
                "organizational" => GoalType.Organizational,
                "team" => GoalType.Team,
                "personal" => GoalType.Personal,
                _ => GoalType.Organizational
            };
            set => TypeString = value switch
            {
                GoalType.Organizational => "organizational",
                GoalType.Team => "team",
                GoalType.Personal => "personal",
                _ => "organizational"
            };
        }

        /// <summary>
        /// Time period (stored as string for PostgreSQL enum).
        /// Maps to: time_period goal_time_period (enum) NOT NULL DEFAULT 'q1'
        /// </summary>
        [Column("time_period")]
        [MaxLength(20)]
        public string TimePeriodString { get; set; } = "q1";

        /// <summary>
        /// Time period as enum.
        /// </summary>
        [NotMapped]
        public TimePeriodEnum TimePeriod
        {
            get => TimePeriodString switch
            {
                "q1" => TimePeriodEnum.Q1,
                "q2" => TimePeriodEnum.Q2,
                "q3" => TimePeriodEnum.Q3,
                "q4" => TimePeriodEnum.Q4,
                "annual" => TimePeriodEnum.Annual,
                "custom" => TimePeriodEnum.Custom,
                _ => TimePeriodEnum.Q1
            };
            set => TimePeriodString = value switch
            {
                TimePeriodEnum.Q1 => "q1",
                TimePeriodEnum.Q2 => "q2",
                TimePeriodEnum.Q3 => "q3",
                TimePeriodEnum.Q4 => "q4",
                TimePeriodEnum.Annual => "annual",
                TimePeriodEnum.Custom => "custom",
                _ => "q1"
            };
        }

        /// <summary>
        /// Year for the time period.
        /// Maps to: year INT4 NOT NULL DEFAULT EXTRACT(year FROM CURRENT_DATE)
        /// </summary>
        [Column("year")]
        public int Year { get; set; } = DateTime.Now.Year;

        /// <summary>
        /// Start date of the goal period.
        /// Maps to: start_date DATE NOT NULL
        /// </summary>
        [Column("start_date")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the goal period.
        /// Maps to: end_date DATE NOT NULL
        /// </summary>
        [Column("end_date")]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Current status (stored as string for PostgreSQL enum).
        /// Maps to: status goal_status (enum) NOT NULL DEFAULT 'not_started'
        /// </summary>
        [Column("status")]
        [MaxLength(20)]
        public string StatusString { get; set; } = "not_started";

        /// <summary>
        /// Status as enum.
        /// </summary>
        [NotMapped]
        public GoalStatus Status
        {
            get => StatusString switch
            {
                "not_started" => GoalStatus.NotStarted,
                "on_track" => GoalStatus.OnTrack,
                "at_risk" => GoalStatus.AtRisk,
                "off_track" => GoalStatus.OffTrack,
                "completed" => GoalStatus.Completed,
                "cancelled" => GoalStatus.Cancelled,
                _ => GoalStatus.NotStarted
            };
            set => StatusString = value switch
            {
                GoalStatus.NotStarted => "not_started",
                GoalStatus.OnTrack => "on_track",
                GoalStatus.AtRisk => "at_risk",
                GoalStatus.OffTrack => "off_track",
                GoalStatus.Completed => "completed",
                GoalStatus.Cancelled => "cancelled",
                _ => "not_started"
            };
        }

        /// <summary>
        /// Manual status override (stored as string for PostgreSQL enum).
        /// Maps to: status_override goal_status (enum) NULL
        /// </summary>
        [Column("status_override")]
        [MaxLength(20)]
        public string? StatusOverrideString { get; set; }

        /// <summary>
        /// Status override as enum.
        /// </summary>
        [NotMapped]
        public GoalStatus? StatusOverride
        {
            get => StatusOverrideString switch
            {
                "not_started" => GoalStatus.NotStarted,
                "on_track" => GoalStatus.OnTrack,
                "at_risk" => GoalStatus.AtRisk,
                "off_track" => GoalStatus.OffTrack,
                "completed" => GoalStatus.Completed,
                "cancelled" => GoalStatus.Cancelled,
                null => null,
                _ => null
            };
            set => StatusOverrideString = value switch
            {
                GoalStatus.NotStarted => "not_started",
                GoalStatus.OnTrack => "on_track",
                GoalStatus.AtRisk => "at_risk",
                GoalStatus.OffTrack => "off_track",
                GoalStatus.Completed => "completed",
                GoalStatus.Cancelled => "cancelled",
                null => null,
                _ => null
            };
        }

        /// <summary>
        /// Progress percentage (0-100), calculated from targets.
        /// Maps to: progress_percent NUMERIC NOT NULL DEFAULT 0
        /// </summary>
        [Column("progress_percent")]
        public decimal ProgressPercent { get; set; }

        /// <summary>
        /// Manual progress override.
        /// Maps to: progress_override NUMERIC NULL
        /// </summary>
        [Column("progress_override")]
        public decimal? ProgressOverride { get; set; }

        /// <summary>
        /// Is this goal visible to the team?
        /// Maps to: is_team_visible BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_team_visible")]
        public bool IsTeamVisible { get; set; } = true;

        /// <summary>
        /// Is this goal visible to the entire organization?
        /// Maps to: is_org_visible BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_org_visible")]
        public bool IsOrgVisible { get; set; }

        /// <summary>
        /// Optional linked project.
        /// Maps to: project_id UUID NULL
        /// </summary>
        [Column("project_id")]
        public Guid? ProjectId { get; set; }

        #region Provenance Columns (where this goal came from)

        /// <summary>
        /// The agenda item from which this goal was created.
        /// Maps to: source_agenda_item_id UUID NULL
        /// NULL if goal was created independently (not from a meeting).
        /// </summary>
        [Column("source_agenda_item_id")]
        public Guid? SourceAgendaItemId { get; set; }

        /// <summary>
        /// The meeting from which this goal originated.
        /// Maps to: source_meeting_id UUID NULL
        /// Denormalized for easier queries. NULL if created independently.
        /// </summary>
        [Column("source_meeting_id")]
        public Guid? SourceMeetingId { get; set; }

        #endregion

        #region Sync Columns

        /// <summary>
        /// Sync ID for offline-online sync.
        /// Maps to: sync_id UUID NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("sync_id")]
        public Guid? SyncId { get; set; }

        /// <summary>
        /// Sync version for conflict resolution.
        /// Maps to: sync_version INT4 NULL DEFAULT 1
        /// </summary>
        [Column("sync_version")]
        public int? SyncVersion { get; set; } = 1;

        /// <summary>
        /// When the record was last synced.
        /// Maps to: sync_modified_at TIMESTAMPTZ NULL DEFAULT now()
        /// </summary>
        [Column("sync_modified_at")]
        public DateTime? SyncModifiedAt { get; set; }

        /// <summary>
        /// Sync status (stored as string for PostgreSQL enum).
        /// Maps to: sync_status sync_status (enum) NULL DEFAULT 'synced'
        /// </summary>
        [Column("sync_status")]
        [MaxLength(20)]
        public string? SyncStatusString { get; set; } = "synced";

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Team member who owns this goal.
        /// </summary>
        [NotMapped]
        public TeamMember? Owner { get; set; }

        /// <summary>
        /// Organization this goal belongs to.
        /// </summary>
        [NotMapped]
        public Organization? Organization { get; set; }

        /// <summary>
        /// User who created this goal.
        /// </summary>
        [NotMapped]
        public User? CreatedByUser { get; set; }

        /// <summary>
        /// Linked project.
        /// </summary>
        [NotMapped]
        public Project? Project { get; set; }

        /// <summary>
        /// Targets (Key Results) that measure progress.
        /// </summary>
        [NotMapped]
        public List<Target> Targets { get; set; } = new();

        /// <summary>
        /// Milestones for this goal.
        /// </summary>
        [NotMapped]
        public List<GoalMilestone> Milestones { get; set; } = new();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Effective status (override or calculated).
        /// </summary>
        [NotMapped]
        public GoalStatus EffectiveStatus => StatusOverride ?? Status;

        /// <summary>
        /// Effective progress (override or calculated).
        /// </summary>
        [NotMapped]
        public decimal EffectiveProgress => ProgressOverride ?? ProgressPercent;

        /// <summary>
        /// Progress percentage (alias for EffectiveProgress for backward compatibility).
        /// </summary>
        [NotMapped]
        public decimal Progress => EffectiveProgress;

        /// <summary>
        /// Is the goal active (not completed, cancelled, or deleted)?
        /// </summary>
        [NotMapped]
        public bool IsActive => 
            !IsDeleted && 
            EffectiveStatus != GoalStatus.Completed && 
            EffectiveStatus != GoalStatus.Cancelled;

        /// <summary>
        /// Is the goal overdue?
        /// </summary>
        [NotMapped]
        public bool IsOverdue => 
            IsActive && 
            EndDate < DateTime.Today && 
            EffectiveProgress < 100;

        /// <summary>
        /// Days until end date (negative if past).
        /// </summary>
        [NotMapped]
        public int DaysRemaining => (int)(EndDate - DateTime.Today).TotalDays;

        #endregion
    }
}
