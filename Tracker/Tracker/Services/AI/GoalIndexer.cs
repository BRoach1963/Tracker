using System.Text;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;
using Microsoft.EntityFrameworkCore;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes Goals and Projects for semantic search.
    /// Goals consolidate OKRs and KPIs from the legacy system with type discrimination.
    /// This indexer handles multiple entity types via override of IndexAllAsync.
    /// Enhanced to provide rich context for AI analysis including progress, status, and targets.
    /// </summary>
    public class GoalIndexer : EntityIndexerBase
    {
        private static readonly Lazy<GoalIndexer> _instance = 
            new(() => new GoalIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static GoalIndexer Instance => _instance.Value;
        
        private DateTime? _currentSinceTime;
        private readonly TrackerDbContext _context;

        private GoalIndexer() : base("GoalIndexer")
        {
            _context = new TrackerDbContext();
        }

        protected override string EntityTypeName => "goals/projects";

        /// <summary>
        /// Indexes Goals and Projects (incremental if sinceTime provided).
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
                // Fetch and index Goals and Projects
                var goals = await _context.Goals
                    .Include(g => g.Targets)
                        .ThenInclude(t => t.Measurables)
                    .Include(g => g.Owner)
                    .Where(g => !g.IsDeleted)
                    .ToListAsync();

                var projects = await _context.Projects
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();

                // Index each goal (filtering by time)
                foreach (var goal in goals.Where(g => PassesTimeFilter(g.CreatedAt, g.LastModifiedAt)))
                    await IndexGoalAsync(goal);
                    
                // Index each project (filtering by time)
                foreach (var project in projects.Where(p => PassesTimeFilter(p.CreatedAt, p.LastModifiedAt)))
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

        private async Task IndexGoalAsync(DataModels.Goal goal)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Goal: {goal.Title}");
                sb.AppendLine($"Type: {goal.Type}");
                sb.AppendLine($"Owner: {goal.Owner?.FullName ?? "Unassigned"}");
                sb.AppendLine($"Progress: {goal.EffectiveProgress:F1}%");
                sb.AppendLine($"Status: {GetGoalStatusDescription(goal.EffectiveStatus)}");
                sb.AppendLine($"Time Period: {goal.TimePeriod}");
                sb.AppendLine($"Year: {goal.Year}");
                sb.AppendLine($"Date Range: {goal.StartDate:MMM d, yyyy} - {goal.EndDate:MMM d, yyyy}");
                sb.AppendLine($"Days Remaining: {goal.DaysRemaining}");
                
                if (!string.IsNullOrEmpty(goal.Description))
                    sb.AppendLine($"Description: {goal.Description}");

                // Targets detail
                var activeTargets = goal.Targets?.Where(t => !t.IsDeleted).ToList() ?? new List<DataModels.Target>();
                if (activeTargets.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine($"Targets ({activeTargets.Count}):");
                    foreach (var target in activeTargets.OrderBy(t => t.SortOrder))
                    {
                        var targetStatus = GetKpiStatusDescription(target.Status);
                        sb.AppendLine($"  - {target.Title}");
                        sb.AppendLine($"    Progress: {target.Progress:F1}%");
                        sb.AppendLine($"    Current: {target.CurrentValue} / Target: {target.TargetValue}");
                        sb.AppendLine($"    Status: {targetStatus}");
                        sb.AppendLine($"    Weight: {target.Weight}");
                        
                        // Include linked measurables
                        var activeMeasurables = target.Measurables?.Where(m => !m.IsDeleted).ToList() ?? new List<DataModels.TargetMeasurable>();
                        if (activeMeasurables.Any())
                        {
                            sb.AppendLine($"    Linked Sources: {activeMeasurables.Count}");
                            foreach (var m in activeMeasurables.Take(3))
                            {
                                var progressStr = m.CurrentProgress.HasValue ? $"{m.CurrentProgress.Value:F1}%" : "N/A";
                                sb.AppendLine($"      • {m.MeasurableType}: {m.DisplayName} ({progressStr})");
                            }
                        }
                    }
                    
                    // Summary analysis
                    var onTrackTargets = activeTargets.Count(t => t.Status == OkrStatus.OnTrack);
                    var atRiskTargets = activeTargets.Count(t => t.Status == OkrStatus.AtRisk);
                    var offTrackTargets = activeTargets.Count(t => t.Status == OkrStatus.OffTrack);
                    
                    sb.AppendLine();
                    sb.AppendLine("Target Summary:");
                    sb.AppendLine($"  On Target: {onTrackTargets}");
                    sb.AppendLine($"  At Risk: {atRiskTargets}");
                    sb.AppendLine($"  Off Track: {offTrackTargets}");
                }

                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "goal",
                    ["id"] = goal.Id,
                    ["title"] = goal.Title,
                    ["goal_type"] = goal.Type.ToString(),
                    ["owner"] = goal.Owner?.FullName ?? "Unassigned",
                    ["status"] = goal.EffectiveStatus.ToString(),
                    ["progress"] = Math.Round(goal.EffectiveProgress, 1),
                    ["days_remaining"] = goal.DaysRemaining,
                    ["target_count"] = activeTargets.Count,
                    ["time_period"] = goal.TimePeriod.ToString(),
                    ["year"] = goal.Year
                };

                await IndexEntityAsync($"goal_{goal.Id}", sb.ToString(), metadata);
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing Goal {0}: {1}", goal.Id, ex.Message);
            }
        }

        private async Task IndexProjectAsync(DataModels.Project project)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Project: {project.Name}");
                sb.AppendLine($"Status: {project.Status}");
                sb.AppendLine($"Progress: {project.ProgressPercent:F1}%");
                
                if (!string.IsNullOrEmpty(project.Description))
                    sb.AppendLine($"Description: {project.Description}");

                var metadata = new Dictionary<string, object>
                {
                    ["type"] = "project",
                    ["id"] = project.Id,
                    ["name"] = project.Name,
                    ["status"] = project.Status,
                    ["progress"] = project.ProgressPercent
                };

                await IndexEntityAsync($"project_{project.Id}", sb.ToString(), metadata);
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing project {0}: {1}", project.Id, ex.Message);
            }
        }

        #region Helper Methods

        private static string GetGoalStatusDescription(OkrStatus status)
        {
            return status switch
            {
                OkrStatus.OnTrack => "On Track (green) - meeting or exceeding expectations",
                OkrStatus.AtRisk => "At Risk (amber) - may not meet target without intervention",
                OkrStatus.OffTrack => "Off Track (red) - significantly behind, needs attention",
                OkrStatus.Completed => "Completed - goal achieved",
                OkrStatus.Cancelled => "Cancelled",
                _ => status.ToString()
            };
        }

        private static string GetKpiStatusDescription(OkrStatus status)
        {
            return status switch
            {
                OkrStatus.OnTrack => "On Target (green) - meeting or exceeding target",
                OkrStatus.AtRisk => "Close to Target (amber) - within 10% of target",
                OkrStatus.OffTrack => "Off Target (red) - more than 10% away from target",
                _ => status.ToString()
            };
        }

        #endregion
    }
}
