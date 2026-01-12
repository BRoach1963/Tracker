using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for PerformanceReview entity.
    /// Provides data access for all performance review-related operations.
    /// 
    /// This is the ONLY place that queries the 'performance_reviews' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Performance reviews are formal assessments done periodically (annual, quarterly, etc.).
    /// </summary>
    public interface IPerformanceReviewRepository : IRepository<PerformanceReview>
    {
        /// <summary>
        /// Get all performance reviews for a specific person.
        /// </summary>
        Task<IEnumerable<PerformanceReview>> GetByPersonAsync(Guid personId);

        /// <summary>
        /// Get performance reviews in a date range.
        /// </summary>
        Task<IEnumerable<PerformanceReview>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get performance reviews by reviewer.
        /// </summary>
        Task<IEnumerable<PerformanceReview>> GetByReviewerAsync(Guid reviewerId);
    }

    public class PerformanceReviewRepository : BaseRepository<PerformanceReview>, IPerformanceReviewRepository
    {
        public PerformanceReviewRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<PerformanceReviewRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "performance_reviews";
        }

        public async Task<IEnumerable<PerformanceReview>> GetByPersonAsync(Guid personId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM performance_reviews
                    WHERE person_id = @PersonId AND is_deleted = false
                    ORDER BY review_date DESC";

                return await connection.QueryAsync<PerformanceReview>(sql, new { PersonId = personId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance reviews by person {PersonId}", personId);
                throw;
            }
        }

        public async Task<IEnumerable<PerformanceReview>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM performance_reviews
                    WHERE review_date >= @StartDate 
                      AND review_date <= @EndDate
                      AND is_deleted = false
                    ORDER BY review_date DESC";

                return await connection.QueryAsync<PerformanceReview>(sql, 
                    new { StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance reviews by date range");
                throw;
            }
        }

        public async Task<IEnumerable<PerformanceReview>> GetByReviewerAsync(Guid reviewerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM performance_reviews
                    WHERE reviewer_id = @ReviewerId AND is_deleted = false
                    ORDER BY review_date DESC";

                return await connection.QueryAsync<PerformanceReview>(sql, new { ReviewerId = reviewerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance reviews by reviewer {ReviewerId}", reviewerId);
                throw;
            }
        }
    }
}
