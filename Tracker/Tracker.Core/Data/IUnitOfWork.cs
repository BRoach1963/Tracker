using System;
using System.Threading.Tasks;

namespace Tracker.Core.Data
{
    /// <summary>
    /// Unit of Work pattern for managing transactions across multiple repositories.
    /// 
    /// Use when you need to:
    /// - Update multiple entities atomically (all or nothing)
    /// - Batch operations that must succeed or fail together
    /// - Ensure data consistency across repository boundaries
    /// 
    /// Example:
    /// using var uow = _unitOfWorkFactory.Create();
    /// var userRepo = uow.GetRepository&lt;User&gt;();
    /// var teamRepo = uow.GetRepository&lt;TeamMember&gt;();
    /// await userRepo.UpdateAsync(user);
    /// await teamRepo.UpdateAsync(teamMember);
    /// await uow.CommitAsync(); // Both succeed or both fail
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Get a repository for a specific entity type.
        /// All repositories retrieved from the same UnitOfWork share the same transaction.
        /// </summary>
        IRepository<T> GetRepository<T>() where T : class;

        /// <summary>
        /// Commit all changes made through repositories in this unit of work.
        /// All database operations execute as a single transaction.
        /// </summary>
        Task<bool> CommitAsync();

        /// <summary>
        /// Rollback all changes (if not yet committed).
        /// </summary>
        Task RollbackAsync();
    }

    /// <summary>
    /// Factory for creating Unit of Work instances.
    /// </summary>
    public interface IUnitOfWorkFactory
    {
        /// <summary>
        /// Create a new Unit of Work for atomic multi-repository operations.
        /// </summary>
        IUnitOfWork Create();
    }
}
