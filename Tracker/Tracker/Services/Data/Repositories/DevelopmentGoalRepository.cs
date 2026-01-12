using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for DevelopmentGoal entity.
    /// Provides data access for all development goal-related operations.
    /// 
    /// This is the ONLY place that queries the 'development_goals' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Development goals represent professional growth objectives for individuals.
    /// </summary>
    public interface IDevelopmentGoalRepository : IRepository<DevelopmentGoal>
    {
        /// <summary>
        /// Get all development goals for a specific person.
        /// </summary>
        Task<IEnumerable<DevelopmentGoal>> GetByPersonAsync(Guid personId);

        /// <summary>
        /// Get active development goals for a person.
        /// </summary>
        Task<IEnumerable<DevelopmentGoal>> GetActiveByPersonAsync(Guid personId);

        /// <summary>
        /// Get development goals by status.
        /// </summary>
        Task<IEnumerable<DevelopmentGoal>> GetByStatusAsync(string status);
    }

    public class DevelopmentGoalRepository : BaseRepository<DevelopmentGoal>, IDevelopmentGoalRepository
    {
        public DevelopmentGoalRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<DevelopmentGoalRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "development_goals";
        }

        public async Task<IEnumerable<DevelopmentGoal>> GetByPersonAsync(Guid personId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM development_goals
                    WHERE person_id = @PersonId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DevelopmentGoal>(sql, new { PersonId = personId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting development goals by person {PersonId}", personId);
                throw;
            }
        }

        public async Task<IEnumerable<DevelopmentGoal>> GetActiveByPersonAsync(Guid personId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM development_goals
                    WHERE person_id = @PersonId 
                      AND status != 'completed'
                      AND status != 'abandoned'
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DevelopmentGoal>(sql, new { PersonId = personId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active development goals by person {PersonId}", personId);
                throw;
            }
        }

        public async Task<IEnumerable<DevelopmentGoal>> GetByStatusAsync(string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM development_goals
                    WHERE status = @Status AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DevelopmentGoal>(sql, new { Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting development goals by status {Status}", status);
                throw;
            }
        }
    }
}
