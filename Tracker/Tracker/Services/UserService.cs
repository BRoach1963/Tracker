using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    /// <summary>
    /// Business logic service for User operations.
    /// Wraps UserRepository and provides high-level user operations.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Get a user by their Supabase ID.
        /// </summary>
        Task<User?> GetUserBySupabaseIdAsync(string supabaseId);

        /// <summary>
        /// Get a user by email.
        /// </summary>
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Get all users in an organization.
        /// </summary>
        Task<IEnumerable<User>> GetOrganizationUsersAsync(Guid organizationId);

        /// <summary>
        /// Get active (not deleted) users in an organization.
        /// </summary>
        Task<IEnumerable<User>> GetActiveOrganizationUsersAsync(Guid organizationId);

        /// <summary>
        /// Create a new user.
        /// </summary>
        Task<User> CreateUserAsync(User user);

        /// <summary>
        /// Update an existing user.
        /// </summary>
        Task UpdateUserAsync(User user);

        /// <summary>
        /// Delete a user (soft delete).
        /// </summary>
        Task DeleteUserAsync(Guid userId);

        /// <summary>
        /// Get a single user by ID.
        /// </summary>
        Task<User?> GetUserAsync(Guid userId);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository repository, ILogger<UserService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<User?> GetUserBySupabaseIdAsync(string supabaseId)
        {
            try
            {
                return await _repository.GetBySupabaseIdAsync(supabaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by Supabase ID");
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _repository.GetByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email");
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetOrganizationUsersAsync(Guid organizationId)
        {
            try
            {
                return await _repository.GetByOrganizationAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization users {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetActiveOrganizationUsersAsync(Guid organizationId)
        {
            try
            {
                return await _repository.GetActiveByOrganizationAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active organization users {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                return await _repository.CreateAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                await _repository.UpdateAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", user.Id);
                throw;
            }
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            try
            {
                await _repository.DeleteAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                throw;
            }
        }

        public async Task<User?> GetUserAsync(Guid userId)
        {
            try
            {
                return await _repository.GetByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                throw;
            }
        }
    }
}
