using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Goal (formerly OKR/Objective) - what we want to achieve.
    /// Maps to Supabase 'goals' table.
    /// Progress is calculated from linked Targets.
    /// </summary>
    public class Goal : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this goal belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team member who owns this goal.
        /// </summary>
        public Guid? OwnerTeamMemberId { get; set; }
        public TeamMember? Owner { get; set; }

        /// <summary>
        /// User who created this goal.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Goal title - what we want to achieve.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Extended description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Time period (Q1-Q4, Annual, Custom).
        /// </summary>
        public TimePeriodEnum TimePeriod { get; set; } = TimePeriodEnum.Q1;

        /// <summary>
        /// Year for the time period.
        /// </summary>
        public int Year { get; set; } = DateTime.Now.Year;

        /// <summary>
        /// Start date of the goal period.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the goal period.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Current status (auto-calculated or overridden).
        /// </summary>
        public OkrStatus Status { get; set; } = OkrStatus.NotStarted;

        /// <summary>
        /// Manual status override (if set, overrides calculated status).
        /// </summary>
        public OkrStatus? StatusOverride { get; set; }

        /// <summary>
        /// Progress percentage (0-100), calculated from targets.
        /// </summary>
        public decimal ProgressPercent { get; set; }

        /// <summary>
        /// Manual progress override.
        /// </summary>
        public decimal? ProgressOverride { get; set; }

        /// <summary>
        /// Is this goal visible to the team?
        /// </summary>
        public bool IsTeamVisible { get; set; } = true;

        /// <summary>
        /// Is this goal visible to the entire organization?
        /// </summary>
        public bool IsOrgVisible { get; set; }

        /// <summary>
        /// Optional linked project.
        /// </summary>
        public Guid? ProjectId { get; set; }
        public Project? Project { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Targets (Key Results) that measure progress.
        /// </summary>
        public List<Target> Targets { get; set; } = new();

        /// <summary>
        /// Milestones for this goal.
        /// </summary>
        public List<GoalMilestone> Milestones { get; set; } = new();

        #region Computed Properties

        /// <summary>
        /// Effective status (override or calculated).
        /// </summary>
        public OkrStatus EffectiveStatus => StatusOverride ?? Status;

        /// <summary>
        /// Effective progress (override or calculated).
        /// </summary>
        public decimal EffectiveProgress => ProgressOverride ?? ProgressPercent;

        /// <summary>
        /// Is the goal active (not completed, cancelled, or deleted)?
        /// </summary>
        public bool IsActive => !IsDeleted && 
            Status != OkrStatus.Completed && 
            Status != OkrStatus.Cancelled;

        /// <summary>
        /// Days remaining until end date.
        /// </summary>
        public int DaysRemaining => (EndDate - DateTime.Today).Days;

        #endregion
    }
}
