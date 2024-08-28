namespace Tracker.DataModels
{
    public class Project
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public TeamMember Owner { get; set; } = new();
        public List<Task> Tasks { get; set; } = new List<Task>();
        public List<ObjectiveKeyResult> OKRs { get; set; } = new List<ObjectiveKeyResult>();
        public List<KeyPerformanceIndicator> KPIs => GetKpis();

        public List<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public decimal Budget { get; set; } = decimal.MinValue;
        public List<Milestone> Milestones { get; set; } = new List<Milestone>();
        public List<ProjectDependency> Dependencies { get; set; } = new List<ProjectDependency>();
        public List<Risk> Risks { get; set; } = new List<Risk>();


        private List<KeyPerformanceIndicator> GetKpis()
        {
            var kpis = new HashSet<KeyPerformanceIndicator>();

            foreach (var okr in OKRs)
            {
                kpis.UnionWith(okr.KeyResults);
            }

            return kpis.ToList();
        }
    }
}
