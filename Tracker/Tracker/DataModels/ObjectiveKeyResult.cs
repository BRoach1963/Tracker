namespace Tracker.DataModels
{
    public class ObjectiveKeyResult
    {
        public int ObjectiveId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Owner Owner { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<KeyResult> KeyResults { get; set; } = new List<KeyResult>();
    }
}
