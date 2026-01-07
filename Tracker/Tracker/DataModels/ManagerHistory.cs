namespace Tracker.DataModels
{
    /// <summary>
    /// Tracks the history of manager assignments for team members.
    /// 
    /// When a team member's manager changes (reorg, promotion, manager departure),
    /// a new record is created with the start date, and the previous record's
    /// end date is populated.
    /// 
    /// This enables:
    /// - Historical reporting (who managed whom during a period)
    /// - Data continuity when team members move between managers
    /// - Audit trail for organizational changes
    /// 
    /// The current manager relationship has EndDate = null.
    /// </summary>
    public class ManagerHistory : AuditableEntity
    {
        /// <summary>
        /// Primary key - GUID for PostgreSQL compatibility.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The organization this history record belongs to.
        /// Required for RLS filtering.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// The team member whose manager assignment is being tracked.
        /// </summary>
        public int TeamMemberId { get; set; }

        /// <summary>
        /// The manager (user) who was assigned to the team member.
        /// References User.Id for the local database user.
        /// </summary>
        public int ManagerUserId { get; set; }

        /// <summary>
        /// The Supabase/PostgreSQL manager ID (for PostgreSQL databases).
        /// Null for SQLite/SQL Server local databases.
        /// </summary>
        public Guid? ManagerSupabaseId { get; set; }

        /// <summary>
        /// Date when this manager assignment started.
        /// </summary>
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when this manager assignment ended.
        /// Null indicates this is the current manager.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Reason for the manager change.
        /// Examples: "reorg", "promotion", "manager_departure", "initial_assignment", "transfer"
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Optional notes about the change.
        /// </summary>
        public string? Notes { get; set; }

        #region Navigation Properties

        /// <summary>
        /// The organization this record belongs to.
        /// </summary>
        public Organization? Organization { get; set; }

        /// <summary>
        /// The team member whose manager changed.
        /// </summary>
        public TeamMember? TeamMember { get; set; }

        /// <summary>
        /// The manager (local user) who was assigned.
        /// </summary>
        public User? Manager { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Whether this is the current manager assignment.
        /// </summary>
        public bool IsCurrent => EndDate == null;

        /// <summary>
        /// Duration of this manager assignment.
        /// </summary>
        public TimeSpan Duration => (EndDate ?? DateTime.UtcNow) - StartDate;

        /// <summary>
        /// Duration as a human-readable string.
        /// </summary>
        public string DurationDisplay
        {
            get
            {
                var days = (int)Duration.TotalDays;
                if (days < 30) return $"{days} days";
                if (days < 365) return $"{days / 30} months";
                return $"{days / 365} years, {(days % 365) / 30} months";
            }
        }

        #endregion
    }
}
