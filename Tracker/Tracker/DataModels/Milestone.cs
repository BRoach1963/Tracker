namespace Tracker.DataModels
{
    public class Milestone : AuditableEntity
    {
        public int ID { get; set; } = 0;

        /// <summary>
        /// The organization this milestone belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        public int ProjectId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TargetDate { get; set; }
        public bool IsAchieved { get; set; }
    }
}
