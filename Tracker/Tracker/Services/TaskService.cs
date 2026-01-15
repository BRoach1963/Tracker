using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TrackerTask>> GetAssigneeTasksAsync(Guid assigneeId);
        Task<IEnumerable<TrackerTask>> GetOpenTasksAsync(Guid assigneeId);
        Task<IEnumerable<TrackerTask>> GetOverdueTasksAsync(Guid assigneeId, DateTime currentDate);
        Task<IEnumerable<TrackerTask>> GetTasksByGoalAsync(Guid goalId);
        Task<TrackerTask> CreateTaskAsync(TrackerTask task);
        Task UpdateTaskAsync(TrackerTask task);
        Task DeleteTaskAsync(Guid taskId, Guid deletedByUserId);
        Task<TrackerTask?> GetTaskAsync(Guid taskId);
    }

    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ITaskRepository repository, ILogger<TaskService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<TrackerTask>> GetAssigneeTasksAsync(Guid assigneeId)
        {
            try
            {
                return await _repository.GetByAssigneeAsync(assigneeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<IEnumerable<TrackerTask>> GetOpenTasksAsync(Guid assigneeId)
        {
            try
            {
                return await _repository.GetOpenByAssigneeAsync(assigneeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open tasks for assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<IEnumerable<TrackerTask>> GetOverdueTasksAsync(Guid assigneeId, DateTime currentDate)
        {
            try
            {
                return await _repository.GetOverdueAsync(assigneeId, currentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue tasks for assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<IEnumerable<TrackerTask>> GetTasksByGoalAsync(Guid goalId)
        {
            try
            {
                return await _repository.GetByGoalAsync(goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<TrackerTask> CreateTaskAsync(TrackerTask task)
        {
            try
            {
                return await _repository.CreateAsync(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                throw;
            }
        }

        public async Task UpdateTaskAsync(TrackerTask task)
        {
            try
            {
                await _repository.UpdateAsync(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", task.Id);
                throw;
            }
        }

        public async Task DeleteTaskAsync(Guid taskId, Guid deletedByUserId)
        {
            try
            {
                await _repository.DeleteAsync(taskId, deletedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<TrackerTask?> GetTaskAsync(Guid taskId)
        {
            try
            {
                return await _repository.GetByIdAsync(taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task {TaskId}", taskId);
                throw;
            }
        }
    }
}
