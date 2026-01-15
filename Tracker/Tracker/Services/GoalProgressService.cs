using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Interfaces;
using Tracker.Managers;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    /// <summary>
    /// Implementation of IGoalProgressService for calculating Goal and Target progress.
    /// Goals represent organizational objectives, and Targets represent measurable key results.
    /// </summary>
    public class GoalProgressService : IGoalProgressService
    {
        private readonly ITargetRepository _targetRepository;
        private readonly IMeasurableService _measurableService;

        public GoalProgressService(ITargetRepository targetRepository, IMeasurableService measurableService)
        {
            _targetRepository = targetRepository;
            _measurableService = measurableService;
        }

        /// <inheritdoc />
        public Task<decimal> CalculateTargetProgressAsync(Guid targetId)
        {
            var target = TrackerDataManager.Instance.Targets
                .FirstOrDefault(t => t.Id == targetId && !t.IsDeleted);

            if (target == null)
                return Task.FromResult(0m);

            return Task.FromResult(target.Progress); // Progress is a computed property on Target
        }

        /// <inheritdoc />
        public Task<decimal> CalculateGoalProgressAsync(Guid goalId)
        {
            var goal = TrackerDataManager.Instance.Goals
                .FirstOrDefault(g => g.Id == goalId && !g.IsDeleted);

            if (goal == null)
                return Task.FromResult(0m);

            var targets = TrackerDataManager.Instance.Targets
                .Where(t => t.GoalId == goalId && !t.IsDeleted).ToList();
            
            if (targets.Count == 0)
                return Task.FromResult(0m);

            // Calculate weighted average if weights are specified
            var totalWeight = targets.Sum(t => t.Weight);
            if (totalWeight == 0)
                return Task.FromResult((decimal)targets.Average(t => t.Progress));

            var weightedSum = targets.Sum(t => t.Progress * t.Weight);
            return Task.FromResult(Math.Round(weightedSum / totalWeight, 1));
        }

        /// <inheritdoc />
        public Task<GoalStatus> DetermineGoalStatusAsync(Guid goalId)
        {
            var goal = TrackerDataManager.Instance.Goals
                .FirstOrDefault(g => g.Id == goalId && !g.IsDeleted);

            if (goal == null)
                return Task.FromResult(GoalStatus.OffTrack);

            // Respect manual override
            if (goal.StatusOverride.HasValue)
                return Task.FromResult(goal.StatusOverride.Value);

            var targets = TrackerDataManager.Instance.Targets
                .Where(t => t.GoalId == goalId && !t.IsDeleted).ToList();
            
            if (targets.Count == 0)
                return Task.FromResult(GoalStatus.OffTrack);

            // If any Target is off target, the Goal is off track
            if (targets.Any(t => t.Status == GoalStatus.OffTrack))
                return Task.FromResult(GoalStatus.OffTrack);

            // If any Target is close to target, the Goal is at risk
            if (targets.Any(t => t.Status == GoalStatus.AtRisk))
                return Task.FromResult(GoalStatus.AtRisk);

            // All Targets on target
            return Task.FromResult(GoalStatus.OnTrack);
        }

        /// <inheritdoc />
        public async Task<int> RefreshAllGoalProgressAsync()
        {
            var goals = TrackerDataManager.Instance.Goals
                .Where(g => !g.IsDeleted).ToList();

            var updatedCount = 0;
            var targetsToUpdate = new List<Target>();

            foreach (var goal in goals)
            {
                var targets = TrackerDataManager.Instance.Targets
                    .Where(t => t.GoalId == goal.Id && !t.IsDeleted).ToList();
                goal.Targets = targets;
                
                foreach (var target in targets)
                {
                    target.Measurables = TrackerDataManager.Instance.Measurables
                        .Where(m => m.TargetId == target.Id && !m.IsDeleted).ToList();
                }
                
                var updatedTargets = await RefreshGoalProgressInternalAsync(goal);
                if (updatedTargets.Any())
                {
                    targetsToUpdate.AddRange(updatedTargets);
                    updatedCount++;
                }
            }

            // Batch update all changed targets
            foreach (var target in targetsToUpdate)
            {
                await _targetRepository.UpdateAsync(target);
            }

            return updatedCount;
        }

        /// <inheritdoc />
        public async Task<bool> RefreshGoalProgressAsync(Guid goalId)
        {
            var goal = TrackerDataManager.Instance.Goals
                .FirstOrDefault(g => g.Id == goalId && !g.IsDeleted);

            if (goal == null)
                return false;

            // Load targets and measurables
            var targets = TrackerDataManager.Instance.Targets
                .Where(t => t.GoalId == goalId && !t.IsDeleted).ToList();
            goal.Targets = targets;
            
            foreach (var target in targets)
            {
                target.Measurables = TrackerDataManager.Instance.Measurables
                    .Where(m => m.TargetId == target.Id && !m.IsDeleted).ToList();
            }

            var updatedTargets = await RefreshGoalProgressInternalAsync(goal);
            
            // Update changed targets in database
            foreach (var target in updatedTargets)
            {
                await _targetRepository.UpdateAsync(target);
            }

            return updatedTargets.Any();
        }

        /// <inheritdoc />
        public async Task<bool> RefreshTargetValueAsync(Guid targetId)
        {
            var target = TrackerDataManager.Instance.Targets
                .FirstOrDefault(t => t.Id == targetId && !t.IsDeleted);

            if (target == null)
                return false;

            // Load measurables
            target.Measurables = TrackerDataManager.Instance.Measurables
                .Where(m => m.TargetId == targetId && !m.IsDeleted).ToList();

            var changed = await UpdateTargetFromMeasurablesAsync(target);

            if (changed)
                await _targetRepository.UpdateAsync(target);

            return changed;
        }

        /// <inheritdoc />
        public Task<GoalProgressSummary> GetGoalProgressSummaryAsync(Guid goalId)
        {
            var goal = TrackerDataManager.Instance.Goals
                .FirstOrDefault(g => g.Id == goalId && !g.IsDeleted);

            if (goal == null)
                return Task.FromResult(new GoalProgressSummary { Goal = new Goal() });

            // Load owner
            goal.Owner = TrackerDataManager.Instance.People
                .FirstOrDefault(p => p.Id == goal.OwnerId);
            
            // Load targets and their measurables
            goal.Targets = TrackerDataManager.Instance.Targets
                .Where(t => t.GoalId == goalId && !t.IsDeleted).ToList();
            foreach (var target in goal.Targets)
            {
                target.Measurables = TrackerDataManager.Instance.Measurables
                    .Where(m => m.TargetId == target.Id && !m.IsDeleted).ToList();
            }

            return BuildGoalProgressSummaryAsync(goal);
        }

        /// <inheritdoc />
        public async Task<List<GoalProgressSummary>> GetGoalsWithProgressAsync(TimePeriodEnum? timePeriod = null, int? year = null)
        {
            var goals = TrackerDataManager.Instance.Goals
                .Where(g => !g.IsDeleted).ToList();

            if (timePeriod.HasValue)
                goals = goals.Where(g => g.TimePeriod == timePeriod.Value).ToList();

            if (year.HasValue)
                goals = goals.Where(g => g.Year == year.Value).ToList();

            goals = goals.OrderBy(g => g.EndDate).ToList();

            // Load related data for each goal
            foreach (var goal in goals)
            {
                goal.Owner = TrackerDataManager.Instance.People
                    .FirstOrDefault(p => p.Id == goal.OwnerId);
                goal.Targets = TrackerDataManager.Instance.Targets
                    .Where(t => t.GoalId == goal.Id && !t.IsDeleted).ToList();
                foreach (var target in goal.Targets)
                {
                    target.Measurables = TrackerDataManager.Instance.Measurables
                        .Where(m => m.TargetId == target.Id && !m.IsDeleted).ToList();
                }
            }

            var summaries = new List<GoalProgressSummary>();
            foreach (var goal in goals)
            {
                summaries.Add(await BuildGoalProgressSummaryAsync(goal));
            }

            return summaries;
        }

        #region Private Helper Methods

        private async Task<List<Target>> RefreshGoalProgressInternalAsync(Goal goal)
        {
            var updatedTargets = new List<Target>();

            // Update each Target from its Measurables
            if (goal.Targets != null)
            {
                foreach (var target in goal.Targets.Where(t => !t.IsDeleted))
                {
                    if (await UpdateTargetFromMeasurablesAsync(target))
                        updatedTargets.Add(target);
                }
            }

            return updatedTargets;
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
                Status = goal.EffectiveStatus,
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

