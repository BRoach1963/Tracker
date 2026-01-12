using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Tracker.Services.Data
{
    /// <summary>
    /// Generic base repository for all Dapper data access.
    /// Implements standard CRUD operations that work for ANY entity type.
    /// 
    /// Concrete repositories (UserRepository, MeetingRepository, etc.) inherit from this
    /// and add entity-specific query methods.
    /// 
    /// KEY PATTERNS:
    /// - Soft deletes only (is_deleted = true, never hard delete in business logic)
    /// - All timestamps use UTC
    /// - All IDs are UUID
    /// - RLS enforced at database layer (Supabase)
    /// </summary>
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly IDapperConnectionFactory _connectionFactory;
        protected readonly ILogger<BaseRepository<T>> _logger;
        
        /// <summary>
        /// The table name in Supabase (e.g., "users", "meetings", "goals").
        /// Must be set by derived classes in constructor.
        /// </summary>
        protected string TableName { get; set; } = string.Empty;

        protected BaseRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<BaseRepository<T>> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ===== SINGLE ENTITY OPERATIONS =====

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "SELECT * FROM {0} WHERE id = @Id AND is_deleted = false LIMIT 1";
                var query = string.Format(sql, TableName);
                
                return await connection.QueryFirstOrDefaultAsync<T>(query, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {TableName} by ID {Id}", TableName, id);
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "SELECT * FROM {0} WHERE is_deleted = false ORDER BY id DESC";
                var query = string.Format(sql, TableName);
                
                return await connection.QueryAsync<T>(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all {TableName}", TableName);
                throw;
            }
        }

        // ===== CREATE OPERATIONS =====

        public virtual async Task<T> CreateAsync(T entity)
        {
            try
            {
                ValidateEntityNotNull(entity);
                
                using var connection = _connectionFactory.CreateConnection();
                
                // Build dynamic INSERT based on entity properties
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var columnNames = string.Join(", ", properties.Select(p => p.Name.ToLower()));
                var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));
                var sql = $"INSERT INTO {TableName} ({columnNames}) VALUES ({parameterNames}) RETURNING *";

                var result = await connection.QueryFirstOrDefaultAsync<T>(sql, entity);
                
                _logger.LogInformation("Created {TableName} with ID {Id}", TableName, GetId(entity));
                return result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating {TableName}", TableName);
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> CreateBatchAsync(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            try
            {
                ValidateEntitiesNotEmpty(entityList);

                using var connection = _connectionFactory.CreateConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                var results = new List<T>();
                
                foreach (var entity in entityList)
                {
                    var properties = typeof(T).GetProperties()
                        .Where(p => p.CanRead && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var columnNames = string.Join(", ", properties.Select(p => p.Name.ToLower()));
                    var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));
                    var sql = $"INSERT INTO {TableName} ({columnNames}) VALUES ({parameterNames}) RETURNING *";

                    var result = await connection.QueryFirstOrDefaultAsync<T>(sql, entity, transaction);
                    results.Add(result!);
                }

                transaction.Commit();
                
                _logger.LogInformation("Batch created {Count} {TableName} entities", entityList.Count, TableName);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch creating {TableName}", TableName);
                throw;
            }
        }

        // ===== UPDATE OPERATIONS =====

        public virtual async Task<bool> UpdateAsync(T entity)
        {
            try
            {
                ValidateEntityNotNull(entity);
                
                using var connection = _connectionFactory.CreateConnection();

                var id = GetId(entity);
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && p.CanWrite && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var setClause = string.Join(", ", properties.Select(p => $"{p.Name.ToLower()} = @{p.Name}"));
                var sql = $"UPDATE {TableName} SET {setClause}, updated_at = NOW() WHERE id = @Id AND is_deleted = false";

                var result = await connection.ExecuteAsync(sql, entity);
                
                _logger.LogInformation("Updated {TableName} ID {Id}", TableName, id);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {TableName}", TableName);
                throw;
            }
        }

        public virtual async Task<bool> UpdateBatchAsync(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            try
            {
                ValidateEntitiesNotEmpty(entityList);

                using var connection = _connectionFactory.CreateConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && p.CanWrite && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var setClause = string.Join(", ", properties.Select(p => $"{p.Name.ToLower()} = @{p.Name}"));
                var sql = $"UPDATE {TableName} SET {setClause}, updated_at = NOW() WHERE id = @Id AND is_deleted = false";

                var result = await connection.ExecuteAsync(sql, entityList, transaction);
                transaction.Commit();

                _logger.LogInformation("Batch updated {Count} {TableName} entities", entityList.Count, TableName);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch updating {TableName}", TableName);
                throw;
            }
        }

        // ===== DELETE OPERATIONS =====

        public virtual async Task<bool> DeleteAsync(Guid id, Guid deletedByUserId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "UPDATE {0} SET is_deleted = true, deleted_at = NOW(), deleted_by = @DeletedBy WHERE id = @Id";
                var query = string.Format(sql, TableName);
                
                var result = await connection.ExecuteAsync(query, new { Id = id, DeletedBy = deletedByUserId });
                
                _logger.LogInformation("Soft deleted {TableName} ID {Id}", TableName, id);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {TableName} ID {Id}", TableName, id);
                throw;
            }
        }

        public virtual async Task<bool> DeleteBatchAsync(IEnumerable<Guid> ids, Guid deletedByUserId)
        {
            var idList = ids.ToList();
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "UPDATE {0} SET is_deleted = true, deleted_at = NOW(), deleted_by = @DeletedBy WHERE id = ANY(@Ids)";
                var query = string.Format(sql, TableName);
                
                var result = await connection.ExecuteAsync(query, 
                    new { Ids = idList.ToArray(), DeletedBy = deletedByUserId });
                
                _logger.LogInformation("Soft deleted batch of {Count} {TableName} entities", idList.Count, TableName);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch deleting {TableName}", TableName);
                throw;
            }
        }

        public virtual async Task<bool> PermanentlyDeleteAsync(Guid id)
        {
            try
            {
                _logger.LogWarning("HARD DELETE on {TableName} ID {Id} - only for admin/test operations", TableName, id);
                
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "DELETE FROM {0} WHERE id = @Id";
                var query = string.Format(sql, TableName);
                
                var result = await connection.ExecuteAsync(query, new { Id = id });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting {TableName} ID {Id}", TableName, id);
                throw;
            }
        }

        // ===== EXISTENCE CHECKS =====

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "SELECT COUNT(*) FROM {0} WHERE id = @Id AND is_deleted = false";
                var query = string.Format(sql, TableName);
                
                var count = await connection.QueryFirstAsync<int>(query, new { Id = id });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of {TableName} ID {Id}", TableName, id);
                throw;
            }
        }

        public virtual async Task<int> CountAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "SELECT COUNT(*) FROM {0} WHERE is_deleted = false";
                var query = string.Format(sql, TableName);
                
                return await connection.QueryFirstAsync<int>(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting {TableName}", TableName);
                throw;
            }
        }

        // ===== QUERY OPERATIONS =====

        public virtual async Task<IEnumerable<T>> GetWhereSqlAsync(string whereSql, object? parameters = null)
        {
            try
            {
                ValidateWhereClause(whereSql);

                using var connection = _connectionFactory.CreateConnection();
                var sql = $"SELECT * FROM {TableName} WHERE ({whereSql}) AND is_deleted = false";
                
                return await connection.QueryAsync<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing WHERE query on {TableName}", TableName);
                throw;
            }
        }

        public virtual async Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string orderBySql = "id DESC",
            string? whereSql = null,
            object? parameters = null)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                    throw new ArgumentException("Page number and page size must be >= 1");

                using var connection = _connectionFactory.CreateConnection();

                var offset = (pageNumber - 1) * pageSize;
                var baseWhere = "is_deleted = false";
                var fullWhere = string.IsNullOrEmpty(whereSql) ? baseWhere : $"({whereSql}) AND {baseWhere}";

                // Get total count
                var countSql = $"SELECT COUNT(*) FROM {TableName} WHERE {fullWhere}";
                var totalCount = await connection.QueryFirstAsync<int>(countSql, parameters);

                // Get paged results
                var dataSql = $"SELECT * FROM {TableName} WHERE {fullWhere} ORDER BY {orderBySql} LIMIT @PageSize OFFSET @Offset";
                var items = await connection.QueryAsync<T>(dataSql, 
                    MergeParameters(parameters, new { PageSize = pageSize, Offset = offset }));

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing paged query on {TableName}", TableName);
                throw;
            }
        }

        // ===== HELPER METHODS =====

        /// <summary>
        /// Get the ID property value from an entity.
        /// Works for any entity with an 'Id' property of type Guid.
        /// </summary>
        protected virtual Guid GetId(T entity)
        {
            var idProperty = typeof(T).GetProperty("Id", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public);
            if (idProperty?.GetValue(entity) is Guid id)
                return id;

            throw new InvalidOperationException($"{typeof(T).Name} does not have a valid Id property");
        }

        /// <summary>
        /// Merge anonymous objects for Dapper parameters.
        /// Example: MergeParameters(new { OrgId = id }, new { PageSize = 10 })
        /// </summary>
        protected static object? MergeParameters(object? obj1, object? obj2)
        {
            if (obj1 == null) return obj2;
            if (obj2 == null) return obj1;

            var dict = new Dictionary<string, object?>();
            
            foreach (var prop in obj1.GetType().GetProperties())
                dict[prop.Name] = prop.GetValue(obj1);
            
            foreach (var prop in obj2.GetType().GetProperties())
                dict[prop.Name] = prop.GetValue(obj2);

            return dict;
        }

        // ===== VALIDATION HELPERS =====

        protected static void ValidateEntityNotNull(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
        }

        protected static void ValidateEntitiesNotEmpty(List<T> entities)
        {
            if (!entities.Any())
                throw new ArgumentException("Entity collection cannot be empty", nameof(entities));
        }

        protected static void ValidateWhereClause(string whereSql)
        {
            if (string.IsNullOrWhiteSpace(whereSql))
                throw new ArgumentException("WHERE clause cannot be empty", nameof(whereSql));
        }
    }
}
