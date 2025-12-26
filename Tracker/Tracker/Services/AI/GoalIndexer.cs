using System.Text;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes OKRs, KPIs, and Projects for semantic search.
    /// This indexer handles multiple entity types, so it overrides the base IndexAllAsync.
    /// </summary>
    public class GoalIndexer : EntityIndexerBase
    {
        private static readonly Lazy<GoalIndexer> _instance = 
            new(() => new GoalIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static GoalIndexer Instance => _instance.Value;
        
        private DateTime? _currentSinceTime;

        private GoalIndexer() : base("GoalIndexer")
        {
        }

        protected override string EntityTypeName => "goals/projects";

        /// <summary>
        /// Indexes OKRs, KPIs, and Projects (incremental if sinceTime provided).
        /// Overrides base to handle multiple entity types.
        /// </summary>
        public new async Task<int> IndexAllAsync(DateTime? sinceTime = null)
        {
            ResetCount();
            _currentSinceTime = sinceTime;
            
            if (sinceTime == null)
                _logger.Info("Starting full {0} indexing...", EntityTypeName);
            else
                _logger.Info("Starting incremental {0} indexing since {1}...", EntityTypeName, sinceTime.Value.ToString("g"));

            try
            {
                // Fetch and index all entity types
                var okrs = await TrackerDataManager.Instance.GetOKRs();
                var kpis = await TrackerDataManager.Instance.GetKPIs();
                var projects = await TrackerDataManager.Instance.GetProjects();

                // Index each type (filtering happens in IndexSingleEntityAsync)
                foreach (var okr in okrs.Where(o => !o.IsDeleted && PassesTimeFilter(o.CreatedAt, o.LastModifiedAt)))
                    await IndexOkrAsync(okr);
                    
                foreach (var kpi in kpis.Where(k => !k.IsDeleted && PassesTimeFilter(k.CreatedAt, k.LastModifiedAt)))
                    await IndexKpiAsync(kpi);
                    
                foreach (var project in projects.Where(p => !p.IsDeleted && PassesTimeFilter(p.CreatedAt, p.LastModifiedAt)))
                    await IndexProjectAsync(project);

                _logger.Info("Indexed {0} {1}", _indexedCount, EntityTypeName);
                return _indexedCount;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error indexing {0}", EntityTypeName);
                return _indexedCount;
            }
        }
        
        private bool PassesTimeFilter(DateTime createdAt, DateTime modifiedAt)
        {
            if (_currentSinceTime == null) return true;
            return createdAt > _currentSinceTime.Value || modifiedAt > _currentSinceTime.Value;
        }

        // Not used directly - we override IndexAllAsync
        protected override Task<IEnumerable<object>> FetchEntitiesAsync() => Task.FromResult(Enumerable.Empty<object>());
        protected override Task IndexSingleEntityAsync(object entity) => Task.CompletedTask;

        private async Task IndexOkrAsync(DataModels.ObjectiveKeyResult okr)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Warn("Error indexing OKR {0}: {1}", okr.ObjectiveId, ex.Message);
            }
        }

        private async Task IndexKpiAsync(DataModels.KeyPerformanceIndicator kpi)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Warn("Error indexing KPI {0}: {1}", kpi.KpiId, ex.Message);
            }
        }

        private async Task IndexProjectAsync(DataModels.Project project)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Warn("Error indexing project {0}: {1}", project.ID, ex.Message);
            }
        }
    }
}
