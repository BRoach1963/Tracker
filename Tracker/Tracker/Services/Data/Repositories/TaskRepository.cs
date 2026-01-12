using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for Task entity.
    /// Provides data access for all task-related operations.
    /// 
    /// This is the ONLY place that queries the 'tasks' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Tasks represent action items assigned to people, linked to Goals, Meetings, and Projects.
    /// They have status filtering, owner queries, date range lookups, and goal/project associations.
    /// </summary>
    public interface ITaskRepository : IRepository<Task>
    {
        /// <summary>
        /// Get all tasks assigned to a specific person.
        /// </summary>
        Task<IEnumerable<Task>> GetByAssigneeAsync(Guid assigneeId);

        /// <summary>
        /// Get all open (not completed) tasks for a user.
        /// </summary>
        Task<IEnumerable<Task>> GetOpenByAssigneeAsync(Guid assigneeId);

        /// <summary>
        /// Get all completed tasks for a user.
        /// </summary>
        Task<IEnumerable<Task>> GetCompletedByAssigneeAsync(Guid assigneeId);

        /// <summary>
        /// Get tasks by specific status (open, completed, blocked, etc.).
        /// </summary>
        Task<IEnumerable<Task>> GetByStatusAsync(string status);

        /// <summary>
        /// Get tasks due by a specific date.
        /// </summary>
        Task<IEnumerable<Task>> GetDueByDateAsync(Guid assigneeId, DateTime dueDate);

        /// <summary>
        /// Get overdue tasks for a user (due_date in past).
        /// </summary>
        Task<IEnumerable<Task>> GetOverdueAsync(Guid assigneeId, DateTime currentDate);

        /// <summary>
        /// Get tasks linked to a specific goal.
        /// </summary>
        Task<IEnumerable<Task>> GetByGoalAsync(Guid goalId);

        /// <summary>
        /// Get tasks linked to a specific project.
        /// </summary>
        Task<IEnumerable<Task>> GetByProjectAsync(Guid projectId);

        /// <summary>
        /// Get tasks linked to a specific meeting (prep or follow-up).
        /// </summary>
        Task<IEnumerable<Task>> GetByMeetingAsync(Guid meetingId);

        /// <summary>
        /// Get all tasks created/assigned by a specific user.
        /// </summary>
        Task<IEnumerable<Task>> GetByCreatedByAsync(Guid createdById);

        /// <summary>
        /// Count open tasks for a user.
        /// </summary>
        Task<int> CountOpenByAssigneeAsync(Guid assigneeId);
    }

    public class TaskRepository : BaseRepository<Task>, ITaskRepository
    {
        public TaskRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<TaskRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "tasks";
        }

        public async Task<IEnumerable<Task>> GetByAssigneeAsync(Guid assigneeId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE assignee_id = @AssigneeId AND is_deleted = false
                    ORDER BY due_date ASC, created_at DESC";

                return await connection.QueryAsync<Task>(sql, new { AssigneeId = assigneeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetOpenByAssigneeAsync(Guid assigneeId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE assignee_id = @AssigneeId 
                      AND status != 'completed'
                      AND is_deleted = false
                    ORDER BY due_date ASC, created_at DESC";

                return await connection.QueryAsync<Task>(sql, new { AssigneeId = assigneeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open tasks by assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetCompletedByAssigneeAsync(Guid assigneeId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE assignee_id = @AssigneeId 
                      AND status = 'completed'
                      AND is_deleted = false
                    ORDER BY completed_at DESC";

                return await connection.QueryAsync<Task>(sql, new { AssigneeId = assigneeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed tasks by assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetByStatusAsync(string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE status = @Status AND is_deleted = false
                    ORDER BY due_date ASC, created_at DESC";

                return await connection.QueryAsync<Task>(sql, new { Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetDueByDateAsync(Guid assigneeId, DateTime dueDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE assignee_id = @AssigneeId 
                      AND due_date = @DueDate
                      AND is_deleted = false
                    ORDER BY status, created_at DESC";

                return await connection.QueryAsync<Task>(sql, 
                    new { AssigneeId = assigneeId, DueDate = dueDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks due by date for assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetOverdueAsync(Guid assigneeId, DateTime currentDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE assignee_id = @AssigneeId 
                      AND due_date < @CurrentDate
                      AND status != 'completed'
                      AND is_deleted = false
                    ORDER BY due_date ASC";

                return await connection.QueryAsync<Task>(sql, 
                    new { AssigneeId = assigneeId, CurrentDate = currentDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue tasks for assignee {AssigneeId}", assigneeId);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetByGoalAsync(Guid goalId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE goal_id = @GoalId AND is_deleted = false
                    ORDER BY due_date ASC, created_at DESC";

                return await connection.QueryAsync<Task>(sql, new { GoalId = goalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetByProjectAsync(Guid projectId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE project_id = @ProjectId AND is_deleted = false
                    ORDER BY due_date ASC, created_at DESC";

                return await connection.QueryAsync<Task>(sql, new { ProjectId = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetByMeetingAsync(Guid meetingId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE meeting_id = @MeetingId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Task>(sql, new { MeetingId = meetingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by meeting {MeetingId}", meetingId);
                throw;
            }
        }

        public async Task<IEnumerable<Task>> GetByCreatedByAsync(Guid createdById)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM tasks
                    WHERE created_by = @CreatedById AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Task>(sql, new { CreatedById = createdById });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by created_by {CreatedById}", createdById);
                throw;
            }
        }

        public async Task<int> CountOpenByAssigneeAsync(Guid assigneeId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM tasks
                    WHERE assignee_id = @AssigneeId 
                      AND status != 'completed'
                      AND is_deleted = false";

                return await connection.QueryFirstAsync<int>(sql, new { AssigneeId = assigneeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting open tasks for assignee {AssigneeId}", assigneeId);
                throw;
            }
        }
    }
}
