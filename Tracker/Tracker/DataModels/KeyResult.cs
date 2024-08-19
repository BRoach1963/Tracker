namespace Tracker.DataModels
{
    public class KeyResult
    {
        public int KeyResultId { get; set; }
        public string Title { get; set; }
        public double TargetValue { get; set; }
        public double CurrentValue { get; set; }
        public Owner Owner { get; set; }
    }
}
