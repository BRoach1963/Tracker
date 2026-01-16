using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tracker.Core.Data
{
    /// <summary>
    /// Generic repository interface for all Dapper-based data access.
    /// Every entity gets a repository implementing this interface.
    /// NO EXCEPTIONS. NO EF CORE. DAPPER ONLY.
    /// </summary>
    /// <typeparam name="T">The entity type (User, Meeting, Goal, Task, etc.)</typeparam>
    public interface IRepository<T> where T : class
    {
        // ===== SINGLE ENTITY OPERATIONS =====
        
        /// <summary>
        /// Get a single entity by its primary key (ID).
        /// </summary>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all entities (respects soft-delete: excludes is_deleted = true records).
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        // ===== CREATE OPERATIONS =====
        
        /// <summary>
        /// Create a new entity in the database.
        /// Returns the created entity with ID populated.
        /// </summary>
        Task<T> CreateAsync(T entity);

        /// <summary>
        /// Create multiple entities in a single database call (batch insert).
        /// More efficient than calling CreateAsync multiple times.
        /// </summary>
        Task<IEnumerable<T>> CreateBatchAsync(IEnumerable<T> entities);

        // ===== UPDATE OPERATIONS =====
        
        /// <summary>
        /// Update an existing entity.
        /// Only updates columns that are explicitly set (not all columns).
        /// </summary>
        Task<bool> UpdateAsync(T entity);

        /// <summary>
        /// Update multiple entities in a single database call (batch update).
        /// </summary>
        Task<bool> UpdateBatchAsync(IEnumerable<T> entities);

        // ===== DELETE OPERATIONS =====
        
        /// <summary>
        /// Soft delete: Mark entity as deleted (is_deleted = true, deleted_at = now, deleted_by = userId).
        /// Hard delete is NEVER used in this architecture - all deletes are soft.
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid deletedByUserId);

        /// <summary>
        /// Soft delete multiple entities at once.
        /// </summary>
        Task<bool> DeleteBatchAsync(IEnumerable<Guid> ids, Guid deletedByUserId);

        /// <summary>
        /// Permanently delete (hard delete) - ONLY used for test cleanup or admin operations.
        /// NOT used in normal business logic.
        /// </summary>
        Task<bool> PermanentlyDeleteAsync(Guid id);

        // ===== EXISTENCE CHECKS =====
        
        /// <summary>
        /// Check if entity exists (by primary key, respecting soft-delete).
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// Count total entities (respecting soft-delete).
        /// </summary>
        Task<int> CountAsync();

        // ===== QUERY OPERATIONS (Overridden in concrete repositories) =====
        
        /// <summary>
        /// Get entities using a custom WHERE clause.
        /// Example: GetWhereSqlAsync("organization_id = @OrgId", new { OrgId = orgId })
        /// Respects soft-delete automatically.
        /// </summary>
        Task<IEnumerable<T>> GetWhereSqlAsync(string whereSql, object? parameters = null);

        /// <summary>
        /// Get paginated results with ordering.
        /// </summary>
        Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string orderBySql = "id DESC",
            string? whereSql = null,
            object? parameters = null);
    }
}
