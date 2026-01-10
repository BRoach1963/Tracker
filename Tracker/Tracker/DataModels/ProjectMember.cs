namespace Tracker.DataModels
{
    /// <summary>
    /// Team member assignment to a project.
    /// Maps to Supabase 'project_members' table.
    /// </summary>
    public class ProjectMember : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Project this member is assigned to.
        /// </summary>
        public Guid ProjectId { get; set; }
        public Project? Project { get; set; }

        /// <summary>
        /// Team member assigned to the project.
        /// </summary>
        public Guid TeamMemberId { get; set; }
        public TeamMember? TeamMember { get; set; }

        /// <summary>
        /// Role on the project (owner, contributor, reviewer).
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// When the member joined the project.
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
