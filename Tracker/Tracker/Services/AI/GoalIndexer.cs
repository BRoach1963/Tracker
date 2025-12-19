using System.Text;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes OKRs and Projects for semantic search
    /// </summary>
    public class GoalIndexer : EntityIndexerBase
    {
        private static readonly Lazy<GoalIndexer> _instance = 
            new(() => new GoalIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static GoalIndexer Instance => _instance.Value;

        private GoalIndexer() : base("GoalIndexer")
        {
        }

        /// <summary>
        /// Indexes OKRs, KPIs, and Projects (incremental if sinceTime provided)
        /// </summary>
        /// <param name="sinceTime">Only index items created/modified after this time (null = all)</param>
        public async Task<int> IndexAllAsync(DateTime? sinceTime = null)
        {
            ResetCount();
            if (sinceTime == null)
                _logger.Info("Starting full goal/project indexing...");
            else
                _logger.Info("Starting incremental goal/project indexing since {0}...", sinceTime.Value.ToString("g"));

            try
            {
                await IndexOKRsAsync(sinceTime);
                await IndexKPIsAsync(sinceTime);
                await IndexProjectsAsync(sinceTime);

                _logger.Info("Indexed {0} goals/projects", _indexedCount);
                return _indexedCount;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error indexing goals/projects");
                return _indexedCount;
            }
        }

        private async Task IndexOKRsAsync(DateTime? sinceTime = null)
        {
            try
            {
                var okrs = await TrackerDataManager.Instance.GetOKRs();
                var activeOkrs = okrs.Where(o => !o.IsDeleted).ToList();

                // Filter by modification time for incremental indexing
                if (sinceTime != null)
                {
                    activeOkrs = activeOkrs
                        .Where(o => o.CreatedAt > sinceTime.Value || o.LastModifiedAt > sinceTime.Value)
                        .ToList();
                }

                foreach (var okr in activeOkrs)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"OKR: {okr.Title}");
                    sb.AppendLine($"Progress: {okr.CompletionPercentage:P0}");
                    sb.AppendLine($"Status: {okr.Status}");
                    
                    if (okr.KeyResults?.Any() == true)
                    {
                        sb.AppendLine($"Key Results ({okr.KeyResults.Count}):");
                        foreach (var kr in okr.KeyResults.Take(5))
                        {
                            sb.AppendLine($"  - {kr.Title}: {kr.Progress:P0}");
                        }
                    }

                    var metadata = new Dictionary<string, object>
                    {
                        ["type"] = "okr",
                        ["id"] = okr.ObjectiveId,
                        ["status"] = okr.Status.ToString(),
                        ["progress"] = okr.CompletionPercentage
                    };

                    await IndexEntityAsync($"okr_{okr.ObjectiveId}", sb.ToString(), metadata);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing OKRs: {0}", ex.Message);
            }
        }

        private async Task IndexKPIsAsync(DateTime? sinceTime = null)
        {
            try
            {
                var kpis = await TrackerDataManager.Instance.GetKPIs();
                var activeKpis = kpis.Where(k => !k.IsDeleted).ToList();

                // Filter by modification time for incremental indexing
                if (sinceTime != null)
                {
                    activeKpis = activeKpis
                        .Where(k => k.CreatedAt > sinceTime.Value || k.LastModifiedAt > sinceTime.Value)
                        .ToList();
                }

                foreach (var kpi in activeKpis)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"KPI: {kpi.Name}");
                    sb.AppendLine($"Current: {kpi.Value:N0} {kpi.Unit}");
                    sb.AppendLine($"Target: {kpi.TargetValue:N0} {kpi.Unit}");
                    
                    var status = kpi.Value >= kpi.TargetValue ? "On Target" : "Below Target";
                    sb.AppendLine($"Status: {status}");

                    var metadata = new Dictionary<string, object>
                    {
                        ["type"] = "kpi",
                        ["id"] = kpi.KpiId,
                        ["value"] = kpi.Value,
                        ["target"] = kpi.TargetValue
                    };

                    await IndexEntityAsync($"kpi_{kpi.KpiId}", sb.ToString(), metadata);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing KPIs: {0}", ex.Message);
            }
        }

        private async Task IndexProjectsAsync(DateTime? sinceTime = null)
        {
            try
            {
                var projects = await TrackerDataManager.Instance.GetProjects();
                var activeProjects = projects.Where(p => !p.IsDeleted).ToList();

                // Filter by modification time for incremental indexing
                if (sinceTime != null)
                {
                    activeProjects = activeProjects
                        .Where(p => p.CreatedAt > sinceTime.Value || p.LastModifiedAt > sinceTime.Value)
                        .ToList();
                }

                foreach (var project in activeProjects)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Project: {project.Name}");
                    sb.AppendLine($"Status: {project.Status}");
                    sb.AppendLine($"Progress: {project.Progress:P0}");
                    
                    if (!string.IsNullOrEmpty(project.Description))
                        sb.AppendLine($"Description: {project.Description}");

                    var metadata = new Dictionary<string, object>
                    {
                        ["type"] = "project",
                        ["id"] = project.ID,
                        ["status"] = project.Status,
                        ["progress"] = project.Progress
                    };

                    await IndexEntityAsync($"project_{project.ID}", sb.ToString(), metadata);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing projects: {0}", ex.Message);
            }
        }
    }
}
