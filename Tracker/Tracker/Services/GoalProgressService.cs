using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Interfaces;

namespace Tracker.Services
{
    /// <summary>
    /// Implementation of IGoalProgressService for calculating Goal and Target progress.
    /// Goals represent organizational objectives, and Targets represent measurable key results.
    /// </summary>
    public class GoalProgressService : IGoalProgressService
    {
        private readonly TrackerDbContext _context;
        private readonly IMeasurableService _measurableService;

        public GoalProgressService(TrackerDbContext context, IMeasurableService measurableService)
        {
            _context = context;
            _measurableService = measurableService;
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateTargetProgressAsync(Guid targetId)
        {
            var target = await _context.Targets
                .Where(t => t.Id == targetId && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (target == null)
                return 0m;

            return target.Progress; // Progress is a computed property on Target
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateGoalProgressAsync(Guid goalId)
        {
            var goal = await _context.Goals
                .Include(g => g.Targets)
                .Where(g => g.Id == goalId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (goal == null)
                return 0m;

            var targets = goal.Targets?.Where(t => !t.IsDeleted).ToList();
            if (targets == null || targets.Count == 0)
                return 0m;

            // Calculate weighted average if weights are specified
            var totalWeight = targets.Sum(t => t.Weight);
            if (totalWeight == 0)
                return targets.Average(t => t.Progress);

            var weightedSum = targets.Sum(t => t.Progress * t.Weight);
            return Math.Round(weightedSum / totalWeight, 1);
        }

        /// <inheritdoc />
        public async Task<ObjectiveStatusEnum> DetermineGoalStatusAsync(Guid goalId)
        {
            var goal = await _context.Goals
                .Include(g => g.Targets)
                .Where(g => g.Id == goalId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (goal == null)
                return ObjectiveStatusEnum.OffTrack;

            // Respect manual override
            if (goal.StatusOverride.HasValue)
                return (ObjectiveStatusEnum)(int)goal.StatusOverride.Value;

            var targets = goal.Targets?.Where(t => !t.IsDeleted).ToList();
            if (targets == null || targets.Count == 0)
                return ObjectiveStatusEnum.OffTrack;

            // If any Target is off target, the Goal is off track
            if (targets.Any(t => t.Status == OkrStatus.OffTrack))
                return ObjectiveStatusEnum.OffTrack;

            // If any Target is close to target, the Goal is at risk
            if (targets.Any(t => t.Status == OkrStatus.AtRisk))
                return ObjectiveStatusEnum.AtRisk;

            // All Targets on target
            return ObjectiveStatusEnum.OnTrack;
        }

        /// <inheritdoc />
        public async Task<int> RefreshAllGoalProgressAsync()
        {
            var goals = await _context.Goals
                .Include(g => g.Targets)
                    .ThenInclude(t => t.Measurables)
                .Where(g => !g.IsDeleted)
                .ToListAsync();

            var updatedCount = 0;

            foreach (var goal in goals)
            {
                if (await RefreshGoalProgressInternalAsync(goal))
                    updatedCount++;
            }

            if (updatedCount > 0)
                await _context.SaveChangesAsync();

            return updatedCount;
        }

        /// <inheritdoc />
        public async Task<bool> RefreshGoalProgressAsync(Guid goalId)
        {
            var goal = await _context.Goals
                .Include(g => g.Targets)
                    .ThenInclude(t => t.Measurables)
                .Where(g => g.Id == goalId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (goal == null)
                return false;

            var changed = await RefreshGoalProgressInternalAsync(goal);
            
            if (changed)
                await _context.SaveChangesAsync();

            return changed;
        }

        /// <inheritdoc />
        public async Task<bool> RefreshTargetValueAsync(Guid targetId)
        {
            var target = await _context.Targets
                .Include(t => t.Measurables)
                .Where(t => t.Id == targetId && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (target == null)
                return false;

            var changed = await UpdateTargetFromMeasurablesAsync(target);

            if (changed)
                await _context.SaveChangesAsync();

            return changed;
        }

        /// <inheritdoc />
        public async Task<GoalProgressSummary> GetGoalProgressSummaryAsync(Guid goalId)
        {
            var goal = await _context.Goals
                .Include(g => g.Owner)
                .Include(g => g.Targets)
                    .ThenInclude(t => t.Measurables)
                .Where(g => g.Id == goalId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (goal == null)
                return new GoalProgressSummary { Goal = new Goal() };

            return await BuildGoalProgressSummaryAsync(goal);
        }

        /// <inheritdoc />
        public async Task<List<GoalProgressSummary>> GetGoalsWithProgressAsync(TimePeriodEnum? timePeriod = null, int? year = null)
        {
            var query = _context.Goals
                .Include(g => g.Owner)
                .Include(g => g.Targets)
                    .ThenInclude(t => t.Measurables)
                .Where(g => !g.IsDeleted);

            if (timePeriod.HasValue)
                query = query.Where(g => g.TimePeriod == timePeriod.Value);

            if (year.HasValue)
                query = query.Where(g => g.Year == year.Value);

            var goals = await query.OrderBy(g => g.EndDate).ToListAsync();

            var summaries = new List<GoalProgressSummary>();
            foreach (var goal in goals)
            {
                summaries.Add(await BuildGoalProgressSummaryAsync(goal));
            }

            return summaries;
        }

        #region Private Helper Methods

        private async Task<bool> RefreshGoalProgressInternalAsync(Goal goal)
        {
            var anyChanged = false;

            // Update each Target from its Measurables
            if (goal.Targets != null)
            {
                foreach (var target in goal.Targets.Where(t => !t.IsDeleted))
                {
                    if (await UpdateTargetFromMeasurablesAsync(target))
                        anyChanged = true;
                }
            }

            return anyChanged;
        }

        private async Task<bool> UpdateTargetFromMeasurablesAsync(Target target)
        {
            var measurables = target.Measurables?.Where(m => !m.IsDeleted).ToList();
            if (measurables == null || measurables.Count == 0)
                return false;

            // Calculate aggregated value from measurables
            var aggregatedValue = await _measurableService.CalculateAggregatedValueAsync(target.Id);
            
            if (!aggregatedValue.HasValue)
                return false;

            // Check if value changed
            if (target.CurrentValue == aggregatedValue.Value)
                return false;

            target.CurrentValue = aggregatedValue.Value;
            return true;
        }

        private Task<GoalProgressSummary> BuildGoalProgressSummaryAsync(Goal goal)
        {
            var summary = new GoalProgressSummary
            {
                Goal = goal,
                Progress = goal.EffectiveProgress,
                Status = (ObjectiveStatusEnum)(int)goal.EffectiveStatus,
                TargetSummaries = new List<TargetProgressSummary>()
            };

            var targets = goal.Targets?.Where(t => !t.IsDeleted).ToList() ?? new List<Target>();

            foreach (var target in targets.OrderBy(t => t.SortOrder))
            {
                var hasMeasurables = target.Measurables?.Any(m => !m.IsDeleted) ?? false;
                var targetSummary = new TargetProgressSummary
                {
                    Target = target,
                    Progress = target.Progress,
                    Status = target.Status,
                    MeasurableCount = target.Measurables?.Count(m => !m.IsDeleted) ?? 0,
                    IsAutoCalculated = hasMeasurables
                };

                summary.TargetSummaries.Add(targetSummary);

                // Count linked measurables by type
                if (target.Measurables != null)
                {
                    foreach (var m in target.Measurables.Where(x => !x.IsDeleted))
                    {
                        switch (m.MeasurableType?.ToLowerInvariant())
                        {
                            case "metric":
                                summary.LinkedMetricCount++;
                                break;
                            case "project":
                                summary.LinkedProjectCount++;
                                break;
                            case "task_collection":
                            case "taskcollection":
                                summary.LinkedTaskCollectionCount++;
                                break;
                        }
                    }
                }
            }

            return Task.FromResult(summary);
        }

        #endregion
    }
}

