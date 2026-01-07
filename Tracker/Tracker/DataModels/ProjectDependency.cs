namespace Tracker.DataModels
{
    public class ProjectDependency : AuditableEntity
    {
        public int ID { get; set; } = 0;

        /// <summary>
        /// The organization this dependency belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        public string Name { get; set; } = string.Empty;
        public int ProjectId { get; set; } = 0;
        public int DependentProjectID { get; set; } = 0;
        public int RequiredProjectID { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
        public DateTime? ExpectedCompletionDate { get; set; }
    }
}
