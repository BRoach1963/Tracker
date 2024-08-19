namespace Tracker.DataModels
{
    public class KeyPerformanceIndicator
    {
        public int KPIId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Value { get; set; }
        public double TargetValue { get; set; }
        public Owner Owner { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
