using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Interfaces;

namespace Tracker.Services
{
    /// <summary>
    /// Implementation of IOkrProgressService for calculating OKR and Key Result progress.
    /// </summary>
    public class OkrProgressService : IOkrProgressService
    {
        private readonly TrackerDbContext _context;
        private readonly IMeasurableService _measurableService;

        public OkrProgressService(TrackerDbContext context, IMeasurableService measurableService)
        {
            _context = context;
            _measurableService = measurableService;
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateKeyResultProgressAsync(int keyResultId)
        {
            var kr = await _context.KeyResults
                .Where(k => k.Id == keyResultId && !k.IsDeleted)
                .FirstOrDefaultAsync();

            if (kr == null)
                return 0m;

            return kr.Progress; // Progress is a computed property on KeyResult
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateOkrProgressAsync(int okrId)
        {
            var okr = await _context.ObjectiveKeyResults
                .Include(o => o.KeyResults)
                .Where(o => o.ObjectiveId == okrId && !o.IsDeleted)
                .FirstOrDefaultAsync();

            if (okr == null)
                return 0m;

            var keyResults = okr.KeyResults?.Where(kr => !kr.IsDeleted).ToList();
            if (keyResults == null || keyResults.Count == 0)
                return 0m;

            // Calculate weighted average if weights are specified
            var totalWeight = keyResults.Sum(kr => kr.Weight);
            if (totalWeight == 0)
                return keyResults.Average(kr => kr.Progress);

            var weightedSum = keyResults.Sum(kr => kr.Progress * kr.Weight);
            return Math.Round(weightedSum / totalWeight, 1);
        }

        /// <inheritdoc />
        public async Task<ObjectiveStatusEnum> DetermineOkrStatusAsync(int okrId)
        {
            var okr = await _context.ObjectiveKeyResults
                .Include(o => o.KeyResults)
                .Where(o => o.ObjectiveId == okrId && !o.IsDeleted)
                .FirstOrDefaultAsync();

            if (okr == null)
                return ObjectiveStatusEnum.OffTrack;

            // Respect manual override
            if (okr.StatusOverride.HasValue)
                return okr.StatusOverride.Value;

            var keyResults = okr.KeyResults?.Where(kr => !kr.IsDeleted).ToList();
            if (keyResults == null || keyResults.Count == 0)
                return ObjectiveStatusEnum.OffTrack;

            // If any KR is off target, the OKR is off track
            if (keyResults.Any(kr => kr.Status == KpiStatusEnum.OffTarget))
                return ObjectiveStatusEnum.OffTrack;

            // If any KR is close to target, the OKR is at risk
            if (keyResults.Any(kr => kr.Status == KpiStatusEnum.CloseToTarget))
                return ObjectiveStatusEnum.AtRisk;

            // All KRs on target
            return ObjectiveStatusEnum.OnTrack;
        }

        /// <inheritdoc />
        public async Task<int> RefreshAllOkrProgressAsync()
        {
            var okrs = await _context.ObjectiveKeyResults
                .Include(o => o.KeyResults)
                    .ThenInclude(kr => kr.Measurables)
                .Where(o => !o.IsDeleted)
                .ToListAsync();

            var updatedCount = 0;

            foreach (var okr in okrs)
            {
                if (await RefreshOkrProgressInternalAsync(okr))
                    updatedCount++;
            }

            if (updatedCount > 0)
                await _context.SaveChangesAsync();

            return updatedCount;
        }

        /// <inheritdoc />
        public async Task<bool> RefreshOkrProgressAsync(int okrId)
        {
            var okr = await _context.ObjectiveKeyResults
                .Include(o => o.KeyResults)
                    .ThenInclude(kr => kr.Measurables)
                .Where(o => o.ObjectiveId == okrId && !o.IsDeleted)
                .FirstOrDefaultAsync();

            if (okr == null)
                return false;

            var changed = await RefreshOkrProgressInternalAsync(okr);
            
            if (changed)
                await _context.SaveChangesAsync();

            return changed;
        }

        /// <inheritdoc />
        public async Task<bool> RefreshKeyResultValueAsync(int keyResultId)
        {
            var kr = await _context.KeyResults
                .Include(k => k.Measurables)
                .Where(k => k.Id == keyResultId && !k.IsDeleted)
                .FirstOrDefaultAsync();

            if (kr == null)
                return false;

            var changed = await UpdateKeyResultFromMeasurablesAsync(kr);

            if (changed)
                await _context.SaveChangesAsync();

            return changed;
        }

        /// <inheritdoc />
        public async Task<OkrProgressSummary> GetOkrProgressSummaryAsync(int okrId)
        {
            var okr = await _context.ObjectiveKeyResults
                .Include(o => o.Owner)
                .Include(o => o.KeyResults)
                    .ThenInclude(kr => kr.Measurables)
                .Where(o => o.ObjectiveId == okrId && !o.IsDeleted)
                .FirstOrDefaultAsync();

            if (okr == null)
                return new OkrProgressSummary { Okr = new ObjectiveKeyResult() };

            return await BuildOkrProgressSummaryAsync(okr);
        }

        /// <inheritdoc />
        public async Task<List<OkrProgressSummary>> GetOkrsWithProgressAsync(TimePeriodEnum? timePeriod = null, int? year = null)
        {
            var query = _context.ObjectiveKeyResults
                .Include(o => o.Owner)
                .Include(o => o.KeyResults)
                    .ThenInclude(kr => kr.Measurables)
                .Where(o => !o.IsDeleted);

            if (timePeriod.HasValue)
                query = query.Where(o => o.TimePeriod == timePeriod.Value);

            if (year.HasValue)
                query = query.Where(o => o.Year == year.Value);

            var okrs = await query.OrderBy(o => o.EndDate).ToListAsync();

            var summaries = new List<OkrProgressSummary>();
            foreach (var okr in okrs)
            {
                summaries.Add(await BuildOkrProgressSummaryAsync(okr));
            }

            return summaries;
        }

        #region Private Helper Methods

        private async Task<bool> RefreshOkrProgressInternalAsync(ObjectiveKeyResult okr)
        {
            var anyChanged = false;

            // Update each Key Result from its Measurables
            if (okr.KeyResults != null)
            {
                foreach (var kr in okr.KeyResults.Where(k => !k.IsDeleted))
                {
                    if (await UpdateKeyResultFromMeasurablesAsync(kr))
                        anyChanged = true;
                }
            }

            return anyChanged;
        }

        private async Task<bool> UpdateKeyResultFromMeasurablesAsync(KeyResult kr)
        {
            var measurables = kr.Measurables?.Where(m => !m.IsDeleted).ToList();
            if (measurables == null || measurables.Count == 0)
                return false;

            // Calculate aggregated value from measurables
            var aggregatedValue = await _measurableService.CalculateAggregatedValueAsync(kr.Id);
            
            if (!aggregatedValue.HasValue)
                return false;

            // Check if value changed
            if (kr.CurrentValue == aggregatedValue.Value)
                return false;

            kr.CurrentValue = aggregatedValue.Value;
            return true;
        }

        private Task<OkrProgressSummary> BuildOkrProgressSummaryAsync(ObjectiveKeyResult okr)
        {
            var summary = new OkrProgressSummary
            {
                Okr = okr,
                Progress = (decimal)okr.CompletionPercentage,
                Status = okr.Status,
                KeyResultSummaries = new List<KeyResultProgressSummary>()
            };

            var keyResults = okr.KeyResults?.Where(kr => !kr.IsDeleted).ToList() ?? new List<KeyResult>();

            foreach (var kr in keyResults.OrderBy(k => k.SortOrder))
            {
                var krSummary = new KeyResultProgressSummary
                {
                    KeyResult = kr,
                    Progress = kr.Progress,
                    Status = kr.Status,
                    MeasurableCount = kr.Measurables?.Count(m => !m.IsDeleted) ?? 0,
                    IsAutoCalculated = kr.HasMeasurables
                };

                summary.KeyResultSummaries.Add(krSummary);

                // Count linked measurables by type
                if (kr.Measurables != null)
                {
                    foreach (var m in kr.Measurables.Where(x => !x.IsDeleted))
                    {
                        switch (m.MeasurableType)
                        {
                            case MeasurableType.Metric:
                                summary.LinkedKpiCount++;
                                break;
                            case MeasurableType.Project:
                                summary.LinkedProjectCount++;
                                break;
                            case MeasurableType.TaskCollection:
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

