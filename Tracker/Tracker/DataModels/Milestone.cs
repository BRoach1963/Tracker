namespace Tracker.DataModels
{
    public class Milestone : AuditableEntity
    {
        public int ID { get; set; } = 0;

        public int ProjectId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TargetDate { get; set; }
        public bool IsAchieved { get; set; }
    }
}
