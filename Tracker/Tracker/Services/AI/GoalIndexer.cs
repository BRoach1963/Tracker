using System.Text;
using Tracker.Common.Enums;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes OKRs, KPIs, and Projects for semantic search.
    /// This indexer handles multiple entity types, so it overrides the base IndexAllAsync.
    /// Enhanced to provide rich context for AI analysis including trends, status, and relationships.
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
                sb.AppendLine($"Owner: {okr.Owner?.FullName ?? "Unassigned"}");
                sb.AppendLine($"Progress: {okr.CompletionPercentage:F1}%");
                sb.AppendLine($"Status: {GetOkrStatusDescription(okr.Status)}");
                sb.AppendLine($"Time Period: {okr.TimePeriodDisplay}");
                sb.AppendLine($"Date Range: {okr.StartDate:MMM d, yyyy} - {okr.EndDate:MMM d, yyyy}");
                sb.AppendLine($"Days Remaining: {okr.DaysRemaining}");
                sb.AppendLine($"Active: {(okr.IsActive ? "Yes" : "No")}");
                
                if (!string.IsNullOrEmpty(okr.Description))
                    sb.AppendLine($"Description: {okr.Description}");

                // Key Results detail
                if (okr.KeyResults?.Any() == true)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Key Results ({okr.KeyResults.Count}):");
                    foreach (var kr in okr.KeyResults.OrderBy(k => k.SortOrder))
                    {
                        var krStatus = GetKpiStatusDescription(kr.Status);
                        sb.AppendLine($"  - {kr.Title}");
                        sb.AppendLine($"    Progress: {kr.Progress:F1}% ({kr.CurrentValue}/{kr.TargetValue} {kr.Unit})");
                        sb.AppendLine($"    Status: {krStatus}");
                        sb.AppendLine($"    Weight: {kr.Weight}");
                        
                        // Include linked measurables
                        if (kr.Measurables?.Any() == true)
                        {
                            sb.AppendLine($"    Linked Sources: {kr.Measurables.Count}");
                            foreach (var m in kr.Measurables.Take(3))
                            {
                                var progressStr = m.CurrentProgress.HasValue ? $"{m.CurrentProgress.Value:F1}%" : "N/A";
                                sb.AppendLine($"      • {m.MeasurableType}: {m.DisplayName} ({progressStr})");
                            }
                        }
                    }
                    
                    // Summary analysis
                    var onTrackKRs = okr.KeyResults.Count(kr => kr.Status == KpiStatusEnum.OnTarget);
                    var atRiskKRs = okr.KeyResults.Count(kr => kr.Status == KpiStatusEnum.CloseToTarget);
                    var offTrackKRs = okr.KeyResults.Count(kr => kr.Status == KpiStatusEnum.OffTarget);
                    
                    sb.AppendLine();
                    sb.AppendLine("Key Result Summary:");
                    sb.AppendLine($"  On Target: {onTrackKRs}");
                    sb.AppendLine($"  At Risk: {atRiskKRs}");
                    sb.AppendLine($"  Off Track: {offTrackKRs}");
                }

                // Linked items counts
                if (okr.LinkedKpiCount > 0)
                    sb.AppendLine($"Linked KPIs: {okr.LinkedKpiCount}");
                if (okr.LinkedProjectCount > 0)
                    sb.AppendLine($"Linked Projects: {okr.LinkedProjectCount}");
                if (okr.LinkedTaskCollectionCount > 0)
                    sb.AppendLine($"Linked Task Collections: {okr.LinkedTaskCollectionCount}");
                
                // Meeting context
                if (okr.MeetingCount > 0)
                    sb.AppendLine($"Discussed in {okr.MeetingCount} 1:1 meeting(s)");

                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "okr",
                    ["id"] = okr.ObjectiveId,
                    ["title"] = okr.Title,
                    ["owner"] = okr.Owner?.FullName ?? "Unassigned",
                    ["status"] = okr.Status.ToString(),
                    ["progress"] = okr.CompletionPercentage,
                    ["is_active"] = okr.IsActive,
                    ["days_remaining"] = okr.DaysRemaining,
                    ["key_result_count"] = okr.KeyResultCount,
                    ["time_period"] = okr.TimePeriodDisplay
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
                sb.AppendLine($"Owner: {kpi.Owner?.FullName ?? "Unassigned"}");
                sb.AppendLine($"Current Value: {kpi.Value:N2} {kpi.Unit}");
                sb.AppendLine($"Target Value: {kpi.TargetValue:N2} {kpi.Unit}");
                sb.AppendLine($"Progress: {kpi.PercentComplete:F1}%");
                sb.AppendLine($"Status: {GetKpiStatusDescription(kpi.Status)}");
                sb.AppendLine($"Direction: {(kpi.TargetDirection == TargetDirectionEnum.GreaterOrEqual ? "Higher is better" : "Lower is better")}");
                sb.AppendLine($"Frequency: {kpi.Frequency}");
                sb.AppendLine($"Last Updated: {kpi.LastUpdated:MMM d, yyyy h:mm tt}");
                
                if (!string.IsNullOrEmpty(kpi.Description))
                    sb.AppendLine($"Description: {kpi.Description}");
                
                if (!string.IsNullOrEmpty(kpi.Category))
                    sb.AppendLine($"Category: {kpi.Category}");

                // Gap analysis
                var gap = kpi.TargetValue - kpi.Value;
                if (kpi.TargetDirection == TargetDirectionEnum.GreaterOrEqual)
                {
                    if (gap > 0)
                        sb.AppendLine($"Gap to Target: {gap:N2} {kpi.Unit} below target");
                    else
                        sb.AppendLine($"Exceeds Target by: {Math.Abs(gap):N2} {kpi.Unit}");
                }
                else
                {
                    if (gap < 0)
                        sb.AppendLine($"Gap to Target: {Math.Abs(gap):N2} {kpi.Unit} above target");
                    else
                        sb.AppendLine($"Below Target by: {gap:N2} {kpi.Unit} (good)");
                }

                // Composite KPI info
                if (kpi.IsComposite && kpi.ChildKpis?.Any() == true)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Composite KPI with {kpi.ChildKpis.Count} child KPIs:");
                    foreach (var child in kpi.ChildKpis.Take(5))
                    {
                        sb.AppendLine($"  - {child.Name}: {child.Value:N2}/{child.TargetValue:N2} {child.Unit} ({child.PercentComplete:F1}%)");
                    }
                }

                // Data sources
                if (kpi.HasDataSources)
                {
                    sb.AppendLine($"Data Sources: {kpi.DataSources.Count} configured");
                }

                // Meeting context
                if (kpi.MeetingCount > 0)
                    sb.AppendLine($"Discussed in {kpi.MeetingCount} 1:1 meeting(s)");

                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "kpi",
                    ["id"] = kpi.KpiId,
                    ["name"] = kpi.Name,
                    ["owner"] = kpi.Owner?.FullName ?? "Unassigned",
                    ["value"] = kpi.Value,
                    ["target"] = kpi.TargetValue,
                    ["progress"] = kpi.PercentComplete,
                    ["status"] = kpi.Status.ToString(),
                    ["category"] = kpi.Category ?? "",
                    ["is_composite"] = kpi.IsComposite,
                    ["unit"] = kpi.Unit
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
                sb.AppendLine($"Progress: {project.Progress:F1}%");
                
                if (!string.IsNullOrEmpty(project.Description))
                    sb.AppendLine($"Description: {project.Description}");

                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "project",
                    ["id"] = project.ID,
                    ["name"] = project.Name,
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

        #region Helper Methods

        private static string GetOkrStatusDescription(ObjectiveStatusEnum status)
        {
            return status switch
            {
                ObjectiveStatusEnum.OnTrack => "On Track (green) - meeting or exceeding expectations",
                ObjectiveStatusEnum.AtRisk => "At Risk (amber) - may not meet target without intervention",
                ObjectiveStatusEnum.OffTrack => "Off Track (red) - significantly behind, needs attention",
                _ => status.ToString()
            };
        }

        private static string GetKpiStatusDescription(KpiStatusEnum status)
        {
            return status switch
            {
                KpiStatusEnum.OnTarget => "On Target (green) - meeting or exceeding target",
                KpiStatusEnum.CloseToTarget => "Close to Target (amber) - within 10% of target",
                KpiStatusEnum.OffTarget => "Off Target (red) - more than 10% away from target",
                _ => status.ToString()
            };
        }

        #endregion
    }
}
