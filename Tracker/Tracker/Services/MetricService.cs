using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    public interface IMetricService
    {
        Task<IEnumerable<Metric>> GetOwnerMetricsAsync(Guid ownerId);
        Task<IEnumerable<Metric>> GetActiveOwnerMetricsAsync(Guid ownerId);
        Task<IEnumerable<Metric>> GetGoalMetricsAsync(Guid goalId);
        Task<IEnumerable<MetricHistory>> GetMetricHistoryAsync(Guid metricId);
        Task<Metric> CreateMetricAsync(Metric metric);
        Task UpdateMetricAsync(Metric metric);
        Task DeleteMetricAsync(Guid metricId, Guid deletedByUserId);
        Task<Metric?> GetMetricAsync(Guid metricId);
    }

    public class MetricService : IMetricService
    {
        private readonly IMetricRepository _repository;
        private readonly ILogger<MetricService> _logger;

        public MetricService(IMetricRepository repository, ILogger<MetricService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Metric>> GetOwnerMetricsAsync(Guid ownerId)
        {
            try
            {
                return await _repository.GetByOwnerAsync(ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<IEnumerable<Metric>> GetActiveOwnerMetricsAsync(Guid ownerId)
        {
            try
            {
                return await _repository.GetActiveByOwnerAsync(ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active metrics for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<IEnumerable<Metric>> GetGoalMetricsAsync(Guid goalId)
        {
            try
            {
                return await _repository.GetByGoalAsync(goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics by goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<IEnumerable<MetricHistory>> GetMetricHistoryAsync(Guid metricId)
        {
            try
            {
                return await _repository.GetHistoryAsync(metricId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for metric {MetricId}", metricId);
                throw;
            }
        }

        public async Task<Metric> CreateMetricAsync(Metric metric)
        {
            try
            {
                return await _repository.CreateAsync(metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating metric");
                throw;
            }
        }

        public async Task UpdateMetricAsync(Metric metric)
        {
            try
            {
                await _repository.UpdateAsync(metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metric {MetricId}", metric.Id);
                throw;
            }
        }

        public async Task DeleteMetricAsync(Guid metricId, Guid deletedByUserId)
        {
            try
            {
                await _repository.DeleteAsync(metricId, deletedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting metric {MetricId}", metricId);
                throw;
            }
        }

        public async Task<Metric?> GetMetricAsync(Guid metricId)
        {
            try
            {
                return await _repository.GetByIdAsync(metricId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metric {MetricId}", metricId);
                throw;
            }
        }
    }
}
