namespace Tracker.DataModels
{
    public class ObjectiveKeyResult
    {
        public int ObjectiveId { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Owner Owner { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<KeyResult> KeyResults { get; set; } = new List<KeyResult>();
    }
}
