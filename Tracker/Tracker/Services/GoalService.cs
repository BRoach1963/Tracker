using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    public interface IGoalService
    {
        Task<IEnumerable<Goal>> GetOwnerGoalsAsync(Guid ownerId);
        Task<IEnumerable<Goal>> GetActiveOwnerGoalsAsync(Guid ownerId);
        Task<IEnumerable<Goal>> GetOrganizationGoalsAsync(Guid organizationId);
        Task<Goal> CreateGoalAsync(Goal goal);
        Task UpdateGoalAsync(Goal goal);
        Task DeleteGoalAsync(Guid goalId);
        Task<Goal?> GetGoalAsync(Guid goalId);
    }

    public class GoalService : IGoalService
    {
        private readonly IGoalRepository _repository;
        private readonly ILogger<GoalService> _logger;

        public GoalService(IGoalRepository repository, ILogger<GoalService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Goal>> GetOwnerGoalsAsync(Guid ownerId)
        {
            try
            {
                return await _repository.GetByOwnerAsync(ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetActiveOwnerGoalsAsync(Guid ownerId)
        {
            try
            {
                return await _repository.GetActiveByOwnerAsync(ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active goals for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetOrganizationGoalsAsync(Guid organizationId)
        {
            try
            {
                return await _repository.GetByOrganizationAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals for organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<Goal> CreateGoalAsync(Goal goal)
        {
            try
            {
                return await _repository.CreateAsync(goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal");
                throw;
            }
        }

        public async Task UpdateGoalAsync(Goal goal)
        {
            try
            {
                await _repository.UpdateAsync(goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal {GoalId}", goal.Id);
                throw;
            }
        }

        public async Task DeleteGoalAsync(Guid goalId)
        {
            try
            {
                await _repository.DeleteAsync(goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<Goal?> GetGoalAsync(Guid goalId)
        {
            try
            {
                return await _repository.GetByIdAsync(goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goal {GoalId}", goalId);
                throw;
            }
        }
    }
}
