namespace Tracker.DataModels
{
    public class KeyResult
    {
        public int KeyResultId { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public double TargetValue { get; set; } = double.NaN;
        public double CurrentValue { get; set; } = double.NaN;
        public Owner Owner { get; set; } = new();
    }
}
