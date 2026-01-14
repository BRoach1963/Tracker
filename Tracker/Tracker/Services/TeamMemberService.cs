using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    /// <summary>
    /// Business logic service for TeamMember operations.
    /// Wraps TeamMemberRepository and provides high-level team member operations.
    /// </summary>
    public interface ITeamMemberService
    {
        /// <summary>
        /// Get all team members in an organization.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetOrganizationMembersAsync(Guid organizationId);

        /// <summary>
        /// Get active team members in an organization.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetActiveOrganizationMembersAsync(Guid organizationId);

        /// <summary>
        /// Get team members managed by a specific person.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetDirectReportsAsync(Guid managerId);

        /// <summary>
        /// Get all teams/orgs a user belongs to.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetUserTeamMembershipsAsync(Guid userId);

        /// <summary>
        /// Check if user is active in organization.
        /// </summary>
        Task<bool> IsUserActiveInOrganizationAsync(Guid userId, Guid organizationId);

        /// <summary>
        /// Create a new team member entry.
        /// </summary>
        Task<TeamMember> CreateTeamMemberAsync(TeamMember teamMember);

        /// <summary>
        /// Update team member information.
        /// </summary>
        Task UpdateTeamMemberAsync(TeamMember teamMember);

        /// <summary>
        /// Remove a team member (soft delete).
        /// </summary>
        Task RemoveTeamMemberAsync(Guid teamMemberId, Guid deletedByUserId);

        /// <summary>
        /// Get a single team member by ID.
        /// </summary>
        Task<TeamMember?> GetTeamMemberAsync(Guid teamMemberId);
    }

    public class TeamMemberService : ITeamMemberService
    {
        private readonly ITeamMemberRepository _repository;
        private readonly ILogger<TeamMemberService> _logger;

        public TeamMemberService(ITeamMemberRepository repository, ILogger<TeamMemberService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<TeamMember>> GetOrganizationMembersAsync(Guid organizationId)
        {
            try
            {
                return await _repository.GetByOrganizationAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization members {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<TeamMember>> GetActiveOrganizationMembersAsync(Guid organizationId)
        {
            try
            {
                return await _repository.GetActiveByOrganizationAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active organization members {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<TeamMember>> GetDirectReportsAsync(Guid managerId)
        {
            try
            {
                return await _repository.GetByManagerAsync(managerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting direct reports for manager {ManagerId}", managerId);
                throw;
            }
        }

        public async Task<IEnumerable<TeamMember>> GetUserTeamMembershipsAsync(Guid userId)
        {
            try
            {
                return await _repository.GetByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team memberships for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> IsUserActiveInOrganizationAsync(Guid userId, Guid organizationId)
        {
            try
            {
                return await _repository.IsUserActiveInOrganizationAsync(userId, organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user active status in organization");
                throw;
            }
        }

        public async Task<TeamMember> CreateTeamMemberAsync(TeamMember teamMember)
        {
            try
            {
                return await _repository.CreateAsync(teamMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team member");
                throw;
            }
        }

        public async Task UpdateTeamMemberAsync(TeamMember teamMember)
        {
            try
            {
                await _repository.UpdateAsync(teamMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team member {TeamMemberId}", teamMember.Id);
                throw;
            }
        }

        public async Task RemoveTeamMemberAsync(Guid teamMemberId, Guid deletedByUserId)
        {
            try
            {
                await _repository.DeleteAsync(teamMemberId, deletedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing team member {TeamMemberId}", teamMemberId);
                throw;
            }
        }

        public async Task<TeamMember?> GetTeamMemberAsync(Guid teamMemberId)
        {
            try
            {
                return await _repository.GetByIdAsync(teamMemberId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team member {TeamMemberId}", teamMemberId);
                throw;
            }
        }
    }
}
