using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    public class ObjectiveKeyResult
    {
        public int ObjectiveId { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TeamMember Owner { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ProjectId { get; set; } = 0;

        public List<KeyPerformanceIndicator> KeyResults { get; set; } = new List<KeyPerformanceIndicator>();

        public ObjectiveStatusEnum Status
        {
            get
            {
                if (KeyResults.Any(kpi => kpi.Status == KpiStatusEnum.OffTarget))
                {
                    return ObjectiveStatusEnum.OffTrack;
                }
                else if (KeyResults.Any(kpi => kpi.Status == KpiStatusEnum.CloseToTarget))
                {
                    return ObjectiveStatusEnum.AtRisk;
                }
                else
                {
                    return ObjectiveStatusEnum.OnTrack;
                }
            }
        }
        public double CompletionPercentage
        {
            get
            {
                if (KeyResults.Count == 0) return 0;

                double completed = KeyResults.Count(kpi => kpi.Status == KpiStatusEnum.OnTarget);
                return (completed / KeyResults.Count) * 100;
            }
        }
    }
}
