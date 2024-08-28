namespace Tracker.DataModels
{
    public class KeyPerformanceIndicator
    {
        public int KpiId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Value { get; set; } = double.NaN;
        public double TargetValue { get; set; } = double.NaN;
        public Owner Owner { get; set; } = new Owner();
        public DateTime LastUpdated { get; set; } 
    }
}
